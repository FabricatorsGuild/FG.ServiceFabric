using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FG.Common.Utils
{
	public static class StringMatchExtensions
	{
		private static readonly Regex RemoveNonWordStringRegex = new Regex(@"[^\w]", RegexOptions.Compiled);

		private static readonly Regex FindInitialLowercaseRegex = new Regex(@"\b([a-z])", RegexOptions.Compiled);

		private static readonly Regex HumanReadableStringRegex =
			new Regex(@"((?<=\p{Ll})\p{Lu}|\p{Lu}(?=\p{Ll}))", RegexOptions.Compiled);

		public static bool Matches(this string that, string pattern, StringComparison stringComparison,
			bool useWildcards = true)
		{
			if (that == null) return (pattern == null);

			var regExPattern = pattern;
			if (useWildcards)
			{
				regExPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
			}
			var options = RegexOptions.None;
			if (stringComparison == StringComparison.InvariantCultureIgnoreCase ||
			    stringComparison == StringComparison.CurrentCultureIgnoreCase ||
			    stringComparison == StringComparison.OrdinalIgnoreCase)
			{
				options = RegexOptions.IgnoreCase;
			}
			return Regex.IsMatch(that, regExPattern, options);
		}

		public static string RemoveNonWordCharacters(this string that)
		{
			return RemoveNonWordStringRegex.Replace(that, "");
		}


		public static string RemoveFromEnd(this string that, string remove)
		{
			if (that == null) return null;
			if (remove == null) return that;
			if (that.EndsWith(remove))
			{
				var length = that.Length;
				var lengthRemove = remove.Length;
				var lengthResult = length - lengthRemove;
				return that.Substring(0, lengthResult);
			}
			return that;
		}

		public static string RemoveFromStart(this string that, string remove)
		{
			if (that == null) return null;
			if (remove == null) return that;
			if (that.StartsWith(remove))
			{
				var length = that.Length;
				var lengthRemove = remove.Length;
				var lengthResult = length - lengthRemove;
				return that.Substring(lengthRemove, lengthResult);
			}
			return that;
		}

		public static string RemoveCommonPrefix(this string that, string compareTo, char componentSeparator)
		{
			var a = that.Split(componentSeparator);
			var b = compareTo.Split(componentSeparator);

			var i = 0;
			var j = 0;

			var result = new StringBuilder();
			while ((i < a.Length) && (j < b.Length))
			{
				var a_i = a[i];
				var b_j = b[j];

				if (a_i.Equals(b_j))
				{
					result.Append(a_i);
					result.Append(componentSeparator);
				}
				i++;
				j++;
			}

			return that.RemoveFromStart(result.ToString());
		}

		public static string GetHumanReadable(this string that)
		{
			return HumanReadableStringRegex.Replace(that, " $1").Trim();
		}

		public static string GetUpperCasedInitial(this string that)
		{
			return $"{that?.Substring(0, 1).ToUpperInvariant()}{that?.Substring(1)}";
		}

		public static string GetLowerCasedInitial(this string that)
		{
			return $"{that?.Substring(0, 1).ToLowerInvariant()}{that?.Substring(1)}";
		}

		public static string GetCSVList<T>(this IEnumerable<T> values, Func<T, string> renderValue)
		{
			renderValue = renderValue ?? (v => v.ToString());
			var valuesLength = values.Count();
			if (valuesLength == 0) return "";
			if (valuesLength == 1) return renderValue(values.Single());
			return values.Aggregate("", (s, i) => $"{s}, {renderValue(i)}").Substring(2);
		}
	}
}