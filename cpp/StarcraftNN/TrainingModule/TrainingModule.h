#pragma once
#include <BWAPI.h>

#include <BWTA.h>
#include <windows.h>
#include <genome.h>

#include "BasicInterface.h"
#include "BroodwarPopulation.h"
#include "Macros.h"
#include "Utils.h"

class TrainingModule : public BWAPI::AIModule {
  public:
    TrainingModule() : BWAPI::AIModule(), _population(0) {}
    virtual void onStart();
    virtual void onFrame();
    virtual void onUnitDestroy(BWAPI::Unit*);
    virtual void onUnitCreate(BWAPI::Unit*);
    virtual void onEnd(bool);
    void beginRound();
    void endRound();
    void signalCompletion();
    void setupResetCenter();
  private:
    int _frameCount;  
    int _maxEnemyUnits, _maxAllyUnits;
    bool _resetting, _completionSignaled;
    int _resetLocation, _completionFrame;
    BWAPI::Position _resetCenter;
    BroodwarPopulation* _population;
};
