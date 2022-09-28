using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;


namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutOpeningStartHandler : IExternalEventHandler
    {
        private readonly RevitPurginqManager purgeManager = SmartToolController.Services.GetRequiredService<RevitPurginqManager>();
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

            Properties.Settings.Default.IsStarted = true;
            purgeManager.PurgeConstructionElementTypes(doc);
            View3D view3d = RevitViewManager.Get3dView(uidoc);
            Properties.Settings.Default.CurrentDocumentUniqueId = doc.ProjectInformation.UniqueId;
            IList<DocumentModel> docModels = RevitDocumentManager.GetDocumentCollection(doc);
            OnCompleted(new BaseCompletedEventArgs(docModels, view3d));
        }


        private void OnCompleted(BaseCompletedEventArgs e)
        {
            Properties.Settings.Default.Save();
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
