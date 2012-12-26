using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Reflection;

namespace EncryptDecryptLibrary
{
    public class EncryptDecryptLibrary
    {
        public const string KEY = "guozhita";

        public static bool CheckRunOrNot()
        {
            try
            {
                //RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"Software\DTUManagement");
                //object obj = rk.GetValue("Path");
                //if (!(obj is string))
                //    return false;
                //StreamReader sr = new StreamReader((string)obj + @"\install.dat");

                string installfile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "install.dat");
                StreamReader sr = new StreamReader(installfile);

                string s = sr.ReadToEnd();
                string s2 = Decrypt(s);
                int i1 = s2.IndexOf("A");
                int i2 = s2.IndexOf("A", i1 + 1);
                string subs = s2.Substring(i1 + 1, i2 - i1 - 1);
                int y = int.Parse(subs.Substring(0, 4));
                int m = int.Parse(subs.Substring(4, 2));
                int d = int.Parse(subs.Substring(6, 2));
                DateTime dt = new DateTime(y, m, d);
                TimeSpan ts = dt.Subtract(DateTime.Now);
                if (ts.TotalSeconds < 0.0)
                    return false;
                else
                    return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// DES Encrypt
        /// </summary>
        /// <param name="pToEncrypt">string to be encrypted</param>
        /// <param name="sKey">key, length must be 8</param>
        /// <returns>Encrypted string in the format of Base64</returns>
        public static string Encrypt(string pToEncrypt, string sKey = EncryptDecryptLibrary.KEY)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(pToEncrypt);
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = Convert.ToBase64String(ms.ToArray());
                ms.Close();
                return str;
            }
        }

        /// <summary>
        /// DES Decrypt
        /// </summary>
        /// <param name="pToDecrypt">string in the format of Base64 to be decrypt</param>
        /// <param name="sKey">key, length must be 8</param>
        /// <returns>decrypted string</returns>
        public static string Decrypt(string pToDecrypt, string sKey = EncryptDecryptLibrary.KEY)
        {
            byte[] inputByteArray = Convert.FromBase64String(pToDecrypt);
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = Encoding.UTF8.GetString(ms.ToArray());
                ms.Close();
                return str;
            }
        }
    }
}
