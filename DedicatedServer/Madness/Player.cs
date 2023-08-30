using ENet;

namespace DedicatedServer.Madness
{

    public class Player
    {
        public byte[] current_aes;
        
        public Account account;
        public Peer peer;

        public Player(Peer _peer)
        {
            peer = _peer;
        }
        
        
        
    }
}