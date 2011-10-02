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
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using MSBuild = Microsoft.Build.Evaluation;
using MSBuildExec = Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using EnvDTE;
using System.Windows.Forms;

namespace Meta
{
    class BuildProfiler
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

        public BuildProfiler(EnvDTE.Project proj, IVsOutputWindowPane bpane, IVsOutputWindowPane ppane, Action onFinished, string singleFile = null)
        {
            if (singleFile != null)
                onlyFile = singleFile;
            project = proj;
            clTool = new VCCompilerHelper(project);
            profilePane = ppane;
            buildPane = bpane;
            profiler = new ActiveObject(500000000);
            signalFinished = onFinished;
            Initialize();
            profiler.Signal();
        }

        public void Initialize()
        {
            profiler.Initialize("BuildProfiler", Execute);
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
                buildPane.Clear();
                buildPane.Activate();
                    
                if( onlyFile == null )
                    buildPane.OutputStringThreadSafe("Starting Build Profile on " + project.Name + " (" + clTool.ActiveConfiguration.Name + "):" + Environment.NewLine);
                else
                    buildPane.OutputStringThreadSafe("Starting Build Profile on " + project.Name + "\\" + onlyFile + " (" + clTool.ActiveConfiguration.Name + "):" + Environment.NewLine);

                profilePane.Clear();
                profilePane.Activate();

                if (onlyFile == null)
                    profilePane.OutputStringThreadSafe("Starting Build Profile on " + project.Name + " (" + clTool.ActiveConfiguration.Name + "):" + Environment.NewLine);
                else
                    profilePane.OutputStringThreadSafe("Starting Build Profile on " + project.Name + "\\" + onlyFile + " (" + clTool.ActiveConfiguration.Name + "):" + Environment.NewLine);

                foreach (EnvDTE.ProjectItem i in project.ProjectItems)
                {
                    if (cancelBuild)
                    {
                        profilePane.OutputStringThreadSafe(Environment.NewLine + "User canceled build profile." + Environment.NewLine);
                        buildPane.OutputStringThreadSafe(Environment.NewLine + "User canceled build profile." + Environment.NewLine);
                        return;
                    }

                    if (!i.Saved)
                        i.Save("");
                }

                VCFile singleFile = null;
                if (onlyFile != null)
                    singleFile = clTool.GetVCFile(onlyFile);

                TaskTimer job = new TaskTimer("Total Build");
                job.StartTask("Total Build Time");

                profilePane.OutputStringThreadSafe(Environment.NewLine + String.Format("{0,-60}: {1,12}", "Translation Unit", "Compile Time (hh:mm:ss:ms)") + Environment.NewLine);
                StringBuilder divider = new StringBuilder(88);
                divider.Append('-', 88);
                profilePane.OutputStringThreadSafe(divider + Environment.NewLine + Environment.NewLine);

                if (singleFile == null)
                {
                    foreach (VCFile file in clTool.Project.GetFilesWithItemType("CLCompile"))
                    {
                        if (cancelBuild)
                        {
                            profilePane.OutputStringThreadSafe(Environment.NewLine + "User canceled build profile." + Environment.NewLine);
                            buildPane.OutputStringThreadSafe(Environment.NewLine + "User canceled build profile." + Environment.NewLine);
                            return;
                        }

                        cl = new Compiler(project, file.ItemName, buildPane);
                        cl.Compile();
                        TimeSpan ts = cl.BuildTime;
                        data.Insert(ts, file.ItemName);
                        string elapsedTime = String.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                        profilePane.OutputStringThreadSafe(String.Format("{0,-60}: {1,12}" + Environment.NewLine, file.ItemName, elapsedTime));
                    }
                }
                else
                {
                    if (cancelBuild)
                    {
                        profilePane.OutputStringThreadSafe(Environment.NewLine + "User canceled build profile." + Environment.NewLine);
                        buildPane.OutputStringThreadSafe(Environment.NewLine + "User canceled build profile." + Environment.NewLine);
                        return;
                    }

                    cl = new Compiler(project, singleFile.ItemName, buildPane);
                    cl.Compile();
                    TimeSpan ts = cl.BuildTime;
                    data.Insert(ts, singleFile.ItemName);
                    string elapsedTime = String.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                    profilePane.OutputStringThreadSafe(String.Format("{0,-60}: {1,12}" + Environment.NewLine, singleFile.ItemName, elapsedTime));
                }
                job.Stop();

                profilePane.OutputStringThreadSafe
                    (
                        Environment.NewLine + Environment.NewLine + "Summary: " + 
                        Environment.NewLine + Environment.NewLine + "Total Build RunTime: " + job.Elapsed() +
                        Environment.NewLine + String.Format("{0,-60}: {1,12}", "Translation Unit", "Compile Time (hh:mm:ss:ms)") + Environment.NewLine +
                        divider + Environment.NewLine + Environment.NewLine
                    );
                var it = data.RBegin;
                while (it.MovePrev())
                {
                    TimeSpan ts = it.Current.Key;
                    string name = it.Current.Value;
                    string elapsedTime = String.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                    profilePane.OutputStringThreadSafe(String.Format("{0,-60}: {1,12}" + Environment.NewLine, name, elapsedTime));
                }
            }
            finally
            {                
                signalFinished();
            }
        }
    }
}
