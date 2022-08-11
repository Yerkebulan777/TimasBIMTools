using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RevitTimasBIMTools.Services
{
    public static class ThreadService
    {
        public static async void GetInfo()
        {
            StringBuilder builder = new StringBuilder();
            Dispatcher dispatch = Dispatcher.CurrentDispatcher;
            System.Threading.Thread thread = dispatch.Thread;
            await dispatch.InvokeAsync(new Action(() =>
            {
                Task.Delay(50);
                Task.Run(() =>
                {
                    builder.AppendLine("Is Event DocumentChanged");
                    builder.AppendLine($"\tThread name: {thread.Name}");
                    builder.AppendLine($"\tPriority: {thread.Priority}");
                    builder.AppendLine("\tIs in Pool thread = " + thread.IsThreadPoolThread.ToString());
                    builder.AppendLine("\tThread state: " + thread.ThreadState.ToString());
                    builder.Clear();
                    builder = null;
                });
            }));
        }
    }
}
