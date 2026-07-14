using Cysharp.Threading.Tasks;
using TEngine;
using YooAsset;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureModule>;

namespace Procedure
{
    public sealed class ProcedureInitPackage : ProcedureBase
    {
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            InitializePackageAsync(procedureOwner).Forget();
        }

        private async UniTaskVoid InitializePackageAsync(ProcedureOwner procedureOwner)
        {
            if (!TryGetResourceModule(out var resourceModule))
            {
                return;
            }

            if (!YooAssets.Initialized)
            {
                Log.Fatal("YooAssets is not initialized. Check ResourceModuleDriver startup configuration.");
                return;
            }

            var operation = await resourceModule.InitPackage(resourceModule.DefaultPackageName, true);
            if (operation == null)
            {
                Log.Fatal("TEngine resource package initialization did not return an operation.");
                return;
            }

            if (operation.Status != EOperationStatus.Succeed)
            {
                Log.Fatal($"TEngine resource package initialization failed: {operation.Error}");
                return;
            }

            Log.Info($"TEngine resource package initialized: {resourceModule.DefaultPackageName}");
            ChangeState<ProcedureInitResources>(procedureOwner);
        }
    }
}
