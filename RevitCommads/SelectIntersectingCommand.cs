using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;


namespace RevitTimasBIMTools.RevitCommads
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    /// SymbolName class CmdSelectTestIntersecting ///
    public class CmdSelectTestIntersecting : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            //Autodesk.Revit.ApplicationServices.SmartToolController app = commandData.SmartToolController.SmartToolController;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = new UIDocument(doc);

            IList<Element> selSet = new List<Element>();
            StringCollection wsc = Properties.Settings.Default.HostElementIdCollection;
            lock (wsc.SyncRoot)
            {
                foreach (string line in wsc)
                {
                    int.TryParse(line, out int elemIdInt);
                    if (elemIdInt > 0)
                    {
                        Element elem = doc.GetElement(new ElementId(elemIdInt));
                        selSet.Add(elem);
                    }
                }
            }

            uidoc.Selection.SetElementIds(selSet.Select(q => q.Id).ToList());

            TaskDialog.Show("Select Intersecting", selSet.Count + " intersecting simbolList found");

            uidoc.RefreshActiveView();

            return Result.Succeeded;
        }
    }
}
