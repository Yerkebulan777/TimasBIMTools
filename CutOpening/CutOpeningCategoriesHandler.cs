using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async.ExternalEvents;
using System;
using System.Collections.Generic;


namespace RevitTimasBIMTools.CutOpening
{
    public class CutOpeningCategoriesHandler : SyncGenericExternalEventHandler<Document, IList<Category>>
    {
        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public override string GetName()
        {
            return "CutOpeningCategoriesHandler";
        }

        protected override IList<Category> Handle(UIApplication app, Document parameter)
        {
            return new List<Category>();
        }
    }
}