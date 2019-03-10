using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace ContentReupload.Util
{
    public static class UtilMethods
    {
        public static string GetSolutionPath()
        {
            return 
                Path.GetFullPath(
                    Path.Combine(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Directory.GetCurrentDirectory()
                            )
                        ), 
                    @"../")
                );
        }

        public static string GetDocumentsPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public static string GetDate(DateTime date)
        {
            return GetMonth(date) + " " + GetDay(date.Day) + " " + date.Year;
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

        public static void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
