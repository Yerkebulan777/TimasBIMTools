using Autodesk.Revit.DB;
using RevitTimasBIMTools.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitPurginqManager
    {
        public void PurgeConstructionElementTypes(Document doc)
        {
            //  Categories whose types will be purged
            List<BuiltInCategory> purgeBuiltInCats = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
            };


            ElementMulticategoryFilter multiCat = new ElementMulticategoryFilter(purgeBuiltInCats);

            ICollection<ElementId> typesToDelete = new FilteredElementCollector(doc).OfClass(typeof(ElementType)).WherePasses(multiCat).ToElementIds();

            FilteredElementCollector collector = new FilteredElementCollector(doc).WherePasses(multiCat).WhereElementIsNotElementType();

            int count = typesToDelete.Count;

            foreach (Element e in collector)
            {
                ElementId typeId = e.GetTypeId();
                if (typesToDelete.Contains(typeId))
                {
                    if (typesToDelete.Remove(typeId))
                    {
                        count--;
                    }
                }
            }

            using (TransactionGroup tg = new TransactionGroup(doc, "Purge types"))
            {
                TransactionStatus status = tg.Start();
                foreach (ElementId id in typesToDelete)
                {
                    using (Transaction t = new Transaction(doc, "delete type"))
                    {
                        // Do not delete type if it would modelList in error such as
                        // "Last type in system family "Stacked Wall" cannot be deleted."
                        FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                        failOpt = failOpt.SetClearAfterRollback(true);
                        failOpt = failOpt.SetFailuresPreprocessor(new RollbackIfErrorOccurs());
                        t.SetFailureHandlingOptions(failOpt);

                        if (TransactionStatus.Started == t.Start())
                        {
                            try
                            {
                                status = doc.Delete(id).Any() ? t.Commit() : t.RollBack();
                            }
                            catch
                            {
                                if (!t.HasEnded())
                                {
                                    status = t.RollBack();
                                }
                            }
                        }
                    }
                }

                status = tg.Assimilate();

                Logger.Info($"All element types deleted count {count}");

            }
        }

        public class RollbackIfErrorOccurs : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                // if there are any failures, rollback the transaction
                return failuresAccessor.GetFailureMessages().Count > 0
                    ? FailureProcessingResult.ProceedWithRollBack
                    : FailureProcessingResult.Continue;
            }
        }
    }
}
