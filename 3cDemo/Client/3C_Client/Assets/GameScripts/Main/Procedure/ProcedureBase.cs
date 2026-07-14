using TEngine;

namespace Procedure
{
    public abstract class ProcedureBase : TEngine.ProcedureBase
    {
        protected static bool TryGetResourceModule(out IResourceModule resourceModule)
        {
            resourceModule = ModuleSystem.GetModule<IResourceModule>();
            if (resourceModule != null)
            {
                return true;
            }

            Log.Fatal("TEngine resource module is missing.");
            return false;
        }
    }
}
