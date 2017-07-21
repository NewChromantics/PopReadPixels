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

#include <SoyPool.h>

class TCache
{
public:
	TCache() :
		mTexturePtr	( nullptr ),
		mRevision	( 0 )
	{
	}
	
	bool			Used() const		{	return mTexturePtr != nullptr;	}
	void			Release()			{	mTexturePtr = nullptr;	}
	void			OnRead()			{	mRevision++;	}
	
public:
	void*			mTexturePtr;
	SoyPixelsMeta	mTextureMeta;

	uint32_t		mRevision;
	SoyPixels		mLastReadPixels;
	std::mutex		mLastReadPixelsLock;
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


	//	gr: probably need some proper cleanup for this
#if defined(ENABLE_DIRECTX)
	std::shared_ptr<TPool<Directx::TTexture>>	DirectxTexturePool;
#endif

#if defined(ENABLE_DIRECTX9)
	std::shared_ptr<TPool<Directx9::TTexture>>	Directx9TexturePool;
#endif
}





int ReadPixelFromTexture(void* TexturePtr,SoyPixelsImpl& Pixels,SoyPixelsMeta TextureMeta)
{
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
		Opengl::TTexture Texture( TexturePtr, TextureMeta, Type );
		Texture.Read( Pixels );
		return 0;
	}
#endif
	
#if defined(ENABLE_DIRECTX)
	if ( DirectxContext )
	{
		if ( PopReadPixels::DirectxTexturePool == nullptr )
			PopReadPixels::DirectxTexturePool.reset( new TPool<Directx::TTexture>() );

		Directx::TTexture Texture( static_cast<ID3D11Texture2D*>(TexturePtr) );
		Texture.Read( Pixels, *DirectxContext, *PopReadPixels::DirectxTexturePool );
		return 0;
	}
#endif

#if defined(ENABLE_DIRECTX9)
	if ( Directx9Context )
	{
		if ( PopReadPixels::Directx9TexturePool == nullptr )
			PopReadPixels::Directx9TexturePool.reset( new TPool<Directx9::TTexture>() );

		Directx9::TTexture Texture( static_cast<IDirect3DTexture9*>(TexturePtr) );
		Texture.Read( Pixels, *Directx9Context, *PopReadPixels::Directx9TexturePool );
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
		SoyPixelsRemote Pixels( PixelData, PixelDataSize, Meta ); 
		ReadPixelFromTexture( TexturePtr, Pixels, Meta );
		return 0;
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in " << __func__ << "; " << e.what();
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in " << __func__;
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
}

__export int ReadPixelFromRenderTexture(void* TexturePtr,uint8_t* PixelData,int PixelDataSize,int* WidthHeightChannels,Unity::RenderTexturePixelFormat::Type PixelFormat)
{
	try
	{
		SoyPixelsMeta Meta( WidthHeightChannels[0], WidthHeightChannels[1], Unity::GetPixelFormat( PixelFormat ) );
		SoyPixelsRemote Pixels( PixelData, PixelDataSize, Meta ); 
		ReadPixelFromTexture( TexturePtr, Pixels, Meta );
		return 0;
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in " << __func__ << "; " << e.what();
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in " << __func__ ;
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



int AllocCacheRenderTexture(void* TexturePtr,SoyPixelsMeta Meta,bool ReadAsFloat)
{
	int CacheIndex = -1;
	auto& Cache = PopReadPixels::AllocCache(CacheIndex);
	Cache.mTexturePtr = TexturePtr;
	Cache.mTextureMeta = Meta;
	Cache.mTextureMeta.DumbSetFormat( ReadAsFloat ? SoyPixelsFormat::GetFloatFormat(Meta.GetFormat()) : SoyPixelsFormat::GetByteFormat(Meta.GetFormat()) );
	return CacheIndex;
}

__export int AllocCacheRenderTexture(void* TexturePtr,int Width,int Height,Unity::boolean ReadAsFloat,Unity::RenderTexturePixelFormat::Type PixelFormat)
{
	try
	{
		SoyPixelsMeta Meta( Width, Height, Unity::GetPixelFormat( PixelFormat ) );
		return AllocCacheRenderTexture( TexturePtr, Meta, ReadAsFloat );
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in " << __func__ << "; " << e.what();
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in " << __func__ << "";
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
}

__export int AllocCacheTexture2D(void* TexturePtr,int Width,int Height,Unity::boolean ReadAsFloat,Unity::Texture2DPixelFormat::Type PixelFormat)
{
	try
	{
		SoyPixelsMeta Meta( Width, Height, Unity::GetPixelFormat( PixelFormat ) );
		return AllocCacheRenderTexture( TexturePtr, Meta, ReadAsFloat );
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in " << __func__ << "; " << e.what();
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in " << __func__ << "";
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
}



__export void ReleaseCache(int Cache)
{
	try
	{
		PopReadPixels::ReleaseCache( Cache );
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in " << __func__ << "; " << e.what();
		PopUnity::DebugLog( Error.str() );
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in " << __func__ << "";
		PopUnity::DebugLog( Error.str() );
	}
}


__api(void) ReadPixelsFromCache(int CacheIndex)
{
	try
	{
		std::Debug << "ReadPixelsFromCache(" << CacheIndex << ")" << std::endl;
		auto& Cache = PopReadPixels::GetCache(CacheIndex);

		std::lock_guard<std::mutex> Lock( Cache.mLastReadPixelsLock );
		ReadPixelFromTexture( Cache.mTexturePtr, Cache.mLastReadPixels, Cache.mTextureMeta );
		Cache.OnRead();
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in " << __func__ << "; " << e.what();
		PopUnity::DebugLog( Error.str() );
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in " << __func__ << "";
		PopUnity::DebugLog( Error.str() );
	}
}

/*
__export int AllocCacheRenderTexture(void* TexturePtr,int Width,int Height,Unity::RenderTexturePixelFormat::Type PixelFormat)
{
	try
	{
		auto CacheIndex32 = AllocCacheRenderTexture( TexturePtr, Width, Height, PixelFormat );
		if ( CacheIndex32 < 0 )
			throw Soy::AssertException("Failed to alloc");
		return CacheIndex32;
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

*/
/*
__export int AllocCacheTexture2D(void* TexturePtr,uint8_t* PixelData,uint8_t* PixelRevision,uint8_t* CacheIndex,int PixelDataSize,int Width,int Height,int Channels,Unity::Texture2DPixelFormat::Type PixelFormat)
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
*/

__export UnityRenderingEvent GetReadPixelsFromCacheFunc()
{
	return ReadPixelsFromCache;
}

__export int ReadPixelBytesFromCache(int CacheIndex,uint8_t* ByteData,int ByteDataSize)
{
	try
	{
		auto& Cache = PopReadPixels::GetCache( CacheIndex );
		std::lock_guard<std::mutex> Lock( Cache.mLastReadPixelsLock );
		if ( !Cache.mLastReadPixels.IsValid() )
			return -1;
		auto ByteDataArray = GetRemoteArray( ByteData, static_cast<size_t>(ByteDataSize) );
		ByteDataArray.Copy( Cache.mLastReadPixels.GetPixelsArray() );
		return Cache.mRevision;
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in " << __func__ << " " << e.what();
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in " << __func__;
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
}


__export int ReadPixelFloatsFromCache(int CacheIndex,Unity::Float* ByteData,int ByteDataSize)
{
	try
	{
		auto& Cache = PopReadPixels::GetCache( CacheIndex );
		std::lock_guard<std::mutex> Lock( Cache.mLastReadPixelsLock );
		if ( !Cache.mLastReadPixels.IsValid() )
			return -1;
		auto ByteDataArray = GetRemoteArray( ByteData, static_cast<size_t>(ByteDataSize) );
		const auto& SourceByteArray = Cache.mLastReadPixels.GetPixelsArray();
		
		//	cast to float. Remember to check alignment etc
		auto SourceFloatArray = GetArrayBridge(SourceByteArray).GetSubArray<float>( 0, SourceByteArray.GetSize() / 4 );
		ByteDataArray.Copy( SourceFloatArray );
		
		return Cache.mRevision;
	}
	catch(const std::exception& e)
	{
		std::stringstream Error;
		Error << "Exception in " << __func__ << " " << e.what();
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
	catch(...)
	{
		std::stringstream Error;
		Error << "Unknown exception in " << __func__;
		PopUnity::DebugLog( Error.str() );
		return -1;
	}
}



