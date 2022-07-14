﻿using JWT.Algorithms;
using Newtonsoft.Json;
using RuriLib.Attributes;
using RuriLib.Functions.Crypto;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using Scrypt;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RuriLib.Blocks.Functions.Crypto
{
    [BlockCategory("Crypto", "Blocks for executing cryptographic functions", "#9acd32")]
    public static class Methods
    {
        [Block("Hashes data using the specified hashing function")]
        public static byte[] Hash(BotData data, byte[] input, HashFunction hashFunction = HashFunction.MD5)
        {
            var hashed = Hash(input, hashFunction);
            data.Logger.LogHeader();
            data.Logger.Log($"Computed hash: {RuriLib.Functions.Conversion.HexConverter.ToHexString(hashed)}", LogColors.YellowGreen);
            return hashed;
        }

        [Block("Hashes a UTF8 string to a HEX-encoded lowercase string using the specified hashing function")]
        public static string HashString(BotData data, string input, HashFunction hashFunction = HashFunction.MD5)
        {
            var hashed = RuriLib.Functions.Conversion.HexConverter.ToHexString(Hash(Encoding.UTF8.GetBytes(input), hashFunction));
            data.Logger.LogHeader();
            data.Logger.Log($"Computed hash: {hashed}", LogColors.YellowGreen);
            return hashed;
        }

        [Block("Hashes a string using NTLM", name = "NTLM Hash")]
        public static byte[] NTLMHash(BotData data, string input)
        {
            var hashed = RuriLib.Functions.Crypto.Crypto.NTLM(input);
            data.Logger.LogHeader();
            data.Logger.Log($"Computed hash: {RuriLib.Functions.Conversion.HexConverter.ToHexString(hashed)}", LogColors.YellowGreen);
            return hashed;
        }

        [Block("Computes the HMAC signature of some data using the specified secret key and hashing function")]
        public static byte[] Hmac(BotData data, byte[] input, byte[] key, HashFunction hashFunction = HashFunction.MD5)
        {
            var hmac = Hmac(input, key, hashFunction);
            data.Logger.LogHeader();
            data.Logger.Log($"Computed HMAC: {RuriLib.Functions.Conversion.HexConverter.ToHexString(hmac)}", LogColors.YellowGreen);
            return hmac;
        }

        [Block("Computes the HMAC signature as a HEX-encoded lowercase string from a given UTF8 string using the specified key and hashing function")]
        public static string HmacString(BotData data, string input, byte[] key, HashFunction hashFunction = HashFunction.MD5)
        {
            var hmac = RuriLib.Functions.Conversion.HexConverter.ToHexString(Hmac(Encoding.UTF8.GetBytes(input), key, hashFunction));
            data.Logger.LogHeader();
            data.Logger.Log($"Computed HMAC: {hmac}", LogColors.YellowGreen);
            return hmac;
        }

        private static byte[] Hash(byte[] input, HashFunction function)
        {
            return function switch
            {
                HashFunction.MD4 => RuriLib.Functions.Crypto.Crypto.MD4(input),
                HashFunction.MD5 => RuriLib.Functions.Crypto.Crypto.MD5(input),
                HashFunction.SHA1 => RuriLib.Functions.Crypto.Crypto.SHA1(input),
                HashFunction.SHA256 => RuriLib.Functions.Crypto.Crypto.SHA256(input),
                HashFunction.SHA384 => RuriLib.Functions.Crypto.Crypto.SHA384(input),
                HashFunction.SHA512 => RuriLib.Functions.Crypto.Crypto.SHA512(input),
                _ => throw new NotSupportedException()
            };
        }

        private static byte[] Hmac(byte[] input, byte[] key, HashFunction function)
        {
            return function switch
            {
                HashFunction.MD5 => RuriLib.Functions.Crypto.Crypto.HMACMD5(input, key),
                HashFunction.SHA1 => RuriLib.Functions.Crypto.Crypto.HMACSHA1(input, key),
                HashFunction.SHA256 => RuriLib.Functions.Crypto.Crypto.HMACSHA256(input, key),
                HashFunction.SHA384 => RuriLib.Functions.Crypto.Crypto.HMACSHA384(input, key),
                HashFunction.SHA512 => RuriLib.Functions.Crypto.Crypto.HMACSHA512(input, key),
                _ => throw new NotSupportedException()
            };
        }

        [Block("Hashes data using the Scrypt algorithm")]
        public static string ScryptString(BotData data, string password, string salt, int iterationCount = 16384, int blockSize = 8, int threadCount = 1)
        {
            var rng = new FakeRNG(salt);
            var encoder = new ScryptEncoder(iterationCount, blockSize, threadCount, rng);
            var hashed = encoder.Encode(password);
            data.Logger.LogHeader();
            data.Logger.Log($"Computed Scrypt: {hashed}", LogColors.YellowGreen);
            return hashed;
        }

        // Used for Scrypt.NET because it doesn't support a parametrized salt
        private class FakeRNG : RandomNumberGenerator
        {
            private readonly byte[] salt;

            public FakeRNG(string salt)
            {
                this.salt = Encoding.UTF8.GetBytes(salt);
            }

            public override void GetBytes(byte[] data)
            {
                for (int i = 0; i < salt.Length; i++)
                {
                    data[i] = salt[i];
                }
            }
        }

        [Block("Encrypts data using RSA", name = "RSA Encrypt")]
        public static byte[] RSAEncrypt(BotData data, byte[] plainText, byte[] modulus, byte[] exponent, bool useOAEP)
        {
            var cipherText = RuriLib.Functions.Crypto.Crypto.RSAEncrypt(plainText, modulus, exponent, useOAEP);
            data.Logger.LogHeader();
            data.Logger.Log($"Encrypted: {RuriLib.Functions.Conversion.HexConverter.ToHexString(cipherText)}", LogColors.YellowGreen);
            return cipherText;
        }

        [Block("Decrypts data using RSA", name = "RSA Decrypt")]
        public static byte[] RSADecrypt(BotData data, byte[] cipherText, byte[] modulus, byte[] d, bool useOAEP)
        {
            var plainText = RuriLib.Functions.Crypto.Crypto.RSADecrypt(cipherText, modulus, d, useOAEP);
            data.Logger.LogHeader();
            data.Logger.Log($"Decrypted: {RuriLib.Functions.Conversion.HexConverter.ToHexString(plainText)}", LogColors.YellowGreen);
            return plainText;
        }

        [Block("Encrypts data using RSA with PKCS1PAD2", name = "RSA PKCS1PAD2")]
        public static byte[] RSAPkcs1Pad2(BotData data, byte[] plainText, byte[] modulus, byte[] exponent)
        {
            var encrypted = RuriLib.Functions.Crypto.Crypto.RSAPkcs1Pad2(plainText, modulus, exponent);
            data.Logger.LogHeader();
            data.Logger.Log($"Encrypted: {RuriLib.Functions.Conversion.HexConverter.ToHexString(encrypted)}", LogColors.YellowGreen);
            return encrypted;
        }

        [Block("Generates a PKCS v5 #2.0 key using a Password-Based Key Derivation Function", name = "PBKDF2PKCS5")]
        public static byte[] PBKDF2PKCS5(BotData data, byte[] password, byte[] salt = null, int saltSize = 8, int iterations = 1, int keyLength = 16, HashFunction type = HashFunction.SHA1)
        {
            var derived = RuriLib.Functions.Crypto.Crypto.PBKDF2PKCS5(password, salt, saltSize, iterations, keyLength, type);
            data.Logger.LogHeader();
            data.Logger.Log($"Derived: {RuriLib.Functions.Conversion.HexConverter.ToHexString(derived)}", LogColors.YellowGreen);
            return derived;
        }

        [Block("Encrypts data with AES", name = "AES Encrypt")]
        public static byte[] AESEncrypt(BotData data, byte[] plainText, byte[] key, byte[] iv,
            CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.None, int blockSize = 128)
        {
            var cipherText = RuriLib.Functions.Crypto.Crypto.AESEncrypt(plainText, key, iv, mode, padding, blockSize);
            data.Logger.LogHeader();
            data.Logger.Log($"Encrypted: {RuriLib.Functions.Conversion.HexConverter.ToHexString(cipherText)}", LogColors.YellowGreen);
            return cipherText;
        }

        [Block("Decrypts data with AES", name = "AES Decrypt")]
        public static byte[] AESDecrypt(BotData data, byte[] cipherText, byte[] key, byte[] iv,
            CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.None, int blockSize = 128)
        {
            var plainText = RuriLib.Functions.Crypto.Crypto.AESDecrypt(cipherText, key, iv, mode, padding, blockSize);
            data.Logger.LogHeader();
            data.Logger.Log($"Decrypted: {RuriLib.Functions.Conversion.HexConverter.ToHexString(plainText)}", LogColors.YellowGreen);
            return plainText;
        }

        [Block("Generates a JSON Web Token using a secret key, payload, optional extra headers and specified algorithm type", name = "JWT Encode", extraInfo = "The header already contains the selected algorithm and token type (JWT) by default")]
        public static string JwtEncode(BotData data, JwtAlgorithmName algorithm, string secret, string extraHeaders = "{}", string payload = "{}")
        {
            var extraHeadersDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(extraHeaders);
            var payloadDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);

            string encoded = RuriLib.Functions.Crypto.Crypto.JwtEncode(algorithm, secret, extraHeadersDictionary, payloadDictionary);

            data.Logger.LogHeader();
            data.Logger.Log($"Encoded: {encoded}", LogColors.YellowGreen);

            return encoded;
        }
    }
}
