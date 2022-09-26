using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;


namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutOpeningStartHandler : IExternalEventHandler
    {
        public event EventHandler<BaseCompletedEventArgs> Completed;

        [STAThread]
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc?.Document;

            if (doc == null)
            {
                return;
            }

            View3D view3d = RevitViewManager.Get3dView(uidoc);
            Properties.Settings.Default.TargetDocumentName = doc.Title;
            Properties.Settings.Default.CurrentDocumentUniqueId = doc.ProjectInformation.UniqueId;
            IList<DocumentModel> docModels = RevitDocumentManager.GetDocumentCollection(doc);
            OnCompleted(new BaseCompletedEventArgs(docModels, view3d));
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
        public IList<DocumentModel> DocumentModels { get; }

        public Dictionary<string, string> StructureMaterials { get; }

        public BaseCompletedEventArgs(IList<DocumentModel> docs, View3D view3d)
        {
            View3d = view3d;
            DocumentModels = docs;
        }
    }
}
