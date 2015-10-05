using System;

namespace JakubBielawa.LineEndingsUnifier
{
    static class GuidList
    {
        public const string guidLine_Endings_UnifierPkgString = "1ce34aed-d80b-4e02-afc9-bd0bd3848443";
        public const string guidLine_Endings_UnifierCmdSetString_File = "e65fc73a-f162-437e-a8b4-b7e3469d83cb";
        public const string guidLine_Endings_UnifierCmdSetString_Folder = "078bdabd-c25e-49b8-acab-61655b84573f";
        public const string guidLine_Endings_UnifierCmdSetString_Project = "c79636f0-0d76-41a1-80bf-feee33bf0ac9";
        public const string guidLine_Endings_UnifierCmdSetString_Solution = "6087fbf4-4264-4a5c-a6d3-d21859ac5ad2";

        public static readonly Guid guidLine_Endings_UnifierCmdSet_File = new Guid(guidLine_Endings_UnifierCmdSetString_File);
        public static readonly Guid guidLine_Endings_UnifierCmdSet_Folder = new Guid(guidLine_Endings_UnifierCmdSetString_Folder);
        public static readonly Guid guidLine_Endings_UnifierCmdSet_Project = new Guid(guidLine_Endings_UnifierCmdSetString_Project);
        public static readonly Guid guidLine_Endings_UnifierCmdSet_Solution = new Guid(guidLine_Endings_UnifierCmdSetString_Solution);
    };
}