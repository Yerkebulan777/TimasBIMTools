﻿using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Windows.Controls;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutVoidDockPaneView.xaml </summary>
    public partial class CutVoidDockPaneView : Page, IDockablePaneProvider
    {
        private bool Disposed { get; set; } = false;
        private readonly CutVoidDataViewModel DataContextHandler;
        private static readonly ExternalEvent externalEvent = CutVoidDataViewModel.RevitExternalEvent;
        public CutVoidDockPaneView(CutVoidDataViewModel viewModel)
        {
            InitializeComponent();
            DataContext = DataContextHandler = viewModel;
            DataContextHandler = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }


        [STAThread]
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.VisibleByDefault = false;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }



        [STAThread]
        internal void RaiseExternalEvent()
        {
            if (!DataContextHandler.IsStarted)
            {
                try
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        if (ExternalEventRequest.Accepted == externalEvent.Raise())
                        {
                            Disposed = false;
                            DataContextHandler.IsStarted = true;
                            DataContextHandler.DockPanelView = this;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            }
        }


        private void DatagridRowButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ElementModel model)
            {
                if (model != null && model.Instanse.IsValidObject)
                {
                    DataContextHandler.GetElementInViewByIntId(model.Instanse.Id);
                }
            }
        }


        private void CheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(DataContextHandler.VerifyAllSelectedData);
        }


        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                DataContextHandler?.Dispose();
            }
        }


    }
}
