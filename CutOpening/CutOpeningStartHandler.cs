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

            View3D view3d = RevitViewManager.Get3dView(uidoc);
            IDictionary<int, ElementId> validIds = purgeManager.PurgeAndGetValidConstructionTypeIds(doc);
            Properties.Settings.Default.ActiveDocumentUniqueId = doc.ProjectInformation.UniqueId;
            IList<DocumentModel> docModels = RevitDocumentManager.GetDocumentCollection(doc);
            OnCompleted(new BaseCompletedEventArgs(docModels, view3d, validIds));
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
        public IDictionary<int, ElementId> ConstructionTypeIds { get; }

        public BaseCompletedEventArgs(IList<DocumentModel> docs, View3D view3d, IDictionary<int, ElementId> validIds)
        {
            View3d = view3d;
            DocumentModels = docs;
            ConstructionTypeIds = validIds;
        }
    }
}
