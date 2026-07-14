using System;
using Cysharp.Threading.Tasks;
using TEngine;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureModule>;

namespace Procedure
{
    public sealed class ProcedureLoadAssembly : ProcedureBase
    {
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            LoadAndEnterAsync(procedureOwner).Forget();
        }

        private async UniTaskVoid LoadAndEnterAsync(ProcedureOwner procedureOwner)
        {
            if (!TryGetResourceModule(out var resourceModule))
            {
                return;
            }

            var result = await HotUpdateAssemblyLoader.LoadAsync(resourceModule, Settings.UpdateSetting);
            if (result.MainAssembly == null)
            {
                Log.Fatal($"Main logic assembly missing: {Settings.UpdateSetting.LogicMainDllName}");
                return;
            }

            var appType = result.MainAssembly.GetType("GameApp");
            if (appType == null)
            {
                Log.Fatal("Main logic type 'GameApp' missing.");
                return;
            }

            var entryMethod = appType.GetMethod("Entrance");
            if (entryMethod == null)
            {
                Log.Fatal("Main logic entry method 'Entrance' missing.");
                return;
            }

            ChangeState<ProcedureStartGame>(procedureOwner);
            entryMethod.Invoke(null, new object[] { new object[] { result.HotUpdateAssemblies } });
        }
    }
}
