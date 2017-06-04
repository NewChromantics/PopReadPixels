using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class UnityEvent_Frame : UnityEngine.Events.UnityEvent <byte[],Vector2,int> {}


public class BlitToJpeg : MonoBehaviour {

	public RenderTexture	DynamicTexture;
	public Material			DynamicShader;
	public UnityEvent_Frame	OnPixelsRead;

	void Update () {
		

		System.Action<byte[],Vector2,int,string> OnTexturePixels = (Pixels, Size, Channels, Error) => {

			if (Error != null)
				Debug.Log ("Read pixels: " + Error);

			if (Pixels != null) {
				try {
					OnPixelsRead.Invoke (Pixels, Size, Channels);
				} catch (System.Exception e) {
					Debug.LogException (e, this);
				}
			}

		};

		Graphics.Blit (null, DynamicTexture, DynamicShader);
		PopReadPixels.ReadPixelsAsync (DynamicTexture, OnTexturePixels);

	}
}
