using System;
using MessagePack;

namespace DedicatedServer.Madness.Packets;

[MessagePackObject()]
public class SPacketHeartbeat : Packet
{
    [Key("AESKey")] public byte[] NewKey;
    [Key("Number")] public int Number;
    public SPacketHeartbeat() : base(PacketType.SPacketHeartbeat)
    {
        NewKey = Array.Empty<byte>();
    }
}