using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class UnityEvent_Bytes : UnityEngine.Events.UnityEvent <byte[]> {}


public class BlitToJpeg : MonoBehaviour {

	public RenderTexture	DynamicTexture;
	public Material			DynamicShader;
	public UnityEvent_Bytes	OnJpegEncoded;

	void Update () {

		System.Action<byte[],Vector2,int,string> OnTexturePixels = (Pixels, Size, Channels,Error) => {
			Debug.Log("Read pixels: " + Error);
		};

		Graphics.Blit (null, DynamicTexture, DynamicShader);
		PopReadPixels.ReadPixelsAsync (DynamicTexture, OnTexturePixels);

	}
}
