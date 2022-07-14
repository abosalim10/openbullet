﻿using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Numerics;
using System.Text;

namespace RuriLib.Functions.Crypto
{
    /// <summary>
    /// The available hashing functions.
    /// </summary>
    public enum HashFunction
    {
        /// <summary>The MD4 hashing function (128 bits digest).</summary>
        MD4,

        /// <summary>The MD5 hashing function (128 bits digest).</summary>
        MD5,

        /// <summary>The SHA-1 hashing function (160 bits digest).</summary>
        SHA1,

        /// <summary>The SHA-256 hashing function (256 bits digest).</summary>
        SHA256,

        /// <summary>The SHA-384 hashing function (384 bits digest).</summary>
        SHA384,

        /// <summary>The SHA-512 hashing function (512 bits digest).</summary>
        SHA512,
    }

    /// <summary>
    /// Provides methods for encrypting, decrypting and generating signatures.
    /// </summary>
    public static class Crypto
    {
        #region Hash and Hmac
        /// <summary>
        /// Hashes a string through NTLM.
        /// </summary>
        /// <param name="input">The string to hash</param>
        /// <returns>The NTLM digest.</returns>
        public static byte[] NTLM(string input)
        {
            // Unicode with little endian
            var bytes = Encoding.Unicode.GetBytes(input);
            return MD4(bytes);
        }

        /// <summary>
        /// Hashes a byte array through MD4.
        /// </summary>
        /// <param name="input">The byte array for which to calculate the hash</param>
        /// <returns>The MD4 digest.</returns>
        public static byte[] MD4(byte[] input)
        {
            // get padded uints from bytes
            var bytes = input.ToList();
            uint bitCount = (uint)(bytes.Count) * 8;
            bytes.Add(128);
            while (bytes.Count % 64 != 56) bytes.Add(0);
            var uints = new List<uint>();
            for (int i = 0; i + 3 < bytes.Count; i += 4)
                uints.Add(bytes[i] | (uint)bytes[i + 1] << 8 | (uint)bytes[i + 2] << 16 | (uint)bytes[i + 3] << 24);
            uints.Add(bitCount);
            uints.Add(0);

            // run rounds
            uint a = 0x67452301, b = 0xefcdab89, c = 0x98badcfe, d = 0x10325476;
            Func<uint, uint, uint> rol = (x, y) => x << (int)y | x >> 32 - (int)y;
            for (int q = 0; q + 15 < uints.Count; q += 16)
            {
                var chunk = uints.GetRange(q, 16);
                uint aa = a, bb = b, cc = c, dd = d;
                Action<Func<uint, uint, uint, uint>, uint[]> round = (f, y) =>
                {
                    foreach (uint i in new[] { y[0], y[1], y[2], y[3] })
                    {
                        a = rol(a + f(b, c, d) + chunk[(int)(i + y[4])] + y[12], y[8]);
                        d = rol(d + f(a, b, c) + chunk[(int)(i + y[5])] + y[12], y[9]);
                        c = rol(c + f(d, a, b) + chunk[(int)(i + y[6])] + y[12], y[10]);
                        b = rol(b + f(c, d, a) + chunk[(int)(i + y[7])] + y[12], y[11]);
                    }
                };
                round((x, y, z) => (x & y) | (~x & z), new uint[] { 0, 4, 8, 12, 0, 1, 2, 3, 3, 7, 11, 19, 0 });
                round((x, y, z) => (x & y) | (x & z) | (y & z), new uint[] { 0, 1, 2, 3, 0, 4, 8, 12, 3, 5, 9, 13, 0x5a827999 });
                round((x, y, z) => x ^ y ^ z, new uint[] { 0, 2, 1, 3, 0, 8, 4, 12, 3, 9, 11, 15, 0x6ed9eba1 });
                a += aa; b += bb; c += cc; d += dd;
            }

            // return bytes
            return new[] { a, b, c, d }.SelectMany(BitConverter.GetBytes).ToArray();
        }

        /// <summary>
        /// Hashes a byte array through MD5.
        /// </summary>
        /// <param name="input">The byte array for which to calculate the hash</param>
        /// <returns>The MD5 digest.</returns>
        public static byte[] MD5(byte[] input)
        {
            using MD5 md5 = System.Security.Cryptography.MD5.Create();
            return md5.ComputeHash(input);
        }

        /// <summary>
        /// Calculates an MD5 hash signature.
        /// </summary>
        /// <param name="input">The message for which a signature will be generated</param>
        /// <param name="key">The secret key to use to sign the message</param>
        /// <returns>The HMAC signature.</returns>
        public static byte[] HMACMD5(byte[] input, byte[] key)
        {
            using HMACMD5 hmac = new HMACMD5(key);
            return hmac.ComputeHash(input);
        }

        /// <summary>
        /// Hashes a byte array through SHA-1.
        /// </summary>
        /// <param name="input">The byte array for which to calculate the hash</param>
        /// <returns>The SHA-1 digest.</returns>
        public static byte[] SHA1(byte[] input)
        {
            using SHA1Managed sha1 = new SHA1Managed();
            return sha1.ComputeHash(input);
        }

        /// <summary>
        /// Calculates a SHA-1 hash signature.
        /// </summary>
        /// <param name="input">The message for which a signature will be generated</param>
        /// <param name="key">The secret key to use to sign the message</param>
        /// <returns>The HMAC signature.</returns>
        public static byte[] HMACSHA1(byte[] input, byte[] key)
        {
            using HMACSHA1 hmac = new HMACSHA1(key);
            return hmac.ComputeHash(input);
        }

        /// <summary>
        /// Hashes a byte array through SHA-256.
        /// </summary>
        /// <param name="input">The byte array for which to calculate the hash</param>
        /// <returns>The SHA-256 digest.</returns>
        public static byte[] SHA256(byte[] input)
        {
            using SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(input);
        }

        /// <summary>
        /// Calculates a SHA-256 hash signature.
        /// </summary>
        /// <param name="input">The message for which a signature will be generated</param>
        /// <param name="key">The secret key to use to sign the message</param>
        /// <returns>The HMAC signature.</returns>
        public static byte[] HMACSHA256(byte[] input, byte[] key)
        {
            using HMACSHA256 hmac = new HMACSHA256(key);
            return hmac.ComputeHash(input);
        }

        /// <summary>
        /// Hashes a byte array through SHA-384.
        /// </summary>
        /// <param name="input">The byte array for which to calculate the hash</param>
        /// <returns>The SHA-384 digest.</returns>
        public static byte[] SHA384(byte[] input)
        {
            using SHA384Managed sha384 = new SHA384Managed();
            return sha384.ComputeHash(input);
        }

        /// <summary>
        /// Calculates a SHA-384 hash signature.
        /// </summary>
        /// <param name="input">The message for which a signature will be generated</param>
        /// <param name="key">The secret key to use to sign the message</param>
        /// <returns>The HMAC signature.</returns>
        public static byte[] HMACSHA384(byte[] input, byte[] key)
        {
            using HMACSHA384 hmac = new HMACSHA384(key);
            return hmac.ComputeHash(input);
        }

        /// <summary>
        /// Hashes a byte array through SHA-512.
        /// </summary>
        /// <param name="input">The byte array for which to calculate the hash</param>
        /// <returns>The SHA-512 digest.</returns>
        public static byte[] SHA512(byte[] input)
        {
            using SHA512Managed sha512 = new SHA512Managed();
            return sha512.ComputeHash(input);
        }

        /// <summary>
        /// Calculates a SHA-512 hash signature.
        /// </summary>
        /// <param name="input">The message for which a signature will be generated</param>
        /// <param name="key">The secret key to use to sign the message</param>
        /// <returns>The HMAC signature.</returns>
        public static byte[] HMACSHA512(byte[] input, byte[] key)
        {
            using HMACSHA512 hmac = new HMACSHA512(key);
            return hmac.ComputeHash(input);
        }

        /// <summary>
        /// Converts from the Hash enum to the HashAlgorithmName default struct.
        /// </summary>
        /// <param name="type">The hash type as a Hash enum</param>
        /// <returns>The HashAlgorithmName equivalent.</returns>
        public static HashAlgorithmName ToHashAlgorithmName(this HashFunction type)
        {
            switch (type)
            {
                case HashFunction.MD5:
                    return HashAlgorithmName.MD5;

                case HashFunction.SHA1:
                    return HashAlgorithmName.SHA1;

                case HashFunction.SHA256:
                    return HashAlgorithmName.SHA256;

                case HashFunction.SHA384:
                    return HashAlgorithmName.SHA384;

                case HashFunction.SHA512:
                    return HashAlgorithmName.SHA512;

                default:
                    throw new NotSupportedException("No such algorithm name");
            }
        }
        #endregion

        #region RSA
        /// <summary>
        /// Encrypts data using RSA.
        /// </summary>
        /// <param name="data">The data to encrypt</param>
        /// <param name="n">The public key's modulus</param>
        /// <param name="e">The public key's exponent</param>
        /// <param name="oaep">Whether to use OAEP-SHA1 padding mode instead of PKCS1</param>
        public static byte[] RSAEncrypt(byte[] data, byte[] n, byte[] e, bool oaep)
        {
            using RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            RSA.ImportParameters(new RSAParameters
            {
                Modulus = n,
                Exponent = e
            });
            return RSA.Encrypt(data, oaep);
        }

        /// <summary>
        /// Decrypts data using RSA.
        /// </summary>
        /// <param name="data">The data to encrypt</param>
        /// <param name="n">The public key's modulus</param>
        /// <param name="d">The private key's exponent</param>
        /// <param name="oaep">Whether to use OAEP-SHA1 padding mode instead of PKCS1</param>
        public static byte[] RSADecrypt(byte[] data, byte[] n, byte[] d, bool oaep)
        {
            using RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            RSA.ImportParameters(new RSAParameters
            {
                Modulus = n,
                D = d
            });
            return RSA.Decrypt(data, oaep);
        }

        /// <summary>
        /// Encrypts a message using RSA with PKCS1PAD2 padding.
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="modulus">The public key's modulus</param>
        /// <param name="exponent">The public key's exponent</param>
        // Thanks to TheLittleTrain for this implementation.
        public static byte[] RSAPkcs1Pad2(byte[] message, byte[] modulus, byte[] exponent)
        {
            // Convert the public key components to numbers
            var n = new BigInteger(modulus);
            var e = new BigInteger(exponent);

            // (modulus.ToByteArray().Length - 1) * 8
            // modulus has 256 bits multiplied by 8 bits equals 2048
            var encryptedNumber = Pkcs1Pad2(message, (2048 + 7) >> 3);

            // And now, the RSA encryption
            encryptedNumber = BigInteger.ModPow(encryptedNumber, e, n);

            //Reverse number
            return encryptedNumber.ToByteArray().Reverse().ToArray();
        }

        private static BigInteger Pkcs1Pad2(byte[] data, int keySize)
        {
            if (keySize < data.Length + 11)
                return new BigInteger();

            var buffer = new byte[256];
            var i = data.Length - 1;

            while (i >= 0 && keySize > 0)
            {
                buffer[--keySize] = data[i--];
            }

            // Padding, I think
            var random = new Random();
            buffer[--keySize] = 0;
            while (keySize > 2)
            {
                buffer[--keySize] = (byte)random.Next(1, 256);
                //buffer[--keySize] = 5;
            }

            buffer[--keySize] = 2;
            buffer[--keySize] = 0;

            Array.Reverse(buffer);

            return new BigInteger(buffer);
        }
        #endregion

        #region KDF
        /// <summary>
        /// Generates a PKCS v5 #2.0 key using a Password-Based Key Derivation Function.
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <param name="salt">The salt to use. If null, a random salt will be generated</param>
        /// <param name="saltSize">The random salt size that gets generated in case no salt is provided</param>
        /// <param name="iterations">The number of times the algorithm should be executed</param>
        /// <param name="type">The hashing algorithm to use</param>
        /// <param name="keyLength">The generated key length in bytes</param>
        public static byte[] PBKDF2PKCS5(byte[] password, byte[] salt = null, int saltSize = 8, int iterations = 1, int keyLength = 16, HashFunction type = HashFunction.SHA1)
        {
            if (salt.Length > 0)
            {
                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, type.ToHashAlgorithmName());
                return deriveBytes.GetBytes(keyLength);
            }
            else
            {
                // Generate a random salt
                var randomSalt = new byte[saltSize];
                RandomNumberGenerator.Create().GetBytes(randomSalt);
                using var deriveBytes = new Rfc2898DeriveBytes(password, randomSalt, iterations, type.ToHashAlgorithmName());
                return deriveBytes.GetBytes(keyLength);
            }
        }
        #endregion

        #region AES
        /// <summary>
        /// Encrypts data with AES.
        /// </summary>
        /// <param name="data">The AES-encrypted data</param>
        /// <param name="key">The encryption key</param>
        /// <param name="iv">The initial value</param>
        /// <param name="mode">The cipher mode</param>
        /// <param name="padding">The padding mode</param>
        public static byte[] AESEncrypt(byte[] data, byte[] key, byte[] iv,
            CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.None, int blockSize = 128)
        {
            byte[][] keys = ConvertKeys(key, iv);
            return EncryptStringToBytes_Aes(data, keys[0], keys[1], mode, padding, blockSize);
        }

        /// <summary>
        /// Decrypts AES-encrypted data.
        /// </summary>
        /// <param name="data">The AES-encrypted data</param>
        /// <param name="key">The decryption key</param>
        /// <param name="iv">The initial value</param>
        /// <param name="mode">The cipher mode</param>
        /// <param name="padding">The padding mode</param>
        public static byte[] AESDecrypt(byte[] data, byte[] key, byte[] iv = null,
            CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.None, int blockSize = 128)
        {
            byte[][] keys = ConvertKeys(key, iv);
            return DecryptStringFromBytes_Aes(data, keys[0], keys[1], mode, padding, blockSize);
        }

        private static byte[][] ConvertKeys(byte[] key, byte[] iv)
        {
            byte[][] result = new byte[2][];

            result[0] = key;

            // If no iv was provided, use the first 16 bytes of the key
            if (iv == null)
            {
                result[1] = key;
                Array.Resize(ref result[1], 16);
            }
            else
            {
                result[1] = iv;
            }

            return result;
        }

        private static byte[] EncryptStringToBytes_Aes(byte[] plainText, byte[] key, byte[] iv, CipherMode mode,
            PaddingMode padding, int blockSize)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException(nameof(plainText));

            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));

            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            using var aesAlg = new AesManaged();
            aesAlg.KeySize = 256;
            aesAlg.BlockSize = blockSize;
            aesAlg.Key = key;
            aesAlg.IV = iv;
            aesAlg.Mode = mode;
            aesAlg.Padding = padding;

            var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }
            return msEncrypt.ToArray();
        }

        private static byte[] DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key, byte[] iv, CipherMode mode,
            PaddingMode padding, int blockSize)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));

            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));

            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            using var aesAlg = Aes.Create();
            aesAlg.KeySize = 256;
            aesAlg.BlockSize = blockSize;
            aesAlg.Key = key;
            aesAlg.IV = iv;
            aesAlg.Mode = mode;
            aesAlg.Padding = padding;

            var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var ms = new MemoryStream();
            var buffer = new byte[512];
            var bytesRead = 0;
            while ((bytesRead = csDecrypt.Read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, bytesRead);
            return ms.ToArray();
        }
        #endregion

        #region JWT
        public static string JwtEncode(JwtAlgorithmName algorithmName, string secret, IDictionary<string, object> extraHeaders, IDictionary<string, object> payload)
        {
            IJwtAlgorithm algorithm = algorithmName switch
            {
                JwtAlgorithmName.HS256 => new HMACSHA256Algorithm(),
                JwtAlgorithmName.HS384 => new HMACSHA384Algorithm(),
                JwtAlgorithmName.HS512 => new HMACSHA512Algorithm(),
                _ => throw new NotSupportedException("This algorith is not supported at the moment")
            };

            var jsonSerializer = new JsonNetSerializer();
            var urlEncoder = new JwtBase64UrlEncoder();
            var jwtEncoder = new JwtEncoder(algorithm, jsonSerializer, urlEncoder);

            return jwtEncoder.Encode(extraHeaders, payload, secret);
        }
        #endregion
    }
}
