using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class UnityEvent_Frame : UnityEngine.Events.UnityEvent <byte[],Vector2,int> {}


public class BlitToJpeg : MonoBehaviour {

	public RenderTexture	DynamicTexture;
	public Material			DynamicShader;
	public UnityEvent_Frame	OnPixelsRead;
	public UnityEvent		OnPixelsError;

	PopReadPixels.JobCache	AsyncRead;

	void Update () {
		

		System.Action<byte[],int,string> OnTexturePixels = (Pixels, Channels, Error) => {

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

		Graphics.Blit (null, DynamicTexture, DynamicShader);

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
			AsyncRead = PopReadPixels.ReadPixelsAsync(DynamicTexture, OnTexturePixels);

		PopReadPixels.FlushDebug ();
	}
}
