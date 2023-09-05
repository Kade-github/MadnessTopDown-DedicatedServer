using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DedicatedServer.Madness.Packets;
using MessagePack;

namespace DedicatedServer.Madness.MessagePack
{
    
    
    public class Cereal
    {
        private static ImmutableDictionary<PacketType, Type> TypesMap;


        public static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

        public static void InitalizeReflectivePacketTypes()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();

            var map = new Dictionary<PacketType, Type>();

            foreach (PacketType type in Enum.GetValues(typeof(PacketType)))
            {
                var t = types.FirstOrDefault(p => p.Name == type.ToString());

                if (t == null)
                    continue;

                map.Add(type, t);
            }

            TypesMap = map.ToImmutableDictionary();
        }
        
        public static Type GetTypeFromPacketType(PacketType type)
        {
            if (!TypesMap.TryGetValue(type, out var t))
                return null;

            return t;
        }

        public static byte[] SerializePacket(Packet packet)
        {
            var t = GetTypeFromPacketType(packet.type);

            var serialized = MessagePackSerializer.Serialize(t, packet, Options);

            using var stream = new MemoryStream();

            using var writer = new BinaryWriter(stream, Encoding.UTF8);

            writer.Write((int) packet.type);
            writer.Write(serialized.Length);
            writer.Write(serialized);

            var newDes = stream.ToArray();

            return newDes;
        }

    }
}