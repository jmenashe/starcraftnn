using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using SWIG.BWAPIC;
using StarcraftNN.OrganismInterfaces;
using StarcraftNN.OrganismInterfaces.Squads;

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
            BroodwarPopulation population;
            bool useGui = false;
            if (args.Length > 0)
            {
                switch(args[0])
                {
                    case "gw":
                        population = new BroodwarPopulation(new Goliath2Wraith2Squad());
                        break;
                    case "mf":
                        population = new BroodwarPopulation(new Marine2Firebat1Squad());
                        break;
                    default:
                        population = new BroodwarPopulation(new Marine2Firebat1Squad());
                        break;
                }
                if (args.Length > 1)
                    switch (args[1])
                    {
                        case "gui": useGui = true; break;
                    }
            }
            else population = new BroodwarPopulation(new Marine2Firebat1Squad());
            
            population.EnableEvolution = true;
            bwapi.BWAPI_init();
            System.Console.WriteLine("Connecting...");
            reconnect();
            int speed = 0;
            while (true)
            {
                while (!bwapi.Broodwar.isInGame())
                {
                    bwapiclient.BWAPIClient.update();
                    if (!bwapiclient.BWAPIClient.isConnected())
                    {
                        System.Console.WriteLine("Reconnecting...\n");
                        reconnect();
                    }
                }
                if(!useGui)
                    bwapi.Broodwar.setGUI(false);
                bwapi.Broodwar.setLocalSpeed(speed);
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
                            case EventType_Enum.MatchEnd:
                                manager.HandleMatchEnd();
                                break;
                        }
                    }
                }
            }
        }
    }
}
