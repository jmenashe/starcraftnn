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
    void finishRound();
    void loadGenomes();
    void saveGenomes();
  private:
    int _frameCount;  
    int _maxEnemyUnits, _maxAllyUnits;
    bool _resetting;
};
