﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Async;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RevitTimasBIMTools.ViewModels
{
    public sealed class AreaRebarMarkFixViewModel : ObservableObject
    {

        private Document doc { get; set; }
        private IList<Element> areaReinforcements { get; set; }
        private IDictionary<string, ValueDataModel> map { get; set; }
        private TaskScheduler taskContext { get; set; } = TaskScheduler.FromCurrentSynchronizationContext();


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


        internal async void FixAreaRebarParameter()
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                foreach (Element element in areaReinforcements)
                {
                    if (param is not null && element is AreaReinforcement areaReinforce)
                    {
                        map = new Dictionary<string, ValueDataModel>();
                        IList<ElementId> rebarIds = areaReinforce.GetRebarInSystemIds();
                        TransactionManager.CreateTransaction(doc, "Set Mark", () =>
                        {
                            int index = 0;
                            while (0 < rebarIds.Count)
                            {
                                index++;
                                if (index > rebarIds.Count - 1) { index = 0; }
                                Element elem = doc.GetElement(rebarIds[index]);
                                if (elem is RebarInSystem rebarIn)
                                {
                                    Logger.Log("Count: " + index.ToString());
                                    if (ValidateParameter(rebarIn, param))
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


        private bool ValidateParameter(RebarInSystem rebar, Parameter param)
        {
            string value = param.GetValue();
            string name = param.Definition.Name;
            bool IsValid = map.Values.All(s => s.Counter > 3);
            if (IsValid && map.TryGetValue(name, out ValueDataModel result))
            {
                _ = rebar.get_Parameter(param.GUID).SetValue(result.Content);
            }
            else if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
            {
                if (!map.TryGetValue(name, out ValueDataModel data))
                {
                    map.Add(name, new ValueDataModel(value));
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