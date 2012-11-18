using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using SWIG.BWAPIC;
using StarcraftNN.OrganismInterfaces;

namespace StarcraftNN
{
    class Program
    {
        static void reconnect()
        {

            while (!bwapiclient.BWAPIClient.connect())
            {
                System.Threading.Thread.Sleep(1000);
            }

        }

        /// <summary>
        /// Create/destroy events are sent a few frames before they take effect, so we advance the frames to get an accurate game state.
        /// </summary>
        private static void advanceFrames()
        {
            for (int i = 0; i < 3; i++)
                bwapiclient.BWAPIClient.update();
        }

        static void Main(string[] args)
        {
            BroodwarPopulation population = new BroodwarPopulation(new BasicInterface());
            bwapi.BWAPI_init();
            System.Console.WriteLine("Connecting...");
            reconnect();
            while (true)
            {
                System.Console.WriteLine("waiting to enter match\n");
                while (!bwapi.Broodwar.isInGame())
                {
                    bwapiclient.BWAPIClient.update();
                    if (!bwapiclient.BWAPIClient.isConnected())
                    {
                        System.Console.WriteLine("Reconnecting...\n");
                        reconnect();
                    }
                }
                bwapi.Broodwar.setLocalSpeed(0);
                RoundManager manager = new RoundManager(population);
                while (bwapi.Broodwar.isInGame())
                {
                    bwapiclient.BWAPIClient.update();
                    manager.HandleFrame();
                    foreach (Event e in bwapi.Broodwar.getEvents().ToList())
                    {
                        switch(e.type)
                        {
                            case EventType_Enum.UnitCreate:
                                advanceFrames();
                                manager.HandleUnitCreate(e.unit);
                                break;
                            case EventType_Enum.UnitDestroy:
                                advanceFrames();
                                manager.HandleUnitDestroy(e.unit);
                                break;
                        }
                    }
                }
            }
        }
    }
}
