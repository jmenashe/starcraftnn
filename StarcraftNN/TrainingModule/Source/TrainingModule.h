#pragma once
#include <BWAPI.h>

#include <BWTA.h>
#include <windows.h>
#include <genome.h>

class TrainingModule : public BWAPI::AIModule {
  public:
    virtual void onStart();
    virtual void onFrame();
    virtual void onUnitDestroy(BWAPI::Unit*);
    virtual void onUnitCreate(BWAPI::Unit*);
    virtual void onEnd(bool);
    void beginRound();
    void endRound();
    void loadPopulation();
    void savePopulation();
  private:
    int _frameCount;  
    int _maxEnemyUnits, _maxAllyUnits;
    bool _resetting;
};
