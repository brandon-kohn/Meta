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
using System.Linq;
using System.Text;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;

namespace Meta
{
    class VCCompilerHelper
    {
        public class ProjectMacros
        {
            public static readonly string RemoveMachine = "$(RemoteMachine)";//! Set to the value of the Remote Machine property on the Debug property page. See Changing Project Settings for a C/C++ Debug Configuration for more information.
            public static readonly string ConfigurationName = "$(ConfigurationName)";//! 	The name of the current project configuration (for example, "Debug").
            public static readonly string PlatformName = "$(PlatformName)";//! 	The name of current project platform (for example, "Win32").
            public static readonly string PlatformToolsetVersion = "$(PlatformToolsetVersion)";//! 	The version of the platform toolset.
            public static readonly string Inherit = "$(Inherit)";//! 	Specifies the order in which inherited properties appear in the command line composed by the project build system. By default, inherited properties appear at the end of the current property.1
            public static readonly string NoInherit = "$(NoInherit)";//! 	Causes any properties that would otherwise be inherited, to not be inherited. The use of $(NoInherit) causes any occurrences of $(Inherit) to be ignored for the same property.1
            public static readonly string ParentName = "$(ParentName)";//! 	Name of the item containing this project item. This will be the parent folder name, or project name.
            public static readonly string RootNameSpace = "$(RootNameSpace)";//! 	The namespace, if any, containing the application.
            public static readonly string IntDir = "$(IntDir)";//! 	Path to the directory specified for intermediate files relative to the project directory. This resolves to the value for the Intermediate Directory property.
            public static readonly string OutDir = "$(OutDir)";//! 	Path to the output file directory, relative to the project directory. This resolves to the value for the Output Directory property.
            public static readonly string DevEnvDir = "$(DevEnvDir)";//! 	The installation directory of Visual Studio .NET (defined as drive + path); includes the trailing backslash '\'.
            public static readonly string InputDir = "$(InputDir)";//! 	The directory of the input file (defined as drive + path); includes the trailing backslash '\'. If the project is the input, then this macro is equivalent to $(ProjectDir).
            public static readonly string InputPath = "$(InputPath)";//! 	The absolute path name of the input file (defined as drive + path + base name + file extension). If the project is the input, then this macro is equivalent to $(ProjectPath).
            public static readonly string InputName = "$(InputName)";//! 	The base name of the input file. If the project is the input, then this macro is equivalent to $(ProjectName).
            public static readonly string InputFileName = "$(InputFileName)";//! 	The file name of the input file (defined as base name + file extension). If the project is the input, then this macro is equivalent to $(ProjectFileName).
            public static readonly string InputExt = "$(InputExt)";//! 	The file extension of the input file. It includes the '.' before the file extension. If the project is the input, then this macro is equivalent to $(ProjectExt).
            public static readonly string ProjectDir = "$(ProjectDir)";//! 	The directory of the project (defined as drive + path); includes the trailing backslash '\'.
            public static readonly string ProjectPath = "$(ProjectPath)";//! 	The absolute path name of the project (defined as drive + path + base name + file extension).
            public static readonly string ProjectName = "$(ProjectName)";//! 	The base name of the project.
            public static readonly string ProjectFileName = "$(ProjectFileName)";//! 	The file name of the project (defined as base name + file extension).
            public static readonly string ProjectExt = "$(ProjectExt)";//! 	The file extension of the project. It includes the '.' before the file extension.
            public static readonly string SolutionDir = "$(SolutionDir)";//! 	The directory of the solution (defined as drive + path); includes the trailing backslash '\'.
            public static readonly string SolutionPath = "$(SolutionPath)";//! 	The absolute path name of the solution (defined as drive + path + base name + file extension).
            public static readonly string SolutionName = "$(SolutionName)";//! 	The base name of the solution.
            public static readonly string SolutionFileName = "$(SolutionFileName)";//! 	The file name of the solution (defined as base name + file extension).
            public static readonly string SolutionExt = "$(SolutionExt)";//! 	The file extension of the solution. It includes the '.' before the file extension.
            public static readonly string TargetDir = "$(TargetDir)";//! 	The directory of the primary output file for the build (defined as drive + path); includes the trailing backslash '\'.
            public static readonly string TargetPath = "$(TargetPath)";//! 	The absolute path name of the primary output file for the build (defined as drive + path + base name + file extension).
            public static readonly string TargetName = "$(TargetName)";//! 	The base name of the primary output file for the build.
            public static readonly string TargetFileName = "$(TargetFileName)";//! 	The file name of the primary output file for the build (defined as base name + file extension).
            public static readonly string TargetExt = "$(TargetExt)";//! 	The file extension of the primary output file for the build. It includes the '.' before the file extension.
            public static readonly string VSInstallDir = "$(VSInstallDir)";//! 	The directory into which you installed Visual Studio .NET.
            public static readonly string VCInstallDir = "$(VCInstallDir)";//! 	The directory into which you installed Visual C++ .NET.
            public static readonly string FrameworkDir = "$(FrameworkDir)";//! 	The directory into which the .NET Framework was installed.
            public static readonly string FrameworkVersion = "$(FrameworkVersion)";//! 	The version of the .NET Framework used by Visual Studio. Combined with $(FrameworkDir), the full path to the version of the .NET Framework use by Visual Studio.
            public static readonly string FrameworkSDKDir = "$(FrameworkSDKDir)";//! 	The directory into which you installed the .NET Framework SDK. The .NET Framework SDK could have been installed as part of Visual Studio .NET or separately.
            public static readonly string WebDeployPath = "$(WebDeployPath)";//! 	The relative path from the web deployment root to where the project outputs belong. Returns the same value as RelativePath.
            public static readonly string WebDeployRoot = "$(WebDeployRoot)";//! 	The absolute path to the location of <localhost>. For example, c:\inetpub\wwwroot.
            public static readonly string SafeParentName = "$(SafeParentName)";//! 	The name of the immediate parent in valid name format. For example, a form is the parent of a .resx file.
            public static readonly string SafeInputName = "$(SafeInputName)";//! The name of the file as a valid class name, minus file extension.
            private static readonly string[] macros;

            static ProjectMacros()
            {
                macros = new string[] 
                {
                    RemoveMachine //// "$(RemoteMachine)";//! Set to the value of the Remote Machine property on the Debug property page. See Changing Project Settings for a C/C++ Debug Configuration for more information.
                  , ConfigurationName// // "$(ConfigurationName)";//! 	The name of the current project configuration (for example, "Debug").
                  , PlatformName // "$(PlatformName)";//! 	The name of current project platform (for example, "Win32").
                  , Inherit // "$(Inherit)";//! 	Specifies the order in which inherited properties appear in the command line composed by the project build system. By default, inherited properties appear at the end of the current property.1
                  , NoInherit // "$(NoInherit)";//! 	Causes any properties that would otherwise be inherited, to not be inherited. The use of $(NoInherit) causes any occurrences of $(Inherit) to be ignored for the same property.1
                  , ParentName // "$(ParentName)";//! 	Name of the item containing this project item. This will be the parent folder name, or project name.
                  , PlatformToolsetVersion
                  , RootNameSpace // "$(RootNameSpace)";//! 	The namespace, if any, containing the application.
                  , IntDir // "$(IntDir)";//! 	Path to the directory specified for intermediate files relative to the project directory. This resolves to the value for the Intermediate Directory property.
                  , OutDir // "$(OutDir)";//! 	Path to the output file directory, relative to the project directory. This resolves to the value for the Output Directory property.
                  , DevEnvDir // "$(DevEnvDir)";//! 	The installation directory of Visual Studio .NET (defined as drive + path); includes the trailing backslash '\'.
                  , InputDir // "$(InputDir)";//! 	The directory of the input file (defined as drive + path); includes the trailing backslash '\'. If the project is the input, then this macro is equivalent to $(ProjectDir).
                  , InputPath // "$(InputPath)";//! 	The absolute path name of the input file (defined as drive + path + base name + file extension). If the project is the input, then this macro is equivalent to $(ProjectPath).
                  , InputName // "$(InputName)";//! 	The base name of the input file. If the project is the input, then this macro is equivalent to $(ProjectName).
                  , InputFileName // "$(InputFileName)";//! 	The file name of the input file (defined as base name + file extension). If the project is the input, then this macro is equivalent to $(ProjectFileName).
                  , InputExt // "$(InputExt)";//! 	The file extension of the input file. It includes the '.' before the file extension. If the project is the input, then this macro is equivalent to $(ProjectExt).
                  , ProjectDir // "$(ProjectDir)";//! 	The directory of the project (defined as drive + path); includes the trailing backslash '\'.
                  , ProjectPath // "$(ProjectPath)";//! 	The absolute path name of the project (defined as drive + path + base name + file extension).
                  , ProjectName // "$(ProjectName)";//! 	The base name of the project.
                  , ProjectFileName // "$(ProjectFileName)";//! 	The file name of the project (defined as base name + file extension).
                  , ProjectExt // "$(ProjectExt)";//! 	The file extension of the project. It includes the '.' before the file extension.
                  , SolutionDir // "$(SolutionDir)";//! 	The directory of the solution (defined as drive + path); includes the trailing backslash '\'.
                  , SolutionPath // "$(SolutionPath)";//! 	The absolute path name of the solution (defined as drive + path + base name + file extension).
                  , SolutionName // "$(SolutionName)";//! 	The base name of the solution.
                  , SolutionFileName // "$(SolutionFileName)";//! 	The file name of the solution (defined as base name + file extension).
                  , SolutionExt // "$(SolutionExt)";//! 	The file extension of the solution. It includes the '.' before the file extension.
                  , TargetDir // "$(TargetDir)";//! 	The directory of the primary output file for the build (defined as drive + path); includes the trailing backslash '\'.
                  , TargetPath // "$(TargetPath)";//! 	The absolute path name of the primary output file for the build (defined as drive + path + base name + file extension).
                  , TargetName // "$(TargetName)";//! 	The base name of the primary output file for the build.
                  , TargetFileName // "$(TargetFileName)";//! 	The file name of the primary output file for the build (defined as base name + file extension).
                  , TargetExt // "$(TargetExt)";//! 	The file extension of the primary output file for the build. It includes the '.' before the file extension.
                  , VSInstallDir // "$(VSInstallDir)";//! 	The directory into which you installed Visual Studio .NET.
                  , VCInstallDir // "$(VCInstallDir)";//! 	The directory into which you installed Visual C++ .NET.
                  , FrameworkDir // "$(FrameworkDir)";//! 	The directory into which the .NET Framework was installed.
                  , FrameworkVersion // "$(FrameworkVersion)";//! 	The version of the .NET Framework used by Visual Studio. Combined with $(FrameworkDir), the full path to the version of the .NET Framework use by Visual Studio.
                  , FrameworkSDKDir // "$(FrameworkSDKDir)";//! 	The directory into which you installed the .NET Framework SDK. The .NET Framework SDK could have been installed as part of Visual Studio .NET or separately.
                  , WebDeployPath // "$(WebDeployPath)";//! 	The relative path from the web deployment root to where the project outputs belong. Returns the same value as RelativePath.
                  , WebDeployRoot // "$(WebDeployRoot)";//! 	The absolute path to the location of <localhost>. For example, c:\inetpub\wwwroot.
                  , SafeParentName // "$(SafeParentName)";//! 	The name of the immediate parent in valid name format. For example, a form is the parent of a .resx file.
                  , SafeInputName // "$(SafeInputName)";//! The name of the file as a valid class name, minus file extension.
                };

                Array.Sort(macros);
            }

            public static string[] Collection
            {
                get{ return macros; }
            }

            public static bool IsMacro( string s )
            {
                return Array.BinarySearch(macros, s) > 0;
            }
        }

        private string version="10.00";
        private VCProject        project = null;
        private IVCCollection    configurations = null;
        private IVCCollection    tools = null;
        private VCConfiguration  config = null;
        private VCCLCompilerTool cltool = null;
        private VCLinkerTool ltool = null;
                
        public VCCompilerHelper( EnvDTE.Project prj )
        {            
            Initialize(prj);
        }
        
        private void Initialize( EnvDTE.Project prj )
        {            
            project = (VCProject)prj.Object;
            configurations = (IVCCollection)project.Configurations;

            EnvDTE.ConfigurationManager configManager = prj.ConfigurationManager; 
            EnvDTE.Configuration activeConfig = configManager.ActiveConfiguration;
            string activeConfigNamePlatform = activeConfig.ConfigurationName + "|" + activeConfig.PlatformName;

            foreach (VCConfiguration c in project.Configurations)
            {
                if (c.Name == activeConfigNamePlatform)
                {
                    config = c;
                    break;
                }
            }
            Debug.Assert(config != null);
            if (config == null)
                throw new InvalidOperationException("The specified file does not have a configuration corresponding to the current active configuration.");

            //config = (VCConfiguration)configurations.Item(1);
            tools = (IVCCollection)config.Tools;
            cltool = (VCCLCompilerTool)tools.Item("VCCLCompilerTool");
            ltool = (VCLinkerTool)tools.Item("VCLinkerTool");    
        }

        public VCProject Project
        {
            get { return project; }
        }

        public string ProjectName
        {
            get { return project.Name; }
        }

        public IVCCollection Configurations
        {
            get { return configurations; }
        }

        public VCConfiguration ActiveConfiguration
        {
            get { return config; }
        }

        public VCCLCompilerTool CompilerTool
        {
            get { return cltool; }
        }

        public IVCCollection Tools
        {
            get { return tools; }
        }
        
        public string Platform
        {
            get { return config.Platform.Name; }
        }

        public dynamic Files
        {
            get { return project.Files; }    
        }

        public string Version
        {
            get { return version;  }
        }

        public string CompilerExecutable
        {
            get { return VCInstallDir + "\\bin\\cl.exe"; }
        }

        public string CompilerExecutableWithEnv
        {
            get 
            {
                string platformArg = "x86";
                if( String.Equals( Platform, "x64", StringComparison.InvariantCultureIgnoreCase ) )
                    platformArg = "x64";
                else if(String.Equals( Platform, "itanium", StringComparison.InvariantCultureIgnoreCase ) )
                    platformArg = "ia64";
                return @"cmd.exe /S /C call """ + VCInstallDir + @"\vcvarsall.bat"" " + platformArg + @" >nul && " + CompilerExecutable.Quote();
            }
        }

        public string Cmd
        {
            get { return "cmd.exe";  }
        }

        public string CompilerExecutableWithEnvAsCmdArgs
        {
            get
            {
                string platformArg = "x86";
                if (String.Equals(Platform, "x64", StringComparison.InvariantCultureIgnoreCase))
                    platformArg = "x64";
                else if (String.Equals(Platform, "itanium", StringComparison.InvariantCultureIgnoreCase))
                    platformArg = "ia64";
                return @"/S /C call """ + VCInstallDir + @"\vcvarsall.bat"" " + platformArg + @" >nul && " + CompilerExecutable.Quote();
            }
        }

        public string VCInstallDir
        {
            get { return config.Evaluate(ProjectMacros.VCInstallDir); }
        }

        public string VSInstallDir
        {
            get { return config.Evaluate(ProjectMacros.VSInstallDir); }
        }

        public VCFile GetVCFile( string name )
        {
            foreach (var file in Files)
            {
                if( file.Name == name )
                    return (VCFile)file;
            }

            return null;
        }

        public void ReplacePreprocessorDefine( VCConfiguration cfg, string oldDef, string newDef )
        {
            VCCLCompilerTool cl = (VCCLCompilerTool)cfg.Tools.Item("VCCLCompilerTool");
            if (cl != null)
            {
                cl.PreprocessorDefinitions = cl.PreprocessorDefinitions.Replace(oldDef, newDef);
            }
        }

        public void ReplaceAdditionalLibraryDir(VCConfiguration cfg, string oldDef, string newDef)
        {
            VCLinkerTool l = (VCLinkerTool)cfg.Tools.Item("VCLinkerTool");
            if (l != null)
            {
                l.AdditionalLibraryDirectories = l.AdditionalLibraryDirectories.Replace(oldDef, newDef);
            }
        }

        public void ReplaceAdditionalIncludeDir(VCConfiguration cfg, string oldDef, string newDef)
        {
            VCCLCompilerTool cl = (VCCLCompilerTool)cfg.Tools.Item("VCCLCompilerTool");
            if (cl != null)
            {
                cl.AdditionalIncludeDirectories = cl.AdditionalIncludeDirectories.Replace(oldDef, newDef);
            }
        }
                
        public string GenerateCLCmdArgs(bool forceCompileOnly = true)
        {
            StringBuilder cmd = new StringBuilder();

            if(config.CharacterSet == charSet.charSetUnicode)
                cmd.Append( " /D _UNICODE /D UNICODE" );
        
            if( config.useOfMfc == useOfMfc.useMfcDynamic )
                cmd.Append( " /D _AFXDLL" );

            if( cltool.Optimization == optimizeOption.optimizeDisabled )
                cmd.Append( " /Od" );
            else if( cltool.Optimization == optimizeOption.optimizeFull )
                cmd.Append( " /Ox" );
            else if( cltool.Optimization == optimizeOption.optimizeMaxSpeed )
                cmd.Append( " /O2" );
            else if( cltool.Optimization == optimizeOption.optimizeMinSpace )
                cmd.Append( " /O1" );
            
            string [] includes = cltool.AdditionalIncludeDirectories.Split(';');
            foreach (string i in includes)
            {
                if( !String.IsNullOrWhiteSpace(i) )
                    cmd.Append(" \"/I " + i + "\"");
            }
            
            string [] forcedIncludes = cltool.ForcedIncludeFiles.Split(';');
            foreach (string i in forcedIncludes)
            {
                if (!String.IsNullOrWhiteSpace(i))
                    cmd.Append(" \"/FI " + i + "\"");
            }

            string [] preprocessorDefs = cltool.PreprocessorDefinitions.Split(';');
            foreach (string d in preprocessorDefs)
            {
                if (!String.IsNullOrWhiteSpace(d))
                    cmd.Append(" /D" + d);
            }

            string [] preprocessorUnDefs = cltool.UndefinePreprocessorDefinitions.Split(';');
            foreach (string d in preprocessorUnDefs)
            {
                if (!String.IsNullOrWhiteSpace(d))
                    cmd.Append(" /U" + d);
            }

            if( cltool.MinimalRebuild )
                cmd.Append(" /Gm");

            if( cltool.ExceptionHandling == cppExceptionHandling.cppExceptionHandlingYes )
                cmd.Append(" /EHsc");
            else if( cltool.ExceptionHandling == cppExceptionHandling.cppExceptionHandlingYesWithSEH )
                cmd.Append(" /EHa");

            if( cltool.BasicRuntimeChecks == basicRuntimeCheckOption.runtimeBasicCheckAll )
                cmd.Append( " /RTC1");
            else if( cltool.BasicRuntimeChecks == basicRuntimeCheckOption.runtimeCheckStackFrame )
                cmd.Append( " /RTCs");
            else if( cltool.BasicRuntimeChecks == basicRuntimeCheckOption.runtimeCheckUninitVariables )
                cmd.Append( " /RTCu");

            if( cltool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreaded )
                cmd.Append( " /MT" );
            else if( cltool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDebug )
                cmd.Append( " /MTd" );
            else if( cltool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDebugDLL )
                cmd.Append( " /MDd" );
            else if( cltool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDLL )
                cmd.Append( " /MD" );

            if( cltool.RuntimeTypeInfo)
                cmd.Append(" /GR");

            if( cltool.BrowseInformation == browseInfoOption.brAllInfo )
                cmd.Append(" /FR" + cltool.BrowseInformationFile);
            else if( cltool.BrowseInformation == browseInfoOption.brNoLocalSymbols )
                cmd.Append(" /Fr " + cltool.BrowseInformationFile);
            
            if( cltool.BufferSecurityCheck )
                cmd.Append( " /GS");

            if( cltool.CallingConvention == callingConventionOption.callConventionCDecl )
                cmd.Append( " /Gd");
            else if( cltool.CallingConvention == callingConventionOption.callConventionFastCall )
                cmd.Append( " /Gr");
            else if( cltool.CallingConvention == callingConventionOption.callConventionStdCall )
                cmd.Append( " /Gz");

            if( cltool.CompileAs == CompileAsOptions.compileAsC )
                cmd.Append( " /TC");
            else if( cltool.CompileAs == CompileAsOptions.compileAsCPlusPlus )
                cmd.Append( " /TP");

            if( cltool.CompileAsManaged == compileAsManagedOptions.managedAssembly )
                cmd.Append( " /clr");
            else if( cltool.CompileAsManaged == compileAsManagedOptions.managedAssemblyPure )
                cmd.Append( " /clr:pure");
            else if( cltool.CompileAsManaged == compileAsManagedOptions.managedAssemblySafe )
                cmd.Append( " /clr:safe");
            else if( cltool.CompileAsManaged == compileAsManagedOptions.managedAssemblyOldSyntax )
                cmd.Append( " /clr:oldSyntax");
            //else if( cltool.CompileAsManaged == compileAsManagedOptions.managedNotSet )
            //    cmd.Append( " /clr:noAssembly");

            if( forceCompileOnly || cltool.CompileOnly )
                cmd.Append( " /c");

            if( cltool.DebugInformationFormat == debugOption.debugDisabled )
                cmd.Append( " /Z7");
            else if( cltool.DebugInformationFormat == debugOption.debugEnabled )
                cmd.Append( " /Zi");
            else if( cltool.DebugInformationFormat == debugOption.debugEditAndContinue )
                cmd.Append( " /ZI");
            else if( cltool.DebugInformationFormat == debugOption.debugOldStyleInfo )
                cmd.Append( " /Zd");

            if( cltool.DefaultCharIsUnsigned )
                cmd.Append(" /J");

            if( cltool.DisableLanguageExtensions )
                cmd.Append(" /Za");

            if( cltool.EnableEnhancedInstructionSet == enhancedInstructionSetType.enhancedInstructionSetTypeSIMD )
                cmd.Append(" /ARCH:sse");
            else if( cltool.EnableEnhancedInstructionSet == enhancedInstructionSetType.enhancedInstructionSetTypeSIMD2 )
                cmd.Append(" /ARCH:sse2");

            if( cltool.EnableFiberSafeOptimizations )
                cmd.Append(" /GT");

            if( cltool.EnableFunctionLevelLinking )
                cmd.Append(" /Gy");

            if( cltool.EnableIntrinsicFunctions )
                cmd.Append(" /Oi");

            if( cltool.ExpandAttributedSource)
                cmd.Append(" /Fx");

            if( cltool.FavorSizeOrSpeed == favorSizeOrSpeedOption.favorSize )
                cmd.Append(" /Os");
            else if( cltool.FavorSizeOrSpeed == favorSizeOrSpeedOption.favorSpeed )
                cmd.Append(" /Ot");

            if( cltool.IgnoreStandardIncludePath )
                cmd.Append(" /X");

            if( cltool.InlineFunctionExpansion == inlineExpansionOption.expandOnlyInline )
                cmd.Append(" /Ob1");
            else if( cltool.InlineFunctionExpansion == inlineExpansionOption.expandAnySuitable )
                cmd.Append(" /Ob2");

            if( cltool.KeepComments )
                cmd.Append(" /C");

            if( !String.IsNullOrEmpty( cltool.ObjectFile ) )
                cmd.Append(" /Fo " + cltool.ObjectFile);

            if( cltool.OmitFramePointers )
                cmd.Append(" /Oy");

            if( cltool.UsePrecompiledHeader == pchOption.pchCreateUsingSpecific )
                cmd.Append( " /Yc \"" + cltool.PrecompiledHeaderThrough + "\"" );
            if( cltool.UsePrecompiledHeader == pchOption.pchUseUsingSpecific )
                cmd.Append( " /Yu \"" + cltool.PrecompiledHeaderThrough + "\"" );

            if( !String.IsNullOrEmpty(cltool.PrecompiledHeaderFile) )
                cmd.Append(" /Fp " + cltool.PrecompiledHeaderFile);
            
            if( !String.IsNullOrEmpty(cltool.ProgramDataBaseFileName) )
                cmd.Append(" /Fd " + cltool.ProgramDataBaseFileName);
            
            if( cltool.WholeProgramOptimization )
                cmd.Append(" /GL");

            if( cltool.RuntimeTypeInfo )
                cmd.Append( "/GR");

            if( cltool.StringPooling )
                cmd.Append(" /GF");

            if( cltool.SuppressStartupBanner )
                cmd.Append(" /nologo");

            if (!cltool.TreatWChar_tAsBuiltInType)
                cmd.Append(" /Zc:wchar_t-");

            if( !cltool.ForceConformanceInForLoopScope )
                cmd.Append(" /Zc:forScope-");
        
            if( cltool.WarningLevel == warningLevelOption.warningLevel_1 )
                cmd.Append( " /W1" );
            else if( cltool.WarningLevel == warningLevelOption.warningLevel_2 )
                cmd.Append( " /W2" );
            else if( cltool.WarningLevel == warningLevelOption.warningLevel_3 )
                cmd.Append( " /W3" );
            else if( cltool.WarningLevel == warningLevelOption.warningLevel_4 )
                cmd.Append( " /W4" );

            if( cltool.WarnAsError )
                cmd.Append(" /WX");

            string [] disableWarnings = cltool.DisableSpecificWarnings.Split(';');
            foreach (string d in disableWarnings)
            {
                if (!String.IsNullOrWhiteSpace(d))
                    cmd.Append(" /wd" + d);
            }

            if( cltool.ErrorReporting == compilerErrorReportingType.compilerErrorReportingPrompt )
                cmd.Append(" /errorReport:prompt");
            else if( cltool.ErrorReporting == compilerErrorReportingType.compilerErrorReportingQueue )
                cmd.Append(" /errorReport:queue");

            if (!String.IsNullOrWhiteSpace(cltool.AdditionalOptions))
                cmd.Append(" " + cltool.AdditionalOptions);
            
            string cmdLine = cmd.ToString();

            //! Replace macros
            foreach (string macro in ProjectMacros.Collection)
            {
                string result = config.Evaluate(macro);
                if (result != null)
                {
                    if (result.HasWhiteSpace())
                        result = result.Quote();//add quotes if there are spaces.
                    cmdLine = cmdLine.Replace(macro, result, StringComparison.CurrentCultureIgnoreCase);
                }
            }

            return cmdLine;
        }

        public VCFileConfiguration GetActiveFileConfiguration(string filename)
        {
            VCFile file = GetVCFile(filename);
            if( file != null )
                return GetActiveFileConfiguration(file);
            return null;
        }

        public VCFileConfiguration GetActiveFileConfiguration(VCFile file)
        {        
            foreach (VCFileConfiguration c in file.FileConfigurations)
            {
                if (c.Name == config.Name)
                {
                    return c;
                }
            }

            return null;
        }

        public string ReplaceMacros(string s, VCConfiguration cfg = null)
        {
            if (cfg == null)
                cfg = config;

            //! Replace macros
            foreach (string macro in ProjectMacros.Collection)
            {
                string result = cfg.Evaluate(macro);
                if (result != null)
                    s = s.Replace(macro, result, StringComparison.CurrentCultureIgnoreCase);
            }

            return s;
        }
        
        public string ReplaceMacros( string s, VCFileConfiguration cfg ) 
        {
            //! Replace macros
            foreach (string macro in ProjectMacros.Collection)
            {
                string result = cfg.Evaluate(macro);
                if (result != null)
                    s = s.Replace(macro, result, StringComparison.CurrentCultureIgnoreCase);
            }

            return s;
        }
        
        public string GenerateCLCmdArgs(string filename, bool skipPrecompiledHeader = true, bool skipObjectFile = true, bool skipMinimalRebuild = true, bool forceCompileOnly = true)
        {
            VCFile file = GetVCFile(filename);

            VCFileConfiguration fileConfig = null;
            foreach (VCFileConfiguration c in file.FileConfigurations)
            {
                if (c.Name == config.Name)
                {
                    fileConfig = c;
                    break;
                }
            }

            if (fileConfig == null)
                throw new InvalidOperationException("Selected file does not have a configuration which matches the active project configuration.");

            VCCLCompilerTool fileTool = (VCCLCompilerTool)fileConfig.Tool;
            if( fileTool == null )
                throw new InvalidOperationException("Selected file does not have a valid build tool specified.");

            StringBuilder cmd = new StringBuilder();

            if (config.CharacterSet == charSet.charSetUnicode)
                cmd.Append(" /D _UNICODE /D UNICODE");

            if (config.useOfMfc == useOfMfc.useMfcDynamic)
                cmd.Append(" /D _AFXDLL");

            if (fileTool.Optimization == optimizeOption.optimizeDisabled)
                cmd.Append(" /Od");
            else if (fileTool.Optimization == optimizeOption.optimizeFull)
                cmd.Append(" /Ox");
            else if (fileTool.Optimization == optimizeOption.optimizeMaxSpeed)
                cmd.Append(" /O2");
            else if (fileTool.Optimization == optimizeOption.optimizeMinSpace)
                cmd.Append(" /O1");

            string[] preprocessorDefs = fileTool.PreprocessorDefinitions.Split(';');
            foreach (string d in preprocessorDefs)
            {
                if (!String.IsNullOrWhiteSpace(d))
                    cmd.Append(" /D" + d);
            }

            preprocessorDefs = cltool.PreprocessorDefinitions.Split(';');
            foreach (string d in preprocessorDefs)
            {
                if (!String.IsNullOrWhiteSpace(d))
                    cmd.Append(" /D" + d);
            }

            string[] preprocessorUnDefs = fileTool.UndefinePreprocessorDefinitions.Split(';');
            foreach (string d in preprocessorUnDefs)
            {
                if (!String.IsNullOrWhiteSpace(d))
                    cmd.Append(" /U" + d);
            }

            preprocessorUnDefs = cltool.UndefinePreprocessorDefinitions.Split(';');
            foreach (string d in preprocessorUnDefs)
            {
                if (!String.IsNullOrWhiteSpace(d))
                    cmd.Append(" /U" + d);
            }

            if (!skipMinimalRebuild && fileTool.MinimalRebuild)
                cmd.Append(" /Gm");

            if (fileTool.ExceptionHandling == cppExceptionHandling.cppExceptionHandlingYes)
                cmd.Append(" /EHsc");
            else if (fileTool.ExceptionHandling == cppExceptionHandling.cppExceptionHandlingYesWithSEH)
                cmd.Append(" /EHa");

            if (fileTool.BasicRuntimeChecks == basicRuntimeCheckOption.runtimeBasicCheckAll)
                cmd.Append(" /RTC1");
            else if (fileTool.BasicRuntimeChecks == basicRuntimeCheckOption.runtimeCheckStackFrame)
                cmd.Append(" /RTCs");
            else if (fileTool.BasicRuntimeChecks == basicRuntimeCheckOption.runtimeCheckUninitVariables)
                cmd.Append(" /RTCu");

            if (fileTool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreaded)
                cmd.Append(" /MT");
            else if (fileTool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDebug)
                cmd.Append(" /MTd");
            else if (fileTool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDebugDLL)
                cmd.Append(" /MDd");
            else if (fileTool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDLL)
                cmd.Append(" /MD");

            if (fileTool.RuntimeTypeInfo)
                cmd.Append(" /GR");

            if (fileTool.BrowseInformation == browseInfoOption.brAllInfo)
                cmd.Append(" /FR" + fileTool.BrowseInformationFile);
            else if (fileTool.BrowseInformation == browseInfoOption.brNoLocalSymbols)
                cmd.Append(" /Fr " + fileTool.BrowseInformationFile);

            if (fileTool.BufferSecurityCheck)
                cmd.Append(" /GS");

            if (fileTool.CallingConvention == callingConventionOption.callConventionCDecl)
                cmd.Append(" /Gd");
            else if (fileTool.CallingConvention == callingConventionOption.callConventionFastCall)
                cmd.Append(" /Gr");
            else if (fileTool.CallingConvention == callingConventionOption.callConventionStdCall)
                cmd.Append(" /Gz");

            if (fileTool.CompileAs == CompileAsOptions.compileAsC)
                cmd.Append(" /TC");
            else if (fileTool.CompileAs == CompileAsOptions.compileAsCPlusPlus)
                cmd.Append(" /TP");

            if (fileTool.CompileAsManaged == compileAsManagedOptions.managedAssembly)
                cmd.Append(" /clr");
            else if (fileTool.CompileAsManaged == compileAsManagedOptions.managedAssemblyPure)
                cmd.Append(" /clr:pure");
            else if (fileTool.CompileAsManaged == compileAsManagedOptions.managedAssemblySafe)
                cmd.Append(" /clr:safe");
            else if (fileTool.CompileAsManaged == compileAsManagedOptions.managedAssemblyOldSyntax)
                cmd.Append(" /clr:oldSyntax");
            //else if( fileTool.CompileAsManaged == compileAsManagedOptions.managedNotSet )
            //    cmd.Append( " /clr:noAssembly");

            if (forceCompileOnly || fileTool.CompileOnly)
                cmd.Append(" /c");

            if (fileTool.DebugInformationFormat == debugOption.debugDisabled)
                cmd.Append(" /Z7");
            else if (fileTool.DebugInformationFormat == debugOption.debugEnabled)
                cmd.Append(" /Zi");
            else if (fileTool.DebugInformationFormat == debugOption.debugEditAndContinue)
                cmd.Append(" /ZI");
            else if (fileTool.DebugInformationFormat == debugOption.debugOldStyleInfo)
                cmd.Append(" /Zd");

            if (fileTool.DefaultCharIsUnsigned)
                cmd.Append(" /J");

            if (fileTool.DisableLanguageExtensions)
                cmd.Append(" /Za");

            if (fileTool.EnableEnhancedInstructionSet == enhancedInstructionSetType.enhancedInstructionSetTypeSIMD)
                cmd.Append(" /ARCH:sse");
            else if (fileTool.EnableEnhancedInstructionSet == enhancedInstructionSetType.enhancedInstructionSetTypeSIMD2)
                cmd.Append(" /ARCH:sse2");

            if (fileTool.EnableFiberSafeOptimizations)
                cmd.Append(" /GT");

            if (fileTool.EnableFunctionLevelLinking)
                cmd.Append(" /Gy");

            if (fileTool.EnableIntrinsicFunctions)
                cmd.Append(" /Oi");

            if (fileTool.ExpandAttributedSource)
                cmd.Append(" /Fx");

            if (fileTool.FavorSizeOrSpeed == favorSizeOrSpeedOption.favorSize)
                cmd.Append(" /Os");
            else if (fileTool.FavorSizeOrSpeed == favorSizeOrSpeedOption.favorSpeed)
                cmd.Append(" /Ot");

            if (fileTool.IgnoreStandardIncludePath)
                cmd.Append(" /X");

            if (fileTool.InlineFunctionExpansion == inlineExpansionOption.expandOnlyInline)
                cmd.Append(" /Ob1");
            else if (fileTool.InlineFunctionExpansion == inlineExpansionOption.expandAnySuitable)
                cmd.Append(" /Ob2");

            if (fileTool.KeepComments)
                cmd.Append(" /C");

            if (!skipObjectFile)
            {
                string objFile = fileTool.ObjectFile;
                if (!String.IsNullOrEmpty(objFile))
                {
                    string path = ReplaceMacros(fileTool.ObjectFile, fileConfig);
                    char[] trims = new char[] { ' ', '\n', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
                    path = path.TrimEnd(trims);                    
                    cmd.Append(" /Fo" + path.Quote());
                }
            }

            if (fileTool.OmitFramePointers)
                cmd.Append(" /Oy");

            if (!skipPrecompiledHeader)
            {
                if (fileTool.UsePrecompiledHeader == pchOption.pchCreateUsingSpecific)
                {
                    string path = ReplaceMacros(fileTool.PrecompiledHeaderThrough, fileConfig);
                    if (String.IsNullOrWhiteSpace(path) && !String.IsNullOrEmpty(fileTool.PrecompiledHeaderFile))
                        cmd.Append(" /Fp" + ReplaceMacros(fileTool.PrecompiledHeaderFile, fileConfig).Quote());
                    else if (path.HasWhiteSpace())
                        path = path.Quote();
                    cmd.Append(" /Yc" + path);
                }
                else if (fileTool.UsePrecompiledHeader == pchOption.pchUseUsingSpecific)
                {
                    string path = ReplaceMacros(fileTool.PrecompiledHeaderThrough, fileConfig);

                    if (String.IsNullOrWhiteSpace(path) && !String.IsNullOrEmpty(fileTool.PrecompiledHeaderFile))
                        cmd.Append(" /Fp" + ReplaceMacros(fileTool.PrecompiledHeaderFile, fileConfig).Quote());                
                    else if (path.HasWhiteSpace())
                        path = path.Quote();
                    cmd.Append(" /Yu" + path);
                }
            }

            if (!String.IsNullOrEmpty(fileTool.ProgramDataBaseFileName))
            {
                string pdbFilename = ReplaceMacros(fileTool.ProgramDataBaseFileName, config);
                pdbFilename = ReplaceMacros(pdbFilename, fileConfig);
                cmd.Append(" /Fd" + pdbFilename.Quote());
            }
                        
            if (fileTool.WholeProgramOptimization)
                cmd.Append(" /GL");
                        
            if (fileTool.StringPooling)
                cmd.Append(" /GF");

            if (fileTool.SuppressStartupBanner)
                cmd.Append(" /nologo");

            if (!fileTool.TreatWChar_tAsBuiltInType)
                cmd.Append(" /Zc:wchar_t-");

            if (!fileTool.ForceConformanceInForLoopScope)
                cmd.Append(" /Zc:forScope-");

            if (fileTool.WarningLevel == warningLevelOption.warningLevel_1)
                cmd.Append(" /W1");
            else if (fileTool.WarningLevel == warningLevelOption.warningLevel_2)
                cmd.Append(" /W2");
            else if (fileTool.WarningLevel == warningLevelOption.warningLevel_3)
                cmd.Append(" /W3");
            else if (fileTool.WarningLevel == warningLevelOption.warningLevel_4)
                cmd.Append(" /W4");

            if (fileTool.WarnAsError)
                cmd.Append(" /WX");

            string[] disableWarnings = fileTool.DisableSpecificWarnings.Split(';');
            foreach (string d in disableWarnings)
            {
                if (!String.IsNullOrWhiteSpace(d))
                    cmd.Append(" /wd" + d);
            }

            disableWarnings = cltool.DisableSpecificWarnings.Split(';');
            foreach (string d in disableWarnings)
            {
                if (!String.IsNullOrWhiteSpace(d))
                    cmd.Append(" /wd" + d);
            }
                        
            string[] includes = fileTool.AdditionalIncludeDirectories.Split(';');
            foreach (string i in includes)
            {
                if (!String.IsNullOrWhiteSpace(i))
                {
                    char[] trims = new char[] { ' ', '\n', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
                    string path = ReplaceMacros(i.TrimEnd(trims), fileConfig).NormalizePathSeparators();
                    cmd.Append(" -I" + path.Quote());                    
                }
            }

            includes = cltool.AdditionalIncludeDirectories.Split(';');
            foreach (string i in includes)
            {
                if (!String.IsNullOrWhiteSpace(i))
                {
                    char[] trims = new char[] { ' ', '\n', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
                    string path = ReplaceMacros(i.TrimEnd(trims), fileConfig).NormalizePathSeparators();
                    cmd.Append(" -I" + path.Quote());                    
                }
            }

            string[] forcedIncludes = fileTool.ForcedIncludeFiles.Split(';');
            foreach (string i in forcedIncludes)
            {
                if (!String.IsNullOrWhiteSpace(i))
                {
                    string path = ReplaceMacros(i, fileConfig).NormalizePathSeparators();
                    cmd.Append(" /FI" + path.Quote());
                }
            }

            forcedIncludes = cltool.ForcedIncludeFiles.Split(';');
            foreach (string i in forcedIncludes)
            {
                if (!String.IsNullOrWhiteSpace(i))
                {
                    string path = ReplaceMacros(i, fileConfig).NormalizePathSeparators();
                    cmd.Append(" /FI" + path.Quote());
                }
            }

            if (fileTool.ErrorReporting == compilerErrorReportingType.compilerErrorReportingPrompt)
                cmd.Append(" /errorReport:prompt");
            else if (fileTool.ErrorReporting == compilerErrorReportingType.compilerErrorReportingQueue)
                cmd.Append(" /errorReport:queue");

            if (!String.IsNullOrWhiteSpace(fileTool.AdditionalOptions))
                cmd.Append(" " + ReplaceMacros(fileTool.AdditionalOptions, fileConfig) );
            
            string cmdLine = cmd.ToString();
            return cmdLine;
        }
    }
}
