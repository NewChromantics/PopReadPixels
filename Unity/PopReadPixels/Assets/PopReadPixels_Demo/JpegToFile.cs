using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JpegToFile : MonoBehaviour {

	public bool			IncrementFilename = false;
	int FilenameCount = 0;

	[FilePath("jpg",order=0)]
	//[ShowIf("IsFixedFilename",order=1)]
	public string		OutputFilename = "Test.jpg";

	[FilePath(FilePathAttribute.PathType.Folder,order=0)]
	//[ShowIf("IsIncrementingFilename",order=1)]
	public string		OutputFileDir = "";

	bool IsFixedFilename()
	{
		return !IncrementFilename;
	}
	bool IsIncrementingFilename()
	{
		return IncrementFilename;
	}

	public void			OnPixels(byte[] Pixels,Vector2 Size,int Channels)
	{
		var NextFilename = OutputFilename;

		if (IncrementFilename) {
			NextFilename = OutputFileDir + System.IO.Path.DirectorySeparatorChar + "Frame" + FilenameCount + ".jpg";
			FilenameCount++;
		}

		//	async
		System.Threading.ThreadPool.QueueUserWorkItem ((WorkItem) => {
			var Jpeg = PopEncodeJpeg.EncodeToJpeg( Pixels, (int)Size.x, (int)Size.y, Channels, true ); 
			System.IO.File.WriteAllBytes( NextFilename, Jpeg );
		}
		);

	}

}
