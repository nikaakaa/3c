using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.ProductResource;
using GameLogic.ProductStartup;
using ThirdPerson.ProductStartup;
using ThirdPersonGameplay.Networking.Fantasy;
using TEngine;
using UnityEngine;

public static class GameApp
{
    const string ProductShellScene = "Assets/Scenes/Product/ProductShell.unity";
    const string GameplayScene = "Assets/Scenes/Standalone/StandaloneGameplay.unity";
    const int DiagnosticsHistoryCapacity = 64;

    static FantasyProductAuthenticationFlow s_AuthenticationFlow;
    static int s_Entered;
    static int s_ReleaseStarted;

    public static void Entrance(object[] objects)
    {
        if (Interlocked.Exchange(ref s_Entered, 1) != 0)
        {
            throw new InvalidOperationException("GameApp.Entrance can only be called once.");
        }

        ProductStartupCompositionInput input = ProductStartupCompositionInput.Parse(objects);
        Utility.Unity.AddDestroyListener(Release);
        StartGameLogicAsync(input).Forget();
    }

    static async UniTaskVoid StartGameLogicAsync(ProductStartupCompositionInput input)
    {
        try
        {
            await FantasyClientBootstrap.InitializeAsync();
            s_AuthenticationFlow = new FantasyProductAuthenticationFlow(input.Profile);
            var composition = new ProductStartupComposition(
                input.Handoff,
                input.Profile,
                input.StartupSnapshots,
                BuildRuntimeDefinition(),
                s_AuthenticationFlow,
                input.DiskSpaceProbe,
                new ProjectTagDownloadService());
            await ProductStartupCoordinator.StartAsync(composition);
            Log.Info($"Product runtime initialized, hot-update assembly count: {input.Handoff.HotUpdateAssemblies.Count}");
        }
        catch (Exception exception)
        {
            Log.Fatal($"Product runtime startup failed ({exception.GetType().Name}): {exception.Message}");
            s_AuthenticationFlow?.Dispose();
            s_AuthenticationFlow = null;
            if (FantasyClientBootstrap.IsInitialized)
            {
                FantasyClientBootstrap.Shutdown();
            }
        }
    }

    static ProductRuntimeDefinition BuildRuntimeDefinition()
    {
        PreloadPlan home = PreloadPlan.Home(
            new[]
            {
                PreloadItem.Asset<TextAsset>("Assets/AssetRaw/Product/Core/HomeSharedUI.json")
            },
            new[]
            {
                PreloadItem.Asset<TextAsset>("Assets/AssetRaw/Product/Core/HomeContent.json")
            },
            new[]
            {
                PreloadItem.Asset<TextAsset>("Assets/AssetRaw/Product/Core/HomePresentation.json")
            });
        PreloadPlan gameplay = PreloadPlan.Gameplay(
            new[]
            {
                PreloadItem.Asset<TextAsset>("Assets/AssetRaw/Product/Gameplay/GameplayShared.json")
            },
            GameplayScene,
            new[]
            {
                PreloadItem.Prefab("Assets/Prefabs/Characters/RuntimeProfiles/CorinStandalonePlayer.prefab"),
                PreloadItem.Prefab("Assets/Prefabs/Characters/RuntimeProfiles/CorinStandaloneTrainingEnemy.prefab")
            });
        return new ProductRuntimeDefinition(
            ProductShellScene,
            GameplayScene,
            home,
            gameplay,
            DiagnosticsHistoryCapacity);
    }

    static void Release()
    {
        if (Interlocked.Exchange(ref s_ReleaseStarted, 1) != 0)
        {
            return;
        }

        ReleaseAsync().Forget();
    }

    static async UniTaskVoid ReleaseAsync()
    {
        try
        {
            await ProductStartupCoordinator.StopForApplicationExitAsync();
        }
        catch (Exception exception)
        {
            Log.Error($"Product runtime shutdown failed ({exception.GetType().Name}).");
        }
        finally
        {
            s_AuthenticationFlow?.Dispose();
            s_AuthenticationFlow = null;
            if (FantasyClientBootstrap.IsInitialized)
            {
                FantasyClientBootstrap.Shutdown();
            }
            Log.Warning("GameLogic product runtime released.");
        }
    }

    readonly struct ProductStartupCompositionInput
    {
        ProductStartupCompositionInput(
            ProductStartupHandoff handoff,
            ProductStartupProfile profile,
            IProductStartupSnapshotSource startupSnapshots,
            IProductDiskSpaceProbe diskSpaceProbe)
        {
            Handoff = handoff;
            Profile = profile;
            StartupSnapshots = startupSnapshots;
            DiskSpaceProbe = diskSpaceProbe;
        }

        public ProductStartupHandoff Handoff { get; }
        public ProductStartupProfile Profile { get; }
        public IProductStartupSnapshotSource StartupSnapshots { get; }
        public IProductDiskSpaceProbe DiskSpaceProbe { get; }

        public static ProductStartupCompositionInput Parse(object[] objects)
        {
            if (objects == null || objects.Length != 4 ||
                objects[0] is not ProductStartupHandoff handoff ||
                objects[1] is not ProductStartupProfile profile ||
                objects[2] is not IProductStartupSnapshotSource snapshots ||
                objects[3] is not IProductDiskSpaceProbe diskSpaceProbe)
            {
                throw new ArgumentException("GameApp requires the exact ProductStartup handoff contract.", nameof(objects));
            }

            return new ProductStartupCompositionInput(
                handoff,
                profile,
                snapshots,
                diskSpaceProbe);
        }
    }
}
