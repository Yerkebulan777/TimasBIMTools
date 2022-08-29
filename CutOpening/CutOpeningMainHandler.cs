using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutOpeningMainHandler : IExternalEventHandler
    {
        public event EventHandler<BaseCompletedEventArgs> Completed;
        private readonly IList<BuiltInCategory> builtInCats = new List<BuiltInCategory>
        {
            BuiltInCategory.OST_Conduit,
            BuiltInCategory.OST_CableTray,
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_GenericModel,
            BuiltInCategory.OST_MechanicalEquipment
        };

        [STAThread]
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc?.Document;

            if (doc == null)
            {
                return;
            }

            IList<DocumentModel> doumens = RevitDocumentManager.GetDocumentCollection(doc);
            IList<Category> categories = RevitFilterManager.GetCategories(doc, builtInCats).ToList();
            Dictionary<string, string> matDict = RevitMaterialManager.GetAllConstructionStructureMaterials(doc);
            FilteredElementCollector collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(FamilySymbol), BuiltInCategory.OST_GenericModel);
            FamilyPlacementType placement = FamilyPlacementType.OneLevelBasedHosted;
            IList<FamilySymbol> symbols = new List<FamilySymbol>(25);
            foreach (FamilySymbol smb in collector)
            {
                Family family = smb.Family;
                if (family.IsValidObject && family.IsEditable)
                {
                    if (family.FamilyPlacementType.Equals(placement))
                    {
                        symbols.Add(smb);
                    }
                }
            }

            OnCompleted(new BaseCompletedEventArgs(doumens, categories, symbols, matDict));
        }


        private void OnCompleted(BaseCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }


        public string GetName()
        {
            return nameof(CutOpeningMainHandler);
        }
    }


    public class BaseCompletedEventArgs : EventArgs
    {
        public IList<DocumentModel> Documents { get; }
        public IList<Category> Categories { get; }
        public IList<FamilySymbol> FamilySymbols { get; }
        public Dictionary<string, string> StructureMaterials { get; }
        public BaseCompletedEventArgs(IList<DocumentModel> documents, IList<Category> categories, IList<FamilySymbol> symbols, Dictionary<string, string> matDict)
        {
            Documents = documents;
            Categories = categories;
            FamilySymbols = symbols;
            StructureMaterials = matDict;
        }
    }
}
