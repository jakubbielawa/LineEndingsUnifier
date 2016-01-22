using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace JakubBielawa.LineEndingsUnifier
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidLine_Endings_UnifierPkgString)]
    [ProvideOptionPage(typeof(OptionsPage), "Line Endings Unifier", "General Settings", 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed class LineEndingsUnifierPackage : Package
    {
        public LineEndingsUnifierPackage()
        {
        }
        
        protected override void Initialize()
        {
            base.Initialize();

            commandEvents = IDE.Events.CommandEvents;
            commandEvents.BeforeExecute += commandEvents_BeforeExecute;

            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                var menuCommandID = new CommandID(GuidList.guidLine_Endings_UnifierCmdSet_File, (int)PkgCmdIDList.cmdidUnifyLineEndings_File);
                var menuItem = new MenuCommand(new EventHandler(UnifyLineEndingsInFileEventHandler), menuCommandID);
                menuItem.Visible = true;
                menuItem.Enabled = true;
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidLine_Endings_UnifierCmdSet_Folder, (int)PkgCmdIDList.cmdidUnifyLineEndings_Folder);
                menuItem = new MenuCommand(new EventHandler(UnifyLineEndingsInFolderEventHandler), menuCommandID);
                menuItem.Visible = true;
                menuItem.Enabled = true;
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidLine_Endings_UnifierCmdSet_Project, (int)PkgCmdIDList.cmdidUnifyLineEndings_Project);
                menuItem = new MenuCommand(new EventHandler(UnifyLineEndingsInProjectEventHandler), menuCommandID);
                menuItem.Visible = true;
                menuItem.Enabled = true;
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidLine_Endings_UnifierCmdSet_Solution, (int)PkgCmdIDList.cmdidUnifyLineEndings_Solution);
                menuItem = new MenuCommand(new EventHandler(UnifyLineEndingsInSolutionEventHandler), menuCommandID);
                menuItem.Visible = true;
                menuItem.Enabled = true;
                mcs.AddCommand(menuItem);
            }
        }

        void commandEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            var command = (VSConstants.VSStd97CmdID)ID;
            switch (command)
            {
                case VSConstants.VSStd97CmdID.SaveProjectItem:
                    if (this.OptionsPage.ForceDefaultLineEndingOnSave)
                    {
                        var currentDocument = this.IDE.ActiveDocument;
                        var textDocument = currentDocument.Object("TextDocument") as TextDocument;
                        var lineEndings = this.DefaultLineEnding;
                        var tmp = 0;

                        var supportedFileFormats = this.SupportedFileFormats;

                        if (currentDocument.Name.EndsWithAny(supportedFileFormats))
                        {
                            UnifyLineEndingsInDocument(textDocument, lineEndings, ref tmp);
                        }
                    }
                    break;
                case VSConstants.VSStd97CmdID.SaveSolution:
                    if (this.OptionsPage.ForceDefaultLineEndingOnSave)
                    {
                        UnifyLineEndingsInSolution(false);
                    }
                    break;
                default:
                    break;
            }
        }

        private void UnifyLineEndingsInFileEventHandler(object sender, EventArgs e)
        {
            UnifyLineEndingsInFile();
        }

        private void UnifyLineEndingsInFile()
        {
            var selectedItem = this.IDE.SelectedItems.Item(1);
            var item = selectedItem.ProjectItem;

            var choiceWindow = new LineEndingChoice(item.Name);
            if (choiceWindow.ShowDialog() == true && choiceWindow.LineEndings != LineEndingsChanger.LineEndings.None)
            {
                var supportedFileFormats = this.SupportedFileFormats;

                if (item.Name.EndsWithAny(supportedFileFormats))
                {
                    System.Threading.Tasks.Task.Run(() =>
                        {
                            var numberOfChanges = 0;
                            var stopWatch = new Stopwatch();
                            stopWatch.Start();
                            UnifyLineEndingsInProjectItem(item, choiceWindow.LineEndings, ref numberOfChanges);
                            stopWatch.Stop();
                            var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                            VsShellUtilities.ShowMessageBox(this, string.Format("Successfully changed {0} line endings in {1} seconds!", numberOfChanges, secondsElapsed), "Success",
                                OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        });
                }
                else
                {
                    VsShellUtilities.ShowMessageBox(this, "This is not a valid source file!", "Error",
                        OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
        }

        private void UnifyLineEndingsInFolderEventHandler(object sender, EventArgs e)
        {
            UnifyLineEndingsInFolder();
        }

        private void UnifyLineEndingsInFolder()
        {
            var selectedItem = IDE.SelectedItems.Item(1);
            var projectItem = selectedItem.ProjectItem;

            var choiceWindow = new LineEndingChoice(selectedItem.Name);
            if (choiceWindow.ShowDialog() == true && choiceWindow.LineEndings != LineEndingsChanger.LineEndings.None)
            {
                System.Threading.Tasks.Task.Run(() =>
                    {
                        var numberOfChanges = 0;
                        var stopWatch = new Stopwatch();
                        stopWatch.Start();
                        UnifyLineEndingsInProjectItems(projectItem.ProjectItems, choiceWindow.LineEndings, ref numberOfChanges);
                        stopWatch.Stop();
                        var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                        VsShellUtilities.ShowMessageBox(this, string.Format("Successfully changed {0} line endings in {1} seconds!", numberOfChanges, secondsElapsed), "Success",
                                OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    });
            }
        }

        private void UnifyLineEndingsInProjectEventHandler(object sender, EventArgs e)
        {
            UnifyLineEndingsInProject();
        }

        private void UnifyLineEndingsInProject()
        {
            var selectedItem = this.IDE.SelectedItems.Item(1);
            var selectedProject = selectedItem.Project;

            var choiceWindow = new LineEndingChoice(selectedProject.Name);
            if (choiceWindow.ShowDialog() == true && choiceWindow.LineEndings != LineEndingsChanger.LineEndings.None)
            {
                System.Threading.Tasks.Task.Run(() =>
                    {
                        var numberOfChanges = 0;
                        var stopWatch = new Stopwatch();
                        stopWatch.Start();
                        UnifyLineEndingsInProjectItems(selectedProject.ProjectItems, choiceWindow.LineEndings, ref numberOfChanges);
                        stopWatch.Stop();
                        var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                        VsShellUtilities.ShowMessageBox(this, string.Format("Successfully changed {0} line endings in {1} seconds!", numberOfChanges, secondsElapsed), "Success",
                                OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    });
            }
        }

        private void UnifyLineEndingsInSolutionEventHandler(object sender, EventArgs e)
        {
            UnifyLineEndingsInSolution();
        }

        private void UnifyLineEndingsInSolution(bool askForLineEnding = true)
        {
            var currentSolution = this.IDE.Solution;

            var properties = currentSolution.Properties;
            foreach (Property property in properties)
            {
                if (property.Name == "Name")
                {
                    if (askForLineEnding)
                    {
                        var choiceWindow = new LineEndingChoice((property as Property).Value.ToString());
                        if (choiceWindow.ShowDialog() == true && choiceWindow.LineEndings != LineEndingsChanger.LineEndings.None)
                        {
                            System.Threading.Tasks.Task.Run(() =>
                                {
                                    var stopWatch = new Stopwatch();
                                    stopWatch.Start();
                                    var numberOfChanges = 0;
                                    foreach (Project project in currentSolution.GetAllProjects())
                                    {
                                        UnifyLineEndingsInProjectItems(project.ProjectItems, choiceWindow.LineEndings, ref numberOfChanges);
                                    }
                                    stopWatch.Stop();
                                    var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                                    VsShellUtilities.ShowMessageBox(this, string.Format("Successfully changed {0} line endings in {1} seconds!", numberOfChanges, secondsElapsed), "Success",
                                        OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                                });
                        }
                    }
                    else
                    {
                        var lineEndings = this.DefaultLineEnding;

                        int numberOfChanges = 0;
                        foreach (Project project in currentSolution.Projects)
                        {
                            UnifyLineEndingsInProjectItems(project.ProjectItems, lineEndings, ref numberOfChanges);
                        }
                    }
                    break;
                }
            }
        }

        private void UnifyLineEndingsInProjectItems(ProjectItems projectItems, LineEndingsChanger.LineEndings lineEndings, ref int numberOfChanges)
        {
            var supportedFileFormats = this.SupportedFileFormats;

            foreach (ProjectItem item in projectItems)
            {
                if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                {
                    UnifyLineEndingsInProjectItems(item.ProjectItems, lineEndings, ref numberOfChanges);
                }
                else
                {
                    if (item.Name.EndsWithAny(supportedFileFormats))
                    {
                        UnifyLineEndingsInProjectItem(item, lineEndings, ref numberOfChanges);
                    }
                }
            }
        }

        private void UnifyLineEndingsInProjectItem(ProjectItem item, LineEndingsChanger.LineEndings lineEndings, ref int numberOfChanges)
        {
            Window documentWindow = null;

            if (!item.IsOpen)
            {
                documentWindow = item.Open();
            }
            var document = item.Document;
            if (document != null)
            {
                var textDocument = document.Object("TextDocument") as TextDocument;
                UnifyLineEndingsInDocument(textDocument, lineEndings, ref numberOfChanges);
                if (this.OptionsPage.SaveFilesAfterUnifying)
                {
                    document.Save();
                }
            }

            if (documentWindow != null)
            {
                documentWindow.Close();
            }
        }

        private void UnifyLineEndingsInDocument(TextDocument textDocument, LineEndingsChanger.LineEndings lineEndings, ref int numberOfChanges)
        {
            var startPoint = textDocument.StartPoint.CreateEditPoint();
            var endPoint = textDocument.EndPoint.CreateEditPoint();

            var text = startPoint.GetText(endPoint.AbsoluteCharOffset);
            var changedText = LineEndingsChanger.ChangeLineEndings(text, lineEndings, ref numberOfChanges);
            startPoint.ReplaceText(text.Length, changedText, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
        }

        private CommandEvents commandEvents;

        private LineEndingsChanger.LineEndings DefaultLineEnding
        {
            get { return (LineEndingsChanger.LineEndings)this.OptionsPage.DefaultLineEnding; }
        }

        private string[] SupportedFileFormats
        {
            get { return this.OptionsPage.SupportedFileFormats.Replace(" ", string.Empty).Split(new[] { ';' }); }
        }

        private OptionsPage optionsPage;

        private OptionsPage OptionsPage
        {
            get { return optionsPage ?? (optionsPage = (OptionsPage)GetDialogPage(typeof(OptionsPage))); }
        }

        private DTE ide;

        public DTE IDE
        {
            get { return ide ?? (ide = (DTE)GetService(typeof(DTE))); }
        }
    }
}
