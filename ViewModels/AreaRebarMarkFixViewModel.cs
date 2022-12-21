using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Async;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class AreaRebarMarkFixViewModel : ObservableObject
    {

        private Document doc { get; set; }
        private IList<Element> areaReinforcements { get; set; }
        private static IDictionary<string, ValueDataModel> ParamData { get; set; }

        private Parameter param;
        public Parameter SelectedParameter
        {
            get => param;
            set
            {
                if (SetProperty(ref param, value))
                {

                }
            }
        }

        private IDictionary<string, Parameter> parameters;
        public IDictionary<string, Parameter> AllParameters
        {
            get => parameters;
            set => SetProperty(ref parameters, value);
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
                doc = app.ActiveUIDocument.Document;
                foreach (Element element in areaReinforcements)
                {
                    //app.ActiveUIDocument.Selection.SetElementIds(new List<ElementId> { element.Id });
                    if (param is not null && element is AreaReinforcement areaReinforce)
                    {
                        IList<ElementId> rebarIds = areaReinforce.GetRebarInSystemIds();
                        TransactionManager.CreateTransaction(doc, "Set Mark", () =>
                        {
                            int counter = 0;
                            Random rnd = new();
                            while (0 < rebarIds.Count)
                            {
                                counter++;
                                int index = rnd.Next(0, rebarIds.Count);
                                Element elem = doc.GetElement(rebarIds[index]);
                                if (elem is RebarInSystem rebarIn)
                                {
                                    Parameter local = rebarIn.get_Parameter(param.GUID);
                                    Logger.Log($"All: {rebarIds.Count} Counter: {counter} Index: {index} IsStarted: {counter > limit}");
                                    if (ValidateParameter(local, rebarIn, counter > limit))
                                    {
                                        if (rebarIds.Remove(rebarIds[index]))
                                        {
                                            if (rebarIds.Count == 0)
                                            {
                                                ParamData.Clear();
                                                ParamData = null;
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


        private bool ValidateParameter(Parameter param, RebarInSystem rebarIn, bool start)
        {
            Logger.Log("Start validate...");
            string value = param.GetValue();
            string name = param.Definition.Name;
            Logger.Log($"Parameter: {name} Value: {value}");
            ParamData ??= new Dictionary<string, ValueDataModel>();
            bool IsValid = start && ParamData.Values.All(val => val.Counter > 0);
            Logger.Log($"IsValid: {IsValid}\tParameter: {name}\tValue: {value}");
            if (IsValid && ParamData.TryGetValue(name, out ValueDataModel result))
            {
                IsValid = rebarIn.get_Parameter(param.GUID).SetValue(result.Content);
            }
            else if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
            {
                if (!ParamData.TryGetValue(name, out ValueDataModel data))
                {
                    ParamData.Add(name, new ValueDataModel(value));
                }
                else
                {
                    data?.SetNewValue(value);
                }
            }
            return IsValid;
        }
    }

}