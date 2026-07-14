using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameEntry : MonoBehaviour
{
    [SerializeField]
    private bool dontDestroyOnLoad = true;

    private bool _started;

    private void Awake()
    {
        ModuleSystem.GetModule<IUpdateDriver>();
        ModuleSystem.GetModule<IResourceModule>();
        ModuleSystem.GetModule<IDebuggerModule>();
        ModuleSystem.GetModule<IFsmModule>();

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        StartProcedureAsync().Forget();
    }

    private async UniTaskVoid StartProcedureAsync()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        await UniTask.Yield();

        if (Settings.ProcedureSetting == null)
        {
            Log.Fatal("TEngine procedure setting is missing.");
            return;
        }

        Settings.ProcedureSetting.StartProcedure().Forget();
    }
}
