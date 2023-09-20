using System;
using System.Diagnostics;
using DedicatedServer.Madness.Packets;
using DedicatedServer.Madness.Server;
using ENet;

namespace DedicatedServer.Madness
{

    public class Player
    {
        public byte[] current_aes;
        public byte[] next_aes = Array.Empty<Byte>();
        public Account? account = null;
        public long timeSinceHeartbeat = 0;
        public long lastHeartbeat = 0;
        public Peer peer;

        public bool isIpBanned = false;
        
        public string playerLog = "";

        public Stopwatch connectTime = new Stopwatch();
        private Stopwatch watch = new Stopwatch();
        public Stopwatch heartBeatWatch = new Stopwatch();

        private bool isWatched = false;
        
        private int packetsLastFew = 0;

        public int heartbeatNumber = 0;
        public int failedHeartbeats = 0;

        public bool HandleRateLimit()
        {
            if (!watch.IsRunning)
                watch.Start();
            if (watch.Elapsed.Seconds >= 1)
            {
                packetsLastFew = 0;
                watch.Restart();
            }
            
            packetsLastFew++;

            if (isWatched && packetsLastFew > 350 && peer.LastReceiveTime < 200)
            {
                if (account != null)
                {
                    PlayerHandle.BanPlayer(account);
                }
                else
                {
                    PlayerHandle.BanIp(peer.IP);
                }

                peer.DisconnectNow((uint)Status.TooManyRequests);
                return false;
            }
            
            if (packetsLastFew > 250 && peer.LastReceiveTime < 250) // Second flag
            {
                isWatched = true;
                return false;
            }

            if (packetsLastFew > 100) // First flag
            {
                
                return false;
            }

            return true;
        }

        public void AddLog(string log)
        {
            playerLog += Logging.FormatString(log);
            if (playerLog.Split(Environment.NewLine).Length > 100)
                playerLog = playerLog.Substring(0, playerLog.Trim().IndexOf(Environment.NewLine, StringComparison.Ordinal));
        }
        
        public Player(Peer _peer)
        {
            peer = _peer;
        }
        
        
        
    }
}