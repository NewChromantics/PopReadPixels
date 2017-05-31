#include "PopWinCap.hpp"
#include "PopDebug.hpp"
#include <sstream>
#include <algorithm>
#include <functional>


//	global that doesn't get destroyed stores the enum string we pass to c#
namespace PopWinCap
{
	char			gEnumWindowsString[1024*1024] = { '\0' };

	const char*		GetEnumWindowsString();
	const char*		GetEnumWindowsString(const std::string& NewString);
	void			EnumWindows(std::function<void(std::string&)> OnEnumWindow);
}


__export const char* EnumWindowNames()
{
	try
	{
		return PopWinCap::GetEnumWindowsString();
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in EnumStrings; " << e.what();
		PopUnity::DebugLog( Error.str() );
		return nullptr;
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in EnumStrings";
		PopUnity::DebugLog( Error.str() );
		return nullptr;
	}
}


const char* PopWinCap::GetEnumWindowsString(const std::string& NewString)
{
	return gEnumWindowsString;
}



const char* PopWinCap::GetEnumWindowsString()
{
	char Seperator = '|';
	char SeperatorReplacement = '?';
	std::stringstream Buffer;

	//	initialise with seperator so this can be dynamic
	Buffer << Seperator;
	auto AddWindow = [](std::string& WindowName)
	{
		//std::replace( WindowName.begin(), WindowName.end(), Seperator, SeperatorReplacement );
		//Buffer << WindowName << Seperator;
	};
	//EnumWindows( AddWindow );

	return GetEnumWindowsString( Buffer.str() );
}


void PopWinCap::EnumWindows(std::function<void(std::string&)> OnEnumWindow)
{
}

