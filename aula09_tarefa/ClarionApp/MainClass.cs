using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ClarionApp;
using ClarionApp.Model;
using ClarionApp.Exceptions;
using Gtk;

namespace ClarionApp
{
    class MainClass
    {
        #region properties
        private WSProxy ws = null;
        private ClarionAgent agent;
        String creatureId = String.Empty;
        String creatureName = String.Empty;
        Random random = new Random();
        private bool keepSpawningJewels = true;
        #endregion

        #region constructor
        public MainClass()
        {
            Application.Init();
            Console.WriteLine("ClarionApp V0.8");
            try
            {
                ws = new WSProxy("localhost", 4011);

                String message = ws.Connect();

                if (ws != null && ws.IsConnected)
                {
                    Console.Out.WriteLine("[SUCCESS] " + message + "\n");
                    ws.SendWorldReset();
                    ws.NewCreature(400, 200, 0, out creatureId, out creatureName);
                    ws.SendCreateLeaflet();

                    // Cria paredes
                    ws.NewBrick(4, 747, 2, 800, 567);
                    ws.NewBrick(4, 50, -4, 747, 47);
                    ws.NewBrick(4, 49, 562, 796, 599);
                    ws.NewBrick(4, -2, 6, 50, 599);

                    // Inicia thread para spawn contínuo de joias
                    Thread jewelSpawner = new Thread(new ThreadStart(SpawnJewelsContinuously));
                    jewelSpawner.Start();

                    if (!String.IsNullOrWhiteSpace(creatureId))
                    {
                        ws.SendStartCamera(creatureId);
                        ws.SendStartCreature(creatureId);
                    }

                    Console.Out.WriteLine("Creature created with name: " + creatureId + "\n");
                    agent = new ClarionAgent(ws, creatureId, creatureName);
                    agent.Run();
                    Console.Out.WriteLine("Running Simulation ...\n");
                }
                else
                {
                    Console.Out.WriteLine("The WorldServer3D engine was not found! You must start WorldServer3D before running this application!");
                    System.Environment.Exit(1);
                }
            }
            catch (WorldServerInvalidArgument invalidArtgument)
            {
                Console.Out.WriteLine(String.Format("[ERROR] Invalid Argument: {0}\n", invalidArtgument.Message));
            }
            catch (WorldServerConnectionError serverError)
            {
                Console.Out.WriteLine(String.Format("[ERROR] Is is not possible to connect to server: {0}\n", serverError.Message));
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(String.Format("[ERROR] Unknown Error: {0}\n", ex.Message));
            }
            Application.Run();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Thread que cria joias continuamente.
        /// </summary>
        private void SpawnJewelsContinuously()
        {
            // Limites do mapa (ajuste conforme seu cenário)
            int minX = 60;
            int maxX = 740;
            int minY = 60;
            int maxY = 540;

            while (keepSpawningJewels)
            {
                int x = random.Next(minX, maxX);
                int y = random.Next(minY, maxY);
                int jewelType = random.Next(1, 5);

                try
                {
                    ws.NewJewel(jewelType, x, y);
                    Console.Out.WriteLine($"[SPAWN] Jewel type {jewelType} created at ({x},{y})");
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine($"[ERROR] Failed to create jewel at ({x},{y}): {e.Message}");
                }

                // Espera antes de criar a próxima (ex.: 5 segundos)
                Thread.Sleep(5000);
            }
        }

        public static void Main(string[] args)
        {
            new MainClass();
        }

        #endregion
    }
}
