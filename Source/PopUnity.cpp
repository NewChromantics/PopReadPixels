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



#if defined(TARGET_WINDOWS)
BOOL APIENTRY DllMain(HMODULE /* hModule */, DWORD ul_reason_for_call, LPVOID /* lpReserved */)
{
	switch (ul_reason_for_call)
	{
		case DLL_PROCESS_ATTACH:
		case DLL_THREAD_ATTACH:
		case DLL_THREAD_DETACH:
		case DLL_PROCESS_DETACH:
			break;
	}
	return TRUE;
}
#endif




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
