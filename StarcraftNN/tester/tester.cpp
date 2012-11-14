// tester.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include <genome.h>
#include <population.h>

using namespace NEAT;

int _tmain(int argc, _TCHAR* argv[])
{
  Genome* g = new Genome(0, 5, 5, 1, 20, true, 0.5);
  Population* p = new Population(g, 20);
  int test;
  std::cin >> test;
	return 0;
}

