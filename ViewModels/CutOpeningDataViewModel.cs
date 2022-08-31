using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;
using UserControl = System.Windows.Controls.UserControl;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutOpeningDataViewModel : ObservableObject, IDisposable
    {
        public CutOpeningDockPanelView DockPanelView { get; set; } = null;
        public static CancellationToken CancelToken { get; set; } = CancellationToken.None;

        private readonly object syncLocker = new object();
        private readonly ElementId elementId = ElementId.InvalidElementId;
        private IList<ElementModel> resultCollection = new List<ElementModel>(150);
        private readonly string roundOpeningId = Properties.Settings.Default.RoundSymbolUniqueId;
        private readonly string rectangOpeningId = Properties.Settings.Default.RectangSymbolUniqueId;
        private readonly CutOpeningCollisionManager manager = SmartToolController.Services.GetRequiredService<CutOpeningCollisionManager>();

        public CutOpeningDataViewModel()
        {
            CloseCommand = new RelayCommand(CancelCallbackLogic);
            ApplyCommand = new AsyncRelayCommand(ExecuteApplyCommandAsync);
            SnoopCommand = new AsyncRelayCommand(ExecuteSnoopCommandAsync);
            ShowSelectedCommand = new RelayCommand(HandleShowSelectedCommand);
        }


        #region INotifyPropertyChanged members

        private bool enable = false;
        public bool IsEnabled
        {
            get => enable;
            set => SetProperty(ref enable, value);
        }

        private bool? isSelected = false;
        public bool? IsAllSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        private Document doc = null;
        public Document CurrentDocument
        {
            get => doc;
            set
            {
                if (value != null)
                {
                    doc = value;
                    OnPropertyChanged(nameof(CurrentDocument));
                    CommandManager.InvalidateRequerySuggested();
                };
            }
        }

        private ObservableCollection<ElementModel> modelCollection = new ObservableCollection<ElementModel>();
        public ObservableCollection<ElementModel> RevitElementModels
        {
            get => modelCollection;
            set
            {
                if (SetProperty(ref modelCollection, value))
                {
                    ViewCollection = CollectionViewSource.GetDefaultView(value);
                    UniqueElementNames = GetUniqueStringList(value);
                    IsEnabled = !ViewCollection.IsEmpty;
                }
            }
        }

        private ICollectionView viewCollect = new CollectionView(new List<ElementModel>(50));
        public ICollectionView ViewCollection
        {
            get => viewCollect;
            set
            {
                if (SetProperty(ref viewCollect, value))
                {
                    ViewCollection.SortDescriptions.Clear();
                    ViewCollection.GroupDescriptions.Clear();
                    ViewCollection.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.CategoryName)));
                    ViewCollection.SortDescriptions.Add(new SortDescription(nameof(ElementModel.SymbolName), ListSortDirection.Ascending));
                    ViewCollection.SortDescriptions.Add(new SortDescription(nameof(ElementModel.FamilyName), ListSortDirection.Ascending));
                    ViewCollection.SortDescriptions.Add(new SortDescription(nameof(ElementModel.Description), ListSortDirection.Ascending));
                }
            }
        }

        #endregion


        #region TextFilter

        private string filterText = string.Empty;
        public string FilterText
        {
            get => filterText;
            set
            {
                if (SetProperty(ref filterText, value))
                {
                    if (!ViewCollection.IsEmpty)
                    {
                        ViewCollection.Refresh();
                        ViewCollection.Filter = FilterModelCollection;
                    };
                }
            }
        }

        private IList<string> uniqueNames = null;
        public IList<string> UniqueElementNames
        {
            get => uniqueNames;
            set => SetProperty(ref uniqueNames, value);
        }

        private IList<string> GetUniqueStringList(Collection<ElementModel> collection)
        {
            return new SortedSet<string>(collection.Select(c => c.SymbolName)).ToList();
        }

        private bool FilterModelCollection(object obj)
        {
            return string.IsNullOrEmpty(FilterText)
            || !(obj is ElementModel model) || model.SymbolName.Contains(FilterText)
            || model.SymbolName.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase)
            || model.SymbolName.Equals(FilterText, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion


        #region CloseCommand
        public ICommand CloseCommand { get; private set; }
        private void CancelCallbackLogic()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            lock (syncLocker)
            {
                try
                {
                    cts.Cancel(true);
                    CancelToken = cts.Token;
                }
                catch (AggregateException)
                {
                    if (CancelToken.IsCancellationRequested)
                    {
                        _ = Task.Delay(1000).ContinueWith((action) => RevitLogger.Warning("Task cansceled"));
                    }
                }
            }
        }
        #endregion


        #region SelectAllCommand
        public ICommand ShowSelectedCommand { get; private set; }
        public void HandleShowSelectedCommand()
        {
            if (ViewCollection == null)
            {
                return;
            }
            foreach (object item in ViewCollection)
            {
                lock (syncLocker)
                {
                    if (item is ElementModel model)
                    {
                        if (model.IsSelected == true)
                        {
                            RevitLogger.Info($"Selectet Item {model.FamilyName}");
                        }
                    }
                }
            }
        }

        #endregion


        #region SnoopCommand
        public ICommand SnoopCommand { get; private set; }
        private async Task ExecuteSnoopCommandAsync()
        {
            RevitElementModels.Clear();
            RevitElementModels = await RevitTask.RunAsync(app =>
            {
                CurrentDocument = app.ActiveUIDocument.Document;
                manager.InitializeActiveDocument(CurrentDocument);
                resultCollection = manager.GetCollisionCommunicateElements();
                RevitLogger.Info($"Found collision {resultCollection.Count()}");
                ActivateFamilySimbol(rectangOpeningId);
                ActivateFamilySimbol(roundOpeningId);
                return resultCollection.ToObservableCollection();
            });
        }

        private void ActivateFamilySimbol(string simbolId)
        {
            if (!string.IsNullOrEmpty(simbolId))
            {
                Element element = CurrentDocument.GetElement(simbolId);
                if (element is FamilySymbol symbol && !symbol.IsActive)
                {
                    symbol.Activate();
                }
            }
        }

        #endregion


        #region ApplyCommand
        public ICommand ApplyCommand { get; private set; }

        [STAThread]
        private async Task ExecuteApplyCommandAsync()
        {
            UserControl presenter = new UserControl
            {
                Height = 300,
                Width = 500
            };
            await RevitTask.RunAsync(app =>
            {
                CurrentDocument = app.ActiveUIDocument.Document;
                presenter.Content = GetContent(app.ActiveUIDocument);
            });
        }


        private ContentControl GetContent(UIDocument uidoc)
        {
            ContentControl content = null;
            Document document = uidoc.Document;
            View3D view3d = RevitViewManager.Get3dView(uidoc);
            try
            {
                Task.Delay(3000).Wait();
                ElementModel model = RevitElementModels.First();
                Element elem = document.GetElement(new ElementId(model.IdInt));
                if (RevitElementModels.Remove(model) && elem.IsValidObject)
                {
                    view3d = RevitViewManager.GetSectionBoxView(uidoc, elem, view3d);
                    content = new PreviewControl(document, view3d.Id);
                    RevitViewManager.SetColorElement(uidoc, elem);
                }
            }
            catch (Exception ex)
            {
                RevitLogger.Log(ex.Message);
            }
            return content;
        }
        #endregion


        public void Dispose()
        {
            manager?.Dispose();
            resultCollection.Clear();
            RevitElementModels.Clear();
            FilterText = string.Empty;
        }
    }
}