using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;

namespace UI.XRay.Business.Algo
{
    public static class SoftLocker
    {
        //key
        static readonly byte[] BsKey2 = { 0x33, 0x7E, 0xCF, 0xC7, 0x99, 0x14, 0x5A, 0x4A };

        //#region 加密字符串
        ///// <summary> /// 加密字符串   
        ///// </summary>  
        ///// <param name="str">要加密的字符串</param>  
        ///// <returns>加密后的字符串</returns>  
        //public static byte[] Encrypt(string str)
        //{
        //    DESCryptoServiceProvider descsp = new DESCryptoServiceProvider();   //实例化加/解密类对象   

        //    MemoryStream ms = new MemoryStream(); //实例化内存流对象      

        //    //使用内存流实例化加密流对象   
        //    CryptoStream encStream = new CryptoStream(ms, descsp.CreateEncryptor(), CryptoStreamMode.Write);

        //    StreamWriter sw = new StreamWriter(encStream);

        //    // Write the plaintext to the stream.
        //    sw.WriteLine(encryptKey);

        //    // Close the StreamWriter and CryptoStream.
        //    sw.Close();
        //    encStream.Close();

        //    // Get an array of bytes that represents
        //    // the memory stream.
        //    byte[] buffer = ms.ToArray();

        //    // Close the memory stream.
        //    ms.Close();

        //    return buffer;
        //}
        //#endregion

        #region 解密字符串

        /// <summary>  
        /// 解密字符串   
        /// </summary>  
        /// <param name="keyBytes"></param>
        /// <returns>解密后的字符串</returns>  
        public static string Decrypt(byte[] keyBytes)
        {
            try
            {
                DESCryptoServiceProvider descsp = new DESCryptoServiceProvider { Key = BsKey2, IV = BsKey2 }; //实例化加/解密类对象

                // Create a memory stream to the passed buffer.
                MemoryStream ms = new MemoryStream(keyBytes);

                // Create a CryptoStream using the memory stream and the 
                // CSP DES key. 
                CryptoStream encStream = new CryptoStream(ms, descsp.CreateDecryptor(), CryptoStreamMode.Read);

                // Create a StreamReader for reading the stream.
                StreamReader sr = new StreamReader(encStream);

                // Read the stream as a string.
                string val = sr.ReadLine();

                // Close the streams.
                sr.Close();
                encStream.Close();
                ms.Close();

                return val;
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            return null;
        }

        // Decrypt the byte array.
        public static string Decrypt(byte[] CypherText, SymmetricAlgorithm key)
        {
            // Create a memory stream to the passed buffer.
            MemoryStream ms = new MemoryStream(CypherText);

            // Create a CryptoStream using the memory stream and the 
            // CSP DES key. 
            CryptoStream encStream = new CryptoStream(ms, key.CreateDecryptor(), CryptoStreamMode.Read);

            // Create a StreamReader for reading the stream.
            StreamReader sr = new StreamReader(encStream);

            // Read the stream as a string.
            string val = sr.ReadLine();

            // Close the streams.
            sr.Close();
            encStream.Close();
            ms.Close();

            return val;
        }

        #endregion
    }
}
