﻿using Autodesk.Revit.DB;
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



            FilteredElementCollector collector;

            ElementMulticategoryFilter multiCat = new(purgeBuiltInCats);

            IDictionary<int, ElementId> validTypeIds = new Dictionary<int, ElementId>(25);
            IDictionary<int, ElementId> invalidTypeIds = new Dictionary<int, ElementId>(25);

            collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
            foreach (Element elm in collector.WherePasses(multiCat))
            {
                ElementId etypeId = elm.GetTypeId();
                int typeIntId = etypeId.IntegerValue;
                if (!validTypeIds.ContainsKey(typeIntId))
                {
                    validTypeIds[typeIntId] = etypeId;
                }
            }

            collector = new FilteredElementCollector(doc).WhereElementIsElementType();
            foreach (Element etp in collector.WherePasses(multiCat))
            {
                int typeIntId = etp.Id.IntegerValue;
                if (!validTypeIds.ContainsKey(typeIntId))
                {
                    invalidTypeIds[typeIntId] = etp.Id;
                }
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
