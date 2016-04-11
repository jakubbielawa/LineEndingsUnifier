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
        private bool saveFilesAfterUnifying = false;
        private bool unifyFilesInSolutionOnSave = true;
        private bool writeReport = false;

        [Category("Line Endings Unifier")]
        [DisplayName("Default Line Ending")]
        [Description("The default line ending")]
        public LineEndingsChanger.LineEndingsList DefaultLineEnding
        {
            get { return defaultLineEnding; }
            set { defaultLineEnding = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Force Default Line Ending On Document Save")]
        [Description("Determines if line endings have to be unified automatically on a document save")]
        public bool ForceDefaultLineEndingOnSave
        {
            get { return forceDefaultLineEndingOnSave; }
            set { forceDefaultLineEndingOnSave = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Supported File Formats")]
        [Description("Files with these formats will have line endings unified")]
        public string SupportedFileFormats
        {
            get { return supportedFileFormats; }
            set { supportedFileFormats = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Save Files After Unifying")]
        [Description("When you click \"Unify Line Endings In This...\" button, changed files won't be saved. Set this to TRUE if you want them to be automatically saved.")]
        public bool SaveFilesAfterUnifying
        {
            get { return saveFilesAfterUnifying; }
            set { saveFilesAfterUnifying = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Unify Files In Solution On Save")]
        [Description("When you click the \"Save All\" or \"Save Solution\" button, all files in the solution will have their line endings unified.")]
        public bool UnifyFilesInSolutionOnSave
        {
            get { return unifyFilesInSolutionOnSave; }
            set { unifyFilesInSolutionOnSave = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Write Report To The Output Window")]
        [Description("Set this to TRUE if you want the extension to write a report in the Output window")]
        public bool WriteReport
        {
            get { return writeReport; }
            set { writeReport = value; }
        }
    }
}
