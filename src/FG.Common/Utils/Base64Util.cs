using System;
using System.Text;

namespace FG.Common.Utils
{
    public static class Base64Util
    {
        public static string ToBase64(this string that)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(that);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string FromBase64(this string that)
        {
            var base64EncodedBytes = Convert.FromBase64String(that);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}