using Autodesk.Revit.DB;
using RevitTimasBIMTools.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitPurginqManager
    {
        public IDictionary<int, ElementId> PurgeAndGetValidConstructionTypeIds(Document doc)
        {
            //  Categories whose types will be purged
            List<BuiltInCategory> purgeBuiltInCats = new()
            {
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
            };

            bool flag = false;

            ElementMulticategoryFilter multiCat = new(purgeBuiltInCats);

            IDictionary<int, ElementId> validTypeIds = new Dictionary<int, ElementId>(25);

            IDictionary<int, ElementId> invalidTypeIds = new FilteredElementCollector(doc).OfClass(typeof(ElementType)).WherePasses(multiCat).ToDictionary(x => x.Id.IntegerValue, x => x.Id);

            foreach (Element e in new FilteredElementCollector(doc).WherePasses(multiCat).WhereElementIsNotElementType())
            {
                ElementId typeId = e.GetTypeId();
                int typeIdKey = typeId.IntegerValue;
                if (!validTypeIds.ContainsKey(typeIdKey))
                {
                    validTypeIds[typeIdKey] = typeId;
                }
            }

            foreach (KeyValuePair<int, ElementId> item in validTypeIds)
            {
                flag = invalidTypeIds.Remove(item.Key);
            }


            using TransactionGroup tg = new(doc, "Purge types");
            TransactionStatus status = tg.Start();
            foreach (KeyValuePair<int, ElementId> item in invalidTypeIds)
            {
                using Transaction t = new(doc, "\tDelete type");
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt = failOpt.SetClearAfterRollback(true);
                failOpt = failOpt.SetFailuresPreprocessor(new RollbackIfErrorOccurs());
                t.SetFailureHandlingOptions(failOpt);

                if (TransactionStatus.Started == t.Start())
                {
                    try
                    {
                        status = doc.Delete(item.Value).Any() ? t.Commit() : t.RollBack();
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
            Logger.Info($"Deleted {invalidTypeIds.Count}");
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
