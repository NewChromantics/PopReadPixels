#pragma once

#include "PopUnity.hpp"
#include <functional>


__export int						ReadPixelFromRenderTexture(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,Unity::RenderTexturePixelFormat::Type PixelFormat);
__export int						ReadPixelFromTexture2D(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,Unity::Texture2DPixelFormat::Type PixelFormat);

__export UnityRenderingEvent		AllocCacheRenderTexture(void* TexturePtr,uint8_t* PixelData,uint8_t* PixelRevision,uint8_t* CacheIndex,int PixelDataSize,int Width,int Height,int Channels,Unity::RenderTexturePixelFormat::Type PixelFormat);
__export UnityRenderingEvent		AllocCacheTexture2D(void* TexturePtr,uint8_t* PixelData,uint8_t* PixelRevision,uint8_t* CacheIndex,int PixelDataSize,int Width,int Height,int Channels,Unity::Texture2DPixelFormat::Type PixelFormat);
__export void						ReleaseCache(uint8_t Cache);
__export UNITY_INTERFACE_API void	ReadPixelsFromCache(int Cache);


