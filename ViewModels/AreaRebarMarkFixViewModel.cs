using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Async;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class AreaRebarMarkFixViewModel : ObservableObject
    {
        private static bool? findValue = null;
        private Document doc { get; set; }
        private IList<Element> areaReinforcements { get; set; } = null;
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
                    foreach (Parameter param in GetAllTextParameters(doc, rebarId))
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


        private IList<Parameter> GetAllTextParameters(Document doc, ElementId rebarId)
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
                        if (param.UserModifiable && param.IsShared && !param.IsReadOnly)
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


        internal async void FixAreaRebarParameter(int percent = 15)
        {
            await RevitTask.RunAsync(app =>
            {
                Random rnd = new();
                int factor = 1 + (percent / 100);
                doc = app.ActiveUIDocument.Document;
                foreach (Element current in areaReinforcements)
                {
                    if (selectedParam is not null && current is AreaReinforcement areaReinforce)
                    {
                        paramData = new Dictionary<string, ValueDataModel>(100);
                        IList<ElementId> rebarIds = areaReinforce.GetRebarInSystemIds();
                        rebarIds = rebarIds.OrderBy(item => rnd.Next()).ToList();
                        TransactionManager.CreateTransaction(doc, "Set Mark", () =>
                        {
                            int amount = rebarIds.Count;
                            findValue = null;
                            int counter = 0;
                            while (true)
                            {
                                counter++;
                                int idx = rnd.Next(amount);
                                bool limited = counter > (amount * factor);
                                Element element = doc.GetElement(rebarIds[idx]);
                                Parameter param = element.get_Parameter(paramGuid);
                                if (element is RebarInSystem rebarIn && param is not null)
                                {
                                    bool isValid = ValidateParameter(param, rebarIn, limited);
                                    if (limited && !isValid && !findValue.HasValue)
                                    {
                                        if (!paramData.Values.Any(v => v.Counter > 0))
                                        {
                                            Logger.Info("Break and counter: " + counter.ToString());
                                            break;
                                        }
                                    }
                                    else if (isValid && rebarIds.Remove(rebarIds[idx]))
                                    {
                                        findValue = true;
                                        amount = rebarIds.Count;
                                        if (amount.Equals(0))
                                        {
                                            paramData?.Clear();
                                            paramData = null;
                                            break;
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

            bool valid = limited && paramData.Values.Any(v => v.Counter > 3);

            if (valid && paramData.TryGetValue(name, out ValueDataModel model))
            {
                Debug.Assert(!string.IsNullOrEmpty(model.Content), "Value can't be null");
                valid = rebar.get_Parameter(paramGuid).SetValue(model.Content);
            }
            else if (!string.IsNullOrWhiteSpace(value))
            {
                if (!paramData.TryGetValue(name, out ValueDataModel dataModel))
                {
                    paramData.Add(name, new ValueDataModel(value));
                }
                else
                {
                    dataModel.SetNewValue(value);
                    valid = dataModel.Content.Equals(value);
                }
            }

            return valid;
        }

    }


}