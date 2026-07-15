namespace Lex.Components.Utils;

public static class StringHelper
{
    public static string Truncate(string text, int reduce = 10)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= 10 ? text : text.Substring(0, reduce) + "...";
    }
}