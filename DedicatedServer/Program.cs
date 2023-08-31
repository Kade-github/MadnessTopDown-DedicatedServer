using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DedicatedServer.Madness;
using DedicatedServer.Madness.Cryptography;
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

        public static List<Account> tempAccounts = new();
        
        public static List<Player> Players = new();


        public static void DeletePlayer(Peer p)
        {
            lock (Players)
            {
                Players.RemoveAll(pl => pl.peer.IP == p.IP && pl.peer.ID == p.ID);
            }
        }
        
        public static Player FindPlayer(Peer p)
        {
            Player pl = null;
            lock (Players)
            {
                pl = Players.FirstOrDefault(pl => pl.peer.IP == p.IP && pl.peer.ID == p.ID);
            }

            return pl;
        }
        
        public static void Main(string[] args)
        {
            number = RandomNumberGenerator.Create();

            log = new Logging();

            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);

            log.Info("MadnessTopDown Dedicated Server starting...");

            try
            {
                Cereal.InitalizeReflectivePacketTypes();
                
                log.Info("Done did the reflective types!");
                
                RSA.Init();
                
                log.Info("Initialized RSA!");
                
                SMTP.InitBlacklist();
                
                log.Info("Initialized TempEmail Blocklist! Entries: " + SMTP.blacklistedEmails.Count);
            }
            catch (Exception e)
            {
                log.Fatal("Failed initialization! Exception: " + e + ". \n[QUIT!!!!] I am now quiting...");
                return;
            }
            Stopwatch check = new Stopwatch();
            
            ENet.Library.Initialize();
            using (Host server = new Host()) {
                Address address = new Address();

                address.Port = 5242;
                server.Create(address, 3000);

                Event netEvent;
                log.Info("Started!");
                while (true) {
                    bool polled = false;
                    
                    if (!check.IsRunning)
                        check.Start();

                    if (check.Elapsed.Minutes >= 1) // check if temp accounts are being used
                    {
                        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                        List<Account> toRemove = new();

                        foreach (Account a in tempAccounts)
                        {
                            if (a.EmailConfirmed) // remove early so we dont care, and have to loop through less.
                            {
                                toRemove.Add(a);
                                continue;
                            }
                            if (currentTimestamp - a.CreationDate > 1800) // 30 minutes
                            {
                                toRemove.Add(a);
                            }
                        }

                        tempAccounts.RemoveAll(m => toRemove.FirstOrDefault(mm => mm.Username == m.Username) != null); // linq :)
                        check.Restart();
                    }
                    
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
                                // Check if a player is already there

                                Player found = FindPlayer(netEvent.Peer);

                                if (found == null)
                                {
                                    try
                                    {
                                        NewConnection(netEvent.Peer, netEvent.Packet);
                                    }
                                    catch (Exception e)
                                    {
                                        log.Error("Error while handling NewConnection packet on " + netEvent.Peer.IP + "(#" + netEvent.Peer.ID + "). " + e);
                                        netEvent.Peer.DisconnectNow((uint)Status.BadRequest);
                                    } 
                                }
                                else
                                {
                                    if (found.HandleRateLimit())
                                    {
                                        try
                                        {
                                            HandlePacket(found, netEvent.Packet);
                                        }
                                        catch (Exception e)
                                        {
                                            log.Error("Error while handling packet on " + found.peer.IP + "(#" + found.peer.ID + "). " + e);
                                            netEvent.Peer.DisconnectNow((uint)Status.Forbidden);
                                        }
                                    }
                                    else
                                    {
                                        if (found.peer.State is PeerState.Disconnecting or PeerState.Disconnected)
                                            DeletePlayer(found.peer);
                                    }
                                }
                                
                                break;
                        }
                    }
                }

                server.Flush();
            }
            log.Dispose();
            ENet.Library.Deinitialize();
        }

        public static void HandlePacket(Player p, Packet packet)
        {
            byte[] enc = new byte[packet.Length];
            packet.CopyTo(enc);
            
            packet.Dispose();

            byte[] dec = AES.DecryptAESPacket(enc, p.current_aes);

            Madness.Packets.Packet pa = Decreal.DeserializePacket(dec);
            
            pa.Handle(p);
        }

        public static void SendPacket(Player pl, Madness.Packets.Packet pa)
        {
            byte[] enc = PacketHelper.JanniePacket(pa, pl.current_aes);

            ENet.Packet packet = default(ENet.Packet);
            packet.Create(enc);
            pl.peer.Send(0, ref packet);
        }
        public static void SendPacket(Peer p, Madness.Packets.Packet pa)
        {
            byte[] enc = PacketHelper.JanniePacket(pa, null);

            ENet.Packet packet = default(ENet.Packet);
            packet.Create(enc);
            p.Send(0, ref packet);
        }

        
        public static void NewConnection(Peer p, Packet helloPacket)
        {
            byte[] hello = new byte[helloPacket.Length];
            
            helloPacket.CopyTo(hello);
            helloPacket.Dispose();
            
            try
            {
                CPacketHello h = Decreal.DeserializePacket<CPacketHello>(hello);

                byte[] aesKey = RSA.Decrypt(h.AESKey);

                Player yooo = new Player(p);
                yooo.current_aes = aesKey;

                lock (Players)
                {
                    Players.Add(yooo);
                }
                
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