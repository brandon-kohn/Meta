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
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Linq;

namespace Meta
{
    public delegate int IntStringDelegate(string text);

    public class DeclareStringDelegate<ReturnType>
    {
        public delegate ReturnType Type(string text);
    }

    //! Adapt a string delegate with a return type to a void return type callback.
    public class CallbackLog<ReturnType> : MarshalByRefObject
    {
        private DeclareStringDelegate<ReturnType>.Type LogLine;

        private GCHandle logHandle;

        public delegate void CallbackDelegate(string text);

        public CallbackDelegate Callback
        {
            get { return (CallbackDelegate)logHandle.Target; }
        }

        public CallbackLog( DeclareStringDelegate<ReturnType>.Type logSink )
        {
            logHandle = GCHandle.Alloc(new CallbackDelegate(LogText));
            LogLine = logSink;
        }

        ~CallbackLog()
        {
            logHandle.Free();
        }
        
        //! Can do preprocessing of text here before sending to next delegate.
        public void LogText(string text)
        {
            if (LogLine != null)
                LogLine(text);
        }
    }
}
