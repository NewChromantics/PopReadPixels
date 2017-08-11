using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class UnityEvent_Frame : UnityEngine.Events.UnityEvent <byte[],Vector2,int> {}

[System.Serializable]
public class UnityEvent_FrameFloat : UnityEngine.Events.UnityEvent <float[],Vector2,int> {}


public class BlitToJpeg : MonoBehaviour {

	public bool				ReadAsFloat = false;

	public RenderTexture	DynamicTexture;
	public Material			DynamicShader;
	public UnityEvent_Frame	OnPixelsRead;
	public UnityEvent_FrameFloat	OnPixelsFloatRead;
	public UnityEvent		OnPixelsError;

	PopReadPixels.JobCache	AsyncRead;
	Color[]					LastPixels;

	void Update () {


		System.Action<byte[],int,string> OnTextureBytePixels = (Pixels, Channels, Error) => {

			if (Error != null)
			{
				Debug.Log ("Read pixels: " + Error);
				OnPixelsError.Invoke();
			}

			if (Pixels != null) {
				try {
					OnPixelsRead.Invoke (Pixels, new Vector2(DynamicTexture.width,DynamicTexture.height), Channels);
				} catch (System.Exception e) {
					Debug.LogException (e, this);
					OnPixelsError.Invoke();
				}
			}

		};

		System.Action<float[],int,string> OnTextureFloatPixels = (Pixels, Channels, Error) => {

			if (Error != null)
			{
				Debug.Log ("Read pixels: " + Error);
				OnPixelsError.Invoke();
			}

			if (Pixels != null) {
				try {
					OnPixelsFloatRead.Invoke (Pixels, new Vector2(DynamicTexture.width,DynamicTexture.height), Channels);
				} catch (System.Exception e) {
					Debug.LogException (e, this);
					OnPixelsError.Invoke();
				}
			}

		};

		if (DynamicTexture.useMipMap)
			Debug.LogWarning (DynamicTexture.name + " has mip maps");
		Graphics.Blit (null, DynamicTexture, DynamicShader);
		/*
		var Temp = new Texture2D (DynamicTexture.width, DynamicTexture.height, TextureFormat.RGBAFloat, false);
		RenderTexture.active = DynamicTexture;
		Temp.ReadPixels (new Rect (0, 0, DynamicTexture.width, DynamicTexture.height), 0, 0);
		LastPixels = Temp.GetPixels ();
		RenderTexture.active = null;
*/
		if (AsyncRead != null) {

			//	trigger a read next time
			AsyncRead.ReadAsync();

			if (AsyncRead.HasChanged ())
			{
				//Debug.Log ("Read changed");

				//	realloc, or reuse
				/*
				AsyncRead.Release ();
				AsyncRead = null;
				System.GC.Collect ();
				*/
			}
			else
			{
				Debug.Log ("Read not changed");
			}
		}

		if ( AsyncRead == null )
		{
			if ( ReadAsFloat )
				AsyncRead = PopReadPixels.ReadPixelsAsync(DynamicTexture, OnTextureFloatPixels,null);
			else
				AsyncRead = PopReadPixels.ReadPixelsAsync(DynamicTexture, OnTextureBytePixels,null);

		}

		PopReadPixels.FlushDebug ();
	}
}
