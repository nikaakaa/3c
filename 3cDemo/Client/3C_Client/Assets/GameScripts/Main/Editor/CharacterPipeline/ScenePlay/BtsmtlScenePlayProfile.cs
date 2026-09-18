using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    [CreateAssetMenu(menuName = "3C/ScenePlay/Profile", fileName = "BtsmtlScenePlayProfile")]
    public sealed class BtsmtlScenePlayProfile : ScriptableObject
    {
        [SerializeField] SceneAsset m_Scene;
        [SerializeField] string m_ContextId = string.Empty;
        [SerializeField] string m_DefaultActorId = string.Empty;

        public SceneAsset Scene => m_Scene;
        public string ScenePath => m_Scene ? AssetDatabase.GetAssetPath(m_Scene) : string.Empty;
        public string ContextId => m_ContextId?.Trim() ?? string.Empty;
        public string DefaultActorId => m_DefaultActorId?.Trim() ?? string.Empty;
        public bool IsValid => m_Scene && !string.IsNullOrEmpty(ScenePath) && !string.IsNullOrEmpty(ContextId);
    }
}
