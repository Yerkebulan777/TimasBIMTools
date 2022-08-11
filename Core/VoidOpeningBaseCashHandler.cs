using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;

namespace RevitTimasBIMTools.Servises
{
    public class VoidOpeningBaseCashHandler : IExternalEventHandler
    {
        public event EventHandler<DataGroupCompletedEventArgs> Completed;
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc?.Document;

            if (doc == null)
            {
                return;
            }

            BuiltInCategory bic = BuiltInCategory.OST_GenericModel;
            IList<RevitElementModel> elements = new List<RevitElementModel>();
            IList<RevitDocumenModel> documents = RevitDocumentManager.GetDocumentCollection(doc);
            FilteredElementCollector collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(FamilySymbol), bic);
            foreach (FamilySymbol symbol in collector)
            {
                Family family = symbol.Family;
                if (family.IsEditable && family.FamilyPlacementType.Equals(FamilyPlacementType.OneLevelBasedHosted))
                {
                    int id = symbol.Id.IntegerValue;
                    string category = symbol.Category.Name;
                    elements.Add(new RevitElementModel(id, symbol.Name, category, family.Name));
                }
            }
            OnCompleted(new DataGroupCompletedEventArgs(documents, elements));
        }

        private void OnCompleted(DataGroupCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }

        public string GetName()
        {
            return nameof(VoidOpeningBaseCashHandler);
        }
    }

    public class DataGroupCompletedEventArgs : EventArgs
    {
        public IList<RevitDocumenModel> Documents { get; }
        public IList<RevitElementModel> Elements { get; }
        public DataGroupCompletedEventArgs(IList<RevitDocumenModel> documents, IList<RevitElementModel> elements)
        {
            Documents = documents;
            Elements = elements;
        }
    }
}
