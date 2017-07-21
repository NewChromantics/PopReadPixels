#pragma once

#include <SoyTypes.h>

#include <cstdint>
#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"

#if defined(TARGET_WINDOWS)
#define __api(returntype)	returntype __stdcall 
#define __export			extern "C" __declspec(dllexport)
#elif defined(TARGET_OSX) || defined(TARGET_IOS)|| defined(TARGET_ANDROID)
#define __api(returntype)	extern "C" returntype
#define __export			extern "C"
#endif

#include <SoyUnity.h>

__export const char*		PopDebugString();
__export void				ReleaseDebugString(const char* String);
