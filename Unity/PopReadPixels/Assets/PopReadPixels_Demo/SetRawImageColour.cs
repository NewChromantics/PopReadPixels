using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetRawImageColour : MonoBehaviour {

	public RawImage		Target;
	public Text			TargetString;
	public Color32		ErrorColour = Color.red;

	void SetColour(Color32 Colour)
	{
		Target.color = Colour;
	}

	void SetColour(Color Colour)
	{
		Target.color = Colour;
	}

	public void SetErrorColour()
	{
		SetColour( ErrorColour );
	}


	public void OnPixels(byte[] Bytes,Vector2 Size,int ChannelCount)
	{
		try
		{
			var Colour = new Color32( Bytes[0], Bytes[1], Bytes[2], 255 );
			SetColour( Colour);

			if ( TargetString != null )
			{
				TargetString.text = "Byte[0] = ( " + Bytes[0] + " " + Bytes[1] + " " + Bytes[2] + " " + Bytes[3] + " )";
			}
		}
		catch
		{
			SetColour( ErrorColour );
		}
	}



	public void OnPixels(float[] Bytes,Vector2 Size,int ChannelCount)
	{
		try
		{
			var Colour = new Color( Bytes[0], Bytes[1], Bytes[2], 255 );
			SetColour( Colour);

			if ( TargetString != null )
			{
				TargetString.text = "Byte[0] = ( " + Bytes[0] + " " + Bytes[1] + " " + Bytes[2] + " " + Bytes[3] + " )";
			}
		}
		catch
		{
			SetColour( ErrorColour );
		}
	}
}


