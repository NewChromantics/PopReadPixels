#include "PopReadPixels.hpp"
#include "PopDebug.hpp"
#include <sstream>
#include <algorithm>
#include <functional>
#include <SoyUnity.h>

#if defined(ENABLE_OPENGL)
#include <SoyOpengl.h>
#include <SoyOpenglContext.h>
#endif

#if defined(ENABLE_DIRECTX)
#include <SoyDirectx.h>
#endif

#if defined(ENABLE_DIRECTX9)
#include <SoyDirectx9.h>
#endif

int ReadPixelFromTexture(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,SoyPixelsMeta Meta)
{
	WidthHeightChannels[2] = Meta.GetChannels();
	SoyPixelsRemote Pixels( PixelData, PixelDataSize, Meta );

#if defined(ENABLE_OPENGL)
	auto OpenglContext = Unity::GetOpenglContextPtr();
#endif

#if defined(ENABLE_DIRECTX)
	auto DirectxContext = Unity::GetDirectxContextPtr();
#endif

#if defined(ENABLE_DIRECTX9)
	auto Directx9Context = Unity::GetDirectx9ContextPtr();
#endif

#if defined(ENABLE_OPENGL)
	if ( OpenglContext )
	{
		//	gr: in this plugin we're not calling the context iteration all the time, so lets do it ourselves for ::init() to create GLGenBuffers
		OpenglContext->Iteration();
		
		//	assuming type atm... maybe we can extract it via opengl?
		GLenum Type = GL_TEXTURE_2D;
		Opengl::TTexture Texture( TexturePtr, Meta, Type );
		Texture.Read( Pixels );
		return 0;
	}
#endif
	
#if defined(ENABLE_DIRECTX)
	if ( DirectxContext )
	{
		Directx::TTexture Texture( static_cast<ID3D11Texture2D*>(TexturePtr) );
		Texture.Read( Pixels, *DirectxContext );
		return 0;
	}
#endif

#if defined(ENABLE_DIRECTX9)
	if ( Directx9Context )
	{
		Directx9::TTexture Texture( static_cast<IDirect3DTexture9*>(TexturePtr) );
		Texture.Read( Pixels, *Directx9Context );
		return 0;
	}
#endif

	throw Soy::AssertException("Missing graphics device");
}



__export int ReadPixelFromTexture2D(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,Unity::Texture2DPixelFormat::Type PixelFormat)
{
	try
	{
		SoyPixelsMeta Meta( WidthHeightChannels[0], WidthHeightChannels[1], Unity::GetPixelFormat( PixelFormat ) );
		ReadPixelFromTexture( TexturePtr, PixelData, PixelDataSize, WidthHeightChannels, Meta );
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

__export int ReadPixelFromRenderTexture(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,Unity::RenderTexturePixelFormat::Type PixelFormat)
{
	try
	{
		SoyPixelsMeta Meta( WidthHeightChannels[0], WidthHeightChannels[1], Unity::GetPixelFormat( PixelFormat ) );
		ReadPixelFromTexture( TexturePtr, PixelData, PixelDataSize, WidthHeightChannels, Meta );
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


