using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using SmartBIMTools.Core;
using SmartBIMTools.Views;
using System;
using System.Globalization;
using System.Threading;


namespace SmartBIMTools.Commands;
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class RoomFinishingCommand : IExternalCommand, IExternalCommandAvailability
{
    private readonly RoomFinishingWindow window = SmartToolApp.Host.Services.GetRequiredService<RoomFinishingWindow>();
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            window.Show();
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }

        return Result.Succeeded;
    }


    public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
    {
        View view = uiapp.ActiveUIDocument?.ActiveGraphicalView;
        return view is ViewPlan;
    }


    public static string GetPath()
    {
        return typeof(RoomFinishingCommand).FullName;
    }
}
