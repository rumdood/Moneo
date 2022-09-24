namespace Moneo.Core;

public static class DateTimeExtensions
{
    public static double HoursSince(this DateTime first, DateTime second)
    {
        return second.Subtract(first).TotalHours;
    }
}
