using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace ContentReupload.Common
{
    public static class FileUtil
    {
        public static string ValidateFileName(string input)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9_ -]"); // only alphanumeric characters
            return rgx.Replace(input.Replace(" ", "-"), "");
        }

        public static string OptimizeTitle(string title)
        {
            // #ToTitleCase format day dates wrongly, such as 21st to 21St
            // return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLower());

            string ev(Match m) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.Value);
            return Regex.Replace(title, @"\b[a-zA-Z]+\b", ev);
        }

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
                    @"..")
                ).Replace("\\", "/");
        }

        public static string GetDocumentsPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).Replace("\\", "/");
        }
    }
}
