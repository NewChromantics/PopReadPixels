#pragma once

#include "PopUnity.hpp"
#include <functional>


__export int					ReadPixelFromRenderTexture(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,Unity::RenderTexturePixelFormat::Type PixelFormat);
__export int					ReadPixelFromTexture2D(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,Unity::Texture2DPixelFormat::Type PixelFormat);

__export UnityRenderingEvent	GetReadPixelsFromCacheFunc();
__export int					AllocCacheRenderTexture(void* TexturePtr,int Width,int Height,Unity::boolean ReadAsFloat,Unity::RenderTexturePixelFormat::Type PixelFormat);
__export int					AllocCacheTexture2D(void* TexturePtr,int Width,int Height,Unity::boolean ReadAsFloat,Unity::Texture2DPixelFormat::Type PixelFormat);
__export void					ReleaseCache(int Cache);
__api(void)						ReadPixelsFromCache(int Cache);

__export int					ReadPixelBytesFromCache(int Cache,uint8_t* ByteData,int ByteDataSize);
__export int					ReadPixelFloatsFromCache(int Cache,Unity::Float* FloatData,int FloatDataSize);


