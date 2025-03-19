using System;

namespace Moneo.Common;

public static class DateTimeExtensions
{
    public static double HoursSince(this DateTime first, DateTime second)
    {
        return second.Subtract(first).TotalHours;
    }

    public static DateTime ToUniversalTime(this DateTime dateTime, TimeZoneInfo fromTimeZone)
    {
        if (fromTimeZone.Equals(TimeZoneInfo.Utc))
        {
            return dateTime;
        }

        var dtUnSpec = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(dtUnSpec, fromTimeZone);
    }

    public static DateTime ToUniversalTime(this DateTime dateTime, string fromTimeZone)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(fromTimeZone);
        return dateTime.ToUniversalTime(tz);
    }

    public static DateTime UniversalTimeToTimeZone(this DateTime dateTime, TimeZoneInfo toTimeZone)
    {
        if (toTimeZone.Equals(TimeZoneInfo.Utc))
        {
            return dateTime;
        }

        var dtUtc = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(dtUtc, toTimeZone);
    }

    public static DateTime UniversalTimeToTimeZone(this DateTime dateTime, string toTimeZone)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(toTimeZone);
        return dateTime.UniversalTimeToTimeZone(tz);
    }

    public static DateTime ConvertTimeZone(this DateTime dateTime, TimeZoneInfo fromTimeZone, TimeZoneInfo toTimeZone)
    {
        if (fromTimeZone.Equals(toTimeZone))
        {
            return dateTime;
        }

        var dtUnSpec = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTime(dtUnSpec, fromTimeZone, toTimeZone);
    }

    public static DateTime ConvertTimeZone(this DateTime dateTime, string fromTimeZone, string toTimeZone)
    {
        var from = TimeZoneInfo.FindSystemTimeZoneById(fromTimeZone);
        var to = TimeZoneInfo.FindSystemTimeZoneById(toTimeZone);

        return dateTime.ConvertTimeZone(from, to);
    }
}
