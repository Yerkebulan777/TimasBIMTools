﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
using System.Windows.Data;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutOpeningDataViewModel : ObservableObject, IDisposable
    {
        public CutOpeningDockPanelView DockPanelView = null;
        public static CancellationToken CancelToken { get; set; } = CancellationToken.None;

        private Task task;
        private readonly View3D view3d;
        private readonly object syncLocker = new();
        private readonly string documentId = Properties.Settings.Default.CurrentDocumentUniqueId;
        //private readonly string roundOpeningId = Properties.Settings.Default.RoundSymbolUniqueId;
        //private readonly string rectangOpeningId = Properties.Settings.Default.IsStarted;
        private readonly CutOpeningCollisionManager manager = SmartToolController.Services.GetRequiredService<CutOpeningCollisionManager>();




        public CutOpeningDataViewModel()
        {
            view3d = DockPanelView.View3d;
            ShowExecuteCommand = new AsyncRelayCommand(ExecuteHandelCommandAsync);
            SelectItemCommand = new RelayCommand(SelectAllVaueHandelCommand);
            CanselCommand = new RelayCommand(CancelCallbackLogic);
        }


        #region Visibility

        private bool isEnabledOptions = false;
        public bool IsOptionsEnabled
        {
            get => isEnabledOptions;
            set
            {
                if (value != isEnabledOptions)
                {
                    if (SetProperty(ref isEnabledOptions, value))
                    {
                        IsDataEnabled = !isEnabledOptions;
                    }
                }
            }
        }


        private bool isEnabledData = false;
        public bool IsDataEnabled
        {
            get => isEnabledData;
            set
            {
                if (value == false || (model != null && category != null))
                {
                    if (SetProperty(ref isEnabledData, value))
                    {
                        IsOptionsEnabled = !isEnabledData;
                    }
                }
            }
        }

        #endregion


        #region Set settings

        private DocumentModel model = null;
        public DocumentModel SearchDocumentModel
        {
            get => model;
            set
            {
                if (SetProperty(ref model, value) && value != null)
                {
                    manager.SearchDocument = model.Document;
                    manager.SearchTransform = model.Transform;
                    manager.SearchLinkInstance = model.LinkInstance;
                }
            }
        }


        private Material material;
        public Material StructureMaterial
        {
            get => material;
            set
            {
                if (SetProperty(ref material, value) && value != null)
                {
                    GetInstancesByMaterialName(material.Name);
                }
            }
        }


        private Category category = null;
        public Category SearchCategory
        {
            get => category;
            set
            {
                if (SetProperty(ref category, value) && value != null)
                {
                    manager.SearchCategoryId = category.Id;
                }
            }
        }


        private FamilySymbol rectangle;
        public FamilySymbol RectangSymbol
        {
            get => rectangle;
            set
            {
                if (SetProperty(ref rectangle, value))
                {
                    ActivateFamilySimbol(rectangle);
                }
            }
        }


        private FamilySymbol rounded;
        public FamilySymbol RoundedSymbol
        {
            get => rounded;
            set
            {
                if (SetProperty(ref rounded, value))
                {
                    ActivateFamilySimbol(rounded);
                }
            }
        }


        private int minSize = Properties.Settings.Default.MinSideSizeInMm;
        public int MinSideSize
        {
            get => minSize;
            set
            {
                if (SetProperty(ref minSize, value))
                {
                    Properties.Settings.Default.MinSideSizeInMm = minSize;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private int maxSize = Properties.Settings.Default.MaxSideSizeInMm;
        public int MaxSideSize
        {
            get => maxSize;
            set
            {
                if (SetProperty(ref maxSize, value))
                {
                    Properties.Settings.Default.MaxSideSizeInMm = minSize;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private int cutOffset = Properties.Settings.Default.CutOffsetInMm;
        public int CutOffsetSize
        {
            get => cutOffset;
            set
            {
                if (SetProperty(ref cutOffset, value))
                {
                    Properties.Settings.Default.CutOffsetInMm = cutOffset;
                    Properties.Settings.Default.Save();
                }
            }
        }

        #endregion


        #region Set filter and snoop data

        private Level level = null;
        public Level SearchLevel
        {
            get => level;
            set
            {
                if (SetProperty(ref level, value) && value != null)
                {
                    Properties.Settings.Default.Upgrade();
                    SnoopIntersectionDataByLevel(level);
                }
            }
        }

        #endregion



        #region Methods

        private void GetInstancesByMaterialName(string materialName)
        {
            IList<Element> instances = null;
            task = RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                if (documentId.Equals(doc.ProjectInformation.UniqueId))
                {
                    instances = RevitFilterManager.GetTypeIdsByStructureMaterial(doc, materialName);
                }
            }).ContinueWith(app =>
            {
                manager.SearchElementList = instances;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        private void SnoopIntersectionDataByLevel(Level level)
        {
            IEnumerable<ElementModel> data = null;
            task = RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                if (documentId.Equals(doc.ProjectInformation.UniqueId))
                {
                    data = manager.GetCollisionByLevel(doc, level);
                }
            }).ContinueWith(app =>
            {
                RevitElementModels = data.ToObservableCollection();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        private void ActivateFamilySimbol(FamilySymbol symbol)
        {
            if (symbol != null && !symbol.IsActive)
            {
                symbol.Activate();
            }
        }


        #endregion


        #region DataGrid

        private bool? isSelected = false;
        public bool? IsAllSelectChecked
        {
            get => isSelected;
            set
            {
                if (SetProperty(ref isSelected, value))
                {
                    if (value.HasValue)
                    {
                        bool val = (bool)value;
                        foreach (ElementModel model in ViewCollection)
                        {
                            model.IsSelected = val;
                        }
                    }

                }
            }
        }


        private ObservableCollection<ElementModel> modelCollection = new();
        public ObservableCollection<ElementModel> RevitElementModels
        {
            get => modelCollection;
            set
            {
                if (SetProperty(ref modelCollection, value))
                {
                    ViewCollection = CollectionViewSource.GetDefaultView(value);
                    UniqueItemNames = GetUniqueStringList(value);
                }
            }
        }


        private ICollectionView viewCollect = new CollectionView(new List<ElementModel>());
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
                    ViewCollection.Filter = FilterModelCollection;
                    SelectAllVaueHandelCommand();
                    ViewCollection.Refresh();
                }
            }
        }

        private IList<string> uniqueNames = null;
        public IList<string> UniqueItemNames
        {
            get => uniqueNames;
            set => SetProperty(ref uniqueNames, value);
        }

        private IList<string> GetUniqueStringList(Collection<ElementModel> collection)
        {
            return new SortedSet<string>(collection.Select(c => c.SymbolName).Append(string.Empty)).ToList();
        }

        private bool FilterModelCollection(object obj)
        {
            return string.IsNullOrEmpty(FilterText)
            || obj is not ElementModel model || model.SymbolName.Contains(FilterText)
            || model.SymbolName.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase)
            || model.SymbolName.Equals(FilterText, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion



        #region SelectItemCommand
        public ICommand SelectItemCommand { get; private set; }
        private void SelectAllVaueHandelCommand()
        {
            IEnumerable<ElementModel> items = ViewCollection.OfType<ElementModel>();
            ElementModel firstItem = ViewCollection.OfType<ElementModel>().FirstOrDefault();
            IsAllSelectChecked = items.All(x => x.IsSelected == firstItem.IsSelected) ? firstItem?.IsSelected : null;
        }

        #endregion


        #region ShowExecuteCommand
        public ICommand ShowExecuteCommand { get; private set; }

        [STAThread]
        private async Task ExecuteHandelCommandAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                View3D view3d = DockPanelView.View3d;
                UIDocument uidoc = app.ActiveUIDocument;
                Document doc = app.ActiveUIDocument.Document;
                string guid = doc.ProjectInformation.UniqueId;
                if (documentId.Equals(guid) && !ViewCollection.IsEmpty)
                {
                    foreach (ElementModel model in ViewCollection)
                    {
                        if (model.IsSelected && RevitElementModels.Remove(model))
                        {
                            Element elem = doc.GetElement(new ElementId(model.IdInt));
                            lock (syncLocker)
                            {
                                try
                                {
                                    // Set Openning Logic with doc regenerate and transaction RollBack                                   
                                    view3d = RevitViewManager.SetCustomSectionBox(uidoc, elem, view3d);
                                    RevitViewManager.SetColorElement(uidoc, elem);
                                }
                                finally
                                {
                                    Task.Delay(1000).Wait();
                                }
                            }
                            break;
                        }
                    }
                    // seletAll update by ViewItems
                    // boolSet to buttom IsDataEnabled
                    UniqueItemNames = GetUniqueStringList(RevitElementModels);
                }
            });

        }

        #endregion


        #region CloseCommand
        public ICommand CanselCommand { get; private set; }


        private void CancelCallbackLogic()
        {
            CancellationTokenSource cts = new();
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
                        _ = Task.Delay(1000).ContinueWith((action) => Logger.Warning("Task cansceled"));
                    }
                }
            }
        }
        #endregion


        public void Dispose()
        {
            manager?.Dispose();
            RevitElementModels.Clear();
            FilterText = string.Empty;
        }
    }
}