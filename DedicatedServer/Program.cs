using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using ENet;

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
        
        
        public static void Main(string[] args)
        {
            number = RandomNumberGenerator.Create();
            
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
            log = new Logging();
            
            log.Info("MadnessTopDown Dedicated Server starting...");
            
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
                                break;

                            case EventType.Timeout:
                                log.Info("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                                break;

                            case EventType.Receive:
                                log.Info("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
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
        
    }
}