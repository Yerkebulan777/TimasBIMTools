using Autodesk.Revit.UI;

namespace RevitTimasBIMTools.Core
{
    public abstract class RevitEventWrapper<TType> : IExternalEventHandler
    {
        private TType savedArgs;
        private readonly object syncLock;
        private readonly ExternalEvent revitEvent;


        /// <summary>
        /// Class for wrapping methods for execution within a "valid" Revit API context.
        /// </summary>
        protected RevitEventWrapper()
        {
            revitEvent = ExternalEvent.Create(this);
            syncLock = new object();
        }


        /// <summary>
        /// Wraps the "Execution" method in a valid Revit API context.
        /// </summary>
        public void Execute(UIApplication app)
        {
            TType args;

            lock (syncLock)
            {
                args = savedArgs;
                savedArgs = default;
            }

            Execute(app, args);
        }


        /// <summary>
        /// Get the name of the operation.
        /// </summary>
        public string GetName()
        {
            return GetType().Name;
        }


        /// <summary>
        /// StartHandlerExecute the wrapped external event in a valid Revit API context.
        /// </summary>
        public void Raise(TType args)
        {
            lock (syncLock)
            {
                savedArgs = args;
            }

            _ = revitEvent.Raise();
        }


        public abstract void Execute(UIApplication app, TType args);
    }
}
