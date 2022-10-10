using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutVoidViewExternalHandler : IExternalEventHandler
    {
        private readonly IServiceProvider provider = SmartToolApp.ServiceProvider;
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
            RevitPurginqManager purgeManager = provider.GetRequiredService<RevitPurginqManager>();
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
            return nameof(CutVoidViewExternalHandler);
        }
    }


    public class BaseCompletedEventArgs : EventArgs
    {
        public ObservableCollection<DocumentModel> DocumentModels { get; }
        public IDictionary<int, ElementId> ConstructionTypeIds { get; }

        public BaseCompletedEventArgs(ICollection<DocumentModel> docs, IDictionary<int, ElementId> validIds)
        {
            DocumentModels = docs.ToObservableCollection();
            ConstructionTypeIds = validIds;
        }
    }
}
