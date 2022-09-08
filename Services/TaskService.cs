using System;

namespace RevitTimasBIMTools.Services
{
    internal sealed class TaskService
    {
        public static void RevitTaskCall(Action action, int msec = 1000)
        {
            //RevitTask.Run(msec).ContinueWith(t => Dispatcher.CurrentDispatcher.Invoke(action));
        }
    }
}
