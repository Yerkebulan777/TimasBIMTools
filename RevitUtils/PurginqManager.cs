using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitPurginqManager
    {
        public void purgeFamiliesAndTypes(Document doc)
        {
            // List of Categories whose families will be purged
            List<int> categoriesToPurge = new List<int>
            {
                (int)BuiltInCategory.OST_StructuralFraming,
                (int)BuiltInCategory.OST_Walls
            };

            List<ElementId> typesToDelete = new List<ElementId>();

            // Check all instance types whose category is contained in the category list	
            foreach (ElementType et in new FilteredElementCollector(doc)
                     .OfClass(typeof(ElementType)).Cast<ElementType>()
                     .Where(q => q.Category != null && categoriesToPurge.Contains(q.Category.Id.IntegerValue)))
            {
                // if there are no simbols with this type, add it to the list for deletion
                if (new FilteredElementCollector(doc).WhereElementIsNotElementType()
                    .Where(q => q.GetTypeId() == et.Id).Count() == 0)
                {
                    typesToDelete.Add(et.Id);
                }
            }

            using (TransactionGroup tg = new TransactionGroup(doc, "Purge families"))
            {
                tg.Start();
                foreach (ElementId id in typesToDelete)
                {
                    using (Transaction t = new Transaction(doc, "delete type"))
                    {
                        // Do not delete type if it would modelList in error such as
                        // "Last type in system family "Stacked Wall" cannot be deleted."
                        FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                        failOpt.SetClearAfterRollback(true);
                        failOpt.SetFailuresPreprocessor(new RollbackIfErrorOccurs());
                        t.SetFailureHandlingOptions(failOpt);

                        t.Start();
                        try
                        {
                            doc.Delete(id);
                        }
                        catch
                        { }
                        t.Commit();
                    }
                }

                // Delete families that now have no types
                IList<ElementId> familiesToDelete = new List<ElementId>();
                foreach (Family family in new FilteredElementCollector(doc)
                    .OfClass(typeof(Family)).Cast<Family>()
                    .Where(q => categoriesToPurge.Contains(q.FamilyCategory.Id.IntegerValue)))
                {
                    // add family to list if there are no instances of any type of this family
                    if (new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                        .Cast<FamilyInstance>().Where(q => q.Symbol.Family.Id == family.Id).Count() == 0)
                    {
                        familiesToDelete.Add(family.Id);
                    }
                }

                using (Transaction t = new Transaction(doc, "delete families"))
                {
                    t.Start();
                    doc.Delete(familiesToDelete);
                    t.Commit();
                }

                tg.Assimilate();
            }
        }

        public class RollbackIfErrorOccurs : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                // if there are any failures, rollback the transaction
                if (failuresAccessor.GetFailureMessages().Count > 0)
                {
                    return FailureProcessingResult.ProceedWithRollBack;
                }
                else
                {
                    return FailureProcessingResult.Continue;
                }
            }
        }
    }
}
