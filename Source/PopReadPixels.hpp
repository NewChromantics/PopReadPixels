#pragma once

#include "PopUnity.hpp"
#include <functional>


__export const char*		EnumWindowNames();


namespace PopWinCap
{
	void			EnumWindows(std::function<std::string&> OnEnumWindow);
}

