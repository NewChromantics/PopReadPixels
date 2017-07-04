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
	public PopRelayClient	Socket;
	bool		DebugUpdate = false;
	
	FrameQueue<PixelFrame,byte[]>	JpegQueue;

	[Range(1,90)]
	public float			SendFrameRate = 60;
	public float			SendDelayMs {	get { return 1.0f / SendFrameRate; } }
	float LastSendTime;
	float Time_time;
	

	void Start()
	{
		JpegQueue = new FrameQueue<PixelFrame,byte[]>( (Frame) =>
		{
			return PopEncodeJpeg.EncodeToJpeg( Frame.Pixels, (int)Frame.Size.x, (int)Frame.Size.y, Frame.Channels, Frame.Rgb );
		},
		(Bytes,OnCompleted) =>
		{
			try
			{
				Socket.Send(Bytes, OnCompleted );
			}
			catch
			{
				OnCompleted.Invoke(false);
			}
			}
		);
	}


	void Update(){

		Time_time = Time.time;
		JpegQueue.Encode(true);
		JpegQueue.Send();
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
