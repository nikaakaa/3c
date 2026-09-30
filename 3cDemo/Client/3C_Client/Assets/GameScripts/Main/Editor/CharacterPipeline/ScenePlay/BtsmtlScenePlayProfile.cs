using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    [CreateAssetMenu(menuName = "3C/ScenePlay/Profile", fileName = "BtsmtlScenePlayProfile")]
    public sealed class BtsmtlScenePlayProfile : ScriptableObject
    {
        [SerializeField] GameObject m_AssemblyPrefab;
        [SerializeField] string m_ContextId = string.Empty;
        [SerializeField] string m_DefaultActorId = string.Empty;

        public GameObject AssemblyPrefab => m_AssemblyPrefab;
        public string AssemblyPath => m_AssemblyPrefab ? AssetDatabase.GetAssetPath(m_AssemblyPrefab) : string.Empty;
        public string ContextId => m_ContextId?.Trim() ?? string.Empty;
        public string DefaultActorId => m_DefaultActorId?.Trim() ?? string.Empty;
        public bool IsValid => m_AssemblyPrefab && PrefabUtility.IsPartOfPrefabAsset(m_AssemblyPrefab) &&
                               !string.IsNullOrEmpty(AssemblyPath) &&
                               !string.IsNullOrEmpty(ContextId) &&
                               !string.IsNullOrEmpty(DefaultActorId);
    }
}
