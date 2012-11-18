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
            for (int i = 0; i < 10; i++)
                bwapiclient.BWAPIClient.update();
        }

        static void Main(string[] args)
        {
            BroodwarPopulation population = new BroodwarPopulation(new MarineFirebat12v12());
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
                bwapi.Broodwar.setLocalSpeed(10);
                RoundManager manager = new RoundManager(population);
                while (bwapi.Broodwar.isInGame())
                {
                    bwapiclient.BWAPIClient.update();
                    manager.HandleFrame();
                    List<Event> events = bwapi.Broodwar.getEvents().ToList();
                    if (events.Any(x => x.type == EventType_Enum.UnitDestroy))
                        advanceFrames();
                    foreach (Event e in events)
                    {
                        switch(e.type)
                        {
                            case EventType_Enum.UnitCreate:
                                manager.HandleUnitCreate(e.unit);
                                break;
                            case EventType_Enum.UnitDestroy:
                                manager.HandleUnitDestroy(e.unit);
                                break;
                        }
                    }
                }
            }
        }
    }
}
