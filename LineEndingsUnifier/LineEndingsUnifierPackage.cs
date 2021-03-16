using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace JakubBielawa.LineEndingsUnifier
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidLine_Endings_UnifierPkgString)]
    [ProvideOptionPage(typeof(OptionsPage), "Line Endings Unifier", "General Settings", 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class LineEndingsUnifierAsyncPackage: AsyncPackage
    {
        public LineEndingsUnifierAsyncPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // runs in the background thread and doesn't affect the responsiveness of the UI thread.
            await Task.Delay(5000);

            await base.InitializeAsync(cancellationToken, progress);

            // Switches to the UI thread in order to consume some services used in command initialization
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
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

            IServiceProvider serviceProvider = new ServiceProvider(IDE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);

            this.runningDocumentTable = new RunningDocumentTable(serviceProvider);
            this.documentSaveListener = new DocumentSaveListener(runningDocumentTable);
            this.documentSaveListener.BeforeSave += DocumentSaveListener_BeforeSave;
            this.changesManager = new ChangesManager();
        }

        private int DocumentSaveListener_BeforeSave(uint docCookie)
        {
            var document = GetDocumentFromDocCookie(docCookie);

            if (!this.isUnifyingLocked)
            {
                if (this.OptionsPage.ForceDefaultLineEndingOnSave)
                {
                    var currentDocument = document;
                    var textDocument = currentDocument.Object("TextDocument") as TextDocument;
                    var lineEndings = this.DefaultLineEnding;
                    var tmp = 0;

                    var supportedFileFormats = this.SupportedFileFormats;
                    var supportedFileNames = this.SupportedFileNames;

                    if (currentDocument.Name.EndsWithAny(supportedFileFormats) || currentDocument.Name.EqualsAny(supportedFileNames))
                    {
                        var numberOfIndividualChanges = 0;
                        var numberOfAllLineEndings = 0;
                        Output("Unifying started...\n");
                        UnifyLineEndingsInDocument(textDocument, lineEndings, ref tmp, out numberOfIndividualChanges, out numberOfAllLineEndings);
                        Output(string.Format("{0}: changed {1} out of {2} line endings\n", currentDocument.FullName, numberOfIndividualChanges, numberOfAllLineEndings));
                        Output("Done\n");
                    }
                }
            }

            return VSConstants.S_OK;
        }

        private void UnifyLineEndingsInFileEventHandler(object sender, EventArgs e)
        {
            var selectedItem = this.IDE.SelectedItems.Item(1);
            var item = selectedItem.ProjectItem;

            var choiceWindow = new LineEndingChoice(item.Name, this.DefaultLineEnding);
            if (choiceWindow.ShowDialog() == true && choiceWindow.LineEndings != LineEndingsChanger.LineEndings.None)
            {
                var supportedFileFormats = this.SupportedFileFormats;
                var supportedFileNames = this.SupportedFileNames;

                if (item.Name.EndsWithAny(supportedFileFormats) || item.Name.EqualsAny(supportedFileNames))
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        Output("Unifying started...\n");
                        var numberOfChanges = 0;
                        this.changeLog = this.changesManager.GetLastChanges(this.IDE.Solution);
                        var stopWatch = new Stopwatch();
                        stopWatch.Start();
                        UnifyLineEndingsInProjectItem(item, choiceWindow.LineEndings, ref numberOfChanges);
                        stopWatch.Stop();
                        var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                        this.changesManager.SaveLastChanges(this.IDE.Solution, this.changeLog);
                        this.changeLog = null;
                        Output(string.Format("Done in {0} seconds\n", secondsElapsed));
                    });
                }
                else
                {
                }
            }
        }

        private void UnifyLineEndingsInFolderEventHandler(object sender, EventArgs e)
        {
            var selectedItem = IDE.SelectedItems.Item(1);
            var projectItem = selectedItem.ProjectItem;

            var choiceWindow = new LineEndingChoice(selectedItem.Name, this.DefaultLineEnding);
            if (choiceWindow.ShowDialog() == true && choiceWindow.LineEndings != LineEndingsChanger.LineEndings.None)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    Output("Unifying started...\n");
                    var numberOfChanges = 0;
                    this.changeLog = this.changesManager.GetLastChanges(this.IDE.Solution);
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    UnifyLineEndingsInProjectItems(projectItem.ProjectItems, choiceWindow.LineEndings, ref numberOfChanges);
                    stopWatch.Stop();
                    var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                    this.changesManager.SaveLastChanges(this.IDE.Solution, this.changeLog);
                    this.changeLog = null;
                    Output(string.Format("Done in {0} seconds\n", secondsElapsed));
                });
            }
        }

        private void UnifyLineEndingsInProjectEventHandler(object sender, EventArgs e)
        {
            var selectedItem = this.IDE.SelectedItems.Item(1);
            var selectedProject = selectedItem.Project;

            var choiceWindow = new LineEndingChoice(selectedProject.Name, this.DefaultLineEnding);
            if (choiceWindow.ShowDialog() == true && choiceWindow.LineEndings != LineEndingsChanger.LineEndings.None)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    Output("Unifying started...\n");
                    var numberOfChanges = 0;
                    this.changeLog = this.changesManager.GetLastChanges(this.IDE.Solution);
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    UnifyLineEndingsInProjectItems(selectedProject.ProjectItems, choiceWindow.LineEndings, ref numberOfChanges);
                    stopWatch.Stop();
                    var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                    this.changesManager.SaveLastChanges(this.IDE.Solution, this.changeLog);
                    this.changeLog = null;
                    Output(string.Format("Done in {0} seconds\n", secondsElapsed));
                });
            }
        }

        private void UnifyLineEndingsInSolutionEventHandler(object sender, EventArgs e)
        {
            var currentSolution = this.IDE.Solution;

            var properties = currentSolution.Properties;
            foreach (Property property in properties)
            {
                if (property.Name == "Name")
                {
                    var choiceWindow = new LineEndingChoice((property as Property).Value.ToString(), this.DefaultLineEnding);
                    if (choiceWindow.ShowDialog() == true && choiceWindow.LineEndings != LineEndingsChanger.LineEndings.None)
                    {
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            Output("Unifying started...\n");
                            this.changeLog = this.changesManager.GetLastChanges(this.IDE.Solution);
                            var stopWatch = new Stopwatch();
                            stopWatch.Start();
                            var numberOfChanges = 0;
                            foreach (Project project in currentSolution.GetAllProjects())
                            {
                                UnifyLineEndingsInProjectItems(project.ProjectItems, choiceWindow.LineEndings, ref numberOfChanges);
                            }
                            stopWatch.Stop();
                            var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;
                            this.changesManager.SaveLastChanges(this.IDE.Solution, this.changeLog);
                            this.changeLog = null;
                            Output(string.Format("Done in {0} seconds\n", secondsElapsed));
                        });
                    }
                    break;
                }
            }
        }

        private void UnifyLineEndingsInProjectItems(ProjectItems projectItems, LineEndingsChanger.LineEndings lineEndings, ref int numberOfChanges, bool saveAllWasHit = false)
        {
            var supportedFileFormats = this.SupportedFileFormats;
            var supportedFileNames = this.SupportedFileNames;

            foreach (ProjectItem item in projectItems)
            {
                if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                {
                    UnifyLineEndingsInProjectItems(item.ProjectItems, lineEndings, ref numberOfChanges, saveAllWasHit);
                }

                if (item.Name.EndsWithAny(supportedFileFormats) || item.Name.EqualsAny(supportedFileNames))
                {
                    UnifyLineEndingsInProjectItem(item, lineEndings, ref numberOfChanges, saveAllWasHit);
                }
            }
        }

        private void UnifyLineEndingsInProjectItem(ProjectItem item, LineEndingsChanger.LineEndings lineEndings, ref int numberOfChanges, bool saveAllWasHit = false)
        {
            Window documentWindow = null;

            if (!item.IsOpen)
            {
                if (!saveAllWasHit || (saveAllWasHit && !this.OptionsPage.UnifyOnlyOpenFiles))
                {
                    documentWindow = item.Open();
                }
            }

            var document = item.Document;
            if (document != null)
            {
                var numberOfIndividualChanges = 0;
                var numberOfAllLineEndings = 0;

                if (!this.OptionsPage.TrackChanges ||
                    (this.OptionsPage.TrackChanges && this.changeLog != null && (!this.changeLog.ContainsKey(document.FullName) ||
                                                                                 this.changeLog[document.FullName].LineEndings != lineEndings ||
                                                                                 this.changeLog[document.FullName].Ticks < File.GetLastWriteTime(document.FullName).Ticks)))
                {
                    var textDocument = document.Object("TextDocument") as TextDocument;
                    UnifyLineEndingsInDocument(textDocument, lineEndings, ref numberOfChanges, out numberOfIndividualChanges, out numberOfAllLineEndings);
                    if (documentWindow != null || (documentWindow == null && this.OptionsPage.SaveFilesAfterUnifying))
                    {
                        this.isUnifyingLocked = true;
                        document.Save();
                        this.isUnifyingLocked = false;
                    }

                    this.changeLog[document.FullName] = new LastChanges(DateTime.Now.Ticks, lineEndings);

                    Output(string.Format("{0}: changed {1} out of {2} line endings\n", document.FullName, numberOfIndividualChanges, numberOfAllLineEndings));
                }
                else
                {
                    Output(string.Format("{0}: no need to modify this file\n", document.FullName));
                }
            }

            if (documentWindow != null)
            {
                documentWindow.Close();
            }
        }

        private void UnifyLineEndingsInDocument(TextDocument textDocument, LineEndingsChanger.LineEndings lineEndings, ref int numberOfChanges, out int numberOfIndividualChanges, out int numberOfAllLineEndings)
        {
            var startPoint = textDocument.StartPoint.CreateEditPoint();
            var endPoint = textDocument.EndPoint.CreateEditPoint();

            var text = startPoint.GetText(endPoint.AbsoluteCharOffset);
            var originalLength = text.Length;
            if (this.OptionsPage.RemoveTrailingWhitespace)
            {
                text = TrailingWhitespaceRemover.RemoveTrailingWhitespace(text);
            }
            var changedText = LineEndingsChanger.ChangeLineEndings(text, lineEndings, ref numberOfChanges, out numberOfIndividualChanges, out numberOfAllLineEndings);

            if (this.OptionsPage.AddNewlineOnLastLine)
            {
                if (!changedText.EndsWith(Utilities.GetNewlineString(lineEndings)))
                {
                    changedText += Utilities.GetNewlineString(lineEndings);
                }
            }

            startPoint.ReplaceText(originalLength, changedText, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
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

        private Document GetDocumentFromDocCookie(uint docCookie)
        {
            var documentInfo = this.runningDocumentTable.GetDocumentInfo(docCookie);
            return IDE.Documents.Cast<Document>().FirstOrDefault(doc => doc.FullName == documentInfo.Moniker);
        }

        private bool isUnifyingLocked = false;

        private IVsOutputWindow outputWindow;

        private Guid guid;

        private RunningDocumentTable runningDocumentTable;

        private DocumentSaveListener documentSaveListener;

        private ChangesManager changesManager;

        private Dictionary<string, LastChanges> changeLog;

        private LineEndingsChanger.LineEndings DefaultLineEnding
        {
            get { return (LineEndingsChanger.LineEndings)this.OptionsPage.DefaultLineEnding; }
        }

        private string[] SupportedFileFormats
        {
            get { return this.OptionsPage.SupportedFileFormats.Replace(" ", string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); }
        }

        private string[] SupportedFileNames
        {
            get { return this.OptionsPage.SupportedFileNames.Replace(" ", string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); }
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
