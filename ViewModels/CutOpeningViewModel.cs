using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
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

namespace RevitTimasBIMTools.ViewModels
{
    public class CutOpeningViewModel : ObservableObject
    {
        private Element element = null;
        private ElementId elementId = null;
        private RevitElementModel model = null;

        private Document document { get; set; } = null;
        public IList<RevitElementModel> RevitElementModelList { get; set; } = null;

        private readonly CutOpeningWindows view = SmartToolController.Services.GetRequiredService<CutOpeningWindows>();


        #region ContentWindow Property

        private UserControl content = null;
        public UserControl ContentViewControl
        {
            get => content;
            set => SetProperty(ref content, value);
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
                document = app.ActiveUIDocument.Document;
                View3D revitView = RevitViewManager.Get3dView(document);
                while (view.IsEnabled)
                {
                    Task.Delay(1000).Wait();
                    if (RevitElementModelList.Count > 0)
                    {
                        try
                        {
                            model = RevitElementModelList.First();
                            elementId = new ElementId(model.IdInt);
                            element = document.GetElement(elementId);
                            if (view.IsActive && RevitElementModelList.Remove(model))
                            {
                                ContentViewControl = new PreviewControl(document, revitView.Id);
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
