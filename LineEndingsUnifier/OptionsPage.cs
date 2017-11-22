using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace JakubBielawa.LineEndingsUnifier
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class OptionsPage : DialogPage
    {
        private LineEndingsChanger.LineEndingsList defaultLineEnding = LineEndingsChanger.LineEndingsList.Windows;
        private bool forceDefaultLineEndingOnSave = false;
        private string supportedFileFormats = ".cpp; .c; .h; .hpp; .cs; .js; .vb; .txt";
        private string supportedFileNames = "Dockerfile";
        private bool saveFilesAfterUnifying = false;
        private bool writeReport = false;
        private bool unifyOnlyOpenFiles = false;
        private bool addNewlineOnLastLine = false;
        private bool trackChanges = false;
        private bool removeTrailingWhitespace = false;

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
        [DisplayName("Supported File Names")]
        [Description("Files with these names will have line endings unified")]
        public string SupportedFileNames
        {
            get { return supportedFileNames; }
            set { supportedFileNames = value; }
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
        [DisplayName("Write Report To The Output Window")]
        [Description("Set this to TRUE if you want the extension to write a report in the Output window")]
        public bool WriteReport
        {
            get { return writeReport; }
            set { writeReport = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Unify Only Open Files On Save All")]
        [Description("Set this to TRUE if you want the extension to unify only files that are open in the editor after hitting \"Save All\"")]
        public bool UnifyOnlyOpenFiles
        {
            get { return unifyOnlyOpenFiles; }
            set { unifyOnlyOpenFiles = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Add Newline On The Last Line")]
        [Description("Set this to TRUE if you want the extension to add a newline character on the last line when unifying line endings")]
        public bool AddNewlineOnLastLine
        {
            get { return addNewlineOnLastLine; }
            set { addNewlineOnLastLine = value; }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Track Changes")]
        [Description("Set this to TRUE if you want the extension to remember when files were unified to improve performance")]
        public bool TrackChanges
        {
            get { return trackChanges; }
            set
            {
                if (value == false)
                {
                    DTE2 ide = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
                    if (!ide.Solution.FullName.Equals(string.Empty))
                    {
                        var path = Path.GetDirectoryName(ide.Solution.FullName) + "\\.leu";
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                }

                trackChanges = value;
            }
        }

        [Category("Line Endings Unifier")]
        [DisplayName("Remove Trailing Whitespace")]
        [Description("Set this to TRUE if you want the extension to remove trailing whitespace characters while unifying newline characters")]
        public bool RemoveTrailingWhitespace
        {
            get { return removeTrailingWhitespace; }
            set { removeTrailingWhitespace = value; }
        }
    }
}
