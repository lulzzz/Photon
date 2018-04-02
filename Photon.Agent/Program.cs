﻿using log4net;
using log4net.Config;
using Photon.Agent.Internal;
using System;
using System.ServiceProcess;

namespace Photon.Agent
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));


        public static int Main(string[] args)
        {
            try {
                XmlConfigurator.Configure();

                var program = new Program();

                // TODO: Load Configuration Data

                return program.Run(args);
            }
            catch (Exception error) {
                Log.Fatal("Unhandled Exception!", error);
                return -1;
            }
            finally {
                PhotonAgent.Instance?.Dispose();
                LogManager.Flush(3000);
            }
        }

        private int Run(string[] args)
        {
            var arguments = new Arguments();

            try {
                arguments.Parse(args);
            }
            catch (Exception error) {
                Log.Fatal("Failed to parse arguments!", error);
                return 1;
            }

            if (arguments.RunAsConsole) {
                RunAsConsole(args);
            }
            else {
                ServiceBase.Run(new [] {
                    new AgentService(),
                });
            }

            return 0;
        }

        private void RunAsConsole(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Photon Agent");
            Console.ResetColor();

            PhotonAgent.Instance.Start();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Agent Started");
            Console.ResetColor();
            Console.ReadKey(true);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Agent Stopping...");
            Console.ResetColor();

            PhotonAgent.Instance.Stop();
            Console.WriteLine();
        }
    }
}