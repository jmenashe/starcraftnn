#include "TrainingModule.h"

using namespace BWAPI;
#define RESET_LOCATION_COUNT 8

void TrainingModule::onStart() {
  Broodwar->setLocalSpeed(0);
  Broodwar->printf("The map is %s, a %d player map",Broodwar->mapName().c_str(),Broodwar->getStartLocations().size());

  if (!Broodwar->isReplay()) {
    Broodwar->printf("The match up is %s v %s",
      Broodwar->self()->getRace().getName().c_str(),
      Broodwar->enemy()->getRace().getName().c_str());
  }
  _frameCount = 0;
  _maxAllyUnits = getAllies().size();
  _maxEnemyUnits = getEnemies().size();
  _resetting = false;
  _resetLocation = 0;
  setupResetCenter();
  _population = new BroodwarPopulation(new OrganismInterface());
  beginRound();
}

void TrainingModule::setupResetCenter() {
  for(std::set<Unit*>::const_iterator i=Broodwar->self()->getUnits().begin();i!=Broodwar->self()->getUnits().end();i++) {
    Unit* unit = *i;
    if(unit->getType().getName() == RESET_UNIT_NAME) {
      _resetCenter = unit->getPosition();
    }
  }
}

void TrainingModule::onFrame() {
  _frameCount++;
  if(_resetting && !_completionSignaled) {
    signalCompletion();
  }
  else if (_resetting && ((_completionFrame - _frameCount) % 500) == 0) {
    signalCompletion();
  }
  else if (_population) {
    _population->performStep();
  }
}

void TrainingModule::onUnitCreate(BWAPI::Unit* unit) {
  if(_completionSignaled && _resetting && getAllies().size() == _maxAllyUnits) {
    beginRound();
  }
}

void TrainingModule::onUnitDestroy(BWAPI::Unit* unit) {
  if(_resetting) return;
  int survivors = getAllies().size();
  int opponents = getEnemies().size();
  if(survivors == 0 || opponents == 0) {
    endRound();
  }
}

void TrainingModule::beginRound() {
  _resetting = false;
  _population->beginIteration();
}

void TrainingModule::endRound() {
  _resetting = true;
  _completionSignaled = false;
  int survivors = getAllies().size();
  int opponents = getEnemies().size();
  int frames = _frameCount;
  _frameCount = 0;
  double score = survivors - opponents;
  score *= 200.0 / frames;
  Broodwar->printf("fitness: %2.2f", score);
  _population->endIteration(score);
}

void TrainingModule::signalCompletion() {
  for(std::set<Unit*>::const_iterator i=Broodwar->self()->getUnits().begin();i!=Broodwar->self()->getUnits().end();i++) {
    Unit* unit = *i;
    if(unit->getType().getName() == RESET_UNIT_NAME) {
      int dx, dy;
      int dist = 125;
      switch(_resetLocation % RESET_LOCATION_COUNT) {
        case 0: dx = dist; dy = dist; break;
        case 1: dx = -dist; dy = dist; break;
        case 2: dx = dist; dy = -dist; break;
        case 3: dx = -dist; dy = -dist; break;
        case 4: dx = 0; dy = dist; break;
        case 5: dx = 0; dy = -dist; break;
        case 6: dx = dist; dy = 0; break;
        case 7: dx = -dist; dy = 0; break;
      }
      BWAPI::Position target(_resetCenter.x() + dx, _resetCenter.y() + dy);
      unit->move(target);
    }
  }
  _completionSignaled = true;
  _completionFrame = _frameCount;
  _resetLocation++;
}

void TrainingModule::onEnd(bool isWinner) {
  _population->save();
}