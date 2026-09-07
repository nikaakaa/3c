using UnityEngine;
using ParadoxNotion;
using ParadoxNotion.Design;
using NodeCanvas.Framework;

namespace FlowCanvas.Nodes
{

    [DoNotList]
    ///<summary>Wraps a SimplexNode</summary>
    abstract public class SimplexNodeWrapper : FlowScriptNode { }

    ///<summary>Wraps a SimplexNode</summary>
    public class SimplexNodeWrapper<T> : SimplexNodeWrapper where T : SimplexNode
    {

        [SerializeField]
        private T _simplexNode;
        public T simplexNode {
            get
            {
                if ( _simplexNode == null ) {
                    _simplexNode = (T)System.Activator.CreateInstance(typeof(T));
                    if ( _simplexNode != null ) {
                        base.GatherPorts();
                    }
                }
                return _simplexNode;
            }
        }

        public override string name => simplexNode != null ? simplexNode.name : "NULL";

        public override string description => simplexNode != null ? simplexNode.description : "NULL";


        public override System.Type GetNodeWildDefinitionType() {
            return typeof(T).GetFirstGenericParameterConstraintType();
        }

        public override void OnCreate(Graph assignedGraph) {
            simplexNode?.SetDefaultParameters(this);
        }

        public override void OnGraphStarted() {
            simplexNode?.OnGraphStarted();
        }

        public override void OnGraphPaused() {
            simplexNode?.OnGraphPaused();
        }

        public override void OnGraphUnpaused() {
            simplexNode?.OnGraphUnpaused();
        }

        public override void OnGraphStoped() {
            simplexNode?.OnGraphStoped();
        }

        protected override void RegisterPorts() {
            simplexNode?.RegisterPorts(this);
        }

        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------

#if UNITY_EDITOR

        //Override of right click node context menu for ability to change type
        protected override UnityEditor.GenericMenu OnContextMenu(UnityEditor.GenericMenu menu) {

            base.OnContextMenu(menu);
            if ( simplexNode == null ) {
                return menu;
            }

            var type = simplexNode.GetType();
            if ( type.IsGenericType ) {
                menu = EditorUtils.GetPreferedTypesSelectionMenu(type.GetGenericTypeDefinition(), (t) => { this.ReplaceWith(typeof(SimplexNodeWrapper<>).MakeGenericType(t)); }, menu, "Change Generic Type");
            }

            return menu;
        }

        protected override void OnNodeInspectorGUI() {
            EditorUtils.ReflectedObjectInspector(simplexNode, graph);
            base.OnNodeInspectorGUI();
        }

#endif

    }
}