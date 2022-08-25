using Autodesk.Revit.DB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Revit.Async;
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
    internal class CutOpeningViewModel : ObservableObject
    {
        private Element element = null;
        private ElementId elementId = null;
        private RevitElementModel model = null;

        private Document document { get; set; } = null;
        public IList<RevitElementModel> RevitElementModelList { get; set; } = null;

        #region ContentWindow Property

        private UserControl content;
        public UserControl ContentWindow
        {
            get => content;
            set => SetProperty(ref content, value);
        }

        #endregion


        internal void SetNewContent(UserControl content)
        {
            ContentWindow = content;
        }


        private async Task ExecuteApplyCommandAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                document = app.ActiveUIDocument.Document;
                CutOpeningWindows openingView = new CutOpeningWindows();
                View3D view = RevitViewManager.Get3dView(document);
                while (true == openingView.ShowDialog())
                {
                    if (RevitElementModelList.Count == 0)
                    {
                        openingView.Close();
                    }
                    else if (openingView.Activate())
                    {
                        try
                        {
                            model = RevitElementModelList.First();
                            elementId = new ElementId(model.IdInt);
                            element = document.GetElement(elementId);
                            if (RevitElementModelList.Remove(model))
                            {
                                Task.Delay(1000).Wait();
                            }
                        }
                        catch (Exception ex)
                        {
                            openingView.Close();
                            RevitLogger.Error(ex.Message);
                        }
                    }
                }
            });
        }
    }
}
