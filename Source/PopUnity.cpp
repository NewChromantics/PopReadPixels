#include "PopUnity.hpp"
#include <exception>
#include <stdexcept>
#include <vector>
#include <sstream>
#include <algorithm>
#include "TStringBuffer.hpp"
#include "PopDebug.hpp"
#include <SoyUnity.h>



int Unity::GetPluginEventId()
{
	return 0xaabb22;
}

bool Unity::IsDebugPluginEventEnabled()
{
	return false;
}



__export const char* PopDebugString()
{
	try
	{
		auto& DebugStrings = PopUnity::GetDebugStrings();
		return DebugStrings.Pop();
	}
	catch(...)
	{
		//	bit recursive if we push one?
		return nullptr;
	}
}

__export void ReleaseDebugString(const char* String)
{
	try
	{
		auto& DebugStrings = PopUnity::GetDebugStrings();
		DebugStrings.Release( String );
	}
	catch(...)
	{
	}
}
