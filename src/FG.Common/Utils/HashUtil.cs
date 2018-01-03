using System;
using System.Security.Cryptography;
using System.Text;

namespace FG.Common.Utils
{
	public static class HashUtil
	{
		public static long GetLongHashCode(this Guid guid)
		{
			return GetLongHashCode(guid.ToString());
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public static long GetLongHashCode(this string stringInput)
		{
			var byteContents = Encoding.Unicode.GetBytes(stringInput);
			var hash = new MD5CryptoServiceProvider();
			var hashText = hash.ComputeHash(byteContents);
			return BitConverter.ToInt64(hashText, 0) ^ BitConverter.ToInt64(hashText, 7);
		}

		public static string GetShortHashString(this Guid guid)
		{
			return $"{guid.ToString().GetHashCode():X}";
		}

		public static int GetIntHashCode(this string stringInput)
		{
			return (int) GetLongHashCode(stringInput);
		}

		public static long Combine(int a, int b)
		{
			return a << 32 | b;
		}

	    public static uint Adler32(string str)
	    {
	        const int mod = 65521;
	        uint a = 1, b = 0;
	        foreach (char c in str)
	        {
	            a = (a + c) % mod;
	            b = (b + a) % mod;
	        }
	        return (b << 16) | a;
	    }

	    public static string Adler32String(string str)
	    {
	        var value = HashUtil.Adler32(str);
	        var bytes = BitConverter.GetBytes(value);            
	        return System.Convert.ToBase64String(bytes);
        }
    }
    
}