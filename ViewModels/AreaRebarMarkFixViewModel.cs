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
        private UIDocument uidoc;
        public UIDocument UIDoc
        {
            get { return uidoc; }
            set { uidoc = value; }
        }


        private readonly Random rnd;
        private IDictionary<string, ValueDataModel> map { get; set; }


        public AreaRebarMarkFixViewModel(UIDocument uidoc)
        {
            UIDoc = uidoc;
            rnd = new Random();
        }


        internal void RetrievAreaRebarParameters()
        {
            Document doc = uidoc.Document;
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(AreaReinforcement));
            foreach (Element item in collector.WhereElementIsNotElementType())
            {
                if (item is AreaReinforcement reinforcement)
                {
                    map = new Dictionary<string, ValueDataModel>();
                    IList<ElementId> rebarIds = reinforcement.GetRebarInSystemIds();
                    ElementId rebarId = rebarIds.FirstOrDefault(rid => rid.IntegerValue > 0);
                    IList<Parameter> parameters = GetStringParameters(doc, rebarId);
                    while (0 < rebarIds.Count)
                    {
                        int num = rnd.Next(0, rebarIds.Count);
                        Element elem = doc.GetElement(rebarIds[num]);
                        if (elem is RebarInSystem rebar)
                        {
                            if (ValidateParameters(rebar, parameters))
                            {
                                rebarIds.Remove(rebarIds[num]);
                            }
                        }
                    }
                }
            }
        }


        private bool ValidateParameters(RebarInSystem rebar, IList<Parameter> parameters, int limit = 3)
        {
            bool allValid = map.Values.All(s => s.Counter > limit);
            bool allFilled = parameters.Count == map.Count;
            for (int i = 0; i < parameters.Count; i++)
            {
                Parameter param = parameters[i];
                string value = param.GetValue();
                string name = param.Definition.Name;
                if (allFilled && allValid && string.IsNullOrEmpty(value))
                {
                    if (map.TryGetValue(name, out ValueDataModel result))
                    {
                        rebar.get_Parameter(param.GUID).SetValue(result.Content);
                    }
                }
                else if (!map.TryGetValue(name, out ValueDataModel data))
                {
                    map.Add(name, new ValueDataModel(value));
                }
                else if (data != null)
                {
                    data.SetNewValue(value);
                }
            }
            return allFilled && allValid;
        }


        private IList<Parameter> GetStringParameters(Document doc, ElementId rebarId)
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
                            parameters.Remove(param);
                        }
                    }
                }
            }
            return parameters;
        }

    }
}
