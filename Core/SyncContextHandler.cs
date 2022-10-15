using Autodesk.Revit.UI;
using Revit.Async.ExternalEvents;
using System;

namespace RevitTimasBIMTools.Core
{
    public class SyncContextHandler : SyncGenericExternalEventHandler<bool, string>
    {
        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public override string GetName()
        {
            return nameof(SyncContextHandler);
        }

        protected override string Handle(UIApplication app, bool parameter)
        {
            string result = string.Empty;
            Properties.Settings.Default.Reload();
            if (parameter)
            {
                result = app.ActiveUIDocument.Document.ProjectInformation.UniqueId;
                Properties.Settings.Default.ActiveDocumentUniqueId = result;
                Properties.Settings.Default.Save();
            }
            return result;
        }
    }
}
