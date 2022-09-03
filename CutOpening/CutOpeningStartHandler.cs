using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;
using System.Linq;


namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutOpeningStartHandler : IExternalEventHandler
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

            string guid = doc.ProjectInformation.UniqueId;
            View3D view3d = RevitViewManager.Get3dView(uidoc);
            IList<FamilySymbol> symbols = new List<FamilySymbol>(50);
            Properties.Settings.Default.TargetDocumentName = string.Empty;
            FamilyPlacementType placement = FamilyPlacementType.OneLevelBasedHosted;
            SortedList<string, Material> materials = new SortedList<string, Material>(100);
            IList<DocumentModel> docs = RevitDocumentManager.GetDocumentCollection(doc);
            IList<Category> cats = RevitFilterManager.GetCategories(doc, builtInCats).ToList();
            Dictionary<string, string> dict = RevitMaterialManager.GetAllConstructionStructureMaterials(doc);
            FilteredElementCollector collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(FamilySymbol), BuiltInCategory.OST_GenericModel);

            foreach (FamilySymbol smb in collector)
            {
                Family fam = smb.Family;
                if (fam.IsValidObject && fam.IsEditable)
                {
                    if (fam.FamilyPlacementType.Equals(placement))
                    {
                        symbols.Add(smb);
                    }
                }
            }

            OnCompleted(new BaseCompletedEventArgs(guid, view3d, cats, symbols, docs, dict));
        }


        private void OnCompleted(BaseCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }


        public string GetName()
        {
            return nameof(CutOpeningStartHandler);
        }
    }


    public class BaseCompletedEventArgs : EventArgs
    {
        public View3D View3d { get; }
        public IList<Category> Categories { get; }
        public string CurrentDocumentGuid { get; }
        public IList<DocumentModel> Documents { get; }
        public IList<FamilySymbol> FamilySymbols { get; }

        public Dictionary<string, string> StructureMaterials { get; }

        public BaseCompletedEventArgs(string guid,
                                      View3D view3d,
                                      IList<Category> cats,
                                      IList<FamilySymbol> smbs,
                                      IList<DocumentModel> docs,
                                      Dictionary<string, string> matDict)
        {
            View3d = view3d;
            Documents = docs;
            Categories = cats;
            FamilySymbols = smbs;
            CurrentDocumentGuid = guid;
            StructureMaterials = matDict;
        }
    }
}
