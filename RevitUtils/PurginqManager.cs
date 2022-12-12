using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitPurginqManager
    {
        public static IDictionary<int, ElementId> PurgeAndGetValidConstructionTypeIds(Document doc)
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


            collector = new FilteredElementCollector(doc).OfClass(typeof(ElementType));
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
                // if there are any failures, rollback the t
                return failuresAccessor.GetFailureMessages().Count > 0
                    ? FailureProcessingResult.ProceedWithRollBack
                    : FailureProcessingResult.Continue;
            }
        }


        public static void Purge(Document doc)
        {
            //The internal GUID of the Performance Adviser Rule 
            const string PurgeGuid = "e8c63650-70b7-435a-9010-ec97660c1bda";

            List<PerformanceAdviserRuleId> performanceAdviserRuleIds = new();

            //Iterating through all PerformanceAdviser rules looking to filled that which matches PURGE_GUID
            foreach (PerformanceAdviserRuleId performanceAdviserRuleId in PerformanceAdviser.GetPerformanceAdviser().GetAllRuleIds())
            {
                if (performanceAdviserRuleId.Guid.ToString() == PurgeGuid)
                {
                    performanceAdviserRuleIds.Add(performanceAdviserRuleId);
                    break;
                }
            }

            //Attempting to recover all purgeable elements and delete them from the docmodel
            List<ElementId> purgeableIds = GetPurgeableElements(doc, performanceAdviserRuleIds);
            if (purgeableIds != null)
            {
                using Transaction t = new(doc, "Purge");
                if (t.Start() == TransactionStatus.Started)
                {
                    try
                    {
                        _ = doc.Delete(purgeableIds);
                        _ = t.Commit();
                    }
                    catch (Exception)
                    {
                        if (!t.HasEnded())
                        {
                            _ = t.RollBack();
                        }
                    }
                }
            }
        }


        internal static List<ElementId> GetPurgeableElements(Document doc, List<PerformanceAdviserRuleId> performanceAdviserRuleIds)
        {
            List<FailureMessage> failureMessages = PerformanceAdviser.GetPerformanceAdviser().ExecuteRules(doc, performanceAdviserRuleIds).ToList();
            if (failureMessages.Count > 0)
            {
                List<ElementId> purgeableElementIds = failureMessages[0].GetFailingElements().ToList();
                return purgeableElementIds;
            }
            return null;
        }


    }
}




