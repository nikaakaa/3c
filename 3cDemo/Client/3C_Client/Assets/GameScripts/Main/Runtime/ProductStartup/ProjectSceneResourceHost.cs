using System;
using System.Threading;
using TEngine;
using UnityEngine;

namespace ThirdPerson.ProductStartup
{
    [DisallowMultipleComponent]
    public sealed class ProjectSceneResourceHost : MonoBehaviour
    {
        [SerializeField] ProductStartupProfile m_Profile;
        [SerializeField] GameObject m_GameplayRoot;
        readonly CancellationTokenSource m_Cancellation = new CancellationTokenSource();

        async void Start()
        {
            try
            {
                if (!m_Profile || !m_GameplayRoot || m_GameplayRoot.activeSelf ||
                    transform.IsChildOf(m_GameplayRoot.transform))
                    throw new InvalidOperationException("Scene resource startup requires a profile and an inactive gameplay root outside its own hierarchy.");
                CancellationToken token = m_Cancellation.Token;
                await ProjectSceneResourcePreparation.PrepareAsync(
                    ModuleSystem.GetModule<IResourceModule>(), m_Profile, token);
                token.ThrowIfCancellationRequested();
                m_GameplayRoot.SetActive(true);
            }
            catch (OperationCanceledException) when (m_Cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        void OnDestroy()
        {
            m_Cancellation.Cancel();
            m_Cancellation.Dispose();
        }
    }
}
