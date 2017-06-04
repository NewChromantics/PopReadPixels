#pragma once

#include "PopUnity.hpp"
#include <functional>


__export int			ReadPixelFromRenderTexture(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,Unity::RenderTexturePixelFormat::Type PixelFormat);
__export int			ReadPixelFromTexture2D(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,Unity::Texture2DPixelFormat::Type PixelFormat);
