using UnityEngine;
using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{

    [Name("Set Internal Var")]
    [Description("Can be used to set an internal variable, to later be retrieved with a 'Get Internal Var' node. If you do not call 'Set', then the input value will work as a relay instead.")]
    [Category("Variables/Internal")]
    [Color("866693")]
    [ContextDefinedInputs(typeof(Wild))]
    [ExposeAsDefinition]
    abstract public class RelayValueInputBase : FlowScriptNode
    {
        abstract public System.Type relayType { get; }
    }

    ///<summary>Relay Input</summary>
    public class RelayValueInput<T> : RelayValueInputBase, IEditorMenuCallbackReceiver
    {

        [DelayedField, Tooltip("The identifier name of the internal var")]
        public string identifier = "MyInternalVarName";

        public ValueInput<T> port { get; private set; }
        public bool cached { get; private set; }
        public T cachedValue { get; private set; }

        public override System.Type relayType => typeof(T);
        public override string name => string.Format("@ {0}", identifier);

        protected override void RegisterPorts() {
            var fOut = AddFlowOutput(" ");
            AddFlowInput("Set", (f) => { cached = true; cachedValue = port.value; fOut.Call(f); });
            port = AddValueInput<T>("Value");
        }

        ///----------------------------------------------------------------------------------------------
#if UNITY_EDITOR
        void IEditorMenuCallbackReceiver.OnMenu(UnityEditor.GenericMenu menu, Vector2 pos, Port contextPort, object dropInstance) {
            if ( contextPort == null || contextPort.type.IsAssignableFrom(this.relayType) ) {
                menu.AddItem(new GUIContent(string.Format("Variables/Internal/Get '{0}'", identifier)), false, () => { flowGraph.AddFlowNode<RelayValueOutput<T>>(pos, contextPort, dropInstance).SetSource(this); });
            }
        }
#endif
        ///----------------------------------------------------------------------------------------------
    }
}