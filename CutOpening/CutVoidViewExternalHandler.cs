using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitUtils;
using System;

namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutVoidViewExternalHandler : IExternalEventHandler
    {


        [STAThread]
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc?.Document;

            if (doc == null)
            {
                return;
            }

        }


        private void OnCompleted(BaseCompletedEventArgs e)
        {
        }


        public string GetName()
        {
            return nameof(CutVoidViewExternalHandler);
        }
    }
}
