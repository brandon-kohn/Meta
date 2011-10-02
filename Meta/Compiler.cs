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
    class Compiler
    {
        private EnvDTE.Project project;
        private VCCompilerHelper clTool;
        private string filename;
        private IVsOutputWindowPane buildPane;
        private TaskTimer taskTimer;
        private System.Diagnostics.Process myProcess;

        public TimeSpan BuildTime
        {
            get { return taskTimer.Mark();  }
        }

        public Compiler(EnvDTE.Project proj, string file, IVsOutputWindowPane pane)
        {
            taskTimer = new TaskTimer("Compile " + file);
            project = proj;
            clTool = new VCCompilerHelper(project);
            filename = file;
            buildPane = pane;            
        }

        public void Cancel()
        {
            if (myProcess != null)
            {
                myProcess.Kill();
            }
        }

        private void Execute( string compiler_binary
                           , string compiler_args
                           , string starting_directory
                           , string source_to_compile)
        {            
            myProcess = new System.Diagnostics.Process();

            try
            {
                string full_args = compiler_binary + " /c /Y- " + source_to_compile.Quote() + " " + compiler_args;

                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.FileName = "cmd.exe";
                //buildPane.OutputStringThreadSafe("Command line: cmd.exe " + full_args + Environment.NewLine);
                myProcess.StartInfo.Arguments = full_args;
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.WorkingDirectory = starting_directory;
                myProcess.StartInfo.RedirectStandardError = true;
                myProcess.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
                myProcess.StartInfo.RedirectStandardOutput = true;
                myProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataHandler);
                taskTimer.StartTask("Compile");
                myProcess.Start();
                myProcess.BeginOutputReadLine();
                myProcess.BeginErrorReadLine();
                myProcess.WaitForExit();
                taskTimer.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                
            }
        }

        private void OutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Collect the net view command output.
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                //buildPane.OutputStringThreadSafe(Environment.NewLine + "  " + outLine.Data);
                //profile_output.WriteLine(outLine.Data);
            }
        }

        private void ErrorDataHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            // Write the error text to the file if there is something
            // to write and an error file has been specified.
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                buildPane.OutputStringThreadSafe(errLine.Data+Environment.NewLine);
                //profile_output.WriteLine(errLine.Data);
            }
        }

        public void Compile()
        {
            try
            {
                VCFile file = clTool.GetVCFile(filename);
                string args = clTool.GenerateCLCmdArgs(filename, false, false, true);
                string clWithEnv = clTool.CompilerExecutableWithEnvAsCmdArgs;

                if (!System.IO.File.Exists(file.FullPath))
                    throw new System.IO.FileNotFoundException(filename);

                string workingDirectory = Path.GetDirectoryName(file.FullPath);
                VCFileConfiguration fileConfig = clTool.GetActiveFileConfiguration(filename);
                if (fileConfig != null)
                {
                    VCCLCompilerTool tool = (VCCLCompilerTool)fileConfig.Tool;
                    if (tool != null)
                    {
                        //var originalOption = tool.GeneratePreprocessedFile;
                        string intermediateDir = clTool.Project.ProjectDirectory + @"\" + fileConfig.Evaluate(VCCompilerHelper.ProjectMacros.IntDir);
                        
                        //! Compile the file.
                        try
                        {
                            Execute(clWithEnv, args, workingDirectory, filename);                            
                        }
                        catch (System.Exception ex)
                        {
                            buildPane.OutputStringThreadSafe(ex.Message);
                            return;
                        }
                        finally
                        {

                        }
                    }
                }
            }
            finally
            {

            }
        }
    }
}
