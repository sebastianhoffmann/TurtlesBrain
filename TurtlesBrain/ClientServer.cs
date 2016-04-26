﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TurtlesBrain.Shared;

namespace TurtlesBrain
{
    public static class ClientServer
    {
        private static List<Client> clients = new List<Client>();
        private static Dictionary<string, Turtle> _turtleMap = new Dictionary<string, Turtle>();
        private static TcpListener _listener;

        public static void Start(int port)
        {

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            _listener.BeginAcceptTcpClient(OnClient, null);
        }
#pragma warning disable CS4014
        private static void OnClient(IAsyncResult ar)
        {
            Console.WriteLine("Client Connected");
            var client = _listener.EndAcceptTcpClient(ar);
            _listener.BeginAcceptTcpClient(OnClient, null);

            Setup(client);


        }

        public static void Execute(string label, string command)
        {
            Turtle t;
            if (_turtleMap.TryGetValue(label, out t))
            {
                t.Client.WriteAsync(new Response { Content = t.Send(command), Label = label });
            }

        }

#pragma warning restore CS4014

        private static async Task Setup(TcpClient client)
        {
            var stream = client.GetStream();
            using (var aes = new AesCryptoServiceProvider())
            using (var dh = new ECDiffieHellmanCng())
            {
                var pk = dh.PublicKey.ToByteArray();
                await stream.WriteAsync(pk, 0, pk.Length);
                if (await stream.ReadAsync(pk, 0, pk.Length) != pk.Length)
                {
                    client.Close();
                    return;
                }

                var key = dh.DeriveKeyMaterial(CngKey.Import(pk, CngKeyBlobFormat.EccPublicBlob));
                aes.Key = key;
                await stream.WriteAsync(aes.IV, 0, aes.IV.Length);


                var decryptor = aes.CreateDecryptor();

                var buffer = new byte[4096];
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);

                var buffer2 = new byte[4096];
                int total = 0;
                if (read <= decryptor.InputBlockSize)
                {
                    var final = decryptor.TransformFinalBlock(buffer, 0, read);
                    Buffer.BlockCopy(final, 0, buffer2, 0, final.Length);
                    total = final.Length;
                }
                else
                {
                    var diff = read % decryptor.InputBlockSize;

                    var offset = diff == 0 ? decryptor.InputBlockSize : diff;

                    var written = decryptor.TransformBlock(buffer, 0, read - offset, buffer2, 0);
                    var final = decryptor.TransformFinalBlock(buffer, read - offset, offset);
                    Buffer.BlockCopy(final, 0, buffer2, written, final.Length);
                    total = final.Length + written;
                }

                var split = Encoding.UTF8.GetString(buffer2, 0, total).Split('\n');

                var username = split[0];
                var password = split[1];

                // Do the auth.
                var info = new ClientInfo { Name = username };
                OnClientReady(new Client(info, stream));
            }
        }

        private static void OnClientReady(Client client)
        {
            Console.WriteLine("Client Setup Done");
            //client.WriteASync()
            clients.Add(client);
            Clients = new Dictionary<string, Client>(Clients) { { client.Username, client } };
            foreach (var t in Program.turtleserver.turtles.Where(kvp => kvp.Key.Contains(client.Username)).Select(kvp => kvp.Value))
            {
                AddTurtle(client,t);
            }
        }

        public static void AddTurtle(Turtle turtle)
        {
            var client = Clients.Values.FirstOrDefault(c => turtle.Label.Contains(c.Username));
            if (client != null)
                AddTurtle(client, turtle);
        }

        public static void AddTurtle(Client client, Turtle turtle)
        {
            if (_turtleMap.ContainsKey(turtle.Label))
                return;

            _turtleMap[turtle.Label] = turtle;
            turtle.Client = client;
            client.WriteAsync(new TurtleMessage { Label = turtle.Label }).Wait();
        }

        public static  Dictionary<string, Client> Clients = new Dictionary<string, Client>();
    }

    public class ClientInfo
    {
        public string Name { get; set; }
        public string McName { get; set; }
    }
}

