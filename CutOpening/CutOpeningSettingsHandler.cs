using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;

namespace RevitTimasBIMTools.CutOpening
{
    internal class CutOpeningSettingsHandler : IExternalEventHandler
    {
        public event EventHandler<SettingsCompletedEventArgs> Completed;

        private readonly IList<BuiltInCategory> builtInCats = new List<BuiltInCategory>
        {
            BuiltInCategory.OST_Conduit,
            BuiltInCategory.OST_Furniture,
            BuiltInCategory.OST_CableTray,
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_GenericModel,
            BuiltInCategory.OST_MechanicalEquipment
        };


        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc?.Document;

            if (doc == null)
            {
                return;
            }

            FilteredElementCollector collector;
            BuiltInCategory bic = BuiltInCategory.OST_GenericModel;
            IList<FamilySymbol> elements = new List<FamilySymbol>();
            IList<Category> categories = GetCategoriesByBuiltIn(doc, builtInCats);
            collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(FamilySymbol), bic);
            foreach (FamilySymbol symbol in collector)
            {
                Family family = symbol.Family;
                if (family.IsValidObject && family.IsEditable)
                {
                    if (family.FamilyPlacementType.Equals(FamilyPlacementType.OneLevelBasedHosted))
                    {
                        elements.Add(symbol);
                    }
                }
            }

            OnCompleted(new SettingsCompletedEventArgs(categories, elements));

        }


        private IList<Category> GetCategoriesByBuiltIn(Document doc, IList<BuiltInCategory> bics)
        {
            IList<Category> output = new List<Category>();
            foreach (BuiltInCategory catId in bics)
            {
                Category cat = null;
                try
                {
                    cat = Category.GetCategory(doc, catId);
                }
                finally
                {
                    if (cat != null)
                    {
                        output.Add(cat);
                    }
                }
            }
            return output;
        }


        private void OnCompleted(SettingsCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }


        public string GetName()
        {
            return nameof(CutOpeningSettingsHandler);
        }

    }


    public class SettingsCompletedEventArgs : EventArgs
    {
        public IList<Category> Categories { get; }
        public IList<FamilySymbol> Symbols { get; }
        public SettingsCompletedEventArgs(IList<Category> categories, IList<FamilySymbol> symbols)
        {
            Categories = categories;
            Symbols = symbols;
        }
    }


}
