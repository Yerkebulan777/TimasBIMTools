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
                                    if (ValidateParameter(local, rebarIn, counter > amount))
                                    {
                                        Logger.Log($"\n <<< VALIDATED >>> \n");
                                        if (rebarIds.Remove(rebarIds[idx]))
                                        {
                                            amount = rebarIds.Count;
                                            if (amount.Equals(0))
                                            {
                                                paramData?.Clear();
                                                paramData = null;
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


        private bool ValidateParameter(Parameter sparam, RebarInSystem rebar, bool isLimited)
        {
            string value = sparam.GetValue();
            string name = sparam.Definition.Name;

            paramData ??= new Dictionary<string, ValueDataModel>();

            bool validCount = paramData.Values.Any(v => v.Counter > 3);
            bool validValue = paramData.Values.Any(v => v.Content is not null);
            bool isValidate = isLimited || (validCount && validValue && paramData.Count > 0);

            if (isValidate && paramData.TryGetValue(name, out ValueDataModel model))
            {
                string msg = $"Parameter: {name} Is limited: {isLimited}";
                Debug.Assert(model is not null, "Dictionary value can't be null\t" + msg);
                Debug.Assert(!string.IsNullOrEmpty(model.Content), "Value can't be null\t" + msg);
                isValidate = rebar.get_Parameter(paramGuid).SetValue(model.Content);
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