#pragma once

#include <BWAPI.h>

class ScoredUnit {
public:
  ScoredUnit(BWAPI::Unit*,double);
  static bool compare(const ScoredUnit&, const ScoredUnit&);
  double score;
  BWAPI::Unit* unit;
};