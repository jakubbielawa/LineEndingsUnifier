﻿using EnvDTE;
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

            SetupOutputWindow();
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
                            var numberOfIndividualChanges = 0;
                            Output("Unifying started...\n");
                            UnifyLineEndingsInDocument(textDocument, lineEndings, ref tmp, out numberOfIndividualChanges);
                            Output(string.Format("{0}: changed {1} line endings\n", currentDocument.FullName, numberOfIndividualChanges));
                            Output("Done\n");
                        }
                    }
                    break;
                case VSConstants.VSStd97CmdID.SaveSolution:
                    if (this.OptionsPage.ForceDefaultLineEndingOnSave && this.OptionsPage.UnifyFilesInSolutionOnSave)
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
                            Output("Unifying started...\n");
                            var numberOfChanges = 0;
                            var stopWatch = new Stopwatch();
                            stopWatch.Start();
                            UnifyLineEndingsInProjectItem(item, choiceWindow.LineEndings, ref numberOfChanges);
                            stopWatch.Stop();
                            var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                            VsShellUtilities.ShowMessageBox(this, string.Format("Successfully changed {0} line endings in {1} seconds!", numberOfChanges, secondsElapsed), "Success",
                                OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                            Output(string.Format("Done in {0} seconds\n", secondsElapsed));
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
                        Output("Unifying started...\n");
                        var numberOfChanges = 0;
                        var stopWatch = new Stopwatch();
                        stopWatch.Start();
                        UnifyLineEndingsInProjectItems(projectItem.ProjectItems, choiceWindow.LineEndings, ref numberOfChanges);
                        stopWatch.Stop();
                        var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                        VsShellUtilities.ShowMessageBox(this, string.Format("Successfully changed {0} line endings in {1} seconds!", numberOfChanges, secondsElapsed), "Success",
                                OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        Output(string.Format("Done in {0} seconds\n", secondsElapsed));
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
                        Output("Unifying started...\n");
                        var numberOfChanges = 0;
                        var stopWatch = new Stopwatch();
                        stopWatch.Start();
                        UnifyLineEndingsInProjectItems(selectedProject.ProjectItems, choiceWindow.LineEndings, ref numberOfChanges);
                        stopWatch.Stop();
                        var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                        VsShellUtilities.ShowMessageBox(this, string.Format("Successfully changed {0} line endings in {1} seconds!", numberOfChanges, secondsElapsed), "Success",
                                OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        Output(string.Format("Done in {0} seconds\n", secondsElapsed));
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
                                    Output("Unifying started...\n");
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
                                    Output(string.Format("Done in {0} seconds\n", secondsElapsed));
                                });
                        }
                    }
                    else
                    {
                        var lineEndings = this.DefaultLineEnding;

                        Output("Unifying started...\n");
                        int numberOfChanges = 0;
                        foreach (Project project in currentSolution.Projects)
                        {
                            UnifyLineEndingsInProjectItems(project.ProjectItems, lineEndings, ref numberOfChanges);
                        }
                        Output("Done\n");
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
                var numberOfIndividualChanges = 0;

                var textDocument = document.Object("TextDocument") as TextDocument;
                UnifyLineEndingsInDocument(textDocument, lineEndings, ref numberOfChanges, out numberOfIndividualChanges);
                if (this.OptionsPage.SaveFilesAfterUnifying)
                {
                    document.Save();
                }

                Output(string.Format("{0}: changed {1} line endings\n", document.FullName, numberOfIndividualChanges));
            }

            if (documentWindow != null)
            {
                documentWindow.Close();
            }
        }

        private void UnifyLineEndingsInDocument(TextDocument textDocument, LineEndingsChanger.LineEndings lineEndings, ref int numberOfChanges, out int numberOfIndividualChanges)
        {
            var startPoint = textDocument.StartPoint.CreateEditPoint();
            var endPoint = textDocument.EndPoint.CreateEditPoint();

            var text = startPoint.GetText(endPoint.AbsoluteCharOffset);
            var changedText = LineEndingsChanger.ChangeLineEndings(text, lineEndings, ref numberOfChanges, out numberOfIndividualChanges);
            startPoint.ReplaceText(text.Length, changedText, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
        }

        private void SetupOutputWindow()
        {
            this.outputWindow = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            this.guid = new Guid("0F44E2D1-F5FA-4d2d-AB30-22BE8ECD9789");
            var windowTitle = "Line Endings Unifier";
            this.outputWindow.CreatePane(ref this.guid, windowTitle, 1, 1);
        }

        private void Output(string message)
        {
            if (this.OptionsPage.WriteReport)
            {
                IVsOutputWindowPane outputWindowPane;
                this.outputWindow.GetPane(ref this.guid, out outputWindowPane);
            
                outputWindowPane.OutputString(message);
            }
        }

        private IVsOutputWindow outputWindow;

        private Guid guid;

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
