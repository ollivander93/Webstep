using System;

namespace TaskApi.Extensions
{
    public static class DateExtensions
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp )
        {
            DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds( unixTimeStamp ). ToUniversalTime();
            return dtDateTime;
        }
    }
}