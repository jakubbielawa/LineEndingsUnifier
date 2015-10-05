using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace JakubBielawa.LineEndingsUnifier
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class OptionsPage : DialogPage
    {
        private LineEndingsChanger.LineEndingsList defaultLineEnding = LineEndingsChanger.LineEndingsList.Windows;
        private bool forceDefaultLineEndingOnSave = false;
        private string supportedFileFormats = ".cpp; .c; .h; .hpp; .cs; .js; .vb; .txt";

        [Category("Line Endings Unifier")]
        [DisplayName("Default Line Ending")]
        [Description("Default Line Ending")]
        public LineEndingsChanger.LineEndingsList DefaultLineEnding
        {
            get { return defaultLineEnding; }
            set { defaultLineEnding = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Force Default Line Ending On Document Save")]
        [Description("Force Default Line Ending On Document Save")]
        public bool ForceDefaultLineEndingOnSave
        {
            get { return forceDefaultLineEndingOnSave; }
            set { forceDefaultLineEndingOnSave = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Supported File Formats")]
        [Description("Supported File Formats")]
        public string SupportedFileFormats
        {
            get { return supportedFileFormats; }
            set { supportedFileFormats = value; }
        }
    }
}
