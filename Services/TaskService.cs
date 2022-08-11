using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RevitTimasBIMTools.Services
{
    internal class TaskService
    {
        public static void DelayCall(Action action, int msec = 100)
        {
            Task.Delay(msec).ContinueWith(t => Dispatcher.CurrentDispatcher.InvokeAsync(action));
        }
    }
}
