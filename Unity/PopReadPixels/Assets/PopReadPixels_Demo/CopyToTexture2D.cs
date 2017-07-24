using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyToTexture2D : MonoBehaviour {

	public UnityEvent_Texture		OnOutputChanged;
	public UnityEvent_String		OnError;
	Texture2D						Output;
	public TextureFormat			OutputFormat = TextureFormat.RGBAFloat;

	[Range(1,200)]
	public int						MaxRowsToRead = 200;

	public void OnPixels(float[] Bytes,Vector2 Size,int ChannelCount)
	{
		//	convert to colours
		var Colours = new Color[(int)Size.x*(int)Size.y];
		Output = new Texture2D( (int)Size.x, (int)Size.y, OutputFormat, false );
		OnError.Invoke ("" + Colours.Length + " pixels");

		var TargetColours = Output.GetPixels ();
		if (Colours.Length != TargetColours.Length) {
			OnError.Invoke ("Colour length(" + Colours.Length + ") mis match to texture (" + TargetColours.Length + ")");
		}

		try
		{
			for ( int p=0;	p<Colours.Length;	p++)
			{
				var i = p * ChannelCount;

				i %= ChannelCount * (int)Size.x * MaxRowsToRead;

				Colours[p].r = Bytes[i+0];
				Colours[p].g = Bytes[i+1];
				Colours[p].b = Bytes[i+2];
				//Colours[p].a = Bytes[i+3];
				Colours[p].a = 1;


			}
		}
		catch (System.Exception e)
		{
			OnError.Invoke (e.Message);
		}

		/*
		var ColourBytes = new byte[Colours.Length * 4 * 4];
		Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
		texture.LoadRawTextureData(bytes);
		texture.Apply();
		Output.LoadRawTextureData(
		*/
		Output.SetPixels( Colours, 0 );
		Output.Apply();
		OnOutputChanged.Invoke( Output );
	}
}
