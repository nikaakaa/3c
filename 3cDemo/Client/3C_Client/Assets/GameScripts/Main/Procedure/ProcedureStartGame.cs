using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine.SceneManagement;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureModule>;

namespace Procedure
{
    public sealed class ProcedureStartGame : ProcedureBase
    {
        const string MainSceneLocation = "Assets/Scenes/Sandbox/SandBox.unity";

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("TEngine start game procedure entered.");
            LoadMainSceneAsync().Forget();
        }

        static async UniTaskVoid LoadMainSceneAsync()
        {
            ISceneModule sceneModule = ModuleSystem.GetModule<ISceneModule>();
            if (sceneModule == null)
            {
                Log.Fatal("TEngine scene module is invalid.");
                return;
            }

            Scene scene = await sceneModule.LoadSceneAsync(MainSceneLocation, LoadSceneMode.Single);
            if (!scene.IsValid())
            {
                Log.Fatal($"Load main gameplay scene failed: {MainSceneLocation}");
                return;
            }

            Log.Info($"Loaded main gameplay scene: {MainSceneLocation}");
        }
    }
}
