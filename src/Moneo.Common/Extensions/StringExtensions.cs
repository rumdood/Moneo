namespace Moneo.Common;

public static class StringExtensions
{
    public static bool IsValidTaskFullId(this string input)
    {
        return input.Contains('_');
    }
}