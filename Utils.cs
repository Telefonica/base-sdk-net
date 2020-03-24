using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BaseSDK
{
    public static class Utils
    {
        public static string Sha1(byte[] data)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                return BitConverter.ToString(sha1.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}