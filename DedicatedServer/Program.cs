using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using DedicatedServer.Madness;
using DedicatedServer.Madness.DB;
using DedicatedServer.Madness.Helpers;
using DedicatedServer.Madness.Mail;
using DedicatedServer.Madness.MessagePack;
using DedicatedServer.Madness.Packets;
using ENet;
using Packet = ENet.Packet;
using RSA = DedicatedServer.Madness.Cryptography.RSA;

namespace DedicatedServer
{
    internal class Program
    {
        public static RandomNumberGenerator number;
        public static Logging log;
        
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig) {
            log.Dispose();
            return true;
        }

        public static List<Player> Players;


        public static void DeletePlayer(Peer p)
        {
            Players.RemoveAll(pl => pl.peer.IP == p.IP && pl.peer.ID == p.ID);
        }
        
        public static Player FindPlayer(Peer p)
        {
            return Players.FirstOrDefault(pl => pl.peer.IP == p.IP && pl.peer.ID == p.ID);
        }
        
        public static void Main(string[] args)
        {
            number = RandomNumberGenerator.Create();

            log = new Logging();

            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);

            log.Info("MadnessTopDown Dedicated Server starting...");

            Players = new List<Player>();
            
            Cereal.InitalizeReflectivePacketTypes();
            
            log.Info("Done did the reflective types!");
            
            RSA.Init();
            
            log.Info("Initialized RSA!");
            
            ENet.Library.Initialize();
            using (Host server = new Host()) {
                Address address = new Address();

                address.Port = 5242;
                server.Create(address, 3000);

                Event netEvent;
                log.Info("Started!");
                while (!Console.KeyAvailable) {
                    bool polled = false;
                    
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
                                log.Info("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                                break;

                            case EventType.Disconnect:
                                log.Info("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                                
                                DeletePlayer(netEvent.Peer);
                                break;

                            case EventType.Timeout:
                                log.Info("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                                
                                DeletePlayer(netEvent.Peer);
                                break;

                            case EventType.Receive:
                                log.Info("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                                
                                // Check if a player is already there

                                Player found = FindPlayer(netEvent.Peer);

                                if (found == null)
                                    NewConnection(netEvent.Peer, netEvent.Packet);
                                else
                                {
                                    if (found.HandleRateLimit())
                                        HandlePacket(netEvent.Peer, netEvent.Packet);
                                    else
                                    {
                                        if (found.peer.State == PeerState.Disconnecting ||
                                            found.peer.State == PeerState.Disconnected)
                                            DeletePlayer(found.peer);
                                    }
                                }

                                netEvent.Packet.Dispose();
                                break;
                        }
                    }
                }

                server.Flush();
            }
            log.Dispose();
            ENet.Library.Deinitialize();
        }

        public static void HandlePacket(Peer p, Packet packet)
        {
            
        }

        public static void SendPacket(Player pl, Madness.Packets.Packet pa)
        {
            byte[] enc = PacketHelper.JanniePacket(pa, pl.current_aes);

            ENet.Packet packet = default(ENet.Packet);
            packet.Create(enc);
            pl.peer.Send(0, ref packet);
        }

        
        public static void NewConnection(Peer p, Packet helloPacket)
        {
            byte[] hello = new byte[helloPacket.Length];
            
            helloPacket.CopyTo(hello);
            
            try
            {
                CPacketHello h = Decreal.DeserializePacket<CPacketHello>(hello);

                byte[] aesKey = RSA.Decrypt(h.AESKey);

                Player yooo = new Player(p);
                yooo.current_aes = aesKey;
                Players.Add(yooo);
                
                
                
                log.Info("Peer " + p.IP + " has connected with a good packet.");
                
                // Send packet back to say we got it

                SPacketHello sh = new SPacketHello();

                SendPacket(yooo, sh);
            }
            catch (Exception e)
            {
                log.Error("Peer " + p.IP + " has submitted a bad GreetSever packet. oops. " + e);
                p.DisconnectNow(0);
            }
        }
        
    }
}