using System;

namespace Electra.Common.Extensions
{
    public static class Base64Extensions
    {
        public static string Base64Decode(this string data)
            => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data));

        public static string Base64Encode(this string data) 
            => Base64Encode(System.Text.Encoding.UTF8.GetBytes(data));
        
        public static string Base64Encode(this byte[] data) => Convert.ToBase64String(data);
    }
}