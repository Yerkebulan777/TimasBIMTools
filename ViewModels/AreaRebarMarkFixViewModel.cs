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

        private Document doc { get; set; }
        private IList<Element> areaReinforcements { get; set; } = null;
        private static IDictionary<string, ValueDataModel> paramData { get; set; } = null;
        private Guid paramGuid { get; set; }

        private Parameter param;
        public Parameter SelectedParameter
        {
            get => param;
            set
            {
                if (SetProperty(ref param, value) && param is not null)
                {
                    paramGuid = param.GUID;
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
                foreach (Element current in areaReinforcements)
                {
                    //app.ActiveUIDocument.Selection.SetElementIds(new List<ElementId> { current.Id });
                    if (param is not null && current is AreaReinforcement areaReinforce)
                    {
                        IList<ElementId> rebarIds = areaReinforce.GetRebarInSystemIds();
                        TransactionManager.CreateTransaction(doc, "Set Mark", () =>
                        {
                            int counter = 0;
                            paramData = null;
                            Random rnd = new();
                            int amount = rebarIds.Count;
                            while (0 < amount)
                            {
                                counter++;
                                int idx = rnd.Next(0, amount);
                                Element element = doc.GetElement(rebarIds[idx]);
                                Parameter local = element.get_Parameter(paramGuid);
                                if (element is RebarInSystem rebarIn && local is not null)
                                {
                                    Logger.Log($"Count: " + counter.ToString());
                                    if (ValidateParameter(local, rebarIn, counter > amount))
                                    {
                                        Logger.Log($"\n <<< VALIDATED >>> \n");
                                        if (rebarIds.Remove(rebarIds[idx]))
                                        {
                                            amount = rebarIds.Count;
                                            Logger.Log($"\nAmount: {amount}");
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
            });
        }


        private bool ValidateParameter(Parameter local, RebarInSystem rebar, bool isLimited)
        {
            string value = local.GetValue();
            string name = local.Definition.Name;

            paramData ??= new Dictionary<string, ValueDataModel>();

            bool isValidate = isLimited || paramData.Values.Any(val => val.Counter > 3);

            if (isValidate && paramData.TryGetValue(name, out ValueDataModel result))
            {
                isValidate = rebar.get_Parameter(paramGuid).SetValue(result.Content);
                Debug.Assert(!string.IsNullOrEmpty(result.Content), "Value is null!");
                Debug.Assert(isValidate, "Parameter value is not set! ");
            }
            else if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
            {
                if (!paramData.TryGetValue(name, out ValueDataModel data))
                {
                    paramData.Add(name, new ValueDataModel(value));
                }
                else
                {
                    data?.SetNewValue(value);
                }
            }

            return isValidate;
        }
    }


}