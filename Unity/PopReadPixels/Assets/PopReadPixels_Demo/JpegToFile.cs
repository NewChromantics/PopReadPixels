using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JpegToFile : MonoBehaviour {

	public string		OutputFilename = "Test.jpg";


	public void			OnPixels(byte[] Pixels,Vector2 Size,int Channels)
	{
		//	async
		System.Threading.ThreadPool.QueueUserWorkItem ((WorkItem) => {
			var Jpeg = PopEncodeJpeg.EncodeToJpeg( Pixels, (int)Size.x, (int)Size.y, Channels, true ); 
			System.IO.File.WriteAllBytes( OutputFilename, Jpeg );
		}
		);

	}

}
