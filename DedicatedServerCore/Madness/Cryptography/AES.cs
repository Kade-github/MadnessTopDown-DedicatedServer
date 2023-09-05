using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

// thank you to lemon for these files!!!

namespace DedicatedServer.Madness.Cryptography
{
    public static class AES
    {
        public static void TestAES()
        {
            var data = "The quick brown fox jumps over the lazy dog.";

            var bytes = Encoding.UTF8.GetBytes(data);

            var key = new byte[32];
            var iv = new byte[16];
            using var provider = new RNGCryptoServiceProvider();
            provider.GetBytes(key);
            provider.GetBytes(iv);

            var enc = Encrypt(bytes, key, iv);

            var dec = Decrypt(enc, key, iv);

            var decString = Encoding.UTF8.GetString(dec);

            if (!string.Equals(data, decString, StringComparison.InvariantCulture))
                throw new CryptographicException("AES Cryto test failed.");
        }

        public static byte[] DecryptAESPacket(byte[] encrypted, byte[] aesKey)
        {
            using var stream = new MemoryStream(encrypted);

            var iv = new byte[16];
            stream.Read(iv, 0, iv.Length);

            var data = new byte[stream.Length - stream.Position];
            stream.Read(data, 0, data.Length);

            return Decrypt(data, aesKey, iv);
        }

        public static byte[] EncryptAESPacket(byte[] data, byte[] aesKey)
        {
            var iv = new byte[16];

            Program.number.GetNonZeroBytes(iv);

            var encrypted = Encrypt(data, aesKey, iv);

            using var stream = new MemoryStream();
            stream.Write(iv, 0, iv.Length);
            stream.Write(encrypted, 0, encrypted.Length);

            return stream.ToArray();
        }

        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            var aesEngine = new AesEngine();
            var blockCipher = new CbcBlockCipher(aesEngine);
            var bufferedBlockCipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());

            var keyParameter = new KeyParameter(key);
            var keyParamWithIV = new ParametersWithIV(keyParameter, iv);

            bufferedBlockCipher.Init(true, keyParamWithIV);

            var outputBuffer = new byte[bufferedBlockCipher.GetOutputSize(data.Length)];
            var len = bufferedBlockCipher.ProcessBytes(data, outputBuffer, 0);
            bufferedBlockCipher.DoFinal(outputBuffer, len);

            return outputBuffer;
        }

        public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            var aesEngine = new AesEngine();
            var blockCipher = new CbcBlockCipher(aesEngine);
            var bufferedBlockCipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());

            var keyParameter = new KeyParameter(key);
            var keyParamWithIV = new ParametersWithIV(keyParameter, iv);

            bufferedBlockCipher.Init(false, keyParamWithIV);

            var outputBuffer = new byte[bufferedBlockCipher.GetOutputSize(data.Length)];
            var len = bufferedBlockCipher.ProcessBytes(data, outputBuffer, 0);
            bufferedBlockCipher.DoFinal(outputBuffer, len);

            return outputBuffer;
        }
    }
}