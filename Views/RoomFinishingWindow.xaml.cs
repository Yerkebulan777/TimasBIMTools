using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
                viewModel.GetValidRooms();
            }
        }, DispatcherPriority.Background);
    }

    //public void DisplayTreeViewItem(Document document)
    //{
    //    // viewtypename and treeviewitem dictionary
    //    SortedDictionary<string, TreeViewItem> ViewTypeDictionary = new SortedDictionary<string, TreeViewItem>();
    //    // viewtypename
    //    List<string> viewTypenames = new List<string>();

    //    // collect view type
    //    List<Element> elements = new FilteredElementCollector(document).OfClass(typeof(View)).ToList();

    //    foreach (Element element in elements)
    //    {
    //        // view
    //        View view = element as View;
    //        // view typename
    //        viewTypenames.Add(view.ViewType.ToString());
    //    }

    //    // create treeviewitem for viewtype
    //    foreach (string viewTypename in viewTypenames.Distinct().OrderBy(name => name).ToList())
    //    {
    //        // create viewtype treeviewitem
    //        TreeViewItem viewTypeItem = new TreeViewItem() { Header = viewTypename };
    //        // store in dict
    //        ViewTypeDictionary[viewTypename] = viewTypeItem;
    //        // add to treeview
    //        treeview.Items.Add(viewTypeItem);
    //    }

    //    foreach (Element element in elements)
    //    {
    //        // view
    //        View view = element as View;
    //        // viewname
    //        string viewName = view.Name;
    //        // view typename
    //        string viewTypename = view.ViewType.ToString();
    //        // create view treeviewitem 
    //        TreeViewItem viewItem = new TreeViewItem() { Header = viewName };
    //        // view item add to view type
    //        ViewTypeDictionary[viewTypename].Items.Add(viewItem);
    //    }
    //}


}
