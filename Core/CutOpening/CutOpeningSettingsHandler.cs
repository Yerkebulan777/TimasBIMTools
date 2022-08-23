using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;

namespace RevitTimasBIMTools.Core
{
    internal class CutOpeningSettingsHandler : IExternalEventHandler
    {
        public event EventHandler<SettingsCompletedEventArgs> Completed;
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc?.Document;

            if (doc == null)
            {
                return;
            }
        }


        public IList<BuiltInCategory> GetFitrableCategories(Document document)
        {
            List<BuiltInCategory> output = new List<BuiltInCategory>();
            foreach (ElementId catId in ParameterFilterUtilities.GetAllFilterableCategories())
            {
                try
                {
                    Category category = Category.GetCategory(document, catId);
                    if (category != null && category.AllowsBoundParameters)
                    {
                        if (category.CategoryType == CategoryType.Model)
                        {
                            output.Add((BuiltInCategory)catId.IntegerValue);
                        }
                    }
                }
                catch (Exception exc)
                {
                    LogManager.Error(exc.Message);
                }
            }

            return output;
        }


        private void OnCompleted(SettingsCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }


        public string GetName()
        {
            return nameof(CutOpeningSettingsHandler);
        }

    }

    public class SettingsCompletedEventArgs : EventArgs
    {
        public IList<Category> Categories { get; }
        public IList<Element> Elements { get; }
        public SettingsCompletedEventArgs(IList<Category> categories, IList<Element> elements)
        {
            Categories = categories;
            Elements = elements;
        }
    }
}
