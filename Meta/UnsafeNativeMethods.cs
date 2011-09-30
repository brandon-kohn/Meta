/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace Meta
{
    internal static class ExternDll
    {

#if FEATURE_PAL

#if !PLATFORM_UNIX
        internal const String DLLPREFIX = "";
        internal const String DLLSUFFIX = ".dll";
#else // !PLATFORM_UNIX
#if __APPLE__
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".dylib";
#elif _AIX
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".a";
#elif __hppa__ || IA64
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".sl";
#else
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".so";
#endif
#endif // !PLATFORM_UNIX

        public const string Kernel32 = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        public const string User32 = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        public const string Mscoree  = DLLPREFIX + "sscoree" + DLLSUFFIX;
#else
        public const string Activeds = "activeds.dll";
        public const string Advapi32 = "advapi32.dll";
        public const string Comctl32 = "comctl32.dll";
        public const string Comdlg32 = "comdlg32.dll";
        public const string Gdi32 = "gdi32.dll";
        public const string Gdiplus = "gdiplus.dll";
        public const string Hhctrl = "hhctrl.ocx";
        public const string Imm32 = "imm32.dll";
        public const string Kernel32 = "kernel32.dll";
        public const string Loadperf = "Loadperf.dll";
        public const string Mscoree = "mscoree.dll";
        public const string Mscorwks = "mscorwks.dll";
        public const string Msi = "msi.dll";
        public const string Mqrt = "mqrt.dll";
        public const string Ntdll = "ntdll.dll";
        public const string Ole32 = "ole32.dll";
        public const string Oleacc = "oleacc.dll";
        public const string Oleaut32 = "oleaut32.dll";
        public const string Olepro32 = "olepro32.dll";
        public const string PerfCounter = "perfcounter.dll";
        public const string Powrprof = "Powrprof.dll";
        public const string Psapi = "psapi.dll";
        public const string Shell32 = "shell32.dll";
        public const string Shfolder = "shfolder.dll";
        public const string User32 = "user32.dll";
        public const string Uxtheme = "uxtheme.dll";
        public const string WinMM = "winmm.dll";
        public const string Winspool = "winspool.drv";
        public const string Wtsapi32 = "wtsapi32.dll";
        public const string Version = "version.dll";
        public const string Vsassert = "vsassert.dll";
        public const string Shlwapi = "shlwapi.dll";
        public const string Crypt32 = "crypt32.dll";

        // system.data specific
        internal const string Odbc32 = "odbc32.dll";
        internal const string SNI = "System.Data.dll";

        // system.data.oracleclient specific
        internal const string OciDll = "oci.dll";
        internal const string OraMtsDll = "oramts.dll";
#endif //!FEATURE_PAL
    }

    internal static class UnsafeNativeMethods
    {
        [DllImport(ExternDll.Kernel32, EntryPoint = "GlobalLock", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr GlobalLock(IntPtr h);

        [DllImport(ExternDll.Kernel32, EntryPoint = "GlobalUnlock", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool GlobalUnLock(IntPtr h);

        [DllImport(ExternDll.Kernel32, EntryPoint = "GlobalSize", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern int GlobalSize(IntPtr h);

        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int OleSetClipboard(Microsoft.VisualStudio.OLE.Interop.IDataObject dataObject);

        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int OleGetClipboard(out Microsoft.VisualStudio.OLE.Interop.IDataObject dataObject);

        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int OleFlushClipboard();

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int OpenClipboard(IntPtr newOwner);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int EmptyClipboard();

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int CloseClipboard();

        [DllImport(ExternDll.Comctl32, CharSet = CharSet.Auto)]
        internal static extern int ImageList_GetImageCount(HandleRef himl);

        [DllImport(ExternDll.Comctl32, CharSet = CharSet.Auto)]
        internal static extern bool ImageList_Draw(HandleRef himl, int i, HandleRef hdcDst, int x, int y, int fStyle);

        [DllImport(ExternDll.Shell32, EntryPoint = "DragQueryFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern uint DragQueryFile(IntPtr hDrop, uint iFile, char[] lpszFile, uint cch);

        [DllImport(ExternDll.User32, EntryPoint = "RegisterClipboardFormatW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern ushort RegisterClipboardFormat(string format);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), DllImport(ExternDll.Shell32, EntryPoint = "SHGetSpecialFolderLocation")]
        internal static extern int SHGetSpecialFolderLocation(IntPtr hwnd, int csidl, [Out, MarshalAs(UnmanagedType.LPArray)] IntPtr[] ppidl);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), DllImport(ExternDll.Shell32, EntryPoint = "SHGetPathFromIDList", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);
    }
}
