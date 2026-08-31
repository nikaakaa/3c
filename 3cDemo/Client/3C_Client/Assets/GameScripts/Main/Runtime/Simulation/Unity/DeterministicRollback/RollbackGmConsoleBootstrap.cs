using System;
using System.IO;
using ThirdPerson.Development.Gm;
using ThirdPersonCharacter.Pipeline.Input;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [DisallowMultipleComponent]
    public sealed class RollbackGmConsoleBootstrap : MonoBehaviour
    {
        [SerializeField] GmDevelopmentProfile m_Profile;
        [SerializeField] DeterministicRollbackCharacterHost[] m_Actors;

        public GmDevelopmentProfile Profile => m_Profile;

#if UNITY_EDITOR
        public void Configure(GmDevelopmentProfile profile, params DeterministicRollbackCharacterHost[] actors)
        {
            m_Profile = profile;
            m_Actors = actors;
        }
#endif

        void Start()
        {
            if (Application.isEditor)
                return;
            if (!Debug.isDebugBuild)
                throw new InvalidOperationException("Rollback GM 只允许正式 Development 测试产品装配。");
            if (!m_Profile || m_Actors == null || m_Actors.Length == 0)
                throw new InvalidOperationException("Rollback GM 缺少正式 Profile 或本场角色绑定。");
            m_Profile.RequireValid();
            CharacterDeviceInputFocus focus = null;
            foreach (DeterministicRollbackCharacterHost actor in m_Actors)
            {
                if (actor.DeviceInputFocus == null)
                    continue;
                if (focus != null)
                    throw new InvalidOperationException("Rollback GM 只能绑定一个本地玩家设备输入。");
                focus = actor.DeviceInputFocus;
            }
            if (focus == null)
                throw new InvalidOperationException("Rollback GM 没有本地玩家设备输入。");
            string path = Path.Combine(Application.streamingAssetsPath, GmHttpProtocol.ClientManifestFileName);
            GmClientManifest manifest = JsonUtility.FromJson<GmClientManifest>(File.ReadAllText(path, System.Text.Encoding.UTF8));
            manifest.RequireValid();
            var options = new GmConsoleOptions(manifest.historyCapacity, manifest.outputCapacity,
                manifest.maximumOutputCharacters, manifest.maximumPendingRequests, manifest.requestTimeoutMilliseconds / 1000d);
            var model = new GmConsoleModel(new UnityGmHttpConnection(manifest), options);
            GmConsoleView view = gameObject.AddComponent<GmConsoleView>();
            try { view.Initialize(model, focus, m_Profile); }
            catch
            {
                model.Dispose();
                Destroy(view);
                throw;
            }
        }
    }
}
