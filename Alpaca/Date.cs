using System;

namespace AlpacaTrade
{
    public class Date
    {
        static TimeZoneInfo eastern()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch (Exception)
            {
                // macOS / Linux
                return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(dt.AddSeconds(unixTimeStamp), eastern());
        }

        public static DateTime UnixTimeToEastern(DateTime dt)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dt, eastern());
        }
    }
}