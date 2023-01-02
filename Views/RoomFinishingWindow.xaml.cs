using Autodesk.Revit.UI;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;

namespace RevitTimasBIMTools.Views;

/// <summary>
/// Логика взаимодействия для RoomFinishingWindow.xaml
/// </summary>
public partial class RoomFinishingWindow : Window
{
    private readonly ExternalEvent externalEvent;
    private readonly RoomFinishingViewModel viewModel;
    public RoomFinishingWindow(RoomFinishingViewModel vm)
    {
        InitializeComponent();
        DataContext = viewModel = vm;
        externalEvent = RoomFinishingViewModel.RevitExternalEvent;
        viewModel = vm ?? throw new ArgumentNullException(nameof(viewModel));
        Loaded += OnWindow_Loaded;
    }


    private void OnWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.CurrentDispatcher.Invoke(() =>
        {
            ExternalEventRequest request = externalEvent.Raise();
            if (ExternalEventRequest.Accepted == request)
            {
                //viewModel.GetValidRooms();
            }
        }, DispatcherPriority.Background);
    }
}
