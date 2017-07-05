using System.Text;

namespace FG.Common.Utils
{
    public static class TextUtil
    {
        public static string CamelCase(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            if (text.Length == 1) return text.Substring(0, 1).ToLower();
            return text.Substring(0, 1).ToLower() + text.Substring(1);
        }

		public static string ToMD5(this string that)
		{
			var hash = System.Security.Cryptography.MD5.Create();

			var input = Encoding.UTF8.GetBytes(that);
			var output = hash.ComputeHash(input);

			var result = Encoding.UTF8.GetString(output);
			return result;
		}
	}
}