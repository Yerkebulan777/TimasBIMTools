using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.ViewModels
{
    public sealed class AreaRebarMarkFixViewModel : ObservableObject
    {

        private readonly Random rnd;
        public UIDocument uidoc { get; set; }
        private IList<Element> areaReinforcements { get; set; }
        private IDictionary<string, ValueDataModel> map { get; set; }


        private Parameter param;
        public Parameter Parameter
        {
            get => param;
            set => SetProperty(ref param, value);
        }


        private IDictionary<string, Parameter> parameters;
        public IDictionary<string, Parameter> Parameters
        {
            get => parameters;
            set => SetProperty(ref parameters, value);
        }


        public AreaRebarMarkFixViewModel(UIDocument uidocument)
        {
            uidoc = uidocument;
            rnd = new Random();
        }


        private void RetrieveParameterData()
        {
            Document doc = uidoc.Document;
            areaReinforcements = GetAllAreaReinforcement(doc);
            Element reinforcement = areaReinforcements.FirstOrDefault();
            if (reinforcement is not null and AreaReinforcement areaReinforcement)
            {
                IList<ElementId> rebarIds = areaReinforcement.GetRebarInSystemIds();
                ElementId rebarId = rebarIds.FirstOrDefault(i => i.IntegerValue > 0);
                foreach (Parameter param in GetAllStringParameters(doc, rebarId))
                {
                    string name = param.Definition.Name;
                    parameters[name] = param;
                }
            }
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
            IList<Parameter> parameters = null;
            if (rebarId is not null and ElementId)
            {
                Element element = doc.GetElement(rebarId);
                if (element is RebarInSystem rebar)
                {
                    parameters = rebar.GetOrderedParameters();
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        Parameter param = parameters[i];
                        if (param.StorageType is not StorageType.String || !param.IsShared || param.IsReadOnly)
                        {
                            _ = parameters.Remove(param);
                        }
                    }
                }
            }
            return parameters;
        }


        internal void RetrievAreaRebarParameters()
        {
            Document doc = uidoc.Document;
            foreach (Element item in areaReinforcements)
            {
                if (item is AreaReinforcement reinforcement)
                {
                    IList<ElementId> rebarIds = reinforcement.GetRebarInSystemIds();
                    map = new Dictionary<string, ValueDataModel>();
                    while (0 < rebarIds.Count)
                    {
                        int num = rnd.Next(0, rebarIds.Count);
                        Element elem = doc.GetElement(rebarIds[num]);
                        if (elem is RebarInSystem)
                        {
                            //if (ValidateParameters(rebar, parameters))
                            //{
                            //    _ = rebarIds.Remove(rebarIds[num]);
                            //}
                        }
                    }
                }
            }
        }


        private bool ValidateParameter(RebarInSystem rebar, Parameter param, int limit = 3)
        {
            string value = param.GetValue();
            string name = param.Definition.Name;
            bool IsValid = map.Values.All(s => s.Counter > limit);
            if (IsValid && string.IsNullOrEmpty(value))
            {
                if (map.TryGetValue(name, out ValueDataModel result))
                {
                    _ = rebar.get_Parameter(param.GUID).SetValue(result.Content);
                }
            }
            else if (!map.TryGetValue(name, out ValueDataModel data))
            {
                map.Add(name, new ValueDataModel(value));
            }
            else
            {
                data?.SetNewValue(value);
            }
            return IsValid;
        }

    }
}
