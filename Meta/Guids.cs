//
//! Copyright © 2008-2011
//! Brandon Kohn
//
//  Distributed under the Boost Software License, Version 1.0. (See
//  accompanying file LICENSE_1_0.txt or copy at
//  http://www.boost.org/LICENSE_1_0.txt)
//

// Guids.cs
// MUST match guids.h
using System;

namespace Meta
{
    static class GuidList
    {
        public const string guidMetaPkgString = "92203069-8ed1-4b40-8356-b4cfab306222";
        
        //! First context command.
        public const string guidMetaCmdSet1String = "0af37c6f-ab62-482b-944b-9580f3ffd0ff";
        public static readonly Guid guidMetaCmdSet1 = new Guid(guidMetaCmdSet1String);
        
        //! Second context command.
        public const string guidMetaCmdSet2String = "917C8C81-77E9-4B2B-93BC-027ED5FFCD9A";
        public static readonly Guid guidMetaCmdSet2 = new Guid(guidMetaCmdSet2String);

        //! Third context command.
        public const string guidMetaCmdSet3String = "B2754D27-A16B-416E-B171-9B7E55D7CADB";
        public static readonly Guid guidMetaCmdSet3 = new Guid(guidMetaCmdSet3String);

        //! Profile output pane in output pane window.
        public const string guidProfileOutputPaneString = "{B674BFDB-B586-487E-A113-282A00550056}";
        public static readonly Guid guidProfileOutputPane = new Guid(guidProfileOutputPaneString);
    };
}