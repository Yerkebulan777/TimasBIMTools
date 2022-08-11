using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace RevitTimasBIMTools.Services
{
    public class Timer
    {
        internal long freq;
        private long startTime, stopTime;

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        public Timer(TimerCallback timerCallback)
        {
            startTime = 0; stopTime = 0;
            if (!QueryPerformanceFrequency(out freq))
            {
                throw new Win32Exception("high-performance counter not supported");
            }
        }

        public void Start()
        {
            Thread.Sleep(0); // let waiting threads work
            QueryPerformanceCounter(out startTime);
        }


        public void Stop()
        {
            QueryPerformanceCounter(out stopTime);
        }

        public double Duration
        {
            get
            {
                return (stopTime - startTime) / (double)freq;
            }
        }
    }
}
