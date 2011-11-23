//
//! Copyright © 2008-2011
//! Brandon Kohn
//
//  Distributed under the Boost Software License, Version 1.0. (See
//  accompanying file LICENSE_1_0.txt or copy at
//  http://www.boost.org/LICENSE_1_0.txt)
//
//! Adapted from code from http://geekswithblogs.net/dbose/archive/2009/10/17/c-activeobject-runnable.aspx
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Meta
{    
    /// <summary>
    /// Active Object (runnable) interface
    /// </summary>
    public interface IActiveObject
    {
        /// <summary>
        /// Initialize an active object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        void Initialize(string name, Action action);

        /// <summary>
        /// Signal the active object to perform its loop action.
        /// </summary>
        /// <remarks>
        /// Application may call this after some simple or complex condition evaluation
        /// </remarks>
        void Signal();

        /// <summary>
        /// Signals to shutdown this active object
        /// </summary>
        void Shutdown();
    }
    
    /// <summary>
    /// Implements a simple active object pattern implementation
    /// </summary>
    /// <remarks>
    /// Although there exists a vast number of active objects patterns (in Java they are just "runnable")
    /// scattered, one of the best I found is located at http://blog.gurock.com/wp-content/uploads/2008/01/activeobjects.pdf
    /// </remarks>
    public class ActiveObject : IActiveObject
    {
        /// <summary>
        /// Name of this active object
        /// </summary>
        private string m_Name;
       
        /// <summary>
        /// Underlying active thread
        /// </summary>
        private Thread m_ActiveThreadContext;

        /// <summary>
        /// Abstracted action that the active thread executes
        /// </summary>
        private Action m_ActiveAction;

        /// <summary>
        /// Primary signal object for this active thread.
        /// See the Signal() method for more.
        /// </summary>
        private AutoResetEvent m_SignalObject;

        /// <summary>
        /// Signal object for shutting down this active object
        /// </summary>
        private ManualResetEvent m_ShutdownEvent;

        /// <summary>
        /// Internal array of signal objects combining primary signal object and
        /// shutdown signal object
        /// </summary>
        private WaitHandle[] m_SignalObjects;

        private int maxStackSize = 1000000;

        public ActiveObject()
        {
        }

        public ActiveObject(int maxStack)
        {
            maxStackSize = maxStack;
        }

        public void Initialize(string name, Action action)
        {
            m_Name = name;
            m_ActiveAction = action;
            m_SignalObject = new AutoResetEvent(false);
            m_ShutdownEvent = new ManualResetEvent(false);
            m_SignalObjects = new WaitHandle[]
                                {
                                    m_ShutdownEvent,
                                    m_SignalObject
                                };

            m_ActiveThreadContext = new Thread(Run, maxStackSize);
            m_ActiveThreadContext.Name = string.Concat("ActiveObject.", m_Name);
            m_ActiveThreadContext.Start();
        }
       
        private bool Guard()
        {
            int index = WaitHandle.WaitAny(m_SignalObjects);
            return index == 0 ? false : true;
        }
       
        /// <summary>
        /// Signal the active object to perform its loop action.
        /// </summary>
        /// <remarks>
        /// Application may call this after some simple of complex condition evaluation
        /// </remarks>
        public void Signal()
        {
            m_SignalObject.Set();
        }
       
        /// <summary>
        /// Signals to shutdown this active object
        /// </summary>
        public void Shutdown()
        {
            m_ShutdownEvent.Set();
           
            if (m_ActiveThreadContext != null)
            {
                m_ActiveThreadContext.Join();
            }
           
            m_ActiveThreadContext = null;
        }
       
        /// <summary>
        /// Core run method of this active thread
        /// </summary>
        private void Run()
        {
            try
            {
                while (Guard())
                {
                    try
                    {
                        m_ActiveAction();
                    }
                    catch (System.OutOfMemoryException /*ex*/)
                    {
                        string message = "The Tools->Meta->Options page specifies a " + maxStackSize + " byte stack reserve size. This exceeds available memory."
                            + Environment.NewLine + "Please try again with a lower stack size reserve value.";
                        string caption = "Stack Reserve Size Too Large...";
                        MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    catch (Exception /*ex*/)
                    {
                        //! Log the exception?
                    }
                    finally
                    {
                        m_ShutdownEvent.Set();
                    }
                }
            }
            catch(Exception /*ex*/)
            {
                //!Log it?
            }
            finally
            {
                m_SignalObject.Close();
                m_ShutdownEvent.Close();
               
                m_SignalObject = null;
                m_ShutdownEvent = null;
            }
        }
    }
}
