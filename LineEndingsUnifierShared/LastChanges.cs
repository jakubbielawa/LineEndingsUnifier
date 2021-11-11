using JakubBielawa.LineEndingsUnifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JakubBielawa.LineEndingsUnifier
{
    public class LastChanges
    {
        public LastChanges(long ticks, LineEndingsChanger.LineEndings lineEndings)
        {
            Ticks = ticks;
            LineEndings = lineEndings;
        }

        public long Ticks { get; }

        public LineEndingsChanger.LineEndings LineEndings { get; }
    }
}
