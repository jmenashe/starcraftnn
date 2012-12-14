using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using SWIG.BWAPIC;
using StarcraftNN.OrganismInterfaces;
using StarcraftNN.OrganismInterfaces.Squads;
using System.Threading;

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
            IOrganismInterface iface;
            bool useGui = false;
            bool evolve = false;
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "mf":
                        iface = new MarineFirebat3v3();
                        break;
                    case "gws":
                        iface = new Goliath2Wraith2Squad();
                        break;
                    case "mfs":
                        iface = new Marine2Firebat1Squad();
                        break;
                    case "20v20":
                        iface = new HeteroIndividual20v20();
                        break;
                    case "mfgw":
                        iface = new MFGW_IG_SquadController();
                        break;
                    default:
                        iface = new MFGW_IG_SquadController();
                        break;
                }
                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++ )
                        switch (args[i])
                        {
                            case "gui": useGui = true; break;
                            case "evolve": evolve = true; break;
                        }
                }
            }
            else iface = new MFGW_IG_SquadController();
            Console.WriteLine("Evolution: {0}", evolve);
            Console.WriteLine("Gui: {0}", useGui);
            Console.WriteLine("Iface: {0}", iface.GetType().Name);
                

            bwapi.BWAPI_init();
            System.Console.WriteLine("Connecting...");
            reconnect();
            BroodwarPopulation population = new BroodwarPopulation(iface, evolve);
            while (true)
            {
                try
                {
                    loop(population, useGui);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                    Thread.Sleep(5000);
                    reconnect();
                }
            }
        }

        static void loop(BroodwarPopulation population, bool useGui) 
        {
            int speed = 20;
            while (!bwapi.Broodwar.isInGame())
            {
                bwapiclient.BWAPIClient.update();
                if (!bwapiclient.BWAPIClient.isConnected())
                {
                    System.Console.WriteLine("Reconnecting...\n");
                    reconnect();
                }
            }
            if (!useGui)
                bwapi.Broodwar.setGUI(false);
            bwapi.Broodwar.setLocalSpeed(speed);
            RoundManager manager = new RoundManager(population);
            while (bwapi.Broodwar.isInGame())
            {
                bwapiclient.BWAPIClient.update();
                if (bwapi.Broodwar == null) break;
                manager.HandleFrame();
                List<Event> events = bwapi.Broodwar.getEvents().ToList();
                if (events.Any(x => x.getType() == EventType_Enum.UnitDestroy))
                    advanceFrames();
                foreach (Event e in events)
                {
                    switch (e.getType())
                    {
                        case EventType_Enum.UnitCreate:
                            manager.HandleUnitCreate(e.getUnit());
                            break;
                        case EventType_Enum.UnitDestroy:
                            manager.HandleUnitDestroy(e.getUnit());
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
