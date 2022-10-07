using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autofac;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;


namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutVoidStartExternalHandler : IExternalEventHandler
    {
        private readonly RevitPurginqManager purgeManager = SmartToolController.Container.Resolve<RevitPurginqManager>();
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

            IDictionary<int, ElementId> validIds = purgeManager.PurgeAndGetValidConstructionTypeIds(doc);
            Properties.Settings.Default.ActiveDocumentUniqueId = doc.ProjectInformation.UniqueId;
            ICollection<DocumentModel> docModels = RevitDocumentManager.GetDocumentCollection(doc);
            OnCompleted(new BaseCompletedEventArgs(docModels, validIds));
        }


        private void OnCompleted(BaseCompletedEventArgs e)
        {
            Properties.Settings.Default.Save();
            Completed?.Invoke(this, e);
        }


        public string GetName()
        {
            return nameof(CutVoidStartExternalHandler);
        }
    }


    public class BaseCompletedEventArgs : EventArgs
    {
        public ICollection<DocumentModel> DocumentModels { get; }
        public IDictionary<int, ElementId> ConstructionTypeIds { get; }

        public BaseCompletedEventArgs(ICollection<DocumentModel> docs, IDictionary<int, ElementId> validIds)
        {
            DocumentModels = docs;
            ConstructionTypeIds = validIds;
        }
    }
}
