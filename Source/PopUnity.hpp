#pragma once


#if defined(_MSC_VER)
#define TARGET_WINDOWS
#endif

#if defined(TARGET_WINDOWS)
#include <SDKDDKVer.h>
#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#endif

#include <cstdint>
#include "Unity/IUnityInterface.h"

#if defined(TARGET_WINDOWS)
#define __export			extern "C" __declspec(dllexport)
#elif defined(TARGET_OSX)
#define __export			extern "C"
#endif

#include <SoyUnity.h>

__export const char*		PopDebugString();
__export void				ReleaseDebugString(const char* String);
