using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeDesigner
{
    [Serializable]
    public sealed class InputActionBindingModule : NodeModule
    {
        [SerializeField, ShowInPanel("Asset")]
        InputActionAsset m_Asset;

        [SerializeField, ShowInPanel("Action Map"), ReadOnly]
        string m_ActionMapName;

        [SerializeField, ShowInPanel("Action"), ReadOnly]
        string m_ActionName;

        [SerializeField, ShowInPanel("Action Id"), ReadOnly]
        string m_ActionId;

        [SerializeField, ShowInPanel("Control Type"), ReadOnly]
        string m_ExpectedControlType;

        public override string DefaultModuleId => "inputAction";
        public InputActionAsset Asset => m_Asset;
        public string ActionMapName => m_ActionMapName;
        public string ActionName => m_ActionName;
        public string ActionId => m_ActionId;
        public string ExpectedControlType => m_ExpectedControlType;
        public string DisplayName => string.IsNullOrEmpty(m_ActionMapName) ? m_ActionName : $"{m_ActionMapName}/{m_ActionName}";

        public override void Init(BaseNode owner, string defaultModuleId)
        {
            base.Init(owner, defaultModuleId);
            TryResolveAction(out _, out _);
        }

        public void Bind(InputAction action)
        {
            if (action?.actionMap?.asset == null)
            {
                Clear();
                return;
            }

            m_Asset = action.actionMap.asset;
            m_ActionId = action.id.ToString();
            SyncDisplay(action);
        }

        public bool TryResolveAction(out InputAction action, out string error)
        {
            action = null;
            error = string.Empty;

            if (!m_Asset)
            {
                error = "InputAction asset is missing.";
                return false;
            }

            if (string.IsNullOrEmpty(m_ActionId) || !Guid.TryParse(m_ActionId, out Guid guid))
            {
                error = "InputAction id is missing or invalid.";
                return false;
            }

            action = m_Asset.FindAction(guid);
            if (action == null)
            {
                error = $"InputAction '{m_ActionId}' was not found in asset '{m_Asset.name}'.";
                return false;
            }

            SyncDisplay(action);
            return true;
        }

        public override IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            yield return new NodeAssetReference(Owner, $"{ModuleId}.m_Asset", "InputAction Asset", m_Asset, true);
        }

        void SyncDisplay(InputAction action)
        {
            m_ActionMapName = action.actionMap != null ? action.actionMap.name : string.Empty;
            m_ActionName = action.name;
            m_ExpectedControlType = action.expectedControlType;
        }

        void Clear()
        {
            m_Asset = null;
            m_ActionMapName = string.Empty;
            m_ActionName = string.Empty;
            m_ActionId = string.Empty;
            m_ExpectedControlType = string.Empty;
        }
    }
}
