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

class TCache
{
public:
	TCache() :
		mTexturePtr	( nullptr )
	{
	}
	
	bool			Used() const		{	return mTexturePtr != nullptr;	}
	void			Release()			{	mTexturePtr = nullptr;	}
	void			OnRead()			{	(*mPixelRevision)++;	}
	
public:
	void*			mTexturePtr;
	uint8_t*		mPixelData;			//	c#
	uint8_t*		mPixelRevision;		//	c#
	uint32_t		mPixelDataSize;
	SoyPixelsMeta	mTextureMeta;
};


namespace PopReadPixels
{
	//	gr: could be big as it's just sitting in memory, but made small so we
	//	can ensure client is releasing in case in future we NEED releasing
#define MAX_CACHES	200
	TCache		gCaches[MAX_CACHES];
	
	TCache&		AllocCache(int& CacheIndex);
	TCache&		GetCache(int CacheIndex);
	void		ReleaseCache(uint32_t CacheIndex);
}





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




TCache& PopReadPixels::AllocCache(int& CacheIndex)
{
	for ( int i=0;	i<MAX_CACHES;	i++ )
	{
		auto& Cache = gCaches[i];
		if ( Cache.Used() )
			continue;
		
		CacheIndex = i;
		return Cache;
	}
	
	throw Soy::AssertException("No free caches");
}

TCache& PopReadPixels::GetCache(int CacheIndex)
{
	if ( CacheIndex < 0 || CacheIndex >= MAX_CACHES )
	{
		throw Soy::AssertException("Invalid Cache Index");
	}
	
	auto& Cache = gCaches[CacheIndex];
	if ( !Cache.Used() )
	{
		throw Soy::AssertException("Cache not allocated");
	}
	
	return Cache;
}


void PopReadPixels::ReleaseCache(uint32_t CacheIndex)
{
	if ( CacheIndex >= MAX_CACHES )
	{
		throw Soy::AssertException("Invalid Cache Index");
	}
	
	gCaches[CacheIndex].Release();
}


int AllocCacheRenderTexture(void* TexturePtr,uint8_t* PixelData,uint8_t* PixelRevision,int PixelDataSize,SoyPixelsMeta Meta)
{
	int CacheIndex = -1;
	auto& Cache = PopReadPixels::AllocCache(CacheIndex);
	Cache.mTexturePtr = TexturePtr;
	Cache.mPixelData = PixelData;
	Cache.mPixelRevision = PixelRevision;
	Cache.mPixelDataSize = PixelDataSize;
	Cache.mTextureMeta = Meta;
	return CacheIndex;
}

int AllocCacheRenderTexture(void* TexturePtr,uint8_t* PixelData,uint8_t* PixelRevision,int PixelDataSize,int Width,int Height,int Channels,Unity::RenderTexturePixelFormat::Type PixelFormat)
{
	try
	{
		SoyPixelsMeta Meta( Width, Height, Unity::GetPixelFormat( PixelFormat ) );
		return AllocCacheRenderTexture( TexturePtr, PixelData, PixelRevision, PixelDataSize, Meta );
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

int AllocCacheTexture2D(void* TexturePtr,uint8_t* PixelData,uint8_t* PixelRevision,int PixelDataSize,int Width,int Height,int Channels,Unity::Texture2DPixelFormat::Type PixelFormat)
{
	try
	{
		SoyPixelsMeta Meta( Width, Height, Unity::GetPixelFormat( PixelFormat ) );
		return AllocCacheRenderTexture( TexturePtr, PixelData, PixelRevision, PixelDataSize, Meta );
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



__export void ReleaseCache(uint8_t Cache)
{
	try
	{
		PopReadPixels::ReleaseCache( Cache );
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in EnumStrings; " << e.what();
		PopUnity::DebugLog( Error.str() );
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in EnumStrings";
		PopUnity::DebugLog( Error.str() );
	}
}

__export UNITY_INTERFACE_API void ReadPixelsFromCache(int CacheIndex)
{
	try
	{
		auto& Cache = PopReadPixels::GetCache(CacheIndex);
		int WidthHeightChannels[3];
		ReadPixelFromTexture( Cache.mTexturePtr, Cache.mPixelData, Cache.mPixelDataSize, WidthHeightChannels, Cache.mTextureMeta );
		Cache.OnRead();
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in EnumStrings; " << e.what();
		PopUnity::DebugLog( Error.str() );
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in EnumStrings";
		PopUnity::DebugLog( Error.str() );
	}
}


__export UnityRenderingEvent AllocCacheRenderTexture(void* TexturePtr,uint8_t* PixelData,uint8_t* PixelRevision,uint8_t* CacheIndex,int PixelDataSize,int Width,int Height,int Channels,Unity::RenderTexturePixelFormat::Type PixelFormat)
{
	try
	{
		auto CacheIndex32 = AllocCacheRenderTexture( TexturePtr, PixelData, PixelRevision, PixelDataSize, Width, Height, Channels, PixelFormat );
		if ( CacheIndex32 < 0 )
			throw Soy::AssertException("Failed to alloc");
		uint8_t CacheIndex8 = CacheIndex32;
		*CacheIndex = CacheIndex8;
		return ReadPixelsFromCache;
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


__export UnityRenderingEvent AllocCacheTexture2D(void* TexturePtr,uint8_t* PixelData,uint8_t* PixelRevision,uint8_t* CacheIndex,int PixelDataSize,int Width,int Height,int Channels,Unity::Texture2DPixelFormat::Type PixelFormat)
{
	try
	{
		auto CacheIndex32 = AllocCacheTexture2D( TexturePtr, PixelData, PixelRevision, PixelDataSize, Width, Height, Channels, PixelFormat );
		if ( CacheIndex32 < 0 )
			throw Soy::AssertException("Failed to alloc");
		uint8_t CacheIndex8 = CacheIndex32;
		*CacheIndex = CacheIndex8;
		return ReadPixelsFromCache;
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

