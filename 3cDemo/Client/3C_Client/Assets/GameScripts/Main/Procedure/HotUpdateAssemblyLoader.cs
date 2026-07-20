using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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
        public static async UniTask<HotUpdateAssemblyLoadResult> LoadAsync(
            IResourceModule resourceModule,
            UpdateSetting setting,
            CancellationToken cancellationToken = default)
        {
            if (resourceModule == null) throw new ArgumentNullException(nameof(resourceModule));
            if (setting == null) throw new InvalidOperationException("TEngine update setting is missing.");

            if (!setting.Enable || resourceModule.PlayMode == EPlayMode.EditorSimulateMode)
            {
                var loadedResult = FindLoadedAssemblies(setting);
                ValidateResult(setting, loadedResult);
                return loadedResult;
            }

            var hotUpdateBytes = await LoadAssemblyAssetsAsync(
                resourceModule,
                setting,
                setting.HotUpdateAssemblies,
                cancellationToken);

#if !UNITY_EDITOR
            var aotMetadataBytes = await LoadAssemblyAssetsAsync(
                resourceModule,
                setting,
                setting.AOTMetaAssemblies,
                cancellationToken);
            for (var index = 0; index < setting.AOTMetaAssemblies.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                HybridClrRuntimeBridge.LoadMetadataForAOTAssembly(
                    aotMetadataBytes[index],
                    setting.AOTMetaAssemblies[index]);
            }
#endif

            var result = LoadHotUpdateAssemblies(setting, hotUpdateBytes, cancellationToken);
            ValidateResult(setting, result);
            return result;
        }

        static HotUpdateAssemblyLoadResult FindLoadedAssemblies(UpdateSetting setting)
        {
            var assembliesByName = new Dictionary<string, Assembly>(StringComparer.Ordinal);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                assembliesByName[$"{assembly.GetName().Name}.dll"] = assembly;
            }

            Assembly mainAssembly = null;
            var hotUpdateAssemblies = new List<Assembly>(setting.HotUpdateAssemblies.Count);
            foreach (var dllName in setting.HotUpdateAssemblies)
            {
                if (!assembliesByName.TryGetValue(dllName, out var assembly))
                {
                    throw new InvalidOperationException($"Loaded hot-update assembly is missing: {dllName}");
                }

                hotUpdateAssemblies.Add(assembly);
                if (dllName == setting.LogicMainDllName)
                {
                    mainAssembly = assembly;
                }
            }

            return new HotUpdateAssemblyLoadResult(mainAssembly, hotUpdateAssemblies);
        }

        static async UniTask<List<byte[]>> LoadAssemblyAssetsAsync(
            IResourceModule resourceModule,
            UpdateSetting setting,
            IReadOnlyList<string> assemblyNames,
            CancellationToken cancellationToken)
        {
            var assemblyBytes = new List<byte[]>(assemblyNames.Count);
            foreach (var assemblyName in assemblyNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var textAsset = await resourceModule.LoadAssetAsync<TextAsset>(
                    GetAssemblyLocation(setting, assemblyName),
                    cancellationToken);
                if (textAsset == null)
                {
                    throw new InvalidOperationException($"Assembly asset is missing: {assemblyName}");
                }

                try
                {
                    var bytes = textAsset.bytes;
                    if (bytes == null || bytes.Length == 0)
                    {
                        throw new InvalidOperationException($"Assembly asset is empty: {assemblyName}");
                    }

                    assemblyBytes.Add(bytes);
                }
                finally
                {
                    resourceModule.UnloadAsset(textAsset);
                }
            }

            return assemblyBytes;
        }

        static HotUpdateAssemblyLoadResult LoadHotUpdateAssemblies(
            UpdateSetting setting,
            IReadOnlyList<byte[]> assemblyBytes,
            CancellationToken cancellationToken)
        {
            Assembly mainAssembly = null;
            var hotUpdateAssemblies = new List<Assembly>(assemblyBytes.Count);
            for (var index = 0; index < assemblyBytes.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var assembly = Assembly.Load(assemblyBytes[index]);
                var loadedDllName = $"{assembly.GetName().Name}.dll";
                if (!string.Equals(loadedDllName, setting.HotUpdateAssemblies[index], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Hot-update assembly identity mismatch: expected {setting.HotUpdateAssemblies[index]}, loaded {loadedDllName}");
                }

                if (loadedDllName == setting.LogicMainDllName)
                {
                    mainAssembly = assembly;
                }

                hotUpdateAssemblies.Add(assembly);
            }

            return new HotUpdateAssemblyLoadResult(mainAssembly, hotUpdateAssemblies);
        }

        static void ValidateResult(UpdateSetting setting, HotUpdateAssemblyLoadResult result)
        {
            if (result.MainAssembly == null)
            {
                throw new InvalidOperationException($"Main logic assembly is missing: {setting.LogicMainDllName}");
            }

            if (result.HotUpdateAssemblies == null ||
                result.HotUpdateAssemblies.Count != setting.HotUpdateAssemblies.Count)
            {
                throw new InvalidOperationException("Hot-update assembly set is incomplete.");
            }
        }

        static string GetAssemblyLocation(UpdateSetting setting, string dllName)
        {
            var assetPath = setting.AssemblyTextAssetPath.Trim().Trim('/', '\\');
            var extension = setting.AssemblyTextAssetExtension.Trim();
            return $"Assets/{assetPath}/{dllName}{extension}";
        }
    }
}
