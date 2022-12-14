using Autodesk.Revit.DB;
using SmartBIMTools.Services;
using System;


namespace SmartBIMTools.RevitUtils
{
    public static class TransactionManager
    {
        private static readonly object SingleLocker = new();
        private static TransactionStatus status = TransactionStatus.Uninitialized;

        /// <summary> The method used to create a single strx </summary>
        public static void CreateTransaction(Document document, string trxName, Action action)
        {
            lock (SingleLocker)
            {
                using Transaction trx = new(document);
                status = trx.Start(trxName);
                if (status == TransactionStatus.Started)
                {
                    try
                    {
                        action?.Invoke();
                        status = trx.Commit();
                    }
                    catch (Exception ex)
                    {
                        if (!trx.HasEnded())
                        {
                            status = trx.RollBack();
                            string msg = $"Transaction: {trxName}\n";
                            SBTLogger.Error(msg + ex.ToString());
                        }
                    }
                }
            }
        }
    }
}