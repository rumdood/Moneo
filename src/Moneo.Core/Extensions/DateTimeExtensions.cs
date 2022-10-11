namespace Moneo.Core;

public static class DateTimeExtensions
{
    public static double HoursSince(this DateTime first, DateTime second)
    {
        return second.Subtract(first).TotalHours;
    }

    public static DateTime GetTimeZoneAdjustedDateTime(this DateTime dateTime, TimeZoneInfo timeZone)
    {
        var dtUnspec = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dtUnspec, timeZone);
        return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }

    public static DateTime GetTimeZoneAdjustedDateTime(this DateTime dateTime, string timeZone = "")
    {
        var tz = string.IsNullOrEmpty(timeZone)
            ? TimeZoneInfo.FindSystemTimeZoneById(timeZone)
            : TimeZoneInfo.Local;

        return dateTime.GetTimeZoneAdjustedDateTime(tz);
    }
}
