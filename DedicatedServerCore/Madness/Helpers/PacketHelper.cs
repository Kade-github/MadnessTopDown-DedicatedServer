using DedicatedServer.Madness.Cryptography;
using DedicatedServer.Madness.MessagePack;
using DedicatedServer.Madness.Packets;

namespace DedicatedServer.Madness.Helpers;

public class PacketHelper
{
    /// <summary>
    /// Prepare packet -lemon
    /// </summary>
    /// <param name="p"></param>
    /// <param name="aesKey"></param>
    /// <returns></returns>
    public static byte[] JanniePacket(Packet p, byte[] aesKey)
    {
        var data = Cereal.SerializePacket(p);
        
        if (aesKey is { Length: 32 })
            data = AES.EncryptAESPacket(data,aesKey);
        
        return data;
    }
}