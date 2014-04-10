//
//! Copyright © 2008-2011
//! Brandon Kohn
//
//  Distributed under the Boost Software License, Version 1.0. (See
//  accompanying file LICENSE_1_0.txt or copy at
//  http://www.boost.org/LICENSE_1_0.txt)
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;
using System.Windows.Forms;

namespace Meta
{
    class HeaderProfiler : IDisposable
    {
        private IActiveObject profiler;
        private EnvDTE.Project project;
        private VCCompilerHelper clTool;
        private IVsOutputWindowPane profilePane; 
        private IVsOutputWindowPane buildPane;
        private Action signalFinished;
        private MultiMap<TimeSpan, string> data = new MultiMap<TimeSpan,string>();
        private volatile bool cancelBuild = false;
        private string onlyFile = null;
        Compiler cl;

        public HeaderProfiler(EnvDTE.Project proj, int stackMaxSize, string singleFile, IVsOutputWindowPane ppane, IVsOutputWindowPane bpane, Action onFinished)
        {
            onlyFile = singleFile;
            project = proj;
            clTool = new VCCompilerHelper(project);
            profilePane = ppane;
            buildPane = bpane;
            profiler = new ActiveObject(stackMaxSize);
            signalFinished = onFinished;
            Initialize();
            profiler.Signal();
        }

        public void Initialize()
        {
            profiler.Initialize("HeaderProfiler", Execute);
        }

        public void Cancel()
        {
            cancelBuild = true;
            if (cl != null)
                cl.Cancel();
        }

        private void Execute()
        {
            try
            {
                VCFile singleFile = clTool.GetVCFile(onlyFile);
                
                profilePane.Clear();
                profilePane.Activate();

                profilePane.OutputStringThreadSafe("Starting Include Profile on " + singleFile.Name + " (" + clTool.ActiveConfiguration.Name + "):" + Environment.NewLine);
                
                foreach (EnvDTE.ProjectItem i in project.ProjectItems)
                {
                    if (cancelBuild)
                    {
                        profilePane.OutputStringThreadSafe(Environment.NewLine + "User canceled Include Profile." + Environment.NewLine);
                        return;
                    }

                    if (!i.Saved)
                        i.Save("");
                }
                                                
                StringBuilder divider = new StringBuilder(88);
                divider.Append('-', 88);
                profilePane.OutputStringThreadSafe(divider + Environment.NewLine + Environment.NewLine);
                profilePane.OutputStringThreadSafe(singleFile.FullPath + Environment.NewLine);

                if (cancelBuild)
                {
                    profilePane.OutputStringThreadSafe(Environment.NewLine + "User canceled Include Profile." + Environment.NewLine);
                    return;
                }

                if (System.IO.File.Exists(singleFile.FullPath))
                {
                    try
                    {
                        cl = new Compiler(project, singleFile.ItemName, new IncludeProfileCompileLogger(buildPane), new IncludeProfileCompileLogger(profilePane));
                        cl.ShowIncludes = true;
                        cl.Compile();
                    }
                    finally
                    {

                    }
                }
                else
                    profilePane.OutputStringThreadSafe(Environment.NewLine + "File not found: " + singleFile.ItemName);
            }
            catch (System.Exception ex)
            {
                profilePane.OutputStringThreadSafe(ex.Message);
            }
            finally
            {                
                signalFinished();
            }
        }

        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass 
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if( profiler != null )
                        profiler.Dispose();
                    if( cl != null )
                        cl.Dispose();
                }

                // Indicate that the instance has been disposed.
                _disposed = true;
            }
        }

        private bool _disposed = false;
    }
}
