using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace UI.XRay.EncryptLock.Smart3XServiceLibrary
{
    public static class SoftLocker
    {
        static readonly byte[] byKey = { 0x29, 0x1D, 0xE7, 0x3F, 0x82, 0xDB, 0x11, 0x38 };

        static readonly byte[] byIV = { 0x4A, 0x33, 0x7E, 0xCF, 0xC7, 0x99, 0x14, 0x5A };

        public static string Encrypt(string data)
        {
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();

            int i = cryptoProvider.KeySize;

            MemoryStream ms = new MemoryStream();

            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);

            StreamWriter sw = new StreamWriter(cst);

            sw.Write(data);

            sw.Flush();

            cst.FlushFinalBlock();

            sw.Flush();

            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);

        }

        public static string Decrypt(string data)
        {
            byte[] byEnc;

            try
            {

                byEnc = Convert.FromBase64String(data);

            }

            catch
            {

                return null;

            }

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();

            MemoryStream ms = new MemoryStream(byEnc);

            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateDecryptor(byKey, byIV), CryptoStreamMode.Read);

            StreamReader sr = new StreamReader(cst);

            return sr.ReadToEnd();

        }
    }
}
