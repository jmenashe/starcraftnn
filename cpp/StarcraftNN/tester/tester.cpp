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
  Population* p = new Population(g, 5);
  for(int i = 0; i < 5; i++){
    Organism* o = p->organisms[i];
    o->fitness = (double)i;
    std::cout << "fitness: " << o->fitness << "\n";
  }
  for(int i = 0; i < p->species.size(); i++) {
    double a = p->organisms[i]->fitness;
    p->species[0]->compute_max_fitness();
    p->species[0]->compute_average_fitness();
  }
  p->verify();
  p->epoch(2);
	return 0;
}

