﻿//
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
using System.Globalization;

namespace Meta
{
    internal sealed class SystemResources
    {
        internal const string AddReferenceDialogTitle = "AddReferenceDialogTitle";
        internal const string AddToNullProjectError = "AddToNullProjectError";
        internal const string Advanced = "Advanced";
        internal const string AssemblyReferenceAlreadyExists = "AssemblyReferenceAlreadyExists";
        internal const string AttributeLoad = "AttributeLoad";
        internal const string BuildAction = "BuildAction";
        internal const string BuildActionDescription = "BuildActionDescription";
        internal const string BuildCaption = "BuildCaption";
        internal const string BuildVerbosity = "BuildVerbosity";
        internal const string BuildVerbosityDescription = "BuildVerbosityDescription";
        internal const string BuildEventError = "BuildEventError";
        internal const string CancelQueryEdit = "CancelQueryEdit";
        internal const string CannotAddFileThatIsOpenInEditor = "CannotAddFileThatIsOpenInEditor";
        internal const string CanNotSaveFileNotOpeneInEditor = "CanNotSaveFileNotOpeneInEditor";
        internal const string cli1 = "cli1";
        internal const string Compile = "Compile";
        internal const string ConfirmExtensionChange = "ConfirmExtensionChange";
        internal const string Content = "Content";
        internal const string CopyToLocal = "CopyToLocal";
        internal const string CopyToLocalDescription = "CopyToLocalDescription";
        internal const string EmbedInteropTypes = "EmbedInteropTypes";
        internal const string EmbedInteropTypesDescription = "EmbedInteropTypesDescription";
        internal const string CustomTool = "CustomTool";
        internal const string CustomToolDescription = "CustomToolDescription";
        internal const string CustomToolNamespace = "CustomToolNamespace";
        internal const string CustomToolNamespaceDescription = "CustomToolNamespaceDescription";
        internal const string DetailsImport = "DetailsImport";
        internal const string DetailsUserImport = "DetailsUserImport";
        internal const string DetailsItem = "DetailsItem";
        internal const string DetailsItemLocation = "DetailsItemLocation";
        internal const string DetailsProperty = "DetailsProperty";
        internal const string DetailsTarget = "DetailsTarget";
        internal const string DetailsUsingTask = "DetailsUsingTask";
        internal const string Detailed = "Detailed";
        internal const string Diagnostic = "Diagnostic";
        internal const string DirectoryExistError = "DirectoryExistError";
        internal const string EditorViewError = "EditorViewError";
        internal const string EmbeddedResource = "EmbeddedResource";
        internal const string Error = "Error";
        internal const string ErrorInvalidFileName = "ErrorInvalidFileName";
        internal const string ErrorInvalidProjectName = "ErrorInvalidProjectName";
        internal const string ErrorReferenceCouldNotBeAdded = "ErrorReferenceCouldNotBeAdded";
        internal const string ErrorMsBuildRegistration = "ErrorMsBuildRegistration";
        internal const string ErrorSaving = "ErrorSaving";
        internal const string Exe = "Exe";
        internal const string ExpectedObjectOfType = "ExpectedObjectOfType";
        internal const string FailedToGetService = "FailedToGetService";
        internal const string FailedToRetrieveProperties = "FailedToRetrieveProperties";
        internal const string FileNameCannotContainALeadingPeriod = "FileNameCannotContainALeadingPeriod";
        internal const string FileCannotBeRenamedToAnExistingFile = "FileCannotBeRenamedToAnExistingFile";
        internal const string FileAlreadyExistsAndCannotBeRenamed = "FileAlreadyExistsAndCannotBeRenamed";
        internal const string FileAlreadyExists = "FileAlreadyExists";
        internal const string FileAlreadyExistsCaption = "FileAlreadyExistsCaption";
        internal const string FileAlreadyInProject = "FileAlreadyInProject";
        internal const string FileAlreadyInProjectCaption = "FileAlreadyInProjectCaption";
        internal const string FileCopyError = "FileCopyError";
        internal const string FileName = "FileName";
        internal const string FileNameDescription = "FileNameDescription";
        internal const string FileOrFolderAlreadyExists = "FileOrFolderAlreadyExists";
        internal const string FileOrFolderCannotBeFound = "FileOrFolderCannotBeFound";
        internal const string FileProperties = "FileProperties";
        internal const string FolderName = "FolderName";
        internal const string FolderNameDescription = "FolderNameDescription";
        internal const string FolderProperties = "FolderProperties";
        internal const string FullPath = "FullPath";
        internal const string FullPathDescription = "FullPathDescription";
        internal const string ItemDoesNotExistInProjectDirectory = "ItemDoesNotExistInProjectDirectory";
        internal const string InvalidAutomationObject = "InvalidAutomationObject";
        internal const string InvalidLoggerType = "InvalidLoggerType";
        internal const string InvalidParameter = "InvalidParameter";
        internal const string Library = "Library";
        internal const string LinkedItemsAreNotSupported = "LinkedItemsAreNotSupported";
        internal const string Minimal = "Minimal";
        internal const string Misc = "Misc";
        internal const string None = "None";
        internal const string Normal = "Normal";
        internal const string NestedProjectFailedToReload = "NestedProjectFailedToReload";
        internal const string OutputPath = "OutputPath";
        internal const string OutputPathDescription = "OutputPathDescription";
        internal const string PasteFailed = "PasteFailed";
        internal const string ParameterMustBeAValidGuid = "ParameterMustBeAValidGuid";
        internal const string ParameterMustBeAValidItemId = "ParameterMustBeAValidItemId";
        internal const string ParameterCannotBeNullOrEmpty = "ParameterCannotBeNullOrEmpty";
        internal const string PathTooLong = "PathTooLong";
        internal const string ProjectContainsCircularReferences = "ProjectContainsCircularReferences";
        internal const string Program = "Program";
        internal const string Project = "Project";
        internal const string ProjectFile = "ProjectFile";
        internal const string ProjectFileDescription = "ProjectFileDescription";
        internal const string ProjectFolder = "ProjectFolder";
        internal const string ProjectFolderDescription = "ProjectFolderDescription";
        internal const string ProjectProperties = "ProjectProperties";
        internal const string Quiet = "Quiet";
        internal const string QueryReloadNestedProject = "QueryReloadNestedProject";
        internal const string ReferenceAlreadyExists = "ReferenceAlreadyExists";
        internal const string ReferencesNodeName = "ReferencesNodeName";
        internal const string ReferenceProperties = "ReferenceProperties";
        internal const string RefName = "RefName";
        internal const string RefNameDescription = "RefNameDescription";
        internal const string RenameFolder = "RenameFolder";
        internal const string RTL = "RTL";
        internal const string SaveCaption = "SaveCaption";
        internal const string SaveModifiedDocuments = "SaveModifiedDocuments";
        internal const string SaveOfProjectFileOutsideCurrentDirectory = "SaveOfProjectFileOutsideCurrentDirectory";
        internal const string StandardEditorViewError = "StandardEditorViewError";
        internal const string Settings = "Settings";
        internal const string URL = "URL";
        internal const string UseOfDeletedItemError = "UseOfDeletedItemError";
        internal const string Warning = "Warning";
        internal const string WinExe = "WinExe";
        internal const string CannotLoadUnknownTargetFrameworkProject = "CannotLoadUnknownTargetFrameworkProject";
        internal const string ReloadPromptOnTargetFxChanged = "ReloadPromptOnTargetFxChanged";
        internal const string ReloadPromptOnTargetFxChangedCaption = "ReloadPromptOnTargetFxChangedCaption";

        static SystemResources loader;
        System.Resources.ResourceManager resources;

        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if(s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    System.Threading.Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        internal SystemResources()
        {
            resources = new System.Resources.ResourceManager("Meta.MetaPackage", this.GetType().Assembly);
        }

        private static SystemResources GetLoader()
        {
            if(loader == null)
            {
                lock (InternalSyncObject)
                {
                    if(loader == null)
                    {
                        loader = new SystemResources();
                    }
                }
            }

            return loader;
        }

        private static CultureInfo Culture
        {
            get { return CultureInfo.CurrentUICulture; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static System.Resources.ResourceManager Resources
        {
            get
            {
                return GetLoader().resources;
            }
        }

        public static string GetString(string name, params object[] args)
        {
            SystemResources sys = GetLoader();
            if(sys == null)
                return null;
            string res = sys.resources.GetString(name, SystemResources.Culture);

            if(args != null && args.Length > 0)
            {
                return String.Format(CultureInfo.CurrentCulture, res, args);
            }
            else
            {
                return res;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string GetString(string name)
        {
            SystemResources sys = GetLoader();
            if(sys == null)
                return null;
            return sys.resources.GetString(name, SystemResources.Culture);
        }

        public static string GetString(string name, CultureInfo culture)
        {
            SystemResources sys = GetLoader();
            if(sys == null)
                return null;
            return sys.resources.GetString(name, culture);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static object GetObject(string name)
        {
            SystemResources sys = GetLoader();
            if(sys == null)
                return null;
            return sys.resources.GetObject(name, SystemResources.Culture);
        }
    }
}
