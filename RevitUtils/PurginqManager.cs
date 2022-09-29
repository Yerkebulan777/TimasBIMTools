using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitPurginqManager
    {

        public ICollection<ElementId> PurgeAndGetValidConstructionTypeIds(Document doc)
        {
            //  Categories whose types will be purged
            List<BuiltInCategory> purgeBuiltInCats = new()
            {
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
            };

            ElementMulticategoryFilter multiCat = new(purgeBuiltInCats);

            ICollection<ElementId> validTypeIds = new FilteredElementCollector(doc).OfClass(typeof(ElementType)).WherePasses(multiCat).ToElementIds();

            FilteredElementCollector collector = new FilteredElementCollector(doc).WherePasses(multiCat).WhereElementIsNotElementType();

            IList<ElementId> typeIdsToDelete = new List<ElementId>(Convert.ToInt16(validTypeIds.Count * 0.25));

            foreach (Element e in collector)
            {
                ElementId typeId = e.GetTypeId();
                if (!validTypeIds.Contains(typeId))
                {
                    if (validTypeIds.Remove(typeId))
                    {
                        typeIdsToDelete.Add(typeId);
                    }
                }
            }

            using TransactionGroup tg = new(doc, "Purge types");
            TransactionStatus status = tg.Start();
            foreach (ElementId id in typeIdsToDelete)
            {
                using Transaction t = new(doc, "delete type");
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
            status = tg.Assimilate();

            return validTypeIds;
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
