using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async.ExternalEvents;


namespace RevitTimasBIMTools.RevitCommads
{
    internal class GetFamilyParamsEventHandler : SyncGenericExternalEventHandler<bool, Family>
    {
        public override object Clone()
        {
            return new GetFamilyParamsEventHandler();
        }

        public override string GetName()
        {
            return nameof(GetFamilyParamsEventHandler);
        }

        protected override Family Handle(UIApplication app, bool parameter)
        {
            throw new System.NotImplementedException();
        }
    }
}