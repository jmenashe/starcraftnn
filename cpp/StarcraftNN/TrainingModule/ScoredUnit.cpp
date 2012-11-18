#include "ScoredUnit.h"

ScoredUnit::ScoredUnit(BWAPI::Unit* unit, double score) {
  this->score = score;
  this->unit = unit;
}

bool ScoredUnit::compare(const ScoredUnit& i, const ScoredUnit& j) {
  return i.score > j.score;
}