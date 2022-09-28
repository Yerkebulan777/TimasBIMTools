using Autodesk.Revit.DB;
using System;

namespace RevitTimasBIMTools.RevitUtils
{
    public static class RevitTransactionManager
    {
        /// <summary> The method used to create a single sub-transaction </summary>
        public static void CreateSubTransaction(Document document, Action action)
        {
            using (SubTransaction transaction = new SubTransaction(document))
            {
                _ = transaction.Start();
                try
                {
                    action?.Invoke();
                    _ = transaction.Commit();
                }
                catch (Exception)
                {
                    if (!transaction.HasEnded())
                    {
                        _ = transaction.RollBack();
                    }
                }
            }
        }

        /// <summary> The method used to create a single transaction </summary>
        public static void CreateTransaction(Document document, string transactionName, Action action)
        {
            using (Transaction transaction = new Transaction(document))
            {
                if (transaction.Start(transactionName) == TransactionStatus.Started)
                {
                    try
                    {
                        action?.Invoke();
                        _ = transaction.Commit();
                    }
                    catch (Exception)
                    {
                        if (!transaction.HasEnded())
                        {
                            _ = transaction.RollBack();
                        }
                    }
                    finally { }
                }
            }
        }
    }
}