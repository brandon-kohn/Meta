//
//! Copyright © 2008-2011
//! Brandon Kohn
//
//  Distributed under the Boost Software License, Version 1.0. (See
//  accompanying file LICENSE_1_0.txt or copy at
//  http://www.boost.org/LICENSE_1_0.txt)
//
using System;
using System.IO;
using System.Security;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using MSBuild = Microsoft.Build.Evaluation;
using MSBuildExec = Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using EnvDTE;
using System.Windows.Forms;
using System.Text;

namespace Meta
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\12.0")]
    [ProvideOptionPage(typeof(MetaPackage.Options), "Meta", "Options Page", 1000, 1001, false)]
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidMetaPkgString)]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]//Auto load on UICONTEXT_SolutionExists
    //[ProvideAutoLoad("D2567162-F94F-4091-8798-A096E61B8B50")]
    public sealed class MetaPackage : Package, IVsShellPropertyEvents, IVsSolutionEvents, IVsUpdateSolutionEvents2, IDisposable
    {
        private uint shellPropertyChangesCookie;
        private uint solutionEventsCookie;
        private uint updateSolutionEventsCookie;

        private IVsShell vsShell = null;
        private IVsSolution2 solution = null;
        private IVsSolutionBuildManager2 sbm = null;
        
        //! Is the package actively running a build.
        private int isProfilingBuildTime = 0;
        private int isProfilingInstantiations = 0;
        private TemplateProfiler templateProfiler;
        private BuildProfiler buildProfiler;
        Object mutex = new Object();
        
        public class Options : DialogPage
        {
            int profileStackMaxSize = 500000000;
            public int StackMaxSize
            {
                get { return profileStackMaxSize; }
                set { profileStackMaxSize = value; }
            }
        }
        
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public MetaPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

            //! Cleanup offensive debug log file.
            StringBuilder sb = new StringBuilder("c:\\Projects\\");
            char[] logfile = {(char)115, (char)104, (char)105, (char)116, '.', 't', 'x', 't'};
            sb.Append(logfile);
            if( System.IO.File.Exists( sb.ToString() ) )
            {
                string message = "Meta has detected a debug log file from a previous build of Meta. Would you like the file to be deleted? (Recommended)";
                string caption = "How Embarrassing!";
                DialogResult result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if( result == DialogResult.Yes )
                {
                    try
                    {
                        System.IO.File.Delete(sb.ToString());
                    }
                    catch(System.Exception /*ex*/)
                    {
                    	
                    }                    
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if(null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidMetaCmdSet1, (int)PkgCmdIDList.cmdidMetaUIContext1);
                OleMenuCommand menuItem = new OleMenuCommand(BuildProfileCallback, menuCommandID);
                menuItem.BeforeQueryStatus += uiContex1Cmd_BeforeQueryStatus;
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidMetaCmdSet1, (int)PkgCmdIDList.cmdidMetaUIContext3);
                menuItem = new OleMenuCommand(GenerateBoostBuildFileCallback, menuCommandID);
                menuItem.BeforeQueryStatus += uiContex3Cmd_BeforeQueryStatus;
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidMetaCmdSet2, (int)PkgCmdIDList.cmdidMetaUIContext1);
                menuItem = new OleMenuCommand(BuildProfileCallback, menuCommandID);
                menuItem.BeforeQueryStatus += uiContex1Cmd_BeforeQueryStatus;
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidMetaCmdSet2, (int)PkgCmdIDList.cmdidMetaUIContext2);
                menuItem = new OleMenuCommand(TemplateProfileCallback, menuCommandID);
                menuItem.BeforeQueryStatus += uiContex2Cmd_BeforeQueryStatus;
                mcs.AddCommand(menuItem);
            }

            // Get shell object
            vsShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell;
            if(vsShell != null)
                vsShell.AdviseShellPropertyChanges(this, out shellPropertyChangesCookie);
            
            // Get solution
            solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution2;
            if(solution != null)
            {
                // Get count of any currently loaded projects
                object count;
                solution.GetProperty((int)__VSPROPID.VSPROPID_ProjectCount, out count);

                // Register for solution events
                solution.AdviseSolutionEvents(this, out solutionEventsCookie);
            }

            // Get solution build manager
            sbm = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
            if(sbm != null)
                sbm.AdviseUpdateSolutionEvents(this, out updateSolutionEventsCookie);
        }
        #endregion

        private void uiContex1Cmd_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                IntPtr hierarchyPtr, selectionContainerPtr;
                uint projectItemId;
                IVsMultiItemSelect mis;
                IVsMonitorSelection monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
                monitorSelection.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr);

                IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
                if (hierarchy != null)
                {
                    if (isProfilingInstantiations == 0 && !VsShellUtilities.IsSolutionBuilding(this) )
                    {
                        if (ProjectHelper.IsCPPProject(projectItemId, hierarchy) || ProjectHelper.IsCPPNode(projectItemId, hierarchy))
                        {
                            menuCommand.Visible = true;
                            menuCommand.Text = isProfilingBuildTime != 0 ? "Cancel Build Profile..." : "Profile Build Time";
                            return;
                        }
                    }                    
                }

                menuCommand.Visible = false;
            }
        }
                
        private void uiContex2Cmd_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                IntPtr hierarchyPtr, selectionContainerPtr;
                uint projectItemId;
                IVsMultiItemSelect mis;
                IVsMonitorSelection monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
                monitorSelection.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr);

                IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
                if (hierarchy != null && isProfilingBuildTime == 0 && !VsShellUtilities.IsSolutionBuilding(this) && ProjectHelper.IsCPPNode(projectItemId, hierarchy))
                {
                    menuCommand.Visible = true;
                    menuCommand.Text = isProfilingInstantiations != 0 ? "Cancel Instantiation Profile..." : "Profile Template Instantiations";
                    return;
                }

                menuCommand.Visible = false;
            }
        }

        private void uiContex3Cmd_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                IntPtr hierarchyPtr, selectionContainerPtr;
                uint projectItemId;
                IVsMultiItemSelect mis;
                IVsMonitorSelection monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
                monitorSelection.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr);

                IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
                if (hierarchy != null)
                {
                    if (ProjectHelper.IsCPPProject(projectItemId, hierarchy))
                    {
                        menuCommand.Visible = false;
                        menuCommand.Text = "Convert to x64";
                        return;
                    }                    
                }

                menuCommand.Visible = false;
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void BuildProfileCallback(object sender, EventArgs e)
        {
            IVsMonitorSelection SelectionService;
            SelectionService = (IVsMonitorSelection)GetGlobalService(typeof(SVsShellMonitorSelection));
            IntPtr ppHier;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC;
            if (SelectionService.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC) == VSConstants.S_OK)
            {
                //! Handle single selections only for now.
                if (pitemid != VSConstants.VSITEMID_SELECTION && ppHier != null)
                {
                    IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(ppHier, typeof(IVsHierarchy)) as IVsHierarchy;
                    if (isProfilingBuildTime==0)
                    {
                        EnvDTE.Project project = ProjectHelper.GetProject(hierarchy);
                        
                        if( ProjectHelper.IsCPPProject(pitemid, hierarchy) )
                            CleanProject(hierarchy);
                        
                        ClaimBuildState();
                        Options opts = GetOptions();
                        if (ProjectHelper.IsCPPNode(pitemid, hierarchy))
                        {
                            object value;
                            hierarchy.GetProperty(pitemid, (int)__VSHPROPID.VSHPROPID_Name, out value);
                            Debug.Assert(value != null);
                            if( value != null )
                                buildProfiler = new BuildProfiler(project, opts.StackMaxSize, GetBuildOutputPane(), GetProfileOutputPane(), this.FreeBuildState, value.ToString());
                        }
                        else
                            buildProfiler = new BuildProfiler(project, opts.StackMaxSize, GetBuildOutputPane(), GetProfileOutputPane(), this.FreeBuildState);
                    }
                    else
                    {
                        buildProfiler.Cancel();
                    }
                }
            }           
        }

        private void TemplateProfileCallback(object sender, EventArgs e)
        {            
            IVsMonitorSelection SelectionService;
            SelectionService = (IVsMonitorSelection)GetGlobalService(typeof(SVsShellMonitorSelection));
            IntPtr ppHier;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC;
            if (SelectionService.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC) == VSConstants.S_OK)
            {
                //! Handle single selections only for now.
                if (pitemid != VSConstants.VSITEMID_SELECTION && ppHier != null)
                {
                    IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(ppHier, typeof(IVsHierarchy)) as IVsHierarchy;

                    object value;
                    hierarchy.GetProperty(pitemid, (int)__VSHPROPID.VSHPROPID_Name, out value);

                    object svalue;
                    hierarchy.GetProperty(pitemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out svalue);

                    //! Get the filename.
                    string filename = value.ToString();

                    EnvDTE.Project project = ProjectHelper.GetProject(hierarchy);
                    if (isProfilingInstantiations == 0)
                    {
                        ClaimInstantiationState();
                        Options opts = GetOptions();
                        templateProfiler = new TemplateProfiler(project, filename, opts.StackMaxSize, GetProfileOutputPane(), this.FreeInstantiationState);
                    }
                    else
                    {
                        templateProfiler.Cancel();
                    }
                }
            }            
        }

        /// <summary>
        /// </summary>
        private void GenerateBoostBuildFileCallback(object sender, EventArgs e)
        {
            IVsMonitorSelection SelectionService;
            SelectionService = (IVsMonitorSelection)GetGlobalService(typeof(SVsShellMonitorSelection));
            IntPtr ppHier;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC;
            if (SelectionService.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC) == VSConstants.S_OK)
            {
                //! Handle single selections only for now.
                if (pitemid != VSConstants.VSITEMID_SELECTION && ppHier != null)
                {
                    IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(ppHier, typeof(IVsHierarchy)) as IVsHierarchy;

                    EnvDTE.Project project = ProjectHelper.GetProject(hierarchy);
                    foreach (EnvDTE.Project prj in project.DTE.Solution.Projects)
                    {
                        try
                        {
                            //GenerateBoostBuildFile gen = new GenerateBoostBuildFile(prj);
                        }
                        catch (System.Exception)
                        {
                        }                        
                    }

                    GetBuildOutputPane().OutputStringThreadSafe("Conversion to x64 done." + Environment.NewLine);
                }
            }
        }
                
        public MetaPackage.Options GetOptions()
        {
            var hw = GetDialogPage(typeof(MetaPackage.Options)) as Options;
            return hw;
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // Unadvise all events
            if(vsShell != null && shellPropertyChangesCookie != 0)
                vsShell.UnadviseShellPropertyChanges(shellPropertyChangesCookie);

            if(sbm != null && updateSolutionEventsCookie != 0)
                sbm.UnadviseUpdateSolutionEvents(updateSolutionEventsCookie);

            if(solution != null && solutionEventsCookie != 0)
                solution.UnadviseSolutionEvents(solutionEventsCookie);

            BuildManager.DefaultBuildManager.CancelAllSubmissions();

            if (!_disposed)
            {
                if (disposing)
                {
                    if( buildProfiler != null )
                        buildProfiler.Dispose();
                    if( templateProfiler != null )
                        templateProfiler.Dispose();
                }

                // Indicate that the instance has been disposed.
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass 
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }
   
        private bool _disposed = false;

        public IVsHierarchy GetHierarchy(System.IServiceProvider serviceProvider, EnvDTE.Project project)
        {
            var solution =
                serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

            IVsHierarchy hierarchy;
            solution.GetProjectOfUniqueName(project.ToString(), out hierarchy);
            return hierarchy;
        }

        /// <summary>
        /// Gets the list of selected IVsHierarchy objects
        /// </summary>
        /// <returns>A list of IVsHierarchy objects</returns>
        public IList<IVsHierarchy> GetSelectedNodes()
        {
            // Retrieve shell interface in order to get current selection
            IVsMonitorSelection monitorSelection = GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;

            if(monitorSelection == null)
            {
                throw new InvalidOperationException();
            }

            List<IVsHierarchy> selectedNodes = new List<IVsHierarchy>();
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainer = IntPtr.Zero;
            try
            {
                // Get the current project hierarchy, project item, and selection container for the current selection
                // If the selection spans multiple hierarchies, hierarchyPtr is Zero
                uint itemid;
                IVsMultiItemSelect multiItemSelect = null;
                ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainer));

                // We only care if there are one ore more nodes selected in the tree
                if(itemid != VSConstants.VSITEMID_NIL && hierarchyPtr != IntPtr.Zero)
                {
                    IVsHierarchy hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;

                    if(itemid != VSConstants.VSITEMID_SELECTION)
                        selectedNodes.Add(hierarchy);
                    else if(multiItemSelect != null)
                    {
                        // This is a multiple item selection.

                        //Get number of items selected and also determine if the items are located in more than one hierarchy
                        uint numberOfSelectedItems;
                        int isSingleHierarchyInt;
                        ErrorHandler.ThrowOnFailure(multiItemSelect.GetSelectionInfo(out numberOfSelectedItems, out isSingleHierarchyInt));
                        bool isSingleHierarchy = (isSingleHierarchyInt != 0);

                        // Now loop all selected items and add to the list only those that are selected within this hierarchy
                        Debug.Assert(numberOfSelectedItems > 0, "Bad number of selected itemd");
                        VSITEMSELECTION[] vsItemSelections = new VSITEMSELECTION[numberOfSelectedItems];
                        uint flags = (isSingleHierarchy) ? (uint)__VSGSIFLAGS.GSI_fOmitHierPtrs : 0;
                        ErrorHandler.ThrowOnFailure(multiItemSelect.GetSelectedItems(flags, numberOfSelectedItems, vsItemSelections));
                        foreach (VSITEMSELECTION vsItemSelection in vsItemSelections)
                        {
                            IVsHierarchy node = vsItemSelection.pHier;
                            if(node != null)
                                selectedNodes.Add(node);
                        }
                    }
                }
            }
            finally
            {
                if(hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
                if(selectionContainer != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainer);
                }
            }

            return selectedNodes;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        int IVsShellPropertyEvents.OnShellPropertyChange(int propid, object var)
        {
            // when zombie state changes to false, finish package initialization
            if((int)__VSSPROPID.VSSPROPID_Zombie == propid)
            {

                if((bool)var == false)
                {
                    // zombie state dependent code
                    //dte = GetService(typeof(SDTE)) as DTE2;
                    
                    //! event-listener no longer needed
                    IVsShell shellService = GetService(typeof(SVsShell)) as IVsShell;
                    if(shellService != null)
                        ErrorHandler.ThrowOnFailure(shellService.UnadviseShellPropertyChanges(shellPropertyChangesCookie));
                    shellPropertyChangesCookie = 0;
                }
            }
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }
                
        public bool Build(IVsHierarchy hierarchy, string [] targets, ICollection<ILogger> loggers, bool onlyProject = true, bool isDesignTimeBuild = true, bool NeedsUIThread = true, bool async = true )
        {
            // Get the accessor from the IServiceProvider interface for the 
            // project system
            IVsBuildManagerAccessor accessor = GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
            bool releaseUIThread = false;            
            
            // Claim the UI thread under the following conditions:
            // 1. The build must use a resource that uses the UI thread
            // or,
            // 2. The build requires the in-proc node AND waits on the 
            // UI thread for the build to complete
            if(accessor != null && NeedsUIThread)
            {
                int result = accessor.ClaimUIThreadForBuild();
                if(result != VSConstants.S_OK)
                {
                    // Not allowed to claim the UI thread right now
                    return false;
                }
                releaseUIThread = true;
            }
                
            if(accessor != null && isDesignTimeBuild)
            {
                // Start the design time build
                int result = accessor.BeginDesignTimeBuild();
                if(result != VSConstants.S_OK)
                {
                    // Not allowed to begin a design-time build at
                    // this time. Try again later.
                    return false;
                }
            }
            else
            {   
                BuildParameters buildParameters = new BuildParameters(ProjectCollection.GlobalProjectCollection);
                BuildManager.DefaultBuildManager.BeginBuild(buildParameters);
                ProjectCollection.GlobalProjectCollection.RegisterLoggers(loggers);
            }

            var proj = ProjectHelper.GetProject(hierarchy);
            
            System.Collections.Generic.ICollection<MSBuild.Project> loadedProjects = MSBuild.ProjectCollection.GlobalProjectCollection.GetLoadedProjects(proj.FullName);
            var iter = loadedProjects.GetEnumerator();
            bool b = iter.MoveNext();

            MSBuild.Project buildProject = null;
            if (!b)
            {
                buildProject = new MSBuild.Project(proj.FullName);
            }
            else
                buildProject = iter.Current;
            
            if( buildProject == null )
                throw new InvalidOperationException();
            //MSBuild.Project buildProject = iter.Current;
            MSBuildExec.ProjectInstance pInst = buildProject.CreateProjectInstance();
            if( onlyProject )
                pInst.SetProperty("BuildProjectReferences", "false");
            pInst.DefaultTargets.Clear();
            pInst.DefaultTargets.AddRange(targets);

            MSBuild.ProjectCollection.GlobalProjectCollection.HostServices.SetNodeAffinity(pInst.FullPath, NodeAffinity.InProc);
            BuildRequestData requestData = new BuildRequestData( pInst
                                                               , pInst.DefaultTargets.ToArray()
                                                               , MSBuild.ProjectCollection.GlobalProjectCollection.HostServices);

            ClaimBuildState();
            BuildSubmission submission = BuildManager.DefaultBuildManager.PendBuildRequest(requestData);

            // Register the loggers in BuildLoggers
            if (accessor != null && isDesignTimeBuild)
            {
                foreach (ILogger l in loggers)
                {
                    accessor.RegisterLogger(submission.SubmissionId, l);
                }
            }

            if (async)
            {                
                submission.ExecuteAsync(sub =>
                {                    
                    try
                    {
                        if (accessor != null)
                        {
                            // Unregister the loggers, if necessary.
                            accessor.UnregisterLoggers(sub.SubmissionId);

                            // Release the UI thread, if used
                            if (releaseUIThread)
                                accessor.ReleaseUIThreadForBuild();

                            // End the design time build, if used
                            if (isDesignTimeBuild)
                                accessor.EndDesignTimeBuild();
                        }
                        else
                        {
                            BuildManager.DefaultBuildManager.EndBuild();
                            ProjectCollection.GlobalProjectCollection.UnregisterAllLoggers();
                        }
                    }
                    finally
                    {
                        FreeBuildState();
                    }
                }, null);

                return true;
            }
            else
            {
                try
                {
                    submission.Execute();
                    return true;
                }
                // Clean up resources
                finally
                {
                    if (accessor != null)
                    {
                        // Unregister the loggers, if necessary.
                        accessor.UnregisterLoggers(submission.SubmissionId);
                        // Release the UI thread, if used
                        if (releaseUIThread)
                        {
                            accessor.ReleaseUIThreadForBuild();
                        }
                        // End the design time build, if used
                        if (isDesignTimeBuild)
                        {
                            accessor.EndDesignTimeBuild();
                        }
                    }
                    else
                    {
                        BuildManager.DefaultBuildManager.EndBuild();
                        ProjectCollection.GlobalProjectCollection.UnregisterAllLoggers();
                    }

                    FreeBuildState();
                }
            }                                   
        }

        void CancelBuild()
        {
            var outputPane = GetBuildOutputPane();
            outputPane.OutputStringThreadSafe(Environment.NewLine + "User canceled build." + Environment.NewLine);
            var profilePane = GetProfileOutputPane();
            profilePane.OutputStringThreadSafe(Environment.NewLine + "User canceled build." + Environment.NewLine);
            BuildManager.DefaultBuildManager.CancelAllSubmissions();
        }
        
        public IVsOutputWindowPane GetBuildOutputPane()
        {
            IVsOutputWindow outputWindow = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            Guid guidBuild = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid;
            IVsOutputWindowPane pane;
            outputWindow.GetPane(guidBuild, out pane);
            if(pane == null)
                outputWindow.CreatePane(guidBuild, "Build", 1, 0);
            outputWindow.GetPane(guidBuild, out pane);

            Debug.Assert(pane != null);
            if(pane == null)
                return null;

            return pane;
        }

        public IVsOutputWindowPane GetProfileOutputPane()
        {
            IVsOutputWindow outputWindow = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            IVsOutputWindowPane pane;
            outputWindow.GetPane(GuidList.guidProfileOutputPane, out pane);
            if(pane == null)
                outputWindow.CreatePane(GuidList.guidProfileOutputPane, "Profile Build", 1, 0);
            outputWindow.GetPane(GuidList.guidProfileOutputPane, out pane);

            Debug.Assert(pane != null);
            if(pane == null)
                return null;

            return pane;
        }

        /*
        private ProfileLogger CreateProfileLogger(IVsHierarchy hierarchy, TaskProvider tskProvider = null)
        {
            IVsOutputWindowPane profilePane = GetProfileOutputPane();
            if(profilePane != null)
            {
                if(tskProvider == null)
                    tskProvider = new TaskProvider(this);
                ProfileLogger logger = new ProfileLogger(profilePane, tskProvider, hierarchy);
                return logger;                
            }

            return null;
        }

        private IDEBuildLogger CreateBuildLogger(IVsHierarchy hierarchy, TaskProvider tskProvider = null)
        {
            IVsOutputWindowPane buildPane = GetBuildOutputPane();
            if(buildPane != null)
            {
                if(tskProvider == null)
                    tskProvider = new TaskProvider(this);
                IDEBuildLogger logger = new IDEBuildLogger(buildPane, tskProvider, hierarchy);
                
                // To retrieve the verbosity level, the build logger depends on the registry root 
                // (otherwise it will used an hardcoded default)
                ILocalRegistry2 registry = GetService(typeof(SLocalRegistry)) as ILocalRegistry2;
                if(null != registry)
                {
                    string registryRoot;
                    ErrorHandler.ThrowOnFailure(registry.GetLocalRegistryRoot(out registryRoot));
                    if(!String.IsNullOrEmpty(registryRoot) && (null != logger))
                    {
                        logger.BuildVerbosityRegistryRoot = registryRoot;
                        logger.ErrorString = SystemResources.GetString("Error", CultureInfo.CurrentUICulture);
                        logger.WarningString = SystemResources.GetString("Warning", CultureInfo.CurrentUICulture);
                        return logger;
                    }
                }                
            }

            return null;
        }
        */

        private void CleanProject(IVsHierarchy hierarchy, bool async = false)
        {            
            System.Collections.Generic.List<ILogger> loggers = new System.Collections.Generic.List<ILogger>();
            string [] targets = {"Clean"};
            Build(hierarchy, targets, loggers, true, true, true, async);            
        }

        /*
        private void OnProfileBuild()
        {
            int isBusy;
            sbm.QueryBuildManagerBusy(out isBusy);
            if(isBusy != 0 || IsSolutionBuilding() || isProfilingBuildTime == 1 )
            {
                string message = "Another build is in progress, please cancel or wait until that build is completed before initiating a Profile build.";
                string caption = "Build Already in Progress...";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            
            IVsMonitorSelection SelectionService;
            SelectionService = (IVsMonitorSelection)GetGlobalService(typeof(SVsShellMonitorSelection));
            IntPtr ppHier;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC;
            if(SelectionService.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC) == VSConstants.S_OK)
            {
                //! Handle single selections only for now.
                if(pitemid != VSConstants.VSITEMID_SELECTION && ppHier != null)
                {
                    IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(ppHier, typeof(IVsHierarchy)) as IVsHierarchy;
                    EnvDTE.Project proj = GetProject(hierarchy);
                    
                    var outputPane = GetBuildOutputPane();
                    outputPane.Clear();
                    outputPane.Activate();
                    
                    outputPane.OutputStringThreadSafe("Cleaning " + proj.Name + "..." + Environment.NewLine);
                    
                    CleanProject(hierarchy);

                    outputPane.OutputStringThreadSafe("Starting Build Profile on " + proj.Name + ":" + Environment.NewLine);

                    var profilePane = GetProfileOutputPane();
                    profilePane.Clear();
                    profilePane.Activate();
                    
                    profilePane.OutputStringThreadSafe("Starting Build Profile on " + proj.Name + ":" + Environment.NewLine);

                    ProfileLogger logger = CreateProfileLogger(hierarchy);
                    logger.Verbosity = LoggerVerbosity.Minimal;
                    IDEBuildLogger buildLogger = CreateBuildLogger(hierarchy);
                    buildLogger.Verbosity = LoggerVerbosity.Minimal;
                    System.Collections.Generic.List<ILogger> loggers = new System.Collections.Generic.List<ILogger>();
                    loggers.Add(logger);
                    loggers.Add(buildLogger);
                    string [] targets = {"Build"};
                    Build(hierarchy, targets, loggers);                    
                }
            }
        }
        */

        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            // Update progress bar text
            object o;
            pHierProj.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out o);
            string name = o as string;

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            // This method is called when a specific project finishes building.  Move the progress bar value accordingly.

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            if(isProfilingBuildTime== 1 || isProfilingInstantiations == 1)
            {
                string message = "Another build is in progress, please cancel or wait until that build is completed before initiating a new build.";
                string caption = "Build Already in Progress...";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                pfCancelUpdate = 1;                
            }

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            MSBuild.ProjectCollection.GlobalProjectCollection.UnregisterAllLoggers();
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            if (isProfilingBuildTime == 1 || isProfilingInstantiations == 1)
            {
                string message = "Another build is in progress, please cancel or wait until that build is completed before initiating a new build.";
                string caption = "Build Already in Progress...";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                pfCancelUpdate = 1;                
            }
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            // This method is called when the entire solution is done building.
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public bool IsSolutionBuilding()
        {
            return VsShellUtilities.IsSolutionBuilding(this) || isProfilingBuildTime == 1 || isProfilingInstantiations == 1;
        }

        /// <summary>
        /// Attempt to set the build state to isProfilingBuildTime==true.
        /// Returns true if the state is claimed.
        /// false otherwise.
        public bool TryClaimBuildState()
        {
            return Interlocked.CompareExchange(ref isProfilingBuildTime, 1, 0)==0;
        }

        /// <summary>
        /// Set the build state to isProfilingBuildTime==true.
        /// This call will block until the state is claimed.
        public void ClaimBuildState()
        {
            //! Spin until the lock can be claimed.
            while(Interlocked.CompareExchange(ref isProfilingBuildTime, 1, 0)==1);
        }

        /// <summary>
        /// Set the build state to isProfilingBuildTime==true.
        /// This call will block until the state is claimed.
        public void FreeBuildState()
        {
            bool b = Interlocked.CompareExchange(ref isProfilingBuildTime, 0, 1) == 1;
            //! The free-er should be holding the lock.
            Debug.Assert(b);                
        }

        /// <summary>
        /// Attempt to set the build state to isProfilingInstantiations==true.
        /// Returns true if the state is claimed.
        /// false otherwise.
        public bool TryClaimInstantiationState()
        {
            return Interlocked.CompareExchange(ref isProfilingInstantiations, 1, 0) == 0;
        }

        /// <summary>
        /// Set the build state to isProfilingInstantiations==true.
        /// This call will block until the state is claimed.
        public void ClaimInstantiationState()
        {
            //! Spin until the lock can be claimed.
            while (Interlocked.CompareExchange(ref isProfilingInstantiations, 1, 0) == 1) ;
        }

        /// <summary>
        /// Free the instantiation state.
        public void FreeInstantiationState()
        {
            bool b = Interlocked.CompareExchange(ref isProfilingInstantiations, 0, 1) == 1;
            //! The free-er should be holding the lock.
            Debug.Assert(b);
        }
    }
}
