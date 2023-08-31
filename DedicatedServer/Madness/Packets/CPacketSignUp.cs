using MessagePack;

namespace DedicatedServer.Madness.Packets;

[MessagePackObject]
public class CPacketSignUp : Packet
{
    
    [Key("Email")] public string Email;
    [Key("Username")] public string Username;
    [Key("Password")] public string Password;
    public CPacketSignUp() : base(PacketType.CPacketSignUp)
    {
    }
}