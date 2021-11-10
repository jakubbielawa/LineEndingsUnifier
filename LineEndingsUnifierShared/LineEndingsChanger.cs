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
            Dominant,
            None
        }

        public enum LineEndingsList
        {
            Windows,
            Linux,
            Macintosh,
            Dominant
        }

        private const string LineEndingsPattern = "\r\n?|\n";

        private const string WindowsLineEndings = "\r\n";

        private const string LinuxLineEndings = "\n";

        private const string MacintoshLineEndings = "\r";

        public static string ChangeLineEndings(string text, LineEndings lineEndings, ref int numberOfChanges, out int numberOfIndividualChanges, out int numberOfAllLineEndings)
        {
            numberOfIndividualChanges = 0;

            string replacementString = string.Empty;

            numberOfAllLineEndings = Regex.Matches(text, LineEndingsPattern).Count;
            var numberOfWindowsLineEndings = Regex.Matches(text, WindowsLineEndings).Count;
            var numberOfLinuxLineEndings = Regex.Matches(text, LinuxLineEndings).Count - numberOfWindowsLineEndings;
            var numberOfMacintoshLineEndings = Regex.Matches(text, MacintoshLineEndings).Count - numberOfWindowsLineEndings;

            switch (lineEndings)
            {
                case LineEndings.Linux:
                    replacementString = LinuxLineEndings;
                    numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfMacintoshLineEndings;
                    break;
                case LineEndings.Windows:
                    replacementString = WindowsLineEndings;
                    numberOfIndividualChanges = numberOfLinuxLineEndings + numberOfMacintoshLineEndings;
                    break;
                case LineEndings.Macintosh:
                    replacementString = MacintoshLineEndings;
                    numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfLinuxLineEndings;
                    break;
                case LineEndings.Dominant:
                    if (numberOfWindowsLineEndings > numberOfLinuxLineEndings && numberOfWindowsLineEndings > numberOfMacintoshLineEndings)
                    {
                        replacementString = WindowsLineEndings;
                        numberOfIndividualChanges = numberOfLinuxLineEndings + numberOfMacintoshLineEndings;
                    }
                    else if (numberOfLinuxLineEndings > numberOfWindowsLineEndings && numberOfLinuxLineEndings > numberOfMacintoshLineEndings)
                    {
                        replacementString = LinuxLineEndings;
                        numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfMacintoshLineEndings;
                    }
                    else
                    {
                        replacementString = MacintoshLineEndings;
                        numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfLinuxLineEndings;
                    }

                    break;
            }

            string modifiedText = Regex.Replace(text, LineEndingsPattern, replacementString);

            numberOfChanges += numberOfIndividualChanges;

            return modifiedText;
        }
    }
}
