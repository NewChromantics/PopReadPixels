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

#define __export			extern "C" __declspec(dllexport)


__export const char*		PopDebugString();
__export void				ReleaseDebugString(const char* String);
