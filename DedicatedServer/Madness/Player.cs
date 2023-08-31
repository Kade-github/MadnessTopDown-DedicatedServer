using System;
using System.Diagnostics;
using ENet;

namespace DedicatedServer.Madness
{

    public class Player
    {
        public byte[] current_aes;
        
        public Account account;
        public Peer peer;

        private Stopwatch watch;

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
                peer.DisconnectNow(0);
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
        
        public Player(Peer _peer)
        {
            peer = _peer;
        }
        
        
        
    }
}