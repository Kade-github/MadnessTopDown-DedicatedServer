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
using ENet;
using Packet = ENet.Packet;
using RSA = DedicatedServer.Madness.Cryptography.RSA;

namespace DedicatedServer
{
    internal class Program
    {
        public static RandomNumberGenerator number;
        public static Logging log;
        
        public static List<Account> tempAccounts = new();
        public static List<Account?> cachedAccounts = new();
        
        public static List<Player> Players = new();

        public static Dictionary<Player, Madness.Packets.Packet> packetQueue = new();

        public static Dictionary<Peer, uint> queueDisconnect = new();

        public static string currentLog = "";

        public static int errors = 0;

        public static async Task SetUpdated(string username)
        {
            Account a =cachedAccounts.FirstOrDefault(u => u.Username == username);

            if (a == null)
                return;

            await a.Update(); // no await cuz lock()
        }
        
        public static async Task<Account?> GetAccount(string username)
        {
            Account? a;
            a = cachedAccounts.FirstOrDefault(u => String.Equals(u.Username, username, StringComparison.CurrentCultureIgnoreCase));
            if (a != null)
            {
                log.Debug("Returning cached " + a.Username);
                return a;
            }
            log.Debug(username + " isn't cached, doing sql call for it.");

            a = await Account.GetAccount(username); // MYSQL Call
            if (a == null)
                return null; // its null
            cachedAccounts.Add(a);
            log.Debug("Cached " + a.Username);

            return a;
        }
        
        public static void QueueDisconnect(Peer id, uint reason)
        {
            lock (queueDisconnect)
            {
                queueDisconnect[id] = reason;
            }
        }
        
        public static void QueuePacket(Player id, Madness.Packets.Packet pa)
        {
            lock (packetQueue)
            {
                packetQueue[id] = pa;
            }
        }
        

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

        /// <summary>
        /// Clean up tempAccounts, along with sending queued packets and disconnecting clients.
        /// </summary>
        /// <param name="check">Check stopwatch for temp accounts</param>
        public static void CheckTimersAndQueues(Stopwatch check)
        {
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
                    if (currentTimestamp - a.CreationDate > 7200) // 2 hours
                    {
                        toRemove.Add(a);
                    }
                }

                tempAccounts.RemoveAll(m => toRemove.FirstOrDefault(mm => mm.Username == m.Username) != null); // linq :)
                check.Restart();
            }

            lock (packetQueue)
            {
                foreach (var p in packetQueue)
                {
                    SendPacket(p.Key,p.Value);
                }
                packetQueue.Clear();
            }
                    
            lock (queueDisconnect)
            {
                foreach (var p in queueDisconnect)
                {
                    DeletePlayer(p.Key);
                    p.Key.DisconnectNow(p.Value);
                }
                queueDisconnect.Clear();
            }
        }
        
        public static void Main(string[] args)
        {   

            number = RandomNumberGenerator.Create();

            log = new Logging();

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                log.Dispose();
            };

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

            bool running = true;
            
            Thread o = new Thread(() =>
            {
                while (running)
                {
                    ConsoleKeyInfo e = Console.ReadKey();
                    switch (e.Key)
                    {
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
                using (Host server = new Host())
                {
                    Address address = new Address();

                    address.Port = 5242;
                    server.Create(address, 3000);

                    Event netEvent;
                    log.Info("Started!");
                    while (running)
                    {
                        bool polled = false;

                        CheckTimersAndQueues(check);

                        while (!polled)
                        {
                            if (server.CheckEvents(out netEvent) <= 0)
                            {
                                if (server.Service(15, out netEvent) <= 0)
                                    break;

                                polled = true;
                            }

                            switch (netEvent.Type)
                            {
                                case EventType.None:
                                    break;

                                case EventType.Connect:
                                    log.Info("Client connected - ID: " + netEvent.Peer.ID + ", IP: " +
                                             netEvent.Peer.IP);
                                    break;

                                case EventType.Disconnect:
                                    log.Info("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " +
                                             netEvent.Peer.IP);

                                    DeletePlayer(netEvent.Peer);
                                    break;

                                case EventType.Timeout:
                                    log.Info("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);

                                    DeletePlayer(netEvent.Peer);
                                    break;

                                case EventType.Receive:
                                    // Check if a player is already there

                                    Peer peer = netEvent.Peer;
                                    Player found = FindPlayer(peer);
                                    byte[] data = new byte[netEvent.Packet.Length];
                                    netEvent.Packet.CopyTo(data);
                                    netEvent.Packet.Dispose();

                                    if (found == null)
                                    {
                                        var peer1 = peer;
                                        var data1 = data;
                                        Thread t = new Thread(() =>
                                        {
                                            try
                                            {
                                                NewConnection(peer1, data1);
                                            }
                                            catch (Exception e)
                                            {
                                                log.Error("Error while handling NewConnection packet on " +
                                                          peer1.IP + "(#" + peer1.ID + "). " + e);
                                                QueueDisconnect(peer1, (uint)Status.BadRequest);
                                            }
                                        });
                                        t.Start();
                                    }
                                    else
                                    {
                                        if (found.HandleRateLimit())
                                        {
                                            if (found.account != null)
                                                found.account.lastUsed.Restart();
                                            var peer1 = peer;
                                            var data1 = data;
                                            var found1 = found;
                                            Thread t = new Thread(() =>
                                            {
                                                try
                                                {
                                                    HandlePacket(found1, data1);
                                                }
                                                catch (Exception e)
                                                {
                                                    log.Error("Error while handling packet on " + peer1.IP + "(#" +
                                                              peer1.ID + "). " + e);
                                                    QueueDisconnect(peer1, (uint)Status.Forbidden);
                                                }
                                            });
                                            t.Start();
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

                Library.Deinitialize();
        }
        
        public static void WriteAtLine(int line, string text)
        {
            Console.SetCursorPosition(0, line);
            Console.Write(text);
        }

        public static void HandlePacket(Player p, byte[] enc)
        {

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
            log.Debug("Sent packet " + pa.type + " to Peer " + pl.peer.IP);
        }
        public static void SendPacket(Peer p, Madness.Packets.Packet pa)
        {
            byte[] enc = PacketHelper.JanniePacket(pa, null);

            ENet.Packet packet = default(ENet.Packet);
            packet.Create(enc);
            p.Send(0, ref packet);
        }

        
        public static void NewConnection(Peer p, byte[] hello)
        {

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
                QueueDisconnect(p, (uint)Status.BadRequest);
            }
            
        }
        
    }
}