using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Views;

namespace RevitTimasBIMTools.ViewModels
{
    public sealed class DataViewModel : ObservableObject
    {
        public string ActiveProjectTitle { get; set; } = null;
        public DockPanelPage DockPanelView { get; set; } = null;

        private readonly CutVoidOpeningManager manager = null;
        private IEnumerable<RevitElementModel> collection = null;
        private StringCollection strCollection = new StringCollection();
        private readonly IServiceProvider services = AppController.ServiceProvider;


        public DataViewModel()
        {
            manager = services.GetService<CutVoidOpeningManager>();
            ApplyCommand = new AsyncRelayCommand(ExecuteApplyCommandAsync);
            UpdateCommand = new AsyncRelayCommand(ExecuteUpdateCommandAsync);
            SelectAllCommand = new RelayCommand<bool?>(HandleSelectAllCommand);
        }


        #region TextFilter
        private string filterText = string.Empty;
        public string FilterText
        {
            get => filterText;
            set
            {
                if (ItemCollectionView != null)
                {
                    Task.Delay(50).Wait();
                    if (filterText != value)
                    {
                        ItemCollectionView.Refresh();
                        SetProperty(ref filterText, value);
                        ItemCollectionView.Filter = FilterModelCollection;
                    }
                }
            }
        }


        private bool FilterModelCollection(object obj)
        {
            return string.IsNullOrEmpty(FilterText)
                || !(obj is RevitElementModel model) || model.Name.Contains(FilterText)
                || model.Name.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase)
                || model.Category.Equals(FilterText, StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion


        #region ViewModel fields
        private Document doc;
        public Document CurrentDocument
        {
            get { return doc; }
            set
            {
                if (SetProperty(ref doc, value) && doc != null)
                {
                    ActiveProjectTitle = doc.Title.ToUpper();
                };
            }
        }


        private bool selectAll = false;
        public bool SelectAllEnabled
        {
            get { return selectAll; }
            set { SetProperty(ref selectAll, value); }
        }


        private ICollectionView collectionView = null;
        public ICollectionView ItemCollectionView
        {
            get => collectionView;
            set
            {
                SetProperty(ref collectionView, value);
                ItemCollectionView.SortDescriptions.Clear();
                ItemCollectionView.GroupDescriptions.Clear();
                ItemCollectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RevitElementModel.Category)));
                ItemCollectionView.SortDescriptions.Add(new SortDescription(nameof(RevitElementModel.Name), ListSortDirection.Ascending));
                ItemCollectionView.CollectionChanged += ItemCollectionView_CollectionChanged;
            }
        }


        private ObservableCollection<RevitElementModel> elemList = null;
        public ObservableCollection<RevitElementModel> ElementList
        {
            get => elemList;
            set
            {
                if (SetProperty(ref elemList, value))
                {
                    ItemCollectionView = CollectionViewSource.GetDefaultView(value);
                }
            }
        }



        private void ItemCollectionView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is ListCollectionView viewList && !viewList.IsEmpty)
            {
                try
                {
                    foreach (object item in ItemCollectionView)
                    {
                        if (item is RevitElementModel model)
                        {
                            model.IsSelected = false;
                        }
                    }
                }
                catch (Exception exc)
                {
                    RevitLogger.Error("Error IsSelected: " + exc.Message);
                }
                finally
                {
                    if (!ItemCollectionView.IsEmpty)
                    {
                        DockPanelView.checkSelectAll.IsEnabled = true;
                        DockPanelView.checkSelectAll.IsChecked = false;
                    }
                }
            }
        }
        #endregion


        #region SelectAllCommand
        public ICommand SelectAllCommand { get; set; }

        [STAThread]
        private void HandleSelectAllCommand(bool? isChecked)
        {
            if (isChecked.HasValue)
            {
                int num = 0;
                bool boolean = isChecked.Value;
                StringCollection collection = new StringCollection();
                try
                {
                    foreach (object item in ItemCollectionView)
                    {
                        if (item is RevitElementModel model)
                        {
                            model.IsSelected = boolean;
                            if (boolean == true)
                            {
                                lock (collection.SyncRoot)
                                {
                                    collection.Add(model.Id.ToString());
                                    num++;
                                }
                            }
                        }
                    }
                    Properties.Settings.Default.HostElementIdCollection = collection;
                }
                catch (Exception exc)
                {
                    RevitLogger.Error(exc.Message);
                }
                finally
                {
                    Properties.Settings.Default.Save();
                    RevitLogger.Info($"Save famTypeSizeData count {num}");
                }
            }
        }
        #endregion


        #region UpdateCommand
        public ICommand UpdateCommand { get; set; }

        private async Task ExecuteUpdateCommandAsync()
        {
            ElementList = await RevitTask.RunAsync(app =>
            {
                CurrentDocument = app.ActiveUIDocument.Document;
                manager.GetTargetRevitLinkInstance(CurrentDocument);
                if (0 < Properties.Settings.Default.OpennigFamilyId)
                {
                    int openingId = Properties.Settings.Default.OpennigFamilyId;
                    FamilySymbol opening = CurrentDocument.GetElement(new ElementId(openingId)) as FamilySymbol;
                    if (opening.IsValidObject && !opening.IsActive)
                    {
                        try
                        {
                            opening.Activate();
                            CurrentDocument.Regenerate();
                        }
                        catch (Exception exc)
                        {
                            RevitLogger.Error("Activate family:" + exc.Message);
                        }
                    }
                }
                collection = manager.FindCollisionCommunicateModelCollection();
                RevitLogger.Info($"Found collision {collection.Count()}");
                return collection.ToObservableCollection();
            });
        }
        #endregion


        #region ApplyCommand
        public ICommand ApplyCommand { get; set; }

        [STAThread]
        private async Task ExecuteApplyCommandAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                IList<Element> selSet = new List<Element>();
                CurrentDocument = app.ActiveUIDocument.Document;
                strCollection = Properties.Settings.Default.HostElementIdCollection;
                lock (strCollection.SyncRoot)
                {
                    foreach (string line in strCollection)
                    {
                        if (int.TryParse(line, out int elemIdInt) && elemIdInt > 0)
                        {
                            Element elem = CurrentDocument.GetElement(new ElementId(elemIdInt));
                            selSet.Add(elem);
                        }
                    }
                }

                app.ActiveUIDocument.Selection.SetElementIds(selSet.Select(q => q.Id).ToList());
                TaskDialog.Show("Intersection", selSet.Count + " intersecting elemList found");
                app.ActiveUIDocument.RefreshActiveView();
            });
        }
        #endregion
    }
}