using System;
using System.Threading;
using System.Threading.Tasks;

namespace RevitTimasBIMTools.Core
{
    public class BaseCompletedEventArgs : EventArgs
    {
        public SynchronizationContext SyncContext { get; }
        public TaskScheduler TaskContext { get; }
        public BaseCompletedEventArgs(SynchronizationContext syncContext, TaskScheduler taskContext)
        {
            SyncContext = syncContext;
            TaskContext = taskContext;
        }

    }
}
