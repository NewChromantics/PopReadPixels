#pragma once

#include "PopUnity.hpp"
#include "TStringBuffer.hpp"


namespace PopUnity
{
	TStringBuffer&					GetDebugStrings();


	template<typename STRING>
	void			DebugLog(const STRING& String);
}


template<typename STRING>
inline void PopUnity::DebugLog(const STRING& String)
{
	//	pump out to the OS output too via soylib
	Platform::DebugPrint(String);
	
	auto& DebugStrings = GetDebugStrings();
	DebugStrings.Push( String );
}


