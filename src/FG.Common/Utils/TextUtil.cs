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
    }
}