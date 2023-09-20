using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DedicatedServer.Madness;
using DedicatedServer.Madness.Cryptography;
using DedicatedServer.Madness.DB;
using DedicatedServer.Madness.Helpers;
using DedicatedServer.Madness.Mail;
using DedicatedServer.Madness.MessagePack;
using DedicatedServer.Madness.Packets;
using DedicatedServer.Madness.Server;
using ENet;
using Packet = ENet.Packet;
using RSA = DedicatedServer.Madness.Cryptography.RSA;

namespace DedicatedServer {
    internal class Program {
        public static string SupportedClientVersion = "indev.05";
        public static string ServerVersion = "dev";
        public static Logging log;

        public static string currentLog = "";

        public static void Preclose()
        {
            foreach(Player p in PlayerHandle.Players)
                p.peer.DisconnectNow((uint)Status.ServerClosing);
        }
        
        public static void Close()
        {
            
            Library.Deinitialize();
            log.Dispose();
        }
        
        public static void Main(string[] args) {

            log = new Logging();

            log.Info("MadnessTopDown Dedicated Server starting...");

            try {
                Cereal.InitalizeReflectivePacketTypes();

                log.Info("Done did the reflective types!");

                RSA.Init();

                log.Info("Initialized RSA!");

                SMTP.InitBlacklist();

                log.Info("Initialized TempEmail Blocklist! Entries: " + SMTP.blacklistedEmails.Count);
            } catch (Exception e) {
                log.Fatal("Failed initialization! Exception: " + e + ". \n[QUIT!!!!] I am now quiting...");
                return;
            }
            Stopwatch check = new Stopwatch();

            bool running = true;

            Thread o = new Thread(() => {
                while (running) {
                    ConsoleKeyInfo e = Console.ReadKey();
                    switch (e.Key) {
                    case ConsoleKey.Backspace:
                        Console.WriteLine("Quitting...");
                        log.Info("Quitting...");
                        running = false;
                        break;
                    }
                }
            });
            o.Start();

            Library.Initialize();
            Host server = new Host();
            Address address = new Address();

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => {
                Preclose();
                if (server != null)
                {
                    server.Flush();
                    server.Dispose();
                }

                Close();
            };

            address.Port = 5242;
            server.Create(address, 3000);

            Event netEvent;
            log.Info("Started!");
            while (running) {
                bool polled = false;

                PlayerHandle.CheckTimersAndQueues(check);

                PacketHandle.HandlePackets();

                while (!polled) {
                    if (server.CheckEvents(out netEvent) <= 0) {
                        if (server.Service(15, out netEvent) <= 0)
                            break;

                        polled = true;
                    }

                    switch (netEvent.Type) {
                    case EventType.None:
                        break;

                    case EventType.Connect:
                        log.Info("Client connected - ID: " + netEvent.Peer.ID + ", IP: " +
                            netEvent.Peer.IP);
                        break;

                    case EventType.Disconnect:
                        log.Info("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " +
                            netEvent.Peer.IP);

                        PlayerHandle.DeletePlayer(netEvent.Peer);
                        break;

                    case EventType.Timeout:
                        log.Info("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);

                        PlayerHandle.DeletePlayer(netEvent.Peer);
                        break;

                    case EventType.Receive:
                        // Check if a player is already there

                        Peer peer = netEvent.Peer;
                        Player ? found = PlayerHandle.FindPlayer(peer);
                        byte[] data = new byte[netEvent.Packet.Length];
                        netEvent.Packet.CopyTo(data);
                        netEvent.Packet.Dispose();

                        if (found == null) {
                            var peer1 = peer;
                            var data1 = data;
                            Thread t = new Thread(() => {
                                try {
                                    PlayerHandle.NewConnection(peer1, data1);
                                } catch (Exception e) {
                                    log.Error("Error while handling NewConnection packet on " +
                                        peer1.IP + "(#" + peer1.ID + "). Disconnecting.");
                                    PacketHandle.QueueDisconnect(peer1, (uint) Status.BadRequest);
                                }
                            });
                            t.Start();
                        } else {
                            if (found.HandleRateLimit()) {

                                if (found.account != null)
                                    found.account.lastUsed.Restart();
                                var peer1 = peer;
                                var data1 = data;
                                var found1 = found;
                                Thread t = new Thread(() => {
                                    try {
                                        PlayerHandle.HandlePacket(found1, data1);
                                    } catch (Exception e) {
                                        log.Error("Error while handling packet on " + peer1.IP + "(#" +
                                            peer1.ID + "). Disconnecting.");
                                        PacketHandle.QueueDisconnect(peer1, (uint) Status.Forbidden);
                                    }
                                });
                                t.Start();
                            } else {
                                if (found.peer.State is PeerState.Disconnecting or PeerState.Disconnected)
                                    PlayerHandle.DeletePlayer(found.peer);
                            }
                        }

                        break;
                    }
                }
            }
            
            Preclose();
            server.Flush();
            server.Dispose();
            Close();
        }

    }
}