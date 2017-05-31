#include "PopDebug.hpp"



namespace PopUnity
{
	std::shared_ptr<TStringBuffer>	gDebugStrings;
};




TStringBuffer& PopUnity::GetDebugStrings()
{
	if ( !gDebugStrings )
	{
		gDebugStrings.reset( new TStringBuffer() );
	}
	return *gDebugStrings;
}

