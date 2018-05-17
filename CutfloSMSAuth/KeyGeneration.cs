using System;
using System.Security.Cryptography;
namespace CutfloSMSAuth
{
    public static class KeyGeneration
    {
        public static string GenerateSession()
        {
            var bytes = new byte[sizeof(Int64)];
            RNGCryptoServiceProvider Gen = new RNGCryptoServiceProvider();
            Gen.GetBytes(bytes);
            return BitConverter.ToString(bytes);
        }
    }
}
