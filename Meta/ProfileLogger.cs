//
//! Copyright © 2008-2011
//! Brandon Kohn
//
//  Distributed under the Boost Software License, Version 1.0. (See
//  accompanying file LICENSE_1_0.txt or copy at
//  http://www.boost.org/LICENSE_1_0.txt)
//
using System;
using System.Windows.Forms.Design;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Win32;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Meta
{
    //! Jobs need to be identifiable from build events using build submission ID, project ID, target ID and/or task ID. (maybe thread id)
    internal class TaskID : IComparable
    {
        public TaskID(BuildEventContext ctx)
        {
            this.ctx = ctx;
        }

        private int Compare<T>(T lhs, T rhs) where T : System.IComparable
        {
            return lhs.CompareTo(rhs);
        }

        public int CompareTo(object obj)
        {
            if (obj is TaskID)
            {
                TaskID rhs = (TaskID)obj;

                int result;
                if ((result = ctx.BuildRequestId.CompareTo(rhs.ctx.BuildRequestId)) != 0)
                    return result;

                if ((result = ctx.SubmissionId.CompareTo(rhs.ctx.SubmissionId)) != 0)
                    return result;

                if ((result = ctx.NodeId.CompareTo(rhs.ctx.NodeId)) != 0)
                    return result;

                if ((result = ctx.ProjectInstanceId.CompareTo(rhs.ctx.ProjectInstanceId)) != 0)
                    return result;

                if ((result = ctx.TargetId.CompareTo(rhs.ctx.TargetId)) != 0)
                    return result;

                if ((result = ctx.TaskId.CompareTo(rhs.ctx.TaskId)) != 0)
                    return result;

                return 0;
            }

            throw new ArgumentException("object is not a TaskID");
        }

        public bool Equals(TaskID rhs)
        {
            // If parameter is null return false:
            if ((object)rhs == null)
                return false;

            return (ctx.BuildRequestId == rhs.ctx.BuildRequestId &&
                     ctx.SubmissionId == rhs.ctx.SubmissionId &&
                     ctx.NodeId == rhs.ctx.NodeId &&
                     ctx.ProjectContextId == rhs.ctx.ProjectContextId &&
                     ctx.ProjectInstanceId == rhs.ctx.ProjectInstanceId &&
                     ctx.TargetId == rhs.ctx.TargetId &&
                     ctx.TaskId == rhs.ctx.TaskId);
        }

        private BuildEventContext ctx;
    }

    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "IDE")]
    internal class ProfileLogger : Logger, IDisposable
    {        
        #region fields
        private SortedDictionary<TaskID, TaskTimer> taskTimerDict = new SortedDictionary<TaskID, TaskTimer>();

        private IVsOutputWindowPane outputPane;
        private TaskProvider taskProvider;
        private IVsHierarchy hierarchy;
        private IServiceProvider serviceProvider;
        private Dispatcher dispatcher;

        // Queues to manage Tasks and Error output plus message logging
        private ConcurrentQueue<Func<ErrorTask>> taskQueue;
        private ConcurrentQueue<string> outputQueue;

        #endregion

        #region disposable
        public void Dispose()
        {
            //serviceProvider.Dispose();
        }
        #endregion

        #region properties

        public IServiceProvider ServiceProvider
        {
            get { return this.serviceProvider; }
        }

        internal IVsOutputWindowPane OutputPane
        {
            get { return this.outputPane; }
            set { this.outputPane = value; }
        }

        #endregion

        #region ctors

        /// <summary>
        /// Constructor.  Initialize member data.
        /// </summary>
        public ProfileLogger(IVsOutputWindowPane profileOutput, TaskProvider taskProvider, IVsHierarchy hierarchy)
        {
            base.Verbosity = LoggerVerbosity.Minimal;
            if(taskProvider == null)
                throw new ArgumentNullException("taskProvider");
            if(hierarchy == null)
                throw new ArgumentNullException("hierarchy");

            Trace.WriteLineIf(Thread.CurrentThread.GetApartmentState() != ApartmentState.STA, "WARNING: ProfileLogger constructor running on the wrong thread.");

            IOleServiceProvider site;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hierarchy.GetSite(out site));

            this.taskProvider = taskProvider;
            outputPane = profileOutput;            
            this.hierarchy = hierarchy;
            serviceProvider = new ServiceProvider(site);
            dispatcher = Dispatcher.CurrentDispatcher;
        }

        #endregion

        #region overridden methods

        public override void Initialize(IEventSource eventSource)
        {
            if(null == eventSource)
            {
                throw new ArgumentNullException("eventSource");
            }

            this.taskQueue = new ConcurrentQueue<Func<ErrorTask>>();
            this.outputQueue = new ConcurrentQueue<string>();

            eventSource.BuildStarted += new BuildStartedEventHandler(BuildStartedHandler);
            eventSource.BuildFinished += new BuildFinishedEventHandler(BuildFinishedHandler);
            eventSource.ProjectStarted += new ProjectStartedEventHandler(ProjectStartedHandler);
            eventSource.ProjectFinished += new ProjectFinishedEventHandler(ProjectFinishedHandler);
            eventSource.TargetStarted += new TargetStartedEventHandler(TargetStartedHandler);
            eventSource.TargetFinished += new TargetFinishedEventHandler(TargetFinishedHandler);
            eventSource.TaskStarted += new TaskStartedEventHandler(TaskStartedHandler);
            eventSource.TaskFinished += new TaskFinishedEventHandler(TaskFinishedHandler);
            eventSource.CustomEventRaised += new CustomBuildEventHandler(CustomHandler);
            eventSource.ErrorRaised += new BuildErrorEventHandler(ErrorHandler);
            eventSource.WarningRaised += new BuildWarningEventHandler(WarningHandler);
            eventSource.MessageRaised += new BuildMessageEventHandler(MessageHandler);
        }

        #endregion

        #region event delegates

        /// <summary>
        /// This is the delegate for BuildStartedHandler events.
        /// </summary>
        protected virtual void BuildStartedHandler(object sender, BuildStartedEventArgs buildEvent)
        {
            // NOTE: This may run on a background thread!
            TaskTimer job = new TaskTimer("Total Build");
            job.StartTask("Total Build Time");
            taskTimerDict[new TaskID(new BuildEventContext(-1, -1, -1, -1))] = job;
            
            ClearQueuedOutput();
            ClearQueuedTasks();

            QueueOutputText(MessageImportance.High, Environment.NewLine + String.Format("{0,-60}: {1,12}", "Translation Unit", "Compile Time (hh:mm:ss:ms)"));
            StringBuilder divider = new StringBuilder(88);
            divider.Append('-', 88);
            QueueOutputText(MessageImportance.High, Environment.NewLine + divider);            

            //QueueOutputEvent(MessageImportance.Low, buildEvent);
        }

        /// <summary>
        /// This is the delegate for BuildFinishedHandler events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="buildEvent"></param>
        protected virtual void BuildFinishedHandler(object sender, BuildFinishedEventArgs buildEvent)
        {
            // NOTE: This may run on a background thread!            
            TaskTimer job = null;
            TaskID id = new TaskID(new BuildEventContext(-1, -1, -1, -1));
            if(taskTimerDict.TryGetValue(id, out job))
            {
                string elapsedTime = job.Elapsed();
                taskTimerDict.Remove(id);
                QueueOutputText(Environment.NewLine);
                QueueOutputText(Environment.NewLine);
                QueueOutputText("Total Build RunTime: " + elapsedTime);
            }
            
            MessageImportance importance = buildEvent.Succeeded ? MessageImportance.Low : MessageImportance.High;
            //QueueOutputText(importance, Environment.NewLine);
            //QueueOutputEvent(importance, buildEvent);

            // flush output and error queues
            ReportQueuedOutput();
            ReportQueuedTasks();
        }

        /// <summary>
        /// This is the delegate for ProjectStartedHandler events.
        /// </summary>
        protected virtual void ProjectStartedHandler(object sender, ProjectStartedEventArgs buildEvent)
        {
            // NOTE: This may run on a background thread!
            //QueueOutputEvent(MessageImportance.Low, buildEvent);
        }

        /// <summary>
        /// This is the delegate for ProjectFinishedHandler events.
        /// </summary>
        protected virtual void ProjectFinishedHandler(object sender, ProjectFinishedEventArgs buildEvent)
        {
            // NOTE: This may run on a background thread!
            //QueueOutputEvent(buildEvent.Succeeded ? MessageImportance.Low : MessageImportance.High, buildEvent);
        }

        /// <summary>
        /// This is the delegate for TargetStartedHandler events.
        /// </summary>
        protected virtual void TargetStartedHandler(object sender, TargetStartedEventArgs buildEvent)
        {
            TaskTimer job = new TaskTimer(buildEvent.TargetFile);
            job.StartTask(null);
            TaskID id = new TaskID(buildEvent.BuildEventContext);
            taskTimerDict[id] = job;
            
            //QueueOutputEvent(MessageImportance.Low, buildEvent);            
        }

        /// <summary>
        /// This is the delegate for TargetFinishedHandler events.
        /// </summary>
        protected virtual void TargetFinishedHandler(object sender, TargetFinishedEventArgs buildEvent)
        {
            TaskTimer job = null;
            TaskID id = new TaskID(buildEvent.BuildEventContext);
            if(taskTimerDict.TryGetValue(id, out job))
            {
                string elapsedTime = job.Elapsed();
                string itemName = job.ItemName;
                taskTimerDict.Remove(id);
            }
            
            //QueueOutputEvent(MessageImportance.Low, buildEvent);
        }

        /// <summary>
        /// This is the delegate for TaskStartedHandler events.
        /// </summary>
        protected virtual void TaskStartedHandler(object sender, TaskStartedEventArgs buildEvent)
        {
            TaskTimer job = new TaskTimer(buildEvent.TaskFile);
            job.StartTask(null);
            TaskID id = new TaskID(buildEvent.BuildEventContext);
            taskTimerDict[id] = job;
            
            //QueueOutputEvent(MessageImportance.Low, buildEvent);            
        }

        /// <summary>
        /// This is the delegate for TaskFinishedHandler events.
        /// </summary>
        protected virtual void TaskFinishedHandler(object sender, TaskFinishedEventArgs buildEvent)
        {
            // NOTE: This may run on a background thread!
            TaskTimer job = null;
            TaskID id = new TaskID(buildEvent.BuildEventContext);
            if(taskTimerDict.TryGetValue(id, out job))
            {
                string elapsedTime = job.Elapsed();
                string itemName = job.ItemName;
                taskTimerDict.Remove(id);

                if(job.ItemName != null)
                {
                    QueueOutputText(MessageImportance.High, Environment.NewLine);
                    QueueOutputText(MessageImportance.High, String.Format("{0,-60}: {1,12}", itemName, elapsedTime));
                }
            }
           
            //QueueOutputEvent(MessageImportance.Low, buildEvent);
        }

        /// <summary>
        /// This is the delegate for CustomHandler events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="buildEvent"></param>
        protected virtual void CustomHandler(object sender, CustomBuildEventArgs buildEvent)
        {
            // NOTE: This may run on a background thread!
            //QueueOutputEvent(MessageImportance.Low, buildEvent);
        }

        /// <summary>
        /// This is the delegate for error events.
        /// </summary>
        protected virtual void ErrorHandler(object sender, BuildErrorEventArgs errorEvent)
        {
            // NOTE: This may run on a background thread!
            //QueueOutputText(Environment.NewLine);
            //QueueOutputText(GetFormattedErrorMessage(errorEvent.File, errorEvent.LineNumber, errorEvent.ColumnNumber, false, errorEvent.Code, errorEvent.Message));
            //QueueTaskEvent(errorEvent);
        }

        /// <summary>
        /// This is the delegate for warning events.
        /// </summary>
        protected virtual void WarningHandler(object sender, BuildWarningEventArgs warningEvent)
        {
            // NOTE: This may run on a background thread!
            //MessageImportance importance = MessageImportance.High;
            //QueueOutputText(Environment.NewLine);
            //QueueOutputText(importance, GetFormattedErrorMessage(warningEvent.File, warningEvent.LineNumber, warningEvent.ColumnNumber, true, warningEvent.Code, warningEvent.Message));
            //QueueTaskEvent(warningEvent);
        }

        /// <summary>
        /// This is the delegate for Message event types
        /// </summary>		
        protected virtual void MessageHandler(object sender, BuildMessageEventArgs messageEvent)
        {
            TaskID id = new TaskID(messageEvent.BuildEventContext);
            TaskTimer job = null;
            if(taskTimerDict.TryGetValue(id, out job))
            {
                if(job != null)
                {
                    Regex filenameRegex = new Regex(@"^[^\\/:<>\|\*""\?]+\.(?:c(pp|xx|c|s))$");
                    if(filenameRegex.IsMatch(messageEvent.Message) || messageEvent.Message == "Generating Code...")
                    {
                        if(job.ItemName != null)
                        {
                            string elapsedTime = job.Elapsed();
                            string itemName = job.ItemName;
                            QueueOutputText(MessageImportance.High, Environment.NewLine);
                            QueueOutputText(MessageImportance.High, String.Format("{0,-60}: {1,12}", itemName, elapsedTime));
                        }

                        job.StartTask(messageEvent.Message);
                    }
                }
            }

            // NOTE: This may run on a background thread!
            //MessageImportance importance = MessageImportance.Low;
            //QueueOutputText(importance, Environment.NewLine);
            //QueueOutputEvent(importance, messageEvent);
        }

        #endregion

        #region output queue

        protected void QueueOutputEvent(MessageImportance importance, BuildEventArgs buildEvent)
        {
            // NOTE: This may run on a background thread!
            if(LogAtImportance(importance) && !string.IsNullOrEmpty(buildEvent.Message))
            {
                QueueOutputText(buildEvent.Message);
            }
        }

        protected void QueueOutputText(MessageImportance importance, string text)
        {
            // NOTE: This may run on a background thread!
            if(LogAtImportance(importance))
            {
                QueueOutputText(text);
            }
        }

        protected void QueueOutputText(string text)
        {
            // NOTE: This may run on a background thread!
            if(this.outputPane != null)
            {
                // Enqueue the output text
                this.outputQueue.Enqueue(text);

                // We want to interactively report the output. But we don't want to dispatch
                // more than one at a time, otherwise we might overflow the main thread's
                // message queue. So, we only report the output if the queue was empty.
                if(this.outputQueue.Count == 1)
                {
                    ReportQueuedOutput();
                }
            }
        }
        
        private void ReportQueuedOutput()
        {
            // NOTE: This may run on a background thread!
            // We need to output this on the main thread. We must use BeginInvoke because the main thread may not be pumping events yet.
            BeginInvokeWithErrorMessage(this.serviceProvider, this.dispatcher, () =>
            {
                if(this.outputPane != null)
                {
                    string outputString;

                    while (this.outputQueue.TryDequeue(out outputString))
                    {
                        Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(this.outputPane.OutputStringThreadSafe(outputString));
                    }
                }
            });
        }

        private void ClearQueuedOutput()
        {
            // NOTE: This may run on a background thread!
            this.outputQueue = new ConcurrentQueue<string>();
        }

        #endregion output queue

        #region task queue

        protected void QueueTaskEvent(BuildEventArgs errorEvent)
        {
            this.taskQueue.Enqueue(() =>
            {
                ErrorTask task = new ErrorTask();

                if(errorEvent is BuildErrorEventArgs)
                {
                    BuildErrorEventArgs errorArgs = (BuildErrorEventArgs)errorEvent;
                    task.Document = errorArgs.File;
                    task.ErrorCategory = TaskErrorCategory.Error;
                    task.Line = errorArgs.LineNumber - 1; // The task list does +1 before showing this number.
                    task.Column = errorArgs.ColumnNumber;
                    task.Priority = TaskPriority.High;
                }
                else if(errorEvent is BuildWarningEventArgs)
                {
                    BuildWarningEventArgs warningArgs = (BuildWarningEventArgs)errorEvent;
                    task.Document = warningArgs.File;
                    task.ErrorCategory = TaskErrorCategory.Warning;
                    task.Line = warningArgs.LineNumber - 1; // The task list does +1 before showing this number.
                    task.Column = warningArgs.ColumnNumber;
                    task.Priority = TaskPriority.Normal;
                }

                task.Text = errorEvent.Message;
                task.Category = TaskCategory.BuildCompile;
                task.HierarchyItem = hierarchy;

                return task;
            });

            // NOTE: Unlike output we don't want to interactively report the tasks. So we never queue
            // call ReportQueuedTasks here. We do this when the build finishes.
        }

        private void ReportQueuedTasks()
        {
            // NOTE: This may run on a background thread!
            // We need to output this on the main thread. We must use BeginInvoke because the main thread may not be pumping events yet.
            BeginInvokeWithErrorMessage(this.serviceProvider, this.dispatcher, () =>
            {
                this.taskProvider.SuspendRefresh();
                try
                {
                    Func<ErrorTask> taskFunc;

                    while (this.taskQueue.TryDequeue(out taskFunc))
                    {
                        // Create the error task
                        ErrorTask task = taskFunc();

                        // Log the task
                        this.taskProvider.Tasks.Add(task);
                    }
                }
                finally
                {
                    this.taskProvider.ResumeRefresh();
                }
            });
        }

        private void ClearQueuedTasks()
        {
            // NOTE: This may run on a background thread!
            this.taskQueue = new ConcurrentQueue<Func<ErrorTask>>();

            // We need to clear this on the main thread. We must use BeginInvoke because the main thread may not be pumping events yet.
            BeginInvokeWithErrorMessage(this.serviceProvider, this.dispatcher, () =>
            {
                this.taskProvider.Tasks.Clear();
            });            
        }

        #endregion task queue

        #region helpers

        private bool LogAtImportance(MessageImportance importance)
        {
            bool logIt = false;

            this.SetVerbosity();

            switch (this.Verbosity)
            {
                case LoggerVerbosity.Quiet:
                    logIt = false;
                    break;
                case LoggerVerbosity.Minimal:
                    logIt = (importance == MessageImportance.High);
                    break;
                case LoggerVerbosity.Normal:
                // Falling through...
                case LoggerVerbosity.Detailed:
                    logIt = (importance != MessageImportance.Low);
                    break;
                case LoggerVerbosity.Diagnostic:
                    logIt = true;
                    break;
                default:
                    Debug.Fail("Unknown Verbosity level");
                    break;
            }

            return logIt;
        }

        /*
        private string GetFormattedErrorMessage
        (
            string fileName
          , int line
          , int column
          , bool isWarning
          , string errorNumber
          , string errorText
        )
        {
            string errorCode = isWarning ? this.WarningString : this.ErrorString;

            StringBuilder message = new StringBuilder();
            if(!string.IsNullOrEmpty(fileName))
                message.AppendFormat(CultureInfo.CurrentCulture, "{0}({1},{2}):", fileName, line, column);

            message.AppendFormat(CultureInfo.CurrentCulture, " {0} {1}: {2}", errorCode, errorNumber, errorText);
            message.AppendLine();

            return message.ToString();
        }
        */

        /// <summary>
        /// Sets the verbosity level.
        /// </summary>
        private void SetVerbosity()
        {
            this.Verbosity = LoggerVerbosity.Minimal;
        }
        
        #endregion helpers

        #region exception handling helpers

        /// <summary>
        /// Call Dispatcher.BeginInvoke, showing an error message if there was a non-critical exception.
        /// </summary>
        /// <param name="serviceProvider">service provider</param>
        /// <param name="dispatcher">dispatcher</param>
        /// <param name="action">action to invoke</param>
        private static void BeginInvokeWithErrorMessage(IServiceProvider serviceProvider, Dispatcher dispatcher, Action action)
        {
            dispatcher.BeginInvoke(new Action(() => CallWithErrorMessage(serviceProvider, action)));
        }

        /// <summary>
        /// Show error message if exception is caught when invoking a method
        /// </summary>
        /// <param name="serviceProvider">service provider</param>
        /// <param name="action">action to invoke</param>
        private static void CallWithErrorMessage(IServiceProvider serviceProvider, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if(Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }

                ShowErrorMessage(serviceProvider, ex);
            }
        }

        /// <summary>
        /// Show error window about the exception
        /// </summary>
        /// <param name="serviceProvider">service provider</param>
        /// <param name="exception">exception</param>
        private static void ShowErrorMessage(IServiceProvider serviceProvider, Exception exception)
        {
            IUIService UIservice = (IUIService)serviceProvider.GetService(typeof(IUIService));
            if(UIservice != null && exception != null)
            {
                UIservice.ShowError(exception);
            }
        }

        #endregion exception handling helpers
    }
}