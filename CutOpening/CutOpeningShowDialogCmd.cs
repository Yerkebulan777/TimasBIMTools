using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RevitTimasBIMTools.CutOpening
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    internal class CutOpeningShowDialogCmd : IExternalCommand, IExternalCommandAvailability
    {
        private readonly CutOpeningWindows openingView = SmartToolController.Services.GetRequiredService<CutOpeningWindows>();
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            //_ = ExecuteApplyCommandAsync(openingView.ShowDialog());
            return Result.Succeeded;
        }

        bool IExternalCommandAvailability.IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            throw new NotImplementedException();
        }

        //[STAThread]
        //private async Task ExecuteApplyCommandAsync(bool? show)
        //{
        //    await RevitTask.RunAsync(app =>
        //    {
        //        if (show is true)
        //        {
        //            openingView.RevitViewContent = GetContent(app.ActiveUIDocument);
        //        }
        //    });
        //}


        //private ContentControl GetContent(UIDocument uidoc)
        //{
        //    ContentControl content = null;
        //    Document document = uidoc.Document;
        //    View3D view3d = RevitViewManager.Get3dView(uidoc);
        //    while (RevitElementModels.Count != 0)
        //    {
        //        try
        //        {
        //            Task.Delay(100).Wait();
        //            RevitElementModel model = RevitElementModels.First();
        //            Element elem = document.GetElement(new ElementId(model.IdInt));
        //            if (RevitElementModels.Remove(model) && elem.IsValidObject)
        //            {
        //                view3d = RevitViewManager.GetSectionBoxView(uidoc, elem, view3d);
        //                content = new PreviewControl(document, view3d.Id);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            RevitLogger.Error(ex.Message);
        //        }
        //    }
        //    return content;
        //}
    }
}
