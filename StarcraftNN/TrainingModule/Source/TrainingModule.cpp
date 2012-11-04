#include "TrainingModule.h"
using namespace BWAPI;

void TrainingModule::onStart() {
  NEAT::Genome* g = new NEAT::Genome(5, 5, 0, 0);
  Broodwar->setLocalSpeed(5);
  Broodwar->printf("The map is %s, a %d player map",Broodwar->mapName().c_str(),Broodwar->getStartLocations().size());

  if (!Broodwar->isReplay()) {
    Broodwar->printf("The match up is %s v %s",
      Broodwar->self()->getRace().getName().c_str(),
      Broodwar->enemy()->getRace().getName().c_str());
  }
  _frameCount = 0;
  _maxAllyUnits = Broodwar->self()->getUnits().size();
  _maxEnemyUnits = Broodwar->enemy()->getUnits().size();
  _resetting = false;
  loadPopulation();
}

void TrainingModule::onFrame() {
  _frameCount++;
}

void TrainingModule::onUnitCreate(BWAPI::Unit* unit) {
  if(_resetting && unit->getPlayer() == Broodwar->self() && Broodwar->self()->getUnits().size() == _maxAllyUnits) {
    _resetting = false;
    beginRound();
  }
}

void TrainingModule::onUnitDestroy(BWAPI::Unit* unit) {
  if(_resetting) return;
  if(unit->getType().isFlyer()) return;
  int survivors = Broodwar->self()->getUnits().size();
  int opponents = Broodwar->enemy()->getUnits().size();
  if(survivors == 0 || opponents == 0) {
    endRound();
  }
}

void TrainingModule::beginRound() {
  Position target(700,270);
  Broodwar->printf("round beginning");
  for(std::set<Unit*>::const_iterator i=Broodwar->self()->getUnits().begin();i!=Broodwar->self()->getUnits().end();i++) {
    Unit* unit = *i;
    if(unit->getType().isFlyer()) continue;
    unit->attack(target);
  }
}

void TrainingModule::endRound() {
  _resetting = true;
  int survivors = Broodwar->self()->getUnits().size();
  int opponents = Broodwar->enemy()->getUnits().size();
  int frames = _frameCount;
  _frameCount = 0;
  double score = survivors - opponents;
  score *= 200.0 / frames;
  Broodwar->printf("%i survivors, %i opponents, %i frames, score: %2.4f", survivors, opponents, frames, score);
}

void TrainingModule::onEnd(bool isWinner) {
  savePopulation();
}

void TrainingModule::loadPopulation() {
}

void TrainingModule::savePopulation() {
}