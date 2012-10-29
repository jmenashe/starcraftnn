#pragma once
#include <BWAPI.h>

#include <BWTA.h>
#include <windows.h>
#include <genome.h>

class ExampleAIModule : public BWAPI::AIModule
{
public:
  virtual void onStart();
  virtual void onFrame();
};
