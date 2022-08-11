namespace TimasRevitBIMTools
{
    using System;
    using System.Diagnostics;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.UI.Selection;
    [Transaction(TransactionMode.ReadOnly)]
    class CmdParameterUnitConverter : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Reference rfs;
            try
            {
                rfs = uidoc.Selection.PickObject(ObjectType.Element);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            Element e = doc.GetElement(rfs.ElementId);

            foreach (Parameter p in e.Parameters)
            {
                if (StorageType.Double == p.StorageType)
                {
                    try
                    {
                        Debug.Print($"Parameter name: {p.Definition.Name}" +
                            $"\tParameter value (imperial): {p.AsDouble()}" +
                            $"\tParameter AsValueString: {p.AsValueString()}");
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Parameter name: {0}\tException: {1}", p.Definition.Name, ex.Message);
                    }
                }
            }
            return Result.Succeeded;
        }
    }
}