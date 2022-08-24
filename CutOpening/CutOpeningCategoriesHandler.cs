using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async.ExternalEvents;
using System;
using System.Collections.Generic;


namespace RevitTimasBIMTools.CutOpening
{
    public class CutOpeningCategoriesHandler : SyncGenericExternalEventHandler<Document, IList<Category>>
    {
        private readonly IList<BuiltInCategory> builtInCats = new List<BuiltInCategory>
        {
            BuiltInCategory.OST_Conduit,
            BuiltInCategory.OST_Furniture,
            BuiltInCategory.OST_CableTray,
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_GenericModel,
            BuiltInCategory.OST_MechanicalEquipment
        };


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
            return GetCategoriesByBuiltIn(parameter, builtInCats);
        }


        private IList<Category> GetCategoriesByBuiltIn(Document doc, IList<BuiltInCategory> bics)
        {
            IList<Category> output = new List<Category>();
            foreach (BuiltInCategory catId in bics)
            {
                Category cat = null;
                try
                {
                    cat = Category.GetCategory(doc, catId);
                }
                finally
                {
                    if (cat != null)
                    {
                        output.Add(cat);
                    }
                }
            }
            return output;
        }
    }
}