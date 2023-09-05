using System;
using System.Diagnostics;
using DedicatedServer.Madness.Packets;
using ENet;

namespace DedicatedServer.Madness
{

    public class Player
    {
        public byte[] current_aes;
        
        public Account account = null;
        public Peer peer;

        public string playerLog = "";

        private Stopwatch watch = new Stopwatch();

        private bool isWatched = false;
        
        private int packetsLastFew = 0;

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
            playerLog += log;
            if (playerLog.Split(Environment.NewLine).Length > 100)
                playerLog = playerLog.Substring(0, playerLog.Trim().IndexOf(Environment.NewLine, StringComparison.Ordinal));
        }
        
        public Player(Peer _peer)
        {
            peer = _peer;
        }
        
        
        
    }
}