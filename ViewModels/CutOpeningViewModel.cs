using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace RevitTimasBIMTools.ViewModels
{
    public class CutOpeningViewModel : ObservableObject
    {
        private Element elem = null;
        private RevitElementModel model = null;

        private Document document { get; set; } = null;
        public IList<RevitElementModel> RevitElementModels { get; set; } = null;

        private readonly CutOpeningWindows view = SmartToolController.Services.GetRequiredService<CutOpeningWindows>();


        #region ContentWindow Property

        private UserControl content = null;
        public UserControl ContentViewControl
        {
            get => content;
            set
            {
                _ = SetProperty(ref content, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        internal void SetNewContent(UserControl content)
        {
            ContentViewControl = content;
        }

        #endregion

 
        private async Task ExecuteApplyCommandAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                var uidoc = app.ActiveUIDocument;
                int count = RevitElementModels.Count;
                document = app.ActiveUIDocument.Document;
                View3D view3d = RevitViewManager.Get3dView(uidoc);
                while (view.IsEnabled)
                {
                    Task.Delay(1000).Wait();
                    if (count > 0 && view.IsActive)
                    {
                        try
                        {
                            model = RevitElementModels.First();
                            elem = document.GetElement(new ElementId(model.IdInt));
                            if (RevitElementModels.Remove(model) && elem.IsValidObject)
                            {
                                view3d = RevitViewManager.GetSectionBoxView(uidoc, elem, view3d);
                                ContentViewControl = new PreviewControl(document, view3d.Id);
                                count = RevitElementModels.Count;
                            }
                        }
                        catch (Exception ex)
                        {
                            RevitLogger.Error(ex.Message);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            });
        }
    }
}
