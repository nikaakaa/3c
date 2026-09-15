using System;
using System.Threading;
using System.Threading.Tasks;
using TEngine;
using ThirdPerson.ProductStartup;
using YooAsset;
using UnityEngine;

namespace ThirdPersonGameplay.ScenePlay
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class BtsmtlScenePlayResourceRuntimeOwner : MonoBehaviour, IBtsmtlScenePlayRuntimeOwner
    {
        const string OwnerIdentity = "btsmtl-scene-play-resources";

        [SerializeField] ProductStartupProfile m_Profile;
        [SerializeField] GameObject m_RuntimeRoot;

        readonly CancellationTokenSource m_Cancellation = new CancellationTokenSource();
        IResourceModule m_ResourceModule;
        BtsmtlScenePlayRuntimeOwnerStatus m_Status = new BtsmtlScenePlayRuntimeOwnerStatus(
            OwnerIdentity,
            BtsmtlScenePlayRuntimeOwnerState.Unavailable);
        bool m_Released;

        public BtsmtlScenePlayRuntimeOwnerStatus Status => m_Status;

#if UNITY_EDITOR
        public void SetAuthoring(ProductStartupProfile profile, GameObject runtimeRoot)
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Scene Play resource authoring cannot change during Play Mode.");
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_RuntimeRoot = runtimeRoot ? runtimeRoot : throw new ArgumentNullException(nameof(runtimeRoot));
        }
#endif

        void Awake()
        {
            if (!Application.isPlaying)
                return;
            m_Status = new BtsmtlScenePlayRuntimeOwnerStatus(
                OwnerIdentity,
                BtsmtlScenePlayRuntimeOwnerState.Preparing);
            if (!m_Profile)
            {
                Fail("resource_profile_missing", "BTSMTL Scene Play resource startup requires a ProductStartupProfile.");
                return;
            }
            if (!m_RuntimeRoot)
            {
                Fail("runtime_root_missing", "BTSMTL Scene Play resource startup requires a Character runtime root.");
                return;
            }
            try
            {
                m_ResourceModule = ModuleSystem.GetModule<IResourceModule>();
                m_ResourceModule.Initialize();
            }
            catch (Exception exception)
            {
                Fail("resource_system_initialization_failed", exception.Message);
            }
        }

        async void Start()
        {
            if (!Application.isPlaying || m_Status.State != BtsmtlScenePlayRuntimeOwnerState.Preparing)
                return;
            try
            {
                await ProjectSceneResourcePreparation.PrepareAsync(
                    m_ResourceModule,
                    m_Profile,
                    m_Cancellation.Token);
                m_Cancellation.Token.ThrowIfCancellationRequested();
                m_Status = new BtsmtlScenePlayRuntimeOwnerStatus(
                    OwnerIdentity,
                    BtsmtlScenePlayRuntimeOwnerState.Ready);
                m_RuntimeRoot.SetActive(true);
            }
            catch (OperationCanceledException) when (m_Cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Fail("resource_preparation_failed", exception.Message);
            }
        }

        public BtsmtlScenePlayRuntimeOwnerReleaseResult Release()
        {
            if (m_Released)
                return new BtsmtlScenePlayRuntimeOwnerReleaseResult(true);
            m_Released = true;
            m_Cancellation.Cancel();
            m_Status = new BtsmtlScenePlayRuntimeOwnerStatus(
                OwnerIdentity,
                BtsmtlScenePlayRuntimeOwnerState.Stopped);
            return new BtsmtlScenePlayRuntimeOwnerReleaseResult(true);
        }

        void OnDestroy()
        {
            if (!m_Released)
                m_Cancellation.Cancel();
            m_Cancellation.Dispose();
        }

        void Fail(string code, string message)
        {
            m_Status = new BtsmtlScenePlayRuntimeOwnerStatus(
                OwnerIdentity,
                BtsmtlScenePlayRuntimeOwnerState.Failed,
                code,
                message);
            Debug.LogError(message, this);
        }
    }
}
