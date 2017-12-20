using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FG.Common.Utils
{
    public static class TextUtil
	{
		private static readonly Regex TrimInternalWhitespaceRegEx = new Regex(@"(\r\n|\n)\s+", RegexOptions.Compiled);

		public static string CamelCase(this string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return text;
			if (text.Length == 1) return text.Substring(0, 1).ToLower();
			return text.Substring(0, 1).ToLower() + text.Substring(1);
		}

		public static string ToMD5(this byte[] that)
		{
			var hash = System.Security.Cryptography.MD5.Create();

			var output = hash.ComputeHash(that);

			var result = Encoding.UTF8.GetString(output);
			return result;
		}

		public static string ToMD5(this string that)
		{
			var hash = System.Security.Cryptography.MD5.Create();

			var input = Encoding.UTF8.GetBytes(that);
			var output = hash.ComputeHash(input);

			var result = Encoding.UTF8.GetString(output);
			return result;
		}

		public static string ToHex(this string that, bool upperCase = false)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(that);
			var result = new StringBuilder(bytes.Length * 2);

			foreach (var t in bytes)
				result.Append(t.ToString(upperCase ? "X2" : "x2"));

			return result.ToString();
		}

		public static string Concat<T>(this IEnumerable<T> items, Func<T, T, int, string> glue)
		{
			var stringBuilder = new StringBuilder();
			T lastItem = default(T);
			var i = -1;
			foreach (var item in items)
			{
				if ((lastItem != null) && !lastItem.Equals(default(T)))
				{
					stringBuilder.Append(glue(lastItem, item, i));
				}
				stringBuilder.Append(item);
				lastItem = item;
				i++;
			}
			return stringBuilder.ToString();
		}

		public static string Concat<T>(this IEnumerable<T> items, string glue)
		{
			var stringBuilder = new StringBuilder();
			T lastItem = default(T);
			foreach (var item in items)
			{
				if ((lastItem != null) && !lastItem.Equals(default(T)))
				{
					stringBuilder.Append(glue);
				}
				stringBuilder.Append(item);
				lastItem = item;
			}
			return stringBuilder.ToString();
		}

		public static string Concat<T>(this IEnumerable<T> items, char glue)
		{
			var stringBuilder = new StringBuilder();
			T lastItem = default(T);
			foreach (var item in items)
			{
				if ((lastItem != null) && !lastItem.Equals(default(T)))
				{
					stringBuilder.Append(glue);
				}
				stringBuilder.Append(item);
				lastItem = item;
			}
			return stringBuilder.ToString();
		}

		public static string TrimInternalWhitespace(this string that, bool removeLineEndings)
		{
			return TrimInternalWhitespaceRegEx.Replace(that, removeLineEndings ? "" : "\n");
		}

		public static bool IsNullOrEmpty(this string that)
		{
			return string.IsNullOrEmpty(that);
		}
	}
}