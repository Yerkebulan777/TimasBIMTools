using Autodesk.Revit.DB;
using RevitTimasBIMTools.Services;
using System;


namespace RevitTimasBIMTools.RevitUtils
{
    public static class TransactionManager
    {
        private static readonly object SingleLocker = new();
        private static TransactionStatus status = TransactionStatus.Uninitialized;

        /// <summary> The method used to create a single strx </summary>
        public static void CreateTransaction(Document document, string transactionName, Action action)
        {
            lock (SingleLocker)
            {
                using Transaction trx = new(document);
                status = trx.Start(transactionName);
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
                            Logger.Error(ex.ToString());
                        }
                    }
                }
            }
        }
    }
}