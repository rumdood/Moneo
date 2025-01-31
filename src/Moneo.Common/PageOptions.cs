using System.Diagnostics.CodeAnalysis;

namespace Moneo.Common;

public record PageOptions(int PageNumber, int PageSize) : IParsable<PageOptions>
{
    private const string PageNumberKey = "pn";
    private const string PageSizeKey = "ps";
    
    public static PageOptions Parse(string s, IFormatProvider? provider)
    {
        var query = System.Web.HttpUtility.ParseQueryString(s);
        var result = new PageOptions(int.TryParse(query[PageNumberKey], out var pageNumber) ? pageNumber : 1,
            int.TryParse(query[PageSizeKey], out var pageSize) ? pageSize : 10);
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out PageOptions result)
    {
        if (string.IsNullOrEmpty(s))
        {
            result = null;
            return false;
        }
        
        try
        {
            result = Parse(s, provider);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
