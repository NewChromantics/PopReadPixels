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
	private const string PluginName = "PopReadPixels";

	[DllImport (PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern System.IntPtr	PopDebugString();

	[DllImport (PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern void		ReleaseDebugString(System.IntPtr String);

	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern int		ReadPixelsAsync(IntPtr Texture,byte[] PixelData,int PixelDataSize,int[] WidthHeightChannels);


	delegate void UnityRenderEvent(int EventId);


	public static IntPtr GetReadPixelsEventFunc(Texture texture,System.Action<byte[],Vector2,int,string> Callback)
	{
		//	must be called on main thread... but causes GPU system. need to cache
		var TexturePtr = texture.GetNativeTexturePtr();
		var PixelBytes = new byte[texture.width * texture.height * 4];

		UnityRenderEvent ReadPixelsWrapper = (int EventId) => {
			try
			{
				var WidthHeightChannels = new int[3]; 
				var Result = ReadPixelsAsync( TexturePtr, PixelBytes, PixelBytes.Length, WidthHeightChannels );
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
		};
	
		var FunctionPtr = Marshal.GetFunctionPointerForDelegate ( ReadPixelsWrapper );
		return FunctionPtr;
	}

	public static void ReadPixelsAsync(Texture texture,System.Action<byte[],Vector2,int,string> Callback)
	{
		var FunctionPtr = GetReadPixelsEventFunc (texture, Callback);
		GL.IssuePluginEvent( FunctionPtr, 0 );
	}

	public static void FlushDebug()
	{
		FlushDebug (Debug.Log);
	}
		
	public static void FlushDebug(System.Action<string> Callback)
	{
		int MaxFlushPerFrame = 100;
		int i = 0;
		while (i++ < MaxFlushPerFrame)
		{
			System.IntPtr StringPtr = PopDebugString();

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
