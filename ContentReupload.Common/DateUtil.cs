using System;
using System.Globalization;

namespace ContentReupload.Common
{
    public static class DateUtil
    {
        public static string GetDate(DateTime date)
        {
            return GetDay(date.Day) + " " + GetMonth(date) + " " + date.Year;
        }

        public static string GetDay(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return day + "st";
                case 2:
                case 22:
                    return day + "nd";
                case 3:
                case 23:
                    return day + "rd";
                default:
                    return day + "th";
            }
        }

        public static string GetMonth(DateTime time)
        {
            return time.ToString("MMMM", new CultureInfo("en-US"));
        }

        // This presumes that weeks start with Monday.
        // Week 1 is the 1st week of the year with a Thursday in it.
        public static int GetWeekOfYear(DateTime time)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
