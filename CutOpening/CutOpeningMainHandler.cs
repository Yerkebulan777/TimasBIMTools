using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;


namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutOpeningMainHandler : IExternalEventHandler
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

            Categories categories = uidoc.Document.Settings.Categories;
            IList<RevitDocumenModel> documents = RevitDocumentManager.GetDocumentCollection(doc);
            OnCompleted(new BaseCompletedEventArgs(documents, categories));
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
        public Categories Categories { get; }
        public IList<RevitDocumenModel> Documents { get; }
        public BaseCompletedEventArgs(IList<RevitDocumenModel> documents, Categories categories)
        {
            Documents = documents;
            Categories = categories;
        }
    }
}
