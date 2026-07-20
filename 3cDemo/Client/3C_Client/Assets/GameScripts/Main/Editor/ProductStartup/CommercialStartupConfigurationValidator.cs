using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using ThirdPerson.ProductStartup;
using ThirdPersonCharacter.Editor.ProductBuild;

namespace ThirdPersonCharacter.Editor.ProductStartup
{
    internal enum ProductBuildKind
    {
        CommercialClient = 1,
        NetworkTestPlayer = 2
    }

    internal static class ProductBuildValidationContext
    {
        static ProductBuildKind? s_ActiveKind;

        public static ProductBuildKind ActiveKind => s_ActiveKind ?? ProductBuildKind.CommercialClient;

        public static IDisposable Enter(ProductBuildKind kind)
        {
            if (s_ActiveKind.HasValue)
                throw new InvalidOperationException($"A product build validation context is already active: {s_ActiveKind.Value}.");
            s_ActiveKind = kind;
            return new Scope(kind);
        }

        sealed class Scope : IDisposable
        {
            readonly ProductBuildKind m_Kind;
            bool m_Disposed;

            public Scope(ProductBuildKind kind)
            {
                m_Kind = kind;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                if (s_ActiveKind != m_Kind)
                    throw new InvalidOperationException("Product build validation context ownership changed before disposal.");
                s_ActiveKind = null;
                m_Disposed = true;
            }
        }
    }

    internal sealed class CommercialStartupConfigurationValidator : IPreprocessBuildWithReport
    {
        private const string BootstrapScene = "Assets/Scenes/Bootstrap.unity";
        private const string ProductShellScene = "Assets/Scenes/Product/ProductShell.unity";
        private const string GameplayScene = "Assets/Scenes/Standalone/StandaloneGameplay.unity";
        private const string CollectorSetting = "Assets/AssetBundleCollectorSetting.asset";
        private const string BootstrapPresentationPath = "Assets/Resources/ProductStartup/ProductBootstrapPresentation.json";
        private const string OptionalHdRoot = "Assets/AssetRaw/Product/OptionalHD/";

        public int callbackOrder => -2000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (ProductBuildValidationContext.ActiveKind != ProductBuildKind.CommercialClient)
                return;
            ValidateAll();
        }

        [MenuItem("Tools/3C/Product Startup/Validate Configuration")]
        private static void ValidateAll()
        {
            ValidateBuildSettings();
            ValidateBootstrapClosure();
            ValidateProductShellClosure();
            ValidateCollectorTags();
            ValidateGameplayClosure();
            ValidateOptionalHdClosure();
            ValidateProductStartupProfile();
        }

        private static void ValidateProductStartupProfile()
        {
            ProductStartupProfile profile = AssetDatabase.LoadAssetAtPath<ProductStartupProfile>(ClientBuildArtifactLayout.ProductStartupProfilePath);
            if (!profile)
            {
                throw new BuildFailedException($"缺少唯一正式启动配置：{ClientBuildArtifactLayout.ProductStartupProfilePath}");
            }
            if (!profile.TryValidate(out ProductStartupErrorCode code, out string safeError))
            {
                throw new BuildFailedException($"正式启动配置无效：{code} {safeError}");
            }

            string[] dependencies = AssetDatabase.GetDependencies(BootstrapScene, true);
            if (!dependencies.Contains(ClientBuildArtifactLayout.ProductStartupProfilePath, StringComparer.Ordinal))
            {
                throw new BuildFailedException("Bootstrap Scene 未绑定唯一 ProductStartupProfile。");
            }
        }

        private static void ValidateBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || !string.Equals(scenes[0], BootstrapScene, StringComparison.Ordinal))
            {
                throw new BuildFailedException($"普通产品唯一启动入口必须是 {BootstrapScene}。");
            }

            if (scenes.Contains(ProductShellScene, StringComparer.Ordinal))
            {
                throw new BuildFailedException("ProductShell 必须由 Core 资源包加载，不能进入 Player 内置场景闭包。");
            }
        }

        private static void ValidateBootstrapClosure()
        {
            var dependencies = AssetDatabase.GetDependencies(BootstrapScene, true);
            RejectDependencies(dependencies, BootstrapScene, new[]
            {
                "Assets/AssetRaw/",
                "Assets/GameScripts/HotFix/",
                "Assets/Scenes/Product/",
                "Assets/Scenes/Standalone/"
            });

            if (!AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(BootstrapPresentationPath))
            {
                throw new BuildFailedException($"Bootstrap 缺少内置展示与错误资源：{BootstrapPresentationPath}");
            }
        }

        private static void ValidateProductShellClosure()
        {
            var dependencies = AssetDatabase.GetDependencies(ProductShellScene, true);
            RejectDependencies(dependencies, ProductShellScene, new[]
            {
                "Assets/Scenes/Standalone/",
                "Assets/Configs/Character/",
                "Assets/GameScripts/Main/Runtime/Simulation/"
            });
        }

        private static void ValidateCollectorTags()
        {
            YooAsset.Editor.AssetBundleCollectorSetting setting =
                AssetDatabase.LoadAssetAtPath<YooAsset.Editor.AssetBundleCollectorSetting>(CollectorSetting);
            if (!setting)
            {
                throw new BuildFailedException($"缺少 YooAsset 收集配置：{CollectorSetting}");
            }

            RequireCollector(setting, "CoreProductShell", ProductShellScene, "Core");
            RequireCollector(setting, "HotUpdateAssemblies", "Assets/AssetRaw/HotUpdate/DLL", "Core");
            RequireCollector(setting, "CoreHomeData", "Assets/AssetRaw/Product/Core", "Core");
            RequireCollector(setting, "GameplayScene", GameplayScene, "Gameplay");
            RequireCollector(setting, "GameplayShared", "Assets/AssetRaw/Product/Gameplay", "Gameplay");
            RequireCollector(setting, "GameplayCorin", "Assets/Prefabs/Characters/RuntimeProfiles/CorinStandalonePlayer.prefab", "Gameplay");
            RequireCollector(setting, "GameplayCorin", "Assets/Prefabs/Characters/RuntimeProfiles/CorinStandaloneTrainingEnemy.prefab", "Gameplay");
            RequireCollector(setting, "OptionalHD", "Assets/AssetRaw/Product/OptionalHD", "OptionalHD");
        }

        private static void ValidateGameplayClosure()
        {
            string[] dependencies = AssetDatabase.GetDependencies(GameplayScene, true);
            string[] required =
            {
                GameplayScene,
                "Assets/Prefabs/Characters/RuntimeProfiles/CorinStandalonePlayer.prefab",
                "Assets/Prefabs/Characters/RuntimeProfiles/CorinStandaloneTrainingEnemy.prefab"
            };
            foreach (string path in required)
            {
                if (!dependencies.Contains(path, StringComparer.Ordinal))
                {
                    throw new BuildFailedException($"Gameplay 标签闭包缺少 Standalone 正式依赖：{path}");
                }
            }
        }

        private static void ValidateOptionalHdClosure()
        {
            string[] optionalAssets = AssetDatabase.FindAssets(string.Empty, new[] { OptionalHdRoot.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path) && !AssetDatabase.IsValidFolder(path))
                .ToArray();
            if (optionalAssets.Length == 0)
            {
                throw new BuildFailedException("OptionalHD 标签不能为空。");
            }

            string[] correctnessDependencies = AssetDatabase.GetDependencies(
                new[] { ProductShellScene, GameplayScene },
                true);
            foreach (string dependency in correctnessDependencies)
            {
                if (dependency.StartsWith(OptionalHdRoot, StringComparison.Ordinal))
                {
                    throw new BuildFailedException($"正确性闭包不得依赖 OptionalHD：{dependency}");
                }
            }
        }

        private static void RequireCollector(
            YooAsset.Editor.AssetBundleCollectorSetting setting,
            string groupName,
            string path,
            string tag)
        {
            YooAsset.Editor.AssetBundleCollectorPackage package = setting.Packages.SingleOrDefault(candidate =>
                string.Equals(candidate.PackageName, "DefaultPackage", StringComparison.Ordinal));
            YooAsset.Editor.AssetBundleCollectorGroup group = package?.Groups.SingleOrDefault(candidate =>
                string.Equals(candidate.GroupName, groupName, StringComparison.Ordinal));
            YooAsset.Editor.AssetBundleCollector collector = group?.Collectors.SingleOrDefault(candidate =>
                string.Equals(candidate.CollectPath, path, StringComparison.Ordinal));
            string expectedGuid = AssetDatabase.AssetPathToGUID(path);
            if (group == null || collector == null ||
                !string.Equals(group.AssetTags, tag, StringComparison.Ordinal) ||
                !string.Equals(collector.AssetTags, tag, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(expectedGuid) ||
                !string.Equals(collector.CollectorGUID, expectedGuid, StringComparison.Ordinal))
            {
                throw new BuildFailedException($"DefaultPackage 缺少正式收集项：{groupName}/{tag}/{path}");
            }
        }

        private static void RejectDependencies(IReadOnlyList<string> dependencies, string owner, IEnumerable<string> forbiddenRoots)
        {
            foreach (var dependency in dependencies)
            {
                foreach (var root in forbiddenRoots)
                {
                    if (dependency.StartsWith(root, StringComparison.Ordinal))
                    {
                        throw new BuildFailedException($"{owner} 非法依赖 {dependency}");
                    }
                }
            }
        }
    }
}
