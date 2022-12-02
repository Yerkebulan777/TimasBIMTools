using Autodesk.Revit.DB;
using System;

namespace RevitTimasBIMTools.RevitUtils
{
    public static class TransactionManager
    {
        /// <summary> The method used to create a single sub-strx </summary>
        public static void CreateSubTransaction(Document document, Action action)
        {
            using (SubTransaction strx = new SubTransaction(document))
            {
                _ = strx.Start();
                try
                {
                    action?.Invoke();
                    _ = strx.Commit();
                }
                catch (Exception)
                {
                    if (!strx.HasEnded())
                    {
                        _ = strx.RollBack();
                    }
                }
            }
        }

        /// <summary> The method used to create a single strx </summary>
        public static void CreateTransaction(Document document, string transactionName, Action action)
        {
            using (Transaction trx = new Transaction(document))
            {
                if (trx.Start(transactionName) == TransactionStatus.Started)
                {
                    try
                    {
                        action?.Invoke();
                        _ = trx.Commit();
                    }
                    catch (Exception)
                    {
                        if (!trx.HasEnded())
                        {
                            _ = trx.RollBack();
                        }
                    }
                    finally { }
                }
            }
        }
    }
}