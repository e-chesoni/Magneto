#include <iostream>
#include "MicronixMMCLib.h"
int main()
{
	// Please refer to the manual for to find the MMC Device COM Port
	// It will not be always COM6
	auto s = openComPortMMC("COM6");

	// WriteCommand return only bool values even if a query was sent
	// T get the response use the pollValues function as below
	auto s2 = writeCommandToMMC("1", "sta?");

	auto s23 = pollValues("1", "VER");
	auto s223 = pollValues("1", "pos");

	std::cout << std::endl << s23;
	std::cout << s223;

	closeComPortMMC();
}

