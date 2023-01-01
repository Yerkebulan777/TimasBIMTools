using RevitTimasBIMTools.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace RevitTimasBIMTools.Views;

/// <summary>
/// Логика взаимодействия для RoomFinishingWindow.xaml
/// </summary>
public partial class RoomFinishingWindow : Window
{
    readonly RoomFinishingViewModel viewModel;
    public RoomFinishingWindow(RoomFinishingViewModel vm)
    {
        viewModel = vm;
        DataContext= viewModel;
        InitializeComponent();
    }
}
