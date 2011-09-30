using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meta
{
    internal class TaskTimer
    {
        public string itemName = null;
        public string name = null;
        Stopwatch sw = new Stopwatch();
        public TaskTimer(string job)
        {
            name = job;
        }

        public string ItemName
        {
            get
            {
                return itemName;
            }

            set
            {
                itemName = value;
            }
        }

        public void StartTask(string task)
        {
            sw.Restart();
            itemName = task;
        }

        public void Stop()
        {
            sw.Stop();
        }

        public string Elapsed()
        {
            TimeSpan ts = sw.Elapsed;
            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}",
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);

            return elapsedTime;
        }

        public TimeSpan Mark()
        {
            return sw.Elapsed;
        }
    }
}
