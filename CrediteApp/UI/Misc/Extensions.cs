using System;

namespace UI.Misc
{
    internal static class Extensions
    {
        public static DateTime ToDateOnly(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}
