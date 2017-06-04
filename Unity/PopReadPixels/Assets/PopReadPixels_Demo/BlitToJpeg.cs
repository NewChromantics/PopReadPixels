using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class UnityEvent_Bytes : UnityEngine.Events.UnityEvent <byte[]> {}


public class BlitToJpeg : MonoBehaviour {

	public RenderTexture	DynamicTexture;
	public Material			DynamicShader;
	public string			OutputFilename = "Test.jpg";
	public UnityEvent_Bytes	OnJpegEncoded;

	void Update () {


		System.Action<byte[],Vector2,int,string> OnTexturePixels = (Pixels, Size, Channels,Error) => {

			if ( Error != null )
				Debug.Log("Read pixels: " + Error);

			if ( Pixels != null )
			{
				try
				{
					var Jpeg = PopEncodeJpeg.EncodeToJpeg( Pixels, (int)Size.x, (int)Size.y, Channels, true ); 
					OnJpegEncoded.Invoke(Jpeg);
					System.IO.File.WriteAllBytes( OutputFilename, Jpeg );
				}
				catch(System.Exception e)
				{
					Debug.LogException( e, this );
				}
			}
		};

		Graphics.Blit (null, DynamicTexture, DynamicShader);
		PopReadPixels.ReadPixelsAsync (DynamicTexture, OnTexturePixels);

		PopReadPixels.FlushDebug ();

	}
}
