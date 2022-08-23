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

            IList<RevitDocumenModel> documents = RevitDocumentManager.GetDocumentCollection(doc);
            OnCompleted(new BaseCompletedEventArgs(documents));
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
        public BaseCompletedEventArgs(IList<RevitDocumenModel> documents)
        {
            Documents = documents;
        }
    }
}
