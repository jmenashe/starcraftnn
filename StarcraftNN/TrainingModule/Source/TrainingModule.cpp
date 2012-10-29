#include "TrainingModule.h"
using namespace BWAPI;

void ExampleAIModule::onStart()
{
  NEAT::Genome* g = new NEAT::Genome(5, 5, 0, 0);
  Broodwar->sendText("Hello world!");
  Broodwar->printf("The map is %s, a %d player map",Broodwar->mapName().c_str(),Broodwar->getStartLocations().size());

  if (!Broodwar->isReplay())
  {
    Broodwar->printf("The match up is %s v %s",
      Broodwar->self()->getRace().getName().c_str(),
      Broodwar->enemy()->getRace().getName().c_str());
  }
}

void ExampleAIModule::onFrame()
{

}