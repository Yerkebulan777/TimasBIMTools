using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async.ExternalEvents;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;


namespace RevitTimasBIMTools.CutOpening
{
    internal class CutOpeningFamilyHandler : SyncGenericExternalEventHandler<Document, IList<FamilySymbol>>
    {
        public override object Clone()
        {
            throw new NotImplementedException();
        }


        public override string GetName()
        {
            return nameof(CutOpeningFamilyHandler);
        }


        protected override IList<FamilySymbol> Handle(UIApplication app, Document parameter)
        {
            FilteredElementCollector collector;
            IList<FamilySymbol> output = new List<FamilySymbol>();
            BuiltInCategory bic = BuiltInCategory.OST_GenericModel;
            collector = RevitFilterManager.GetInstancesOfCategory(parameter, typeof(FamilySymbol), bic);
            foreach (FamilySymbol symbol in collector)
            {
                Family family = symbol.Family;
                if (family.IsValidObject && family.IsEditable)
                {
                    if (family.FamilyPlacementType.Equals(FamilyPlacementType.OneLevelBasedHosted))
                    {
                        output.Add(symbol);
                    }
                }
            }
            return output;
        }
    }
}
