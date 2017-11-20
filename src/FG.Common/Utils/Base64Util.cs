namespace FG.Common.Utils
{
	public static class Base64Util
	{
		public static string ToBase64(this string that)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(that);
			return System.Convert.ToBase64String(plainTextBytes);
		}

		public static string FromBase64(this string that)
		{
			var base64EncodedBytes = System.Convert.FromBase64String(that);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
		}
	}
}