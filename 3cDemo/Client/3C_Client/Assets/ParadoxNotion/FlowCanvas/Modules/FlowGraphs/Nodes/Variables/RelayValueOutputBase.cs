using UnityEngine;
using ParadoxNotion.Design;
using System.Linq;

namespace FlowCanvas.Nodes
{
    ///----------------------------------------------------------------------------------------------

    [DoNotList]
    [Description("Returns the selected and previously set Internal Variable's input value.")]
    [Color("866693")]
    [ContextDefinedOutputs(typeof(Wild))]
    abstract public class RelayValueOutputBase : FlowScriptNode
    {
        abstract public void SetSource(RelayValueInputBase source);
    }

    ///----------------------------------------------------------------------------------------------

    ///<summary>Relay Output</summary>
    public class RelayValueOutput<T> : RelayValueOutputBase
    {

        [SerializeField]
        private string _sourceInputUID;
        private string sourceInputUID {
            get { return _sourceInputUID; }
            set { _sourceInputUID = value; }
        }

        private System.WeakReference<RelayValueInputBase> _sourceInputRef;
        private RelayValueInput<T> sourceInput {
            get
            {
                RelayValueInputBase reference;
                if ( _sourceInputRef == null ) {
                    reference = graph.GetAllNodesOfType<RelayValueInput<T>>().FirstOrDefault(i => i.UID == sourceInputUID);
                    _sourceInputRef = new System.WeakReference<RelayValueInputBase>(reference);
                }

                _sourceInputRef.TryGetTarget(out reference);
                return reference as RelayValueInput<T>;
            }
        }

        public override string name { get { return string.Format("{0}", sourceInput != null ? sourceInput.ToString() : "@ NONE"); } }

        public override void SetSource(RelayValueInputBase source) {
            _sourceInputUID = source?.UID;
            _sourceInputRef = new System.WeakReference<RelayValueInputBase>(source);
            GatherPorts();
        }

        protected override void RegisterPorts() {
            AddValueOutput<T>("Value", () =>
            {
                if ( sourceInput == null ) { return default(T); }
                return sourceInput.cached ? sourceInput.cachedValue : sourceInput.port.value;
            });
        }


        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        protected override void OnNodeExternalGUI() {
            if ( sourceInput != null && ( sourceInput.isSelected || this.isSelected ) ) {
                UnityEditor.Handles.color = Color.grey;
                UnityEditor.Handles.DrawAAPolyLine(rect.center, sourceInput.rect.center);
                UnityEditor.Handles.color = Color.white;
            }
        }

        protected override void OnNodeInspectorGUI() {
            var relayInputs = graph.GetAllNodesOfType<RelayValueInputBase>();
            var newInput = EditorUtils.Popup<RelayValueInputBase>("Internal Var Source", sourceInput, relayInputs);
            if ( newInput != sourceInput ) {
                if ( newInput == null ) {
                    SetSource(null);
                    return;
                }
                if ( newInput.relayType == typeof(T) ) {
                    SetSource(newInput);
                    return;
                }

                var newNode = (RelayValueOutputBase)ReplaceWith(typeof(RelayValueOutput<>).MakeGenericType(newInput.relayType));
                newNode.SetSource((RelayValueInputBase)newInput);
            }
        }

#endif
        ///----------------------------------------------------------------------------------------------
    }
}