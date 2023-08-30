using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DedicatedServer.Madness.Secrets;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

// thank you to lemon for these files!!!

namespace DedicatedServer.Madness.Cryptography
{
    public static class RSA
    {
        public static RsaKeyParameters PublicRsaKey;
        public static RsaKeyParameters PrivateRsaKey;

        private static OaepEncoding RsaEncryptEngine;
        private static OaepEncoding RsaDecryptEngine;

        public static void Init()
        {
            var privateKeyParam = ReadAsymmetricKeyParameter(Constants.RSAPrivate);
            var publicKeyParam = ReadAsymmetricKeyParameter(Constants.RSAPublic);
            

            PublicRsaKey = publicKeyParam as RsaKeyParameters;
            PrivateRsaKey = privateKeyParam as RsaKeyParameters;

            var rsaEngineDecrypt = new RsaEngine();
            RsaDecryptEngine = new OaepEncoding(rsaEngineDecrypt);

            RsaDecryptEngine.Init(false, PrivateRsaKey);

            var rsaEngineEncrypt = new RsaEngine();
            RsaEncryptEngine = new OaepEncoding(rsaEngineEncrypt);

            RsaEncryptEngine.Init(true, PublicRsaKey);
        }

        public static void TestRSA()
        {
            var testData = "the quick brown fox jumps over the lazy dog.";

            var data = Encoding.UTF8.GetBytes(testData);

            var encrypted = Encrypt(data);

            var decrypted = Decrypt(encrypted);

            var dec = Encoding.UTF8.GetString(decrypted);

            if (!string.Equals(testData, dec, StringComparison.InvariantCulture))
                throw new CryptographicException("RSA Enc/Dec failed");

            Console.WriteLine(Encoding.UTF8.GetString(decrypted));

            var signature = Sign(data);

            if (!VerifySignature(signature, data))
                throw new CryptographicException("RSA Sig failed (1)");

            data[1] = 2;

            if (VerifySignature(signature, data))
                throw new CryptographicException("RSA Sig failed (2)");
        }

        public static byte[] Decrypt(byte[] data)
        {
            lock (RsaDecryptEngine)
            {
                var decrypted = RsaDecryptEngine.ProcessBlock(data, 0, data.Length);

                return decrypted;
            }
        }

        public static byte[] Encrypt(byte[] data)
        {
            lock (RsaEncryptEngine)
            {
                var encrypted = RsaEncryptEngine.ProcessBlock(data, 0, data.Length);

                return encrypted;
            }
        }

        public static byte[] Sign(byte[] data)
        {
            var signer = SignerUtilities.GetSigner("SHA256WITHRSA");
            signer.Init(true, PrivateRsaKey);

            signer.BlockUpdate(data, 0, data.Length);

            return signer.GenerateSignature();
        }

        public static bool VerifySignature(byte[] signature, byte[] data)
        {
            var signer = SignerUtilities.GetSigner("SHA256WITHRSA");

            signer.Init(false, PublicRsaKey);

            signer.BlockUpdate(data, 0, data.Length);
            return signer.VerifySignature(signature);
        }

        public static AsymmetricKeyParameter ReadAsymmetricKeyParameter(string key)
        {
            AsymmetricKeyParameter keyParam;
            using TextReader reader = new StringReader(key);
            var pemReader = new PemReader(reader);

            keyParam = (AsymmetricKeyParameter) pemReader.ReadObject();

            return keyParam;
        }
    }
}