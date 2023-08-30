using System;
using System.IO;
using System.Text;
using DedicatedServer.Madness.Packets;
using MessagePack;

namespace DedicatedServer.Madness.MessagePack
{
    public class Decreal
    {
        public static Packet DeserializePacket(byte[] data)
        {
            return DeserializePacket(data, out _);
        }

        public static Packet DeserializePacket(byte[] data, out PacketType packetType)
        {
            
    
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
    
            packetType = (PacketType) reader.ReadInt32();

            var length = reader.ReadInt32();

            if (length > stream.Length - stream.Position)
                throw new InvalidOperationException("Cannot read longer than memory stream length");
    
            var packetData = reader.ReadBytes(length);

            var type = Cereal.GetTypeFromPacketType(packetType);

            if (type == null)
            {
                packetType = PacketType.Null;
                return null;
            }
            
            var j = MessagePackSerializer.Deserialize(type, packetData, Cereal.Options);

            if (j is Packet packet)
                return packet;

            throw new InvalidOperationException("Object is not packet, error.");
        }


        public static T DeserializePacket<T>(byte[] data) where T : Packet
        {
            return DeserializePacket(data) as T;
        }
    }
}