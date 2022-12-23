using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Async;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RevitTimasBIMTools.ViewModels;

public sealed class AreaRebarMarkFixViewModel : ObservableObject
{
    private IList<Element> reinforcements { get; set; } = null;
    private static IDictionary<string, ValueDataModel> paramData { get; set; } = null;
    private Guid paramGuid { get; set; }

    private Parameter selectedParam;
    public Parameter SelectedParameter
    {
        get => selectedParam;
        set
        {
            if (SetProperty(ref selectedParam, value) && selectedParam is not null)
            {
                paramGuid = selectedParam.GUID;
            }
        }
    }


    private bool inView = false;
    public bool CollectInView
    {
        get => inView;
        set => SetProperty(ref inView, value);
    }


    private IDictionary<string, Parameter> parameters;
    public IDictionary<string, Parameter> AllParameters
    {
        get => parameters;
        set => SetProperty(ref parameters, value);
    }


    private IList<Element> sourceData;
    public IList<Element> SourceElementData
    {
        get => sourceData;
        set => SetProperty(ref sourceData, value);
    }


    public async void SelectAreaReinElement()
    {
        SourceElementData = await RevitTask.RunAsync(app =>
        {
            IList<Element> elements = null;
            try
            {
                SelectionFilterAreaRein filter = new();
                string prompt = "Select Area Reinforcement";
                elements = app.ActiveUIDocument.Selection.PickElementsByRectangle(filter, prompt);
            }
            finally
            {
                //handler
            }
            return elements;
        });
    }


    public async void GetAllAreaReinforceses()
    {
        SourceElementData = await RevitTask.RunAsync(app =>
        {
            Document doc = app.ActiveUIDocument.Document;
            return new FilteredElementCollector(doc)
            .OfClass(typeof(AreaReinforcement))
            .WhereElementIsNotElementType()
            .ToElements();
        });
    }


    public async void RetrieveParameterData()
    {
        AllParameters = await RevitTask.RunAsync(app =>
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = app.ActiveUIDocument.Document;
            Element reinforcement = sourceData.FirstOrDefault();
            IDictionary<string, Parameter> result = new SortedList<string, Parameter>();
            if (reinforcement is not null and AreaReinforcement areaReinforcement)
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


    internal async void FixAreaRebarParameter()
    {
        await RevitTask.RunAsync(app =>
        {
            Random rnd = new();
            Document doc = app.ActiveUIDocument.Document;
            foreach (Element current in reinforcements)
            {
                if (selectedParam is not null && current is AreaReinforcement areaReinforce)
                {
                    paramData = new Dictionary<string, ValueDataModel>(100);
                    IList<ElementId> rebarIds = areaReinforce.GetRebarInSystemIds();
                    ISet<int> uniqueNumbers = new HashSet<int>(rebarIds.Count);
                    TransactionManager.CreateTransaction(doc, "Set Mark", () =>
                    {
                        int num = rebarIds.Count;
                        int counter = 0;
                        while (true)
                        {
                            counter++;
                            int rndIdx = rnd.Next(num);
                            if (uniqueNumbers.Add(rndIdx))
                            {
                                Element element = doc.GetElement(rebarIds[rndIdx]);
                                Parameter param = element.get_Parameter(paramGuid);
                                if (element is RebarInSystem rebarIn && param is not null)
                                {
                                    if (ValidateParameter(param, rebarIn, counter > num))
                                    {
                                        rebarIds.RemoveAt(rndIdx);
                                        uniqueNumbers.Clear();
                                        num = rebarIds.Count;
                                        if (num.Equals(0))
                                        {
                                            break;
                                        }

                                    }
                                }
                            }
                        }
                    });
                }
            }
        });
    }


    private bool ValidateParameter(Parameter param, RebarInSystem rebar, bool limited)
    {
        string value = param.GetValue();
        string name = param.Definition.Name;

        ICollection<ValueDataModel> values = paramData.Values;
        bool founded = values.Any(v => v.Counter > 0);
        bool refined = values.Any(v => v.Counter > 3);
        bool IsValid = (limited && founded) || refined;

        // Set value if dictionary data is refined value
        if (IsValid && paramData.TryGetValue(name, out ValueDataModel model))
        {
            Debug.Assert(!string.IsNullOrEmpty(model.Content), "Value can't be null");
            IsValid = rebar.get_Parameter(paramGuid).SetValue(model.Content);
        }
        // Set empty to item in not found value
        else if (limited && !founded && string.IsNullOrWhiteSpace(value))
        {
            IsValid = rebar.get_Parameter(paramGuid).SetValue(string.Empty);
            Debug.Assert(IsValid, "Value must be set");
        }
        // Set value to dictionary data
        else if (!string.IsNullOrWhiteSpace(value))
        {
            if (!paramData.TryGetValue(name, out ValueDataModel dataModel))
            {
                paramData.Add(name, new ValueDataModel(value));
            }
            else
            {
                dataModel.SetNewValue(value);
            }
        }

        return IsValid;
    }


}


internal sealed class SelectionFilterAreaRein : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        if (elem is null)
        {
            return false;
        }

        if (elem is not FamilyInstance)
        {
            return false;
        }

        BuiltInCategory builtInCategory = (BuiltInCategory)GetCategoryIdAsInteger(elem);
        return builtInCategory == BuiltInCategory.OST_AreaRein;
    }
    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
    private int GetCategoryIdAsInteger(Element element)
    {
        return element?.Category?.Id?.IntegerValue ?? -1;
    }
}