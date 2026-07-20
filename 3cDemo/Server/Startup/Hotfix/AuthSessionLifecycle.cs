using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;

namespace Fantasy;

public sealed class AuthGatewaySceneCreated : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        Scene scene = self.Scene;
        if (scene.SceneType == SceneType.AuthGateway)
        {
            scene.AddComponent<AuthSessionRegistryComponent>();
            Log.Info($"AuthGateway Registry created in Scene '{scene.Id}'.");
        }

        await FTask.CompletedTask;
    }
}

public sealed class AuthSessionRegistryComponentDestroySystem : DestroySystem<AuthSessionRegistryComponent>
{
    protected override void Destroy(AuthSessionRegistryComponent self)
    {
        self.CurrentByAccount.Clear();
        self.LastGenerationByAccount.Clear();
    }
}

public sealed class AuthenticatedGuestComponentDestroySystem : DestroySystem<AuthenticatedGuestComponent>
{
    protected override void Destroy(AuthenticatedGuestComponent self)
    {
        AuthSessionRegistryComponent? registry = self.Registry;
        string accountId = self.AccountId;
        ulong generation = self.SessionGeneration;
        long sessionRuntimeId = self.SessionRuntimeId;
        self.Registry = null;
        self.AccountId = string.Empty;
        self.SessionGeneration = 0;
        self.SessionRuntimeId = 0;
        if (registry is { IsDisposed: false })
        {
            AuthSessionRegistryRuntime.TryRemoveCurrent(registry, accountId, generation, sessionRuntimeId);
        }
    }
}
