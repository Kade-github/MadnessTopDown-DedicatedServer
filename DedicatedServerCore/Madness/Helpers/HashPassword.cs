using System;
using Org.BouncyCastle.Crypto.Generators;

namespace DedicatedServer.Madness.Helpers;

public class HashPassword
{
    public static string HashIntoBase64(byte[] password, byte[] salt = null)
    {
        if (salt == null)
        {
            salt = new byte[16];
            Program.number.GetNonZeroBytes(salt);
        }
        byte[] hash = null;
        try
        {
            // generate a BCrypt hash with a cost of 15, a salt of 128, and an md5 hashed string as the base.
            hash = BCrypt.Generate(password, salt, 15);
        }
        catch (Exception e)
        {
            Console.WriteLine("[BCrypt Error] " + e);
            throw;
        }
        return Convert.ToBase64String(hash);
    }
}