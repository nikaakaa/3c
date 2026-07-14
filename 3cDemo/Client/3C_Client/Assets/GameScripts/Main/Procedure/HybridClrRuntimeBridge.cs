#if ENABLE_HYBRIDCLR
using HybridCLR;
#endif
using TEngine;

namespace Procedure
{
    public static class HybridClrRuntimeBridge
    {
        public static void LoadMetadataForAOTAssembly(byte[] dllBytes, string assetName)
        {
#if ENABLE_HYBRIDCLR
            var mode = HomologousImageMode.SuperSet;
            var code = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Log.Warning($"Load AOT metadata: {assetName}, mode: {mode}, code: {code}");
#else
            Log.Fatal($"HybridCLR is not enabled, cannot load AOT metadata: {assetName}");
#endif
        }
    }
}
