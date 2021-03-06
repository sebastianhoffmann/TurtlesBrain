﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
namespace TurtlesBrain
{
    public class Computer
    {
        public string Label { get; private set; }

        private Queue<KeyValuePair<string, TurtleServer.Result>> commands = new Queue<KeyValuePair<string, TurtleServer.Result>>();

        private KeyValuePair<string, TurtleServer.Result> currentCommand;
        private HttpListenerResponse currentResponse;

        public KeyValuePair<string, TurtleServer.Result> GetNextCommand()
        {
            if (commands.Count >= 2)
            {
                Console.WriteLine(commands.Count);
                return commands.Dequeue();
            }
            return commands.Dequeue();
        }

        public Computer(string label)
        {
            Label = label;
        }

        public void AddCommand(string command, TurtleServer.Result callback)
        {
            commands.Enqueue(new KeyValuePair<string, TurtleServer.Result>(command, callback));
        }

        public string Send(string command)
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);

            string response = null;

            AddCommand(command, (label, result) =>
            {
                response = result;
                waitHandle.Set();
            });
            waitHandle.WaitOne(1500);
            while(response == null)
            {
                Resend((label, result) =>
                {
                    response = result;
                    waitHandle.Set();
                });
            waitHandle.WaitOne(1500);
            }
            return response;
        }

        public string Send(string command, int timeout)
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);

            string response = null;

            AddCommand(command, (label, result) =>
            {
                response = result;
                waitHandle.Set();
            });
            waitHandle.WaitOne(timeout);
            return response;
        }

        public void QueryCommand(HttpListenerResponse response)
        {
            while (commands.Count == 0)
            {
                Thread.Sleep(10);
            }

            KeyValuePair<string, TurtleServer.Result> nextCommand = GetNextCommand();

            currentCommand = nextCommand;

            lock (Program.server.commandPoolOderSo)
            {
                Program.server.commandPoolOderSo.Add(Label, nextCommand);
            }

            currentResponse = response;

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(nextCommand.Key);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        public void Resend(TurtleServer.Result callback)
        {
            lock (Program.server.commandPoolOderSo)
            {
                try
                {
                    Program.server.commandPoolOderSo.Remove(Label);
                }
                finally
                {
                    Program.server.commandPoolOderSo.Add(Label,
                                       new KeyValuePair<string, TurtleServer.Result>(currentCommand.Key, callback));
                }
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(currentCommand.Key);
            currentResponse.ContentLength64 = buffer.Length;
            currentResponse.OutputStream.Write(buffer, 0, buffer.Length);
        }

        public bool GetBool(string theString)
        {
            return bool.Parse(theString.Split('|')[1]);
        }

        public byte GetByte(string theString)
        {
            return byte.Parse(theString.Split('|')[1]);
        }

        public int GetInt(string theString)
        {
            return int.Parse(theString.Split('|')[1]);
        }

        public string GetReason(string theString)
        {
            return theString.Split('|')[2];
        }

        public string[] GetArray(string theString, int skipAmount)
        {
            return theString.Split('|').Skip(skipAmount).ToArray();
        }
    }
}