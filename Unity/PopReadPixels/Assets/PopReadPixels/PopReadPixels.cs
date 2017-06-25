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
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
	private const string PluginName = "PopReadPixels_Osx";
#elif UNITY_IOS
	//private const string PluginName = "PopReadPixels_Ios";
	private const string PluginName = "__Internal";
#elif UNITY_ANDROID
	private const string PluginName = "PopReadPixels";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
	private const string PluginName = "PopReadPixels";
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


	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern int			AllocCacheRenderTexture(IntPtr TexturePtr,int Width,int Height,RenderTextureFormat PixelFormat);

	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern int			AllocCacheTexture2D(IntPtr TexturePtr,int Width,int Height,TextureFormat PixelFormat);

	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern void			ReleaseCache(int Cache);

	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern void			ReadPixelsFromCache(int Cache);

	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr		GetReadPixelsFromCacheFunc();

	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern int			ReadPixelBytesFromCache(int Cache,byte[] ByteData,int ByteDataSize);



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
		#if UNITY_EDITOR
		//	when the editor stops, the static references are cleaned up, before the render thread is finished using it below
		if (!AddedPlaymodeStateChangedFunc) {
			UnityEditor.EditorApplication.playmodeStateChanged += KeepAlivePendingJobs;
			AddedPlaymodeStateChangedFunc = true;
		}
		#endif

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


	public class JobCache
	{
		int?		CacheIndex = null;
		int?		LastRevision = null;
		int			Channels = 4;
		byte[]		PixelBytes;
		System.Action<byte[],int,string>	Callback;
		IntPtr		TexturePtr;
		IntPtr		PluginFunction;

		public JobCache(RenderTexture texture,System.Action<byte[],int,string> _Callback)
		{
			TexturePtr = texture.GetNativeTexturePtr();
			CacheIndex = AllocCacheRenderTexture( TexturePtr, texture.width, texture.height, texture.format );
			if ( CacheIndex == -1 )
				throw new System.Exception("Failed to allocate cache index");

			PixelBytes = new byte[texture.width * texture.height * Channels];
			Callback = _Callback;
			PluginFunction = GetReadPixelsFromCacheFunc();
		}

		public JobCache(Texture2D texture,System.Action<byte[],int,string> _Callback)
		{
			TexturePtr = texture.GetNativeTexturePtr();
			CacheIndex = AllocCacheTexture2D( TexturePtr, texture.width, texture.height, texture.format );
			if ( CacheIndex == -1 )
				throw new System.Exception("Failed to allocate cache index");

			PixelBytes = new byte[texture.width * texture.height * Channels];
			Callback = _Callback;
			PluginFunction = GetReadPixelsFromCacheFunc();
		}

		public void ReadAsync()
		{
			GL.IssuePluginEvent( PluginFunction, CacheIndex.Value );
		}

		protected virtual void Finalize()
		{
			Release();
		}

		public bool	HasChanged()
		{
			if ( !this.CacheIndex.HasValue )
				return false;

			//	read & copy latest bytes
			try
			{
				var Revision = ReadPixelBytesFromCache(this.CacheIndex.Value, PixelBytes, PixelBytes.Length);
				if (Revision < 0)
					throw new System.Exception("ReadPixelBytesFromCache returned " + Revision);

				var Changed = LastRevision.HasValue ? (LastRevision.Value != Revision) : true;
				if (Changed)
					Callback.Invoke(PixelBytes, Channels, null);
				LastRevision = Revision;
				return Changed;
			}
			catch (System.Exception e)
			{
				Callback.Invoke (null, 0, e.Message);
				return false;
			}
		}

		public void	Release()
		{
			if ( CacheIndex.HasValue )
				ReleaseCache( CacheIndex.Value );
		}
	}

	public static JobCache ReadPixelsAsync2(Texture texture,System.Action<byte[],int,string> Callback)
	{
		Debug.Log ("allocating");
		if ( texture is RenderTexture )
		{
			var Job = new JobCache( texture as RenderTexture, Callback );
			Job.ReadAsync();
			return Job;
		}

		if ( texture is Texture2D )
		{
			var Job = new JobCache( texture as Texture2D, Callback );
			Job.ReadAsync();
			return Job;
		}

		throw new System.Exception("Texture type not handled");
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
