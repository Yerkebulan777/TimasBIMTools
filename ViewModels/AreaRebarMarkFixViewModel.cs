using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Async;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RevitTimasBIMTools.ViewModels
{
    public sealed class AreaRebarMarkFixViewModel : ObservableObject
    {

        private Document doc { get; set; }
        private IList<Element> areaReinforcements { get; set; }
        private IDictionary<string, ValueDataModel> map { get; set; }
        private TaskScheduler taskContext { get; set; } = TaskScheduler.FromCurrentSynchronizationContext();
        TimeSpan timeSpan { get; } = TimeSpan.FromSeconds(90);

        private Parameter param;
        public Parameter SelectedParameter
        {
            get => param;
            set
            {
                if (SetProperty(ref param, value))
                {
                    _ = param.Definition.Name;
                }
            }
        }


        private IDictionary<string, Parameter> parameters;
        public IDictionary<string, Parameter> AllParameters
        {
            get => parameters;
            set => SetProperty(ref parameters, value);
        }


        public AreaRebarMarkFixViewModel()
        {

        }


        public async void RetrieveParameterData()
        {
            AllParameters = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                areaReinforcements = GetAllAreaReinforcement(doc);
                Element reinforcement = areaReinforcements.FirstOrDefault();
                IDictionary<string, Parameter> result = new SortedList<string, Parameter>();
                if (reinforcement is not null and AreaReinforcement areaReinforcement)
                {
                    IList<ElementId> rebarIds = areaReinforcement.GetRebarInSystemIds();
                    ElementId rebarId = rebarIds.FirstOrDefault(i => i.IntegerValue > 0);
                    foreach (Parameter param in GetAllStringParameters(doc, rebarId))
                    {
                        string name = param.Definition.Name;
                        if (5 < name.Length)
                        {
                            result[name] = param;
                        }
                    }
                }
                return result;
            });
        }


        private IList<Element> GetAllAreaReinforcement(Document doc)
        {
            return new FilteredElementCollector(doc)
            .OfClass(typeof(AreaReinforcement))
            .WhereElementIsNotElementType()
            .ToElements();
        }


        private IList<Parameter> GetAllStringParameters(Document doc, ElementId rebarId)
        {
            IList<Parameter> result = new List<Parameter>();
            if (rebarId is not null and ElementId)
            {
                Element element = doc.GetElement(rebarId);
                if (element is RebarInSystem rebar)
                {
                    IList<Parameter> prmList = rebar.GetOrderedParameters();
                    for (int i = 0; i < prmList.Count; i++)
                    {
                        Parameter param = prmList[i];
                        if (param.UserModifiable || param.IsShared || !param.IsReadOnly)
                        {
                            ParameterType paramType = param.Definition.ParameterType;
                            if (paramType == ParameterType.Text)
                            {
                                result.Add(param);
                            }
                        }
                    }
                }
            }
            return result;
        }


        internal async void FixAreaRebarParameter(int limit = 5)
        {
            await RevitTask.RunAsync(app =>
            {
                map = null;
                doc = app.ActiveUIDocument.Document;
                foreach (Element element in areaReinforcements)
                {
                    if (map is null && element is AreaReinforcement areaReinforce)
                    {
                        IList<ElementId> rebarIds = areaReinforce.GetRebarInSystemIds();
                        TransactionManager.CreateTransaction(doc, "Set Mark", () =>
                        {
                            int counter = 0;
                            Random rnd = new();
                            while (0 < rebarIds.Count)
                            {
                                counter++;
                                Task.Delay(timeSpan);
                                int index = rnd.Next(0, rebarIds.Count);
                                Element elem = doc.GetElement(rebarIds[index]);
                                if (param is not null && elem is RebarInSystem rebarIn)
                                {
                                    Logger.Log($"All: {rebarIds.Count} Counter: {counter} Index: {index} IsStarted: {counter > limit}");
                                    if (ValidateParameter(param, rebarIn, counter > limit))
                                    {
                                        if (rebarIds.Remove(rebarIds[index]))
                                        {
                                            
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
            });
        }


        private bool ValidateParameter(Parameter param, RebarInSystem rebar, bool start)
        {
            Task.Delay(timeSpan);
            Logger.Log("Start validate...");
            string value = param.GetValue();
            string name = param.Definition.Name;
            Logger.Log($"Parameter: {name} Value: {value}");
            bool IsValid = start && map.Values.All(val => val.Counter > 0);
            Logger.Log($"IsValid: {IsValid}\tParameter: {name}\tValue: {value}");
            if (IsValid && map.TryGetValue(name, out ValueDataModel result))
            {
                IsValid = rebar.get_Parameter(param.GUID).SetValue(result.Content);
            }
            else if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
            {
                if (!map.TryGetValue(name, out ValueDataModel data))
                {
                    Task.Delay(timeSpan);
                    Logger.Log("Set new parameter...");
                    map ??= new Dictionary<string, ValueDataModel>();
                    map.Add(name, new ValueDataModel(value));
                }
                else
                {
                    Task.Delay(timeSpan);
                    Logger.Log("Update parameter...");
                    data?.SetNewValue(value);
                }
            }
            return IsValid;
        }
    }

}