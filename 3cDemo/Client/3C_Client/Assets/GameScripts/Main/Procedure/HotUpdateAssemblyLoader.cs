using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using YooAsset;

namespace Procedure
{
    public readonly struct HotUpdateAssemblyLoadResult
    {
        public HotUpdateAssemblyLoadResult(Assembly mainAssembly, IReadOnlyList<Assembly> hotUpdateAssemblies)
        {
            MainAssembly = mainAssembly;
            HotUpdateAssemblies = hotUpdateAssemblies;
        }

        public Assembly MainAssembly { get; }

        public IReadOnlyList<Assembly> HotUpdateAssemblies { get; }
    }

    public static class HotUpdateAssemblyLoader
    {
        public static async UniTask<HotUpdateAssemblyLoadResult> LoadAsync(IResourceModule resourceModule, UpdateSetting setting)
        {
            if (setting == null)
            {
                Log.Fatal("TEngine update setting is missing.");
                return new HotUpdateAssemblyLoadResult(null, Array.Empty<Assembly>());
            }

            await LoadAotMetadataAsync(resourceModule, setting);

            if (!setting.Enable || resourceModule.PlayMode == EPlayMode.EditorSimulateMode)
            {
                return FindLoadedAssemblies(setting);
            }

            return await LoadHotUpdateAssembliesAsync(resourceModule, setting);
        }

        private static HotUpdateAssemblyLoadResult FindLoadedAssemblies(UpdateSetting setting)
        {
            Assembly mainAssembly = null;
            var hotUpdateAssemblies = new List<Assembly>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var dllName = $"{assembly.GetName().Name}.dll";
                if (dllName == setting.LogicMainDllName)
                {
                    mainAssembly = assembly;
                }

                if (setting.HotUpdateAssemblies.Contains(dllName))
                {
                    hotUpdateAssemblies.Add(assembly);
                }
            }

            return new HotUpdateAssemblyLoadResult(mainAssembly, hotUpdateAssemblies);
        }

        private static async UniTask<HotUpdateAssemblyLoadResult> LoadHotUpdateAssembliesAsync(IResourceModule resourceModule, UpdateSetting setting)
        {
            Assembly mainAssembly = null;
            var hotUpdateAssemblies = new List<Assembly>();

            foreach (var hotUpdateDllName in setting.HotUpdateAssemblies)
            {
                var textAsset = await resourceModule.LoadAssetAsync<TextAsset>(hotUpdateDllName);
                if (textAsset == null)
                {
                    Log.Fatal($"Load hot update assembly failed: {hotUpdateDllName}");
                    continue;
                }

                var assembly = Assembly.Load(textAsset.bytes);
                var loadedDllName = $"{assembly.GetName().Name}.dll";
                if (loadedDllName == setting.LogicMainDllName)
                {
                    mainAssembly = assembly;
                }

                hotUpdateAssemblies.Add(assembly);
                resourceModule.UnloadAsset(textAsset);
            }

            return new HotUpdateAssemblyLoadResult(mainAssembly, hotUpdateAssemblies);
        }

        private static async UniTask LoadAotMetadataAsync(IResourceModule resourceModule, UpdateSetting setting)
        {
#if UNITY_EDITOR
            await UniTask.CompletedTask;
#else
            if (!setting.Enable)
            {
                await UniTask.CompletedTask;
                return;
            }

            foreach (var aotDllName in setting.AOTMetaAssemblies)
            {
                var textAsset = await resourceModule.LoadAssetAsync<TextAsset>(aotDllName);
                if (textAsset == null)
                {
                    Log.Fatal($"Load AOT metadata failed: {aotDllName}");
                    continue;
                }

                HybridClrRuntimeBridge.LoadMetadataForAOTAssembly(textAsset.bytes, textAsset.name);
                resourceModule.UnloadAsset(textAsset);
            }
#endif
        }
    }
}
