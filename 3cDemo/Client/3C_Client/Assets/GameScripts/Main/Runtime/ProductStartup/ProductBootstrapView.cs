using System;
using UnityEngine;

namespace ThirdPerson.ProductStartup
{
    [DisallowMultipleComponent]
    public sealed class ProductBootstrapView : MonoBehaviour
    {
        const string PresentationResourcePath = "ProductStartup/ProductBootstrapPresentation";

        [Serializable]
        sealed class Presentation
        {
            public string title;
            public string transportStatus;
            public string errorPrefix;
            public string retryLabel;
            public string exitLabel;
            public string downloadConsent;
            public string downloadLabel;
        }

        IProductStartupSnapshotSource m_Source;
        IProductStartupCommands m_Commands;
        ProductStartupSnapshot m_Snapshot;
        Vector2 m_Scroll;
        Presentation m_Presentation;

        void Awake()
        {
            TextAsset presentationAsset = Resources.Load<TextAsset>(PresentationResourcePath);
            if (!presentationAsset)
            {
                throw new InvalidOperationException($"Missing built-in bootstrap presentation: {PresentationResourcePath}.");
            }
            m_Presentation = JsonUtility.FromJson<Presentation>(presentationAsset.text);
            if (m_Presentation == null ||
                string.IsNullOrWhiteSpace(m_Presentation.title) ||
                string.IsNullOrWhiteSpace(m_Presentation.transportStatus) ||
                string.IsNullOrWhiteSpace(m_Presentation.errorPrefix) ||
                string.IsNullOrWhiteSpace(m_Presentation.retryLabel) ||
                string.IsNullOrWhiteSpace(m_Presentation.exitLabel) ||
                string.IsNullOrWhiteSpace(m_Presentation.downloadConsent) ||
                string.IsNullOrWhiteSpace(m_Presentation.downloadLabel))
            {
                throw new InvalidOperationException("Built-in bootstrap presentation is incomplete.");
            }
        }

        public void Bind(IProductStartupSnapshotSource source, IProductStartupCommands commands)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (m_Source != null) throw new InvalidOperationException("Bootstrap view is already bound.");

            m_Source = source;
            m_Commands = commands;
            m_Snapshot = source.Current;
            m_Source.SnapshotChanged += OnSnapshotChanged;
        }

        void OnDestroy()
        {
            if (m_Source != null)
            {
                m_Source.SnapshotChanged -= OnSnapshotChanged;
            }
        }

        void OnSnapshotChanged(ProductStartupSnapshot snapshot)
        {
            m_Snapshot = snapshot;
        }

        void OnGUI()
        {
            if (m_Snapshot == null)
            {
                return;
            }

            var width = Mathf.Min(760f, Screen.width - 48f);
            var height = Mathf.Min(720f, Screen.height - 48f);
            GUILayout.BeginArea(new Rect(24f, 24f, width, height), GUI.skin.box);
            m_Scroll = GUILayout.BeginScrollView(m_Scroll);
            GUILayout.Label(m_Presentation.title);
            GUILayout.Space(8f);
            GUILayout.Label(StageTimeline(m_Snapshot.Stage));
            GUILayout.Label($"Stage: {m_Snapshot.Stage}   Generation: {m_Snapshot.Generation}   Elapsed: {FormatDuration(m_Snapshot.StageElapsed)}");
            GUILayout.Label($"Client: {ValueOrDash(m_Snapshot.ClientBuildVersion)}   Minimum: {ValueOrDash(m_Snapshot.MinimumClientBuildVersion)}");
            GUILayout.Label($"Resource: {ValueOrDash(m_Snapshot.ResourcePackageVersion)}   Protocol: {ValueOrDash(m_Snapshot.AuthProtocolVersion)}");
            GUILayout.Label($"Endpoint: {ValueOrDash(m_Snapshot.ResourceEndpointHost)}   Tag: {ValueOrDash(m_Snapshot.ResourceTag)}");
            GUILayout.Space(8f);
            GUILayout.Label($"Cache verification: CRC High   Progress: {m_Snapshot.CacheVerificationProgress:P0}");
            GUILayout.Label($"Cache valid: {FormatCount(m_Snapshot.ValidCacheFileCount)}   invalid: {FormatCount(m_Snapshot.InvalidCacheFileCount)}");
            GUILayout.Label(m_Presentation.transportStatus);
            GUILayout.Space(8f);
            GUILayout.Label($"Files: {m_Snapshot.CompletedFileCount}/{m_Snapshot.TotalFileCount}");
            GUILayout.Label($"Bytes: {FormatBytes(m_Snapshot.CompletedBytes)} / {FormatBytes(m_Snapshot.TotalBytes)}");
            GUILayout.HorizontalSlider(
                m_Snapshot.TotalBytes > 0 ? (float)m_Snapshot.CompletedBytes / m_Snapshot.TotalBytes : 0f,
                0f,
                1f);
            GUILayout.Label($"Speed: {FormatBytes((long)m_Snapshot.BytesPerSecond)}/s   ETA: {FormatDuration(m_Snapshot.EstimatedRemaining)}");
            GUILayout.Label($"Current file: {ValueOrDash(m_Snapshot.CurrentFile)}   Retries: {m_Snapshot.RetryCount}");

            if (m_Snapshot.RequiredDiskBytes > 0)
            {
                GUILayout.Label($"Disk required: {FormatBytes(m_Snapshot.RequiredDiskBytes)}   available: {FormatBytes(m_Snapshot.AvailableDiskBytes)}");
            }

            if (m_Snapshot.HasError)
            {
                GUILayout.Space(10f);
                GUILayout.Label($"{m_Presentation.errorPrefix} {m_Snapshot.ErrorCode}: {m_Snapshot.SafeError}");
                GUILayout.BeginHorizontal();
                if (m_Snapshot.Retryable && GUILayout.Button(m_Presentation.retryLabel, GUILayout.Width(120f)))
                {
                    m_Commands.Retry();
                }

                if (GUILayout.Button(m_Presentation.exitLabel, GUILayout.Width(120f)))
                {
                    m_Commands.Exit();
                }
                GUILayout.EndHorizontal();
            }
            else if (m_Snapshot.WaitingForConsent)
            {
                GUILayout.Space(10f);
                GUILayout.Label(m_Presentation.downloadConsent);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(m_Presentation.downloadLabel, GUILayout.Width(120f)))
                {
                    m_Commands.ConfirmCoreDownload();
                }

                if (GUILayout.Button(m_Presentation.exitLabel, GUILayout.Width(120f)))
                {
                    m_Commands.Exit();
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static string StageTimeline(ProductStartupStage current)
        {
            return $"Launch > Policy > Verify > Version > Manifest > Core Plan > Consent > Download > Cleanup > HotFix > Runtime   [{current}]";
        }

        static string ValueOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        static string FormatCount(int value)
        {
            return value < 0 ? "unavailable" : value.ToString();
        }

        static string FormatDuration(TimeSpan value)
        {
            return value.TotalHours >= 1d
                ? value.ToString(@"hh\:mm\:ss")
                : value.ToString(@"mm\:ss");
        }

        static string FormatBytes(long bytes)
        {
            if (bytes < 1024L) return $"{bytes} B";
            if (bytes < 1048576L) return $"{bytes / 1024d:F1} KiB";
            if (bytes < 1073741824L) return $"{bytes / 1048576d:F1} MiB";
            return $"{bytes / 1073741824d:F2} GiB";
        }
    }
}
