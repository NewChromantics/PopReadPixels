using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using WebSocketSharp;

using UnityEngine.Events;


public class FrameQueue<INPUT,OUTPUT>
{
	List<INPUT>			EncodeQueue = new List<INPUT>();
	List<OUTPUT>		SendQueue = new List<OUTPUT>();
	int					MaxEncodeConcurrent = 3;
	int					EncodeConcurrent = 0;
	int					MaxSendConcurrent = 3;
	int					SendConcurrent = 0;
	public bool			OnlySendLatest = false;
		

	System.Func<INPUT,OUTPUT>			EncodeFunction;
	System.Action<OUTPUT,System.Action<bool>>	SendFunction;

	public FrameQueue(System.Func<INPUT,OUTPUT> Encode,System.Action<OUTPUT,System.Action<bool>> Send)
	{
		EncodeFunction = Encode;
		SendFunction = Send;
	}

	public void			Push(INPUT Frame)
	{
		EncodeQueue.Add( Frame );
	}
	

	public void			Encode(bool Async)
	{
		if ( EncodeConcurrent >= MaxEncodeConcurrent )
			return;

		if ( EncodeQueue.Count == 0 )
			return;

		if ( OnlySendLatest && EncodeQueue.Count > 1 )
			EncodeQueue.RemoveRange( 0, EncodeQueue.Count - 1 );

		var Data = EncodeQueue[0];
		EncodeQueue.RemoveAt(0);

		Interlocked.Increment( ref EncodeConcurrent );

		System.Action DoEncode = () =>
		{
			var Output = EncodeFunction.Invoke( Data );
			Interlocked.Decrement( ref EncodeConcurrent );
			SendQueue.Add( Output );
		};

		if ( Async )
		{
	#if WINDOWS_UWP
			Windows.System.Threading.ThreadPool.RunAsync( (workitem)=>
	#else
			System.Threading.ThreadPool.QueueUserWorkItem( (workitem)=>
	#endif
			{
				DoEncode.Invoke();
			});
		}
		else
		{
			DoEncode.Invoke();
		}
	}

	public void			Send()
	{
		if ( SendConcurrent >= MaxSendConcurrent )
			return;

		if ( SendQueue.Count == 0 )
			return;

		if ( OnlySendLatest && SendQueue.Count > 1 )
			SendQueue.RemoveRange( 0, SendQueue.Count - 1 );

		var Data = SendQueue[0];
		SendQueue.RemoveAt(0);

		Interlocked.Increment( ref SendConcurrent );
		SendFunction.Invoke( Data, (Success)=>
		{
			Interlocked.Decrement( ref SendConcurrent );
		} );
	}
};


public class PixelFrame
{
	public byte[]	Pixels;
	public Vector2	Size;
	public int		Channels;
	public bool		Rgb;
};


public class SendWebsocketFrame : MonoBehaviour
{
	WebSocket	Socket;
	bool		SocketConnecting = false;
	bool		DebugUpdate = false;
	
	public string[] _hosts = new string[1] {"localhost:8181"};
	private int			_currentHostIndex = -1;
		

	[Range(0,10)]
	public float	RetryTimeSecs = 5;
	private float	RetryTimeout = 1;

	//	websocket commands come on a different thread, so queue them for the next update
	public List<System.Action>	JobQueue;

	FrameQueue<PixelFrame,byte[]>	JpegQueue;

	[Range(1,90)]
	public float			SendFrameRate = 60;
	public float			SendDelayMs {	get { return 1.0f / SendFrameRate; } }
	float LastSendTime;
	float Time_time;
	
    public void setHost(string host) {
        _hosts = new string[1]{ host };
    }

	void SetStatus(string Status){
		Debug.Log("Websocket: " + Status);
	}
		

	string GetCurrentHost(){
		if (_hosts == null || _hosts.Length == 0) return null;

		return _hosts[_currentHostIndex];
	}

	void Start()
	{
		JpegQueue = new FrameQueue<PixelFrame,byte[]>( (Frame) =>
		{
			return PopEncodeJpeg.EncodeToJpeg( Frame.Pixels, (int)Frame.Size.x, (int)Frame.Size.y, Frame.Channels, Frame.Rgb );
		},
		(Json,OnCompleted) =>
		{
			if ( Socket != null )
				Socket.SendAsync( Json, OnCompleted );
			else
				OnCompleted.Invoke(false);
		}
		);
	}

	void Connect(){
	
		if ( Socket != null )
			return;

		if ( SocketConnecting )
			return;

		if (_hosts == null || _hosts.Length == 0) {
			SetStatus("No hosts specified");
			return;
		}

        _currentHostIndex++;
        if(_currentHostIndex >= _hosts.Length) _currentHostIndex = 0;
		
		var Host = GetCurrentHost();
		SetStatus("Connecting to " + Host + "...");

        Debug.Log("Trying to connect to: " + Host );
		
		var NewSocket = new WebSocket("ws://" + Host);
		SocketConnecting = true;
		//NewSocket.Log.Level = LogLevel.TRACE;

		NewSocket.OnOpen += (sender, e) => {
            QueueJob (() => {
				Socket = NewSocket;
				OnConnected();
			});
		};

		NewSocket.OnError += (sender, e) => {
            QueueJob (() => {
				OnError( e.Message, true );
			});
		};

		NewSocket.OnClose += (sender, e) => {
			SocketConnecting = false;
			/*
			if ( LastConnectedHost != null ){
				QueueJob (() => {
					SetStatus("Disconnected from " + LastConnectedHost );
				});
			}
			*/
			OnError( "Closed", true);
		};

		NewSocket.OnMessage += (sender, e) => {

			if ( e.Type == Opcode.TEXT )
				OnTextMessage( e.Data );
			else if ( e.Type == Opcode.BINARY )
				OnBinaryMessage( e.RawData );
			else
				OnError("Unknown opcode " + e.Type, false );
		};

		//Socket.Connect ();
        NewSocket.ConnectAsync ();
		
	}

	void Update(){

		Time_time = Time.time;

		/*
		if (Socket != null && !Socket.IsAlive) {
			OnError ("Socket not alive");
			Socket.Close ();
			Socket = null;
		}
*/
		if (Socket == null ) {

			if (RetryTimeout <= 0) {
				Connect ();
				RetryTimeout = RetryTimeSecs;
			} else {
				RetryTimeout -= Time.deltaTime;
			}
		}
	
		//	commands to execute from other thread
		if (JobQueue != null) {
			while (JobQueue.Count > 0) {

				if ( DebugUpdate )
					Debug.Log("Executing job 0/" + JobQueue.Count);
				var Job = JobQueue [0];
				JobQueue.RemoveAt (0);
				try
				{
					Job.Invoke ();
					if ( DebugUpdate )
						Debug.Log("Job Done.");
				}
				catch(System.Exception e)
				{
					Debug.Log("Job invoke exception: " + e.Message );
				}
			}
		}




		JpegQueue.Encode(true);
		JpegQueue.Send();
		
	}

	


	void OnConnected(){
		SetStatus ("Connected");
	}

	void OnTextMessage(string Message){
		Debug.Log ("Message: " + Message);
	}

	void OnBinaryMessage(byte[] Message){
		SetStatus("Binary Message: " + Message.Length + " bytes");
	}

	void OnError(string Message,bool Close){
		//SetStatus("Error: " + Message );
		Debug.Log("Error: " + Message );

		if (Close) {
			if (Socket != null) {

				//	recurses if we came here from on close
				if ( Socket.IsAlive )
					Socket.Close ();
				Socket = null;
				SocketConnecting = false;
			}
		}
	}


	void OnApplicationQuit(){
		
		if (Socket != null) {
			//	if (Socket.IsAlive)
			Socket.Close ();
		}
		
	}

	void QueueJob(System.Action Job){
		if (JobQueue == null)
			JobQueue = new List<System.Action> ();
		JobQueue.Add( Job );
	}
	


	public void		OnPixels(byte[] Pixels,Vector2 Size,int Channels)
	{
		var Frame = new PixelFrame ();
		Frame.Pixels = Pixels;
		Frame.Size = Size;
		Frame.Channels = Channels;
		Frame.Rgb = true;

		var TimeSinceLastSend = Time_time - LastSendTime;
		if (TimeSinceLastSend < SendDelayMs) {
			return;
		}

		JpegQueue.Push (Frame);
		LastSendTime = Time_time;

	}
	

}
