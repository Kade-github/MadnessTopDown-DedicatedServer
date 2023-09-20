using System.Collections.Generic;
using System.Threading;
using DedicatedServer.Madness.Helpers;
using ENet;

namespace DedicatedServer.Madness.Server;

public class PacketHandle
{
    public static Dictionary<Player, List<Madness.Packets.Packet>> packetQueue = new();

    public static Dictionary<Peer, uint> queueDisconnect = new();
    
    public static void QueueDisconnect(Peer id, uint reason)
    {
        lock (queueDisconnect)
        {
            queueDisconnect[id] = reason;
        }
    }
        
    public static void QueuePacket(Player id, Madness.Packets.Packet pa)
    {
        // monitor enter
        lock (packetQueue)
        {
            if (!packetQueue.ContainsKey(id))
                packetQueue.Add(id, new List<Madness.Packets.Packet>());
                    
            packetQueue[id].Add(pa);
        }
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

    public static void HandlePackets()
    {
        if (Monitor.TryEnter(packetQueue))
        {
            try
            {
                foreach (var p in packetQueue)
                {
                    foreach (var pa in p.Value)
                        SendPacket(p.Key, pa);
                    p.Value.Clear();
                }

                    
            }
            finally
            {
                Monitor.Exit(packetQueue);
            }
        }
                    
        if (Monitor.TryEnter(queueDisconnect))
        {
            try
            {
                foreach (var p in queueDisconnect)
                {
                    PlayerHandle.DeletePlayer(p.Key);
                    p.Key.DisconnectNow(p.Value);
                }

                queueDisconnect.Clear();
            }
            finally
            {
                Monitor.Exit(queueDisconnect);
            }
        }
    }
}