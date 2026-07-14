using TEngine;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureModule>;

namespace Procedure
{
    public sealed class ProcedureLaunch : ProcedureBase
    {
        private bool _changed;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            _changed = false;
            Log.Info("TEngine launch procedure entered.");
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (_changed)
            {
                return;
            }

            _changed = true;
            ChangeState<ProcedureInitPackage>(procedureOwner);
        }
    }
}
