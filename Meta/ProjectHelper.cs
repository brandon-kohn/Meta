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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.VCProject;
using EnvDTE;
using EnvDTE80;

namespace Meta
{
    public class ProjectHelper
    {
        public static EnvDTE.Project GetProject(IVsHierarchy hierarchy)
        {
            object project;
            ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out project));
            return (project as EnvDTE.Project);
        }

        public static EnvDTE.ProjectItem GetProjectItem(uint pid, IVsHierarchy hierarchy)
        {
            object item;
            ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(pid, (int)__VSHPROPID.VSHPROPID_ExtObject, out item));
            return (item as EnvDTE.ProjectItem);
        }

        public static bool IsCPPNode(uint pid, IVsHierarchy node)
        {
            object value;
            node.GetProperty(pid, (int)__VSHPROPID.VSHPROPID_Name, out value);
            return (
                        value != null &&
                        (
                            value.ToString().EndsWith(".cpp") ||
                            value.ToString().EndsWith(".cxx") ||
                            value.ToString().EndsWith(".cc") ||
                            value.ToString().EndsWith(".c")
                        )
                    );
        }

        public static bool IsCPPProject(uint pid, IVsHierarchy node)
        {
            try
            {
                //! Should not be a project item.
                var pItem = GetProjectItem(pid, node);
                if (pItem != null && !(pItem is EnvDTE.Project))
                    return false;
            }
            catch(System.SystemException)
            {

            }
            
            try
            {
                string vsppProjectGuid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
                Project proj = GetProject(node);
                if (proj.Kind == vsppProjectGuid)
                    return true;
            }
            catch(System.SystemException)
            {

            }

            return false;
        }
    }
}
