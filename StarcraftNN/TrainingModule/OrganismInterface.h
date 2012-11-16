#pragma once

#include <organism.h>
#include <BWAPI.h>
#include <math.h>
#include <stdarg.h>
#include "Macros.h"
#include "Utils.h"
#include "ScoredUnit.h"

using namespace NEAT;

class OrganismInterface {
public:
  OrganismInterface(void);
  ~OrganismInterface(void);
  Genome* createGenome();
  void sendInputs(Organism*);
  void applyOutputs();
  void updateUnits();
protected:
  BWAPI::Position getCentroid(std::vector<BWAPI::Unit*>);
  Organism* _organism;
  std::vector<BWAPI::Unit*> _allies, _enemies;
  std::vector<BWAPI::Unit*> _lastAttack;
  int UNIT_COUNT;
  enum Sensors {
    ENEMY1_DISTANCE,
    ENEMY1_ANGLE,
    ENEMY2_DISTANCE,
    ENEMY2_ANGLE,
    ENEMY3_DISTANCE,
    ENEMY3_ANGLE,
    SENSOR_COUNT
  };

  enum Effectors {
    ALLY1_ATTACK1,
    ALLY1_ATTACK2,
    ALLY1_ATTACK3,

    ALLY2_ATTACK1,
    ALLY2_ATTACK2,
    ALLY2_ATTACK3,

    ALLY3_ATTACK1,
    ALLY3_ATTACK2,
    ALLY3_ATTACK3,

    EFFECTOR_COUNT
  };
};
