﻿using System;
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
                    case "mf":
                        population = new BroodwarPopulation(new MarineFirebat3v3());
                        break;
                    case "gws":
                        population = new BroodwarPopulation(new Goliath2Wraith2Squad());
                        break;
                    case "mfs":
                        population = new BroodwarPopulation(new Marine2Firebat1Squad());
                        break;
                    case "mfgw":
                        population = new BroodwarPopulation(new MFGW_IG_SquadController());
                        Console.WriteLine("Starting marine firebat goliath wraith squad controller");
                        break;
                    default:
                        population = new BroodwarPopulation(new MFGW_IG_SquadController());
                        break;
                }
                if (args.Length > 1)
                    switch (args[1])
                    {
                        case "gui": useGui = true; break;
                    }
            }
            else population = new BroodwarPopulation(new MarineFirebat3v3());
            
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
                    if (events.Any(x => x.getType() == EventType_Enum.UnitDestroy))
                        advanceFrames();
                    foreach (Event e in events)
                    {
                        switch(e.getType())
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
}
