using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Async;
using SmartBIMTools.RevitModel;
using SmartBIMTools.RevitSelectionFilter;
using SmartBIMTools.RevitUtils;
using SmartBIMTools.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using Parameter = Autodesk.Revit.DB.Parameter;
using Reference = Autodesk.Revit.DB.Reference;


namespace SmartBIMTools.ViewModels;


public sealed class AreaRebarMarkViewModel : ObservableObject
{

    public AreaRebarMarkFixWindow RepresentedView { get; internal set; }
    private Guid paramGuid { get; set; }

    private bool selected;
    public bool IsSelected
    {
        get => selected;
        set => SetProperty(ref selected, value);
    }


    private Parameter parameter;
    public Parameter SelectedParameter
    {
        get => parameter;
        set
        {
            if (SetProperty(ref parameter, value))
            {
                IsSelected = parameter is not null;
                if (IsSelected)
                {
                    paramGuid = parameter.GUID;
                }
            }
        }
    }


    private IDictionary<string, Parameter> allParameters;
    public IDictionary<string, Parameter> AllParameters
    {
        get => allParameters;
        set => SetProperty(ref allParameters, value);
    }


    private ObservableCollection<ElementModel> modelData = null;
    public ObservableCollection<ElementModel> ElementModelData
    {
        get => modelData;
        set
        {
            if (SetProperty(ref modelData, value) && modelData != null)
            {
                ViewDataCollection = new ListCollectionView(modelData);
                RepresentedView.InfoPanel.Visibility = 0;
            }
        }
    }


    private ListCollectionView viewData = null;
    public ListCollectionView ViewDataCollection
    {
        get => viewData;
        set
        {
            if (SetProperty(ref viewData, value))
            {
                using (viewData.DeferRefresh())
                {
                    viewData.SortDescriptions.Clear();
                    viewData.GroupDescriptions.Clear();
                    viewData.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.HostCategoryIntId)));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.HostCategoryIntId), ListSortDirection.Ascending));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.LevelName), ListSortDirection.Ascending));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.HostMark), ListSortDirection.Ascending));
                }
            }
        }
    }


    internal async void GetAllParameterData()
    {
        AllParameters = await RevitTask.RunAsync(app =>
        {
            Document doc = app.ActiveUIDocument.Document;
            Element element = new FilteredElementCollector(doc).OfClass(typeof(AreaReinforcement)).WhereElementIsNotElementType().FirstElement();
            IDictionary<string, Parameter> result = new SortedList<string, Parameter>();
            if (element is not null and AreaReinforcement areaReinforcement)
            {
                IList<ElementId> rebarIds = areaReinforcement.GetRebarInSystemIds();
                ElementId rebarId = rebarIds.FirstOrDefault(i => i.IntegerValue > 0);
                foreach (Parameter param in GetAllTextParameters(doc, rebarId))
                {
                    string name = param.Definition.Name.Trim();
                    if (3 < name.Length)
                    {
                        result[name] = param;
                    }
                }
            }
            return result;
        });
    }


    private IList<Parameter> GetAllTextParameters(Document doc, ElementId rebarId)
    {
        IList<Parameter> result = new List<Parameter>();
        if (rebarId is not null and ElementId)
        {
            Element element = doc.GetElement(rebarId);
            if (element is RebarInSystem rebar)
            {
                IList<Parameter> plist = rebar.GetOrderedParameters();
                for (int i = 0; i < plist.Count; i++)
                {
                    Parameter param = plist[i];
                    if (param.UserModifiable && param.IsShared && !param.IsReadOnly)
                    {
                        ParameterType prmType = param.Definition.ParameterType;
                        if (prmType == ParameterType.Text)
                        {
                            result.Add(param);
                        }
                    }
                }
            }
        }
        return result;
    }


    internal async void SelectAreaReinElement()
    {
        ElementModelData = await RevitTask.RunAsync(app =>
        {
            Reference pickReference = null;
            IList<Element> list = new List<Element>();
            try
            {
                SelectionFilterAreaRein filter = new();
                string prompt = "Select Area Reinforcement";
                Selection choices = app.ActiveUIDocument.Selection;
                pickReference = choices.PickObject(ObjectType.Element, filter, prompt);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
            {
                Debug.Print(ex.Message);
            }
            finally
            {
                if (pickReference is not null)
                {
                    Document doc = app.ActiveUIDocument.Document;
                    list.Add(doc.GetElement(pickReference));
                }
            }
            return ConvertToModelData(list);
        });
    }


    internal async void GetAllAreaReinforceses()
    {
        ElementModelData = await RevitTask.RunAsync(app =>
        {
            Document doc = app.ActiveUIDocument.Document;
            IList<Element> elements = new FilteredElementCollector(doc)
            .OfClass(typeof(AreaReinforcement))
            .WhereElementIsNotElementType()
            .ToElements();
            return ConvertToModelData(elements);
        });
    }


    private ObservableCollection<ElementModel> ConvertToModelData(in IList<Element> source)
    {
        ObservableCollection<ElementModel> models = new();
        foreach (Element elem in source)
        {
            if (elem is AreaReinforcement instance)
            {
                Document doc = instance.Document;
                Element host = doc.GetElement(instance.GetHostId());
                string mark = GetAreaRebarMark(doc, instance);
                ElementModel model = new(instance, host)
                {
                    IsSelected = true,
                    Mark = mark,
                };
                models.Add(model);
            }
        }
        return models;
    }


    private string GetAreaRebarMark(Document doc, AreaReinforcement areaRein)
    {
        int counter = 0;
        string result = null;
        Dictionary<string, int> data = new();
        IList<ElementId> rebarIds = areaRein.GetRebarInSystemIds();
        for (int i = 0; i < rebarIds.Count; i++)
        {
            Element element = doc.GetElement(rebarIds[i]);
            Parameter param = element.get_Parameter(paramGuid);
            string value = param?.GetValue();

            if (string.IsNullOrWhiteSpace(value)) { continue; }
            else if (data.TryGetValue(value, out int count))
            {
                int number = count + 1;
                data[value] = number;
                if (counter < number)
                {
                    counter = number;
                    result = value;
                }
            }
            else
            {
                data.Add(value, 0);
            }
        }
        return result;
    }


    internal async void SetAreaRebarMarkHandler()
    {
        if (modelData is null || parameter is null) { return; }
        await RevitTask.RunAsync(app =>
        {
            Document doc = app.ActiveUIDocument.Document;
            foreach (ElementModel current in modelData)
            {
                string mark = current.Mark;
                if (!current.IsSelected) { continue; }
                if (current.Instanse is AreaReinforcement areaReinforce)
                {
                    IList<ElementId> rebarIds = areaReinforce.GetRebarInSystemIds();
                    TransactionManager.CreateTransaction(doc, "Set Mark", () =>
                    {
                        for (int i = 0; i < rebarIds.Count; i++)
                        {
                            Element element = doc.GetElement(rebarIds[i]);
                            Parameter param = element.get_Parameter(paramGuid);
                            if (param is not null && param.SetValue(mark))
                            {
                                continue;
                            }
                        }
                    });
                }
            }
        });
    }

}
