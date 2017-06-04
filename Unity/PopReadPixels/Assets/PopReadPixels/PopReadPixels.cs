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
	private static extern int		ReadPixelFromRenderTexture(IntPtr Texture,byte[] PixelData,int PixelDataSize,int[] WidthHeightChannels,RenderTextureFormat PixelFormat);

	[DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
	private static extern int		ReadPixelFromTexture2D(IntPtr Texture,byte[] PixelData,int PixelDataSize,int[] WidthHeightChannels,TextureFormat PixelFormat);



	delegate void UnityRenderEvent(int EventId);


	public static IntPtr GetReadPixelsEventFunc(Texture texture,System.Action<byte[],Vector2,int,string> Callback)
	{
		//	must be called on main thread... but causes GPU system. need to cache
		var TexturePtr = texture.GetNativeTexturePtr();
		var PixelBytes = new byte[texture.width * texture.height * 4];
		var WidthHeightChannels = new int[3]{ texture.width, texture.height, 4 };
		var PixelBytesLength = PixelBytes.Length;

		RenderTextureFormat FormatRT = (texture is RenderTexture) ? (texture as RenderTexture).format : RenderTextureFormat.ARGBFloat;
		TextureFormat Format2D = (texture is Texture2D) ? (texture as Texture2D).format : TextureFormat.RGBA32;


		UnityRenderEvent ReadPixelsWrapper = (int EventId) => {
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
