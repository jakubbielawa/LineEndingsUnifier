using System.ComponentModel;
using System.Linq;

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

        public static string ChangeLineEndings(string line, LineEndings lineEndings, ref int numberOfChanges)
        {
            switch (lineEndings)
            {
                case LineEndings.Linux:
                    numberOfChanges += line.Count(x => x == '\r');
                    line = line.Replace("\r\n", "\n").Replace('\r', '\n');
                    break;
                case LineEndings.Windows:
                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line[i] == '\r')
                        {
                            if (i < line.Length - 1 && line[i + 1] != '\n')
                            {
                                numberOfChanges++;
                                line = line.Insert(i + 1, "\n");
                                i++;
                            }
                            else if (i == line.Length - 1)
                            {
                                numberOfChanges++;
                                line = line.Insert(i + 1, "\n");
                            }
                        }
                        else if (line[i] == '\n' && i > 0 && line[i - 1] != '\r')
                        {
                            numberOfChanges++;
                            line = line.Insert(i, "\r");
                            i++;
                        }
                    }
                    break;
                case LineEndings.Macintosh:
                    numberOfChanges += line.Count(x => x == '\n');
                    line = line.Replace("\r\n", "\r").Replace('\n', '\r');
                    break;
            }

            return line;
        }
    }
}
