#include "PopReadPixels.hpp"
#include "PopDebug.hpp"
#include <sstream>
#include <algorithm>
#include <functional>



__export int			ReadPixelsAsync(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels)
{
	try
	{
		return 0;
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in EnumStrings; " << e.what();
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in EnumStrings";
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
}


