﻿using System;
using System.Threading;

namespace TurtlesBrain
{
    public class Program
    {
        public static TurtleServer server;
        public static WebSocketTurtleServer webserver;

        static void Main(string[] args)
        {
            
            Thread TurtleServerThread = new Thread(o => { server = new TurtleServer(); });
            TurtleServerThread.Name = "ServerThread";
            TurtleServerThread.Start();

            Console.WriteLine("Success");
            Thread WebsocketTurtleThread = new Thread(o => { webserver = new WebSocketTurtleServer(); });
            WebsocketTurtleThread.Name = "WebsocketThread";
            WebsocketTurtleThread.Start();
            while(true)
            {
                Console.Write(">");
                string command = Console.ReadLine();
                if (command == "exit")
                    break;
                else if (command == "script")
                {
                    Thread thread = new Thread(sdothis);
                    thread.Name = "ScriptThread";
                    thread.Start();
                }
            }
        }

        private static void sdothis()
        {
            string[] str = new string[5] { "tester", "suschstinkt", "myTurtle", "rave1000_1", "first" };
            WriteText s = new WriteText(str,"suschstinkt");
        }
    }
}