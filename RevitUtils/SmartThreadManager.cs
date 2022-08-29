using Amib.Threading;
using RevitTimasBIMTools.Services;
using System;
using System.Threading;


namespace RevitTimasBIMTools.RevitUtils
{
    internal class SmartThreadManager
    {
        public void DoWork(object state)
        {

            // create a STPStartInfo object and change the default values
            STPStartInfo stpStartInfo = new STPStartInfo
            {
                MinWorkerThreads = 8,
                MaxWorkerThreads = 16,
                DisposeOfStateObjects = true,
                ThreadPriority = ThreadPriority.AboveNormal
            };

            // create the SmartThreadPool instance
            SmartThreadPool smartThreadPool = new SmartThreadPool(stpStartInfo);
            IWorkItemResult wir = smartThreadPool.QueueWorkItem(new WorkItemCallback(DoRealWork), state);

            // Wait for the completion of all work items
            smartThreadPool.WaitForIdle();

            object obj = wir.GetResult(out Exception exc);

            if (null == exc)
            {
                int result = (int)obj;
            }
            else
            {
                RevitLogger.Error(exc.Message);
            }
            smartThreadPool.Shutdown();
        }

        private object DoRealWork(object state)
        {
            object result = null;

            // Do the real work here and put 
            // the result in 'result'

            return result;
        }
    }
}
