#include <iostream>

#include "../MicronixMMCLib.h"
using namespace std;

int main()
{
	std::cout << "Hello World" << std::endl;

	// Move targets
	string axis = "1";
	string abs_move = "MVA";
	string distance = "-5";

	// Connect to port
	auto s = openComPortMMC("COM4");

	// Poll commands
	auto s2 = pollValues("1", "VER");
	auto s3 = pollValues("1", "POS");

	// Execute poll commands
	std::cout << std::endl << s2;
	std::cout << s3;

	// Move command
	string command = abs_move + distance;
	writeCommandToMMC(axis, command);

	closeComPortMMC();
}