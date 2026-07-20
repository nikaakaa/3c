using System;
using ThirdPerson.ProductStartup;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.ProductBuild
{
    internal sealed class CommercialClientBuildWindow : EditorWindow
    {
        BuildTarget m_Target;
        string m_ResourcePackageVersion = string.Empty;
        string m_MinimumClientBuildVersion = string.Empty;

        [MenuItem("Tools/3C/Build/Commercial Client")]
        static void Open()
        {
            GetWindow<CommercialClientBuildWindow>("Commercial Client Build");
        }

        void OnEnable()
        {
            m_Target = EditorUserBuildSettings.activeBuildTarget;
        }

        void OnGUI()
        {
            ProductStartupProfile profile = AssetDatabase.LoadAssetAtPath<ProductStartupProfile>(ClientBuildArtifactLayout.ProductStartupProfilePath);
            string clientVersion = profile && profile.TryGetClientBuildVersion(out ClientBuildVersion parsed) ? parsed.ToString() : "配置无效";
            m_Target = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", m_Target);
            EditorGUILayout.LabelField("ClientBuildVersion", clientVersion);
            m_ResourcePackageVersion = EditorGUILayout.TextField("ResourcePackageVersion", m_ResourcePackageVersion);
            m_MinimumClientBuildVersion = EditorGUILayout.TextField("MinimumClientBuildVersion", m_MinimumClientBuildVersion);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Content", PreviewContentPath());
            EditorGUILayout.LabelField("Player", PreviewPlayerPath(clientVersion));
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Build Content"))
                    Execute(CommercialClientBuildMode.Content);
                if (GUILayout.Button("Build Player"))
                    Execute(CommercialClientBuildMode.Player);
                if (GUILayout.Button("Build Content + Player"))
                    Execute(CommercialClientBuildMode.ContentAndPlayer);
            }
        }

        void Execute(CommercialClientBuildMode mode)
        {
            try
            {
                var request = new CommercialClientBuildRequest(m_Target, m_ResourcePackageVersion, m_MinimumClientBuildVersion, mode);
                CommercialClientBuildResult result = CommercialClientBuildWorkflow.Build(request);
                string message = $"Content: {result.ContentPath}\nPlayer: {result.PlayerPath}";
                Debug.Log($"Commercial client build completed. {message}");
                EditorUtility.DisplayDialog("Commercial Client Build", message, "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Commercial Client Build Failed", exception.Message, "OK");
            }
        }

        string PreviewContentPath()
        {
            try
            {
                return ClientBuildArtifactLayout.GetContentVersionRoot(m_Target, m_ResourcePackageVersion);
            }
            catch
            {
                return "等待填写 ResourcePackageVersion";
            }
        }

        string PreviewPlayerPath(string clientVersion)
        {
            try
            {
                return ClientBuildArtifactLayout.GetPlayerVersionRoot(m_Target, clientVersion);
            }
            catch
            {
                return "等待有效的 ProductStartupProfile.ClientBuildVersion";
            }
        }
    }
}
