using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace JakubBielawa.LineEndingsUnifier
{
    public static class LineEndingsChanger
    {
        public enum LineEndings
        {
            Windows,
            Linux,
            Macintosh,
            None
        }

        public enum LineEndingsList
        {
            Windows,
            Linux,
            Macintosh
        }

        private const string LineEndingsPattern = "\r\n?|\n";

        private const string WindowsLineEndings = "\r\n";

        private const string LinuxLineEndings = "\n";

        private const string MacintoshLineEndings = "\r";

        public static string ChangeLineEndings(string text, LineEndings lineEndings, ref int numberOfChanges, out int numberOfIndividualChanges)
        {
            numberOfIndividualChanges = 0;

            string replacementString = string.Empty;

            switch (lineEndings)
            {
                case LineEndings.Linux:
                    replacementString = LinuxLineEndings;
                    break;
                case LineEndings.Windows:
                    replacementString = WindowsLineEndings;
                    break;
                case LineEndings.Macintosh:
                    replacementString = MacintoshLineEndings;
                    break;
            }

            int changesCount = 0;

            string modifiedText = Regex.Replace(text, LineEndingsPattern,
                (match) =>
                {
                    changesCount++;
                    return match.Result(replacementString);
                });

            numberOfIndividualChanges = changesCount;
            numberOfChanges += numberOfIndividualChanges;

            return modifiedText;
        }
    }
}
