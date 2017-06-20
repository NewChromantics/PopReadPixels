using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class UnityEvent_Frame : UnityEngine.Events.UnityEvent <byte[],Vector2,int> {}


public class BlitToJpeg : MonoBehaviour {

	public RenderTexture	DynamicTexture;
	public Material			DynamicShader;
	public UnityEvent_Frame	OnPixelsRead;

	PopReadPixels.JobCache	AsyncRead;

	void Update () {
		

		System.Action<byte[],int,string> OnTexturePixels = (Pixels, Channels, Error) => {

			if (Error != null)
				Debug.Log ("Read pixels: " + Error);

			if (Pixels != null) {
				try {
					OnPixelsRead.Invoke (Pixels, new Vector2(DynamicTexture.width,DynamicTexture.height), Channels);
				} catch (System.Exception e) {
					Debug.LogException (e, this);
				}
			}

		};

		Graphics.Blit (null, DynamicTexture, DynamicShader);

		if (AsyncRead != null) {
			if (AsyncRead.HasChanged ()) {
				Debug.Log ("Read changed");

				/*
				AsyncRead.Release ();
				AsyncRead = null;
				System.GC.Collect ();
				*/
			} else
				Debug.Log ("REad not changed");
		}

		if ( AsyncRead == null )
			AsyncRead = PopReadPixels.ReadPixelsAsync2(DynamicTexture, OnTexturePixels);

		System.GC.Collect ();
	}
}
