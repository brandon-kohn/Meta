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
    public delegate void TaskFinishedCallback();

    class TemplateProfiler
    {
        private IActiveObject profiler;
        private EnvDTE.Project project;
        VCCompilerHelper clTool;
        private string filename;
        private IVsOutputWindowPane profilePane;
        private StreamWriter profile_output;
        private Action signalFinished;
        private System.Diagnostics.Process myProcess;
        private volatile bool cancelProfile = false;

        public TemplateProfiler(EnvDTE.Project proj, string file, IVsOutputWindowPane pane, Action onFinished )
        {
            project = proj;
            clTool = new VCCompilerHelper(project);
            filename = file;
            profilePane = pane;
            profiler = new ActiveObject(500000000);
            signalFinished = onFinished;
            Initialize();
            profiler.Signal();
        }

        public void Cancel()
        {
            cancelProfile = true;
            if (myProcess != null)
                myProcess.Kill();
        }

        public void Initialize()
        {
            profiler.Initialize("TemplateProfiler", Execute);
        }

        public void Instrument( string compiler_binary
                              , string compiler_args
                              , string starting_directory
                              , string source_to_instrument
                              , string output )
        {
            myProcess = new System.Diagnostics.Process();

            try
            {
                string full_args = compiler_binary + " " + source_to_instrument.Quote() + " /P /Fi" + output.Quote() + " " + compiler_args;

                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.FileName = "cmd.exe";
                //profilePane.OutputStringThreadSafe("Command line: cmd.exe " + full_args + Environment.NewLine);
                myProcess.StartInfo.Arguments = full_args;
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.WorkingDirectory = starting_directory;
                //System.Collections.Specialized.StringDictionary dict = myProcess.StartInfo.EnvironmentVariables;
                //dict["Path"] += (";" + clTool.VSInstallDir + "Common7\\IDE");
                //myProcess.StartInfo.RedirectStandardError = true;
                //myProcess.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
                //myProcess.StartInfo.RedirectStandardOutput = true;
                //myProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataHandler);
                if (cancelProfile)
                    return;
                myProcess.Start();
                //myProcess.BeginOutputReadLine();
                //myProcess.BeginErrorReadLine();
                myProcess.WaitForExit();
                // This code assumes the process you are starting will terminate itself. 
                // Given that is is started without a window so you cannot terminate it 
                // on the desktop, it must terminate itself or you can do it programmatically
                // from this application using the Kill method.
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
            }
        }
        
        public void Profile( string compiler_binary
                           , string compiler_args
                           , string starting_directory
                           , string source_to_profile
                           , string output_profile )
        {
            profile_output = new StreamWriter(output_profile);

            myProcess = new System.Diagnostics.Process();

            try
            {
                string full_args = compiler_binary + " /c " + source_to_profile.Quote() + " " + compiler_args;
                
                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.FileName = "cmd.exe";
                //profilePane.OutputStringThreadSafe("Command line: cmd.exe " + full_args + Environment.NewLine);
                myProcess.StartInfo.Arguments = full_args;
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.WorkingDirectory = starting_directory;
                //System.Collections.Specialized.StringDictionary dict = myProcess.StartInfo.EnvironmentVariables;
                //dict["Path"] += (";" + clTool.VSInstallDir + "Common7\\IDE");
                myProcess.StartInfo.RedirectStandardError = true;
                myProcess.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
                myProcess.StartInfo.RedirectStandardOutput = true;
                myProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataHandler);
                if (cancelProfile)
                    return;
                myProcess.Start();
                myProcess.BeginOutputReadLine();
                myProcess.BeginErrorReadLine();
                myProcess.WaitForExit();
                // This code assumes the process you are starting will terminate itself. 
                // Given that is is started without a window so you cannot terminate it 
                // on the desktop, it must terminate itself or you can do it programmatically
                // from this application using the Kill method.
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                profile_output.Close();
            }
        }

        private void OutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Collect the net view command output.
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                //profilePane.OutputStringThreadSafe(Environment.NewLine + "  " + outLine.Data);
                profile_output.WriteLine(outLine.Data);
            }
        }

        private void ErrorDataHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            // Write the error text to the file if there is something
            // to write and an error file has been specified.
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                profilePane.OutputStringThreadSafe(errLine.Data);
                profile_output.WriteLine(errLine.Data);
            }
        }

        private void Execute()
        {
            try
            {
                profilePane.Clear();
                profilePane.OutputStringThreadSafe("Profiling Instantiations on " + filename + ":" + Environment.NewLine + Environment.NewLine);
                profilePane.Activate();
                VCFile file = clTool.GetVCFile(filename);
                string preprocessorArgs = clTool.GenerateCLCmdArgs(filename, false);
                string profileArgs = clTool.GenerateCLCmdArgs(filename, true);
                string clWithEnv = clTool.CompilerExecutableWithEnvAsCmdArgs;

                if (!System.IO.File.Exists(file.FullPath))
                    return;

                string workingDirectory = Path.GetDirectoryName(file.FullPath);
                VCFileConfiguration fileConfig = clTool.GetActiveFileConfiguration(filename);
                if (fileConfig != null)
                {
                    VCCLCompilerTool tool = (VCCLCompilerTool)fileConfig.Tool;
                    if (tool != null)
                    {
                        //var originalOption = tool.GeneratePreprocessedFile;
                        string intermediateDir = clTool.Project.ProjectDirectory + @"\" + fileConfig.Evaluate(VCCompilerHelper.ProjectMacros.IntDir);
                        string outputPreprocessed = intermediateDir + @"\" + Path.GetFileNameWithoutExtension(filename) + ".instrumented";
                        string outputPreprocessedCpp = workingDirectory + @"\" + Path.GetFileNameWithoutExtension(filename) + ".instrumented.cpp";
                        string outputProfile = intermediateDir + "\\" + Path.GetFileNameWithoutExtension(filename) + ".template.profile";

                        //! Preprocess the file.
                        try
                        {
                            profilePane.OutputStringThreadSafe("Instrumenting Code..." + Environment.NewLine);
                            Instrument(clWithEnv, preprocessorArgs, workingDirectory, filename, outputPreprocessed);
                            if (cancelProfile)
                            {
                                profilePane.OutputStringThreadSafe(Environment.NewLine + "User canceled profile." + Environment.NewLine);
                                return;
                            }
                            if (!File.Exists(outputPreprocessed))
                                throw new FileNotFoundException(outputPreprocessed);
                            NativeMethods.TemplateProfilePreprocess(outputPreprocessed, outputPreprocessedCpp);
                        }
                        catch( FileNotFoundException ex )
                        {
                            profilePane.OutputStringThreadSafe("Unable to preprocess " + filename + ". Please check that the file compiles and try again." + Environment.NewLine );
                            return;
                        }
                        catch (System.Exception ex)
                        {
                            profilePane.OutputStringThreadSafe(ex.Message);
                            return;
                        }
                        finally
                        {
                            File.Delete(outputPreprocessed);
                        }

                        //! Now compile the output and put the output into another file to be input to the postprocessor.
                        try
                        {
                            profilePane.OutputStringThreadSafe("Running Profile..." + Environment.NewLine);
                            Profile(clWithEnv, profileArgs, workingDirectory, outputPreprocessedCpp, outputProfile);
                            if (cancelProfile)
                            {
                                profilePane.OutputStringThreadSafe(Environment.NewLine + "User canceled profile." + Environment.NewLine);
                                return;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            profilePane.OutputStringThreadSafe(ex.Message);
                            return;
                        }
                        finally
                        {
                            File.Delete(outputPreprocessedCpp);
                            File.Delete(outputPreprocessed + ".obj");
                        }

                        try
                        {
                            profilePane.OutputStringThreadSafe("Finalizing Data..." + Environment.NewLine);
                            IntStringDelegate log = new IntStringDelegate(profilePane.OutputStringThreadSafe);
                            NativeMethods.TemplateProfilePostProcess(outputProfile, log);
                            if (cancelProfile)
                            {
                                profilePane.OutputStringThreadSafe(Environment.NewLine + "User canceled profile." + Environment.NewLine);
                                return;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            profilePane.OutputStringThreadSafe(ex.Message);
                        }
                        finally
                        {
                            File.Delete(outputProfile);
                        }
                    }
                }
            }
            finally
            {
                signalFinished();
            }
        }
    }
}
