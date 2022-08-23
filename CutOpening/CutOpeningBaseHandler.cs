using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;


namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutOpeningBaseHandler : IExternalEventHandler
    {
        public event EventHandler<BaseCompletedEventArgs> Completed;
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
                if (family.IsValidObject && family.IsEditable)
                {
                    if (family.FamilyPlacementType.Equals(FamilyPlacementType.OneLevelBasedHosted))
                    {
                        elements.Add(new RevitElementModel(symbol));
                    }
                }
            }
            OnCompleted(new BaseCompletedEventArgs(documents, elements));
        }


        private void OnCompleted(BaseCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }


        public string GetName()
        {
            return nameof(CutOpeningBaseHandler);
        }
    }


    public class BaseCompletedEventArgs : EventArgs
    {
        public IList<RevitDocumenModel> Documents { get; }
        public IList<RevitElementModel> Elements { get; }
        public BaseCompletedEventArgs(IList<RevitDocumenModel> documents, IList<RevitElementModel> elements)
        {
            Documents = documents;
            Elements = elements;
        }
    }
}
