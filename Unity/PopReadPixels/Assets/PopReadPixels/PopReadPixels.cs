using UnityEngine;
using System.Collections;					// required for Coroutines
using System.Runtime.InteropServices;		// required for DllImport
using System;								// requred for IntPtr
using System.Text;
using System.Collections.Generic;



/// <summary>
///	Low level interface
/// </summary>
public static class PopReadPixels 
{
#if UNITY_EDITOR_OSX || UNITY_OSX
	private const string PluginName = "PopReadPixels_Osx";
#elif UNITY_IOS
	//private const string PluginName = "PopReadPixels_Ios";
	private const string PluginName = "__Internal";
#else
#error Unsupported platform
#endif

	[DllImport (PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern System.IntPtr	PopDebugString();

	[DllImport (PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern void		ReleaseDebugString(System.IntPtr String);

	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern int		ReadPixelFromRenderTexture(IntPtr Texture,byte[] PixelData,int PixelDataSize,int[] WidthHeightChannels,RenderTextureFormat PixelFormat);

	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern int		ReadPixelFromTexture2D(IntPtr Texture,byte[] PixelData,int PixelDataSize,int[] WidthHeightChannels,TextureFormat PixelFormat);



	delegate void UnityRenderEvent(int EventId);


	class RenderEventJob
	{
		public UnityRenderEvent	Lambda;
		int					EventId;
		bool				Finished = false;

		public bool			IsFinished	{	get { return Finished; } }

		public RenderEventJob(UnityRenderEvent _Lambda,int _EventId)
		{
			EventId = _EventId;
			Lambda = _Lambda;
		}


		public bool			Finish(int _EventId)
		{
			if (EventId == _EventId) {
				Finished = true;
				return true;
			}
			return false;
		}
	};

	//	gr: mono/unity crashes and locks up a lot using this wrapper method.
	//		by keeping a copy of the action the crash goes away (guess it's a refcount thing)
	static List<RenderEventJob> PendingJobs = new List<RenderEventJob>();
	static int JobCounter = 0;
	static void MarkJobFinished(int EventId)
	{
		foreach ( var Job in PendingJobs )
			if ( Job.Finish( EventId ) )
				return;
	}
	static void CullFinishedJobs()
	{
		//	if I cull these, when the editor stops, the garbage collector kicks in and we lock up
		for (int j = PendingJobs.Count-1;	j >= 0;	j--)
			if (PendingJobs[j].IsFinished)
				PendingJobs.RemoveAt (j);
		
	}
	static void KeepAlivePendingJobs()
	{
		if (PendingJobs == null) {
			Debug.Log ("Keep alive null jobs");
			return;
		}

		//	this will leak an object, but it only happens in the editor, so should be tolerable.
		//Debug.Log ("Keep alive " + PendingJobs.Count);
		foreach (var Job in PendingJobs)
			if (!Job.IsFinished)
				GC.KeepAlive (Job.Lambda);
	}
	static bool AddedPlaymodeStateChangedFunc = false;

	public static IntPtr GetReadPixelsEventFunc(Texture texture,System.Action<byte[],Vector2,int,string> Callback,int JobEventId)
	{
		//	when the editor stops, the static references are cleaned up, before the render thread is finished using it below
		if (!AddedPlaymodeStateChangedFunc) {
			UnityEditor.EditorApplication.playmodeStateChanged += KeepAlivePendingJobs;
			AddedPlaymodeStateChangedFunc = true;
		}

		//	must be called on main thread... but causes GPU system. need to cache
		var TexturePtr = texture.GetNativeTexturePtr();
		var PixelBytes = new byte[texture.width * texture.height * 4];
		var WidthHeightChannels = new int[3]{ texture.width, texture.height, 4 };
		var PixelBytesLength = PixelBytes.Length;

		RenderTextureFormat FormatRT = (texture is RenderTexture) ? (texture as RenderTexture).format : RenderTextureFormat.ARGBFloat;
		TextureFormat Format2D = (texture is Texture2D) ? (texture as Texture2D).format : TextureFormat.RGBA32;


		UnityRenderEvent ReadPixelsWrapper = (int EventId) => 
		{			
			try
			{				
				var Result = -1;

				if (texture is RenderTexture) {
					var Format = FormatRT;
					Result = ReadPixelFromRenderTexture (TexturePtr, PixelBytes, PixelBytesLength, WidthHeightChannels, Format);
				} else if (texture is Texture2D) {
					var Format = Format2D;
					Result = ReadPixelFromTexture2D (TexturePtr, PixelBytes, PixelBytesLength, WidthHeightChannels, Format);
				}
				else 
					throw new System.Exception("Unhandled texture type " + typeof(Texture) );
										
				if ( Result < 0 )
					throw new System.Exception("ReadPixelsAsync result=" + Result);
				
				var WidthHeight = new Vector2( WidthHeightChannels[0], WidthHeightChannels[1] );
				var Channels = WidthHeightChannels[2];
				Callback.Invoke( PixelBytes, WidthHeight, Channels, null );
			}
			catch(System.Exception e)
			{
				Callback.Invoke( null, Vector2.zero, 0, e.Message );
			}

			MarkJobFinished( EventId );
		};

		CullFinishedJobs ();
		//Debug.Log ("Post cull, pending job count=" + PendingJobs.Count);
		PendingJobs.Add ( new RenderEventJob( ReadPixelsWrapper, JobEventId ) );
	
		//	this stops the editor prematurely cleaning up the lambda, but it'll leak ALL of them
		//GC.KeepAlive (ReadPixelsWrapper);

		var FunctionPtr = Marshal.GetFunctionPointerForDelegate ( ReadPixelsWrapper );
		return FunctionPtr;
	}

	public static void ReadPixelsAsync(Texture texture,System.Action<byte[],Vector2,int,string> Callback)
	{
		var EventId = JobCounter++;
		var FunctionPtr = GetReadPixelsEventFunc (texture, Callback, EventId );
		GL.IssuePluginEvent( FunctionPtr, EventId );
	}

	public static void FlushDebug()
	{
		FlushDebug ((str)=>
			{
				Debug.Log(str);
			}
		);
	}
		
	public static void FlushDebug(System.Action<string> Callback)
	{
		//	gr: this func is crashing unity. But I can't figure out why.
		return;
		
		int MaxFlushPerFrame = 100;
		int i = 0;
		while (i++ < MaxFlushPerFrame)
		{
			System.IntPtr StringPtr = System.IntPtr.Zero;
			try
			{
				StringPtr = PopDebugString();
			}
			catch
			{
			}

			//	no more strings to pop
			if (StringPtr == System.IntPtr.Zero)
				break;

			try
			{
				string Str = Marshal.PtrToStringAnsi(StringPtr);
				if (Callback != null)
					Callback.Invoke(Str);
				ReleaseDebugString(StringPtr);
			}
			catch
			{
				ReleaseDebugString(StringPtr);
				throw;
			}
		}
	}

}
