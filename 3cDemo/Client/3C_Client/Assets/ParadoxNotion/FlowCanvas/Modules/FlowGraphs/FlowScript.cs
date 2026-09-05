using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ParadoxNotion;
using NodeCanvas.Framework;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using ParadoxNotion.Design;
using Logger = ParadoxNotion.Services.Logger;

namespace FlowCanvas
{

    ///<summary>FlowScripts are assigned or bound to FlowScriptControllers</summary>
    [UnityEngine.CreateAssetMenu(menuName = "ParadoxNotion/FlowCanvas/FlowScript Asset")]
    public class FlowScript : FlowGraph
    {

        private List<IUpdatable> updatableNodes;
        private List<MacroNodeWrapper> macroWrappers;
        private Dictionary<string, IInvokable> functions;

        ///----------------------------------------------------------------------------------------------
        public override System.Type baseNodeType => typeof(FlowScriptNode);
        public override bool allowBlackboardOverrides => true;
        public override bool requiresAgent => false;
        public override bool requiresPrimeNode => false;
        public override bool isTree => false;
        public override bool canAcceptVariableDrops => true;
        public override PlanarDirection flowDirection => PlanarDirection.Horizontal;
        ///----------------------------------------------------------------------------------------------

        ///<summary>Calls and returns a value of a custom function in the flowgraph</summary>
        public T CallFunction<T>(string name, params object[] args) {
            return (T)CallFunction(name, args);
        }

        ///<summary>Calls and returns a value of a custom function in the flowgraph</summary>
        public object CallFunction(string name, params object[] args) {
            Debug.Assert(isRunning, "Trying to Execute Function but graph is not running");
            if ( functions.TryGetValue(name, out IInvokable func) ) {
                return func.Invoke(args);
            }
            return null;
        }

        ///<summary>Calls a custom function in the flowgraph async. When the function is done, it will callback with return value</summary>
        public void CallFunctionAsync(string name, System.Action<object> callback, params object[] args) {
            Debug.Assert(isRunning, "Trying to Execute Function but graph is not running");
            if ( functions.TryGetValue(name, out IInvokable func) ) {
                func.InvokeAsync(callback, args);
            }
        }

        //...
        protected override void OnGraphInitialize() {
            updatableNodes = new List<IUpdatable>();
            macroWrappers = new List<MacroNodeWrapper>();
            functions = new Dictionary<string, IInvokable>(System.StringComparer.Ordinal);

            for ( var i = 0; i < allNodes.Count; i++ ) {
                var node = allNodes[i];
                if ( node is MacroNodeWrapper macroWrapper ) {
                    if ( macroWrapper.macro != null ) {
                        macroWrappers.Add(macroWrapper);
                        ThreadSafeInitCall(macroWrapper.MakeInstance);
                    }
                }

                if ( node is IUpdatable updatable ) {
                    updatableNodes.Add(updatable);
                }

                if ( node is IInvokable func ) {
                    functions[func.GetInvocationID()] = func;
                }
            }

            //2nd pass after macros have been instanced
            ThreadSafeInitCall(InitSecondPass);
        }

        void InitSecondPass() {
            for ( var i = 0; i < allNodes.Count; i++ ) {
                if ( allNodes[i] is FlowNode flowNode ) {
                    flowNode.BindPorts();
                    flowNode.AssignSelfInstancePort();
                }
            }
        }

        //...
        protected override void OnGraphStarted() {
            for ( var i = 0; i < macroWrappers.Count; i++ ) {
                var macroWrapper = macroWrappers[i];
                macroWrapper.macro?.StartGraph(agent, blackboard.parent, Graph.UpdateMode.Manual, null);
            }
        }

        //Update IUpdatable nodes. Basicaly for events like Input, Update etc
        //This is the only thing that updates per-frame
        protected override void OnGraphUpdate() {
            if ( updatableNodes != null && updatableNodes.Count > 0 ) {
                for ( var i = 0; i < updatableNodes.Count; i++ ) {
                    updatableNodes[i].Update();
                }
            }
        }

        //...
        protected override void OnGraphStoped() {
            for ( var i = 0; i < allNodes.Count; i++ ) {
                var node = allNodes[i];
                if ( node is MacroNodeWrapper macroWrapper ) {
                    macroWrapper.macro?.Stop();
                }
            }
        }



        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        //Override typical flowgraph node menu
        public override UnityEditor.GenericMenu GetNodesMenu(Vector2 mousePos, Port context, Object dropInstance) {
            return FlowScriptExtensions.GetNodesMenu(this, mousePos, context, dropInstance);
        }

        //Append ConvertToMacro feature
        protected override UnityEditor.GenericMenu OnNodesContextMenu(UnityEditor.GenericMenu menu, Node[] nodes) {
            menu.AddItem(new GUIContent("Convert To Macro"), false, () => { FlowScriptExtensions.ConvertNodesToMacro(nodes.ToList()); });
            return menu;
        }

        //Unity object droped in graph
        protected override void OnObjectDropInGraph(Object obj, Vector2 mousePos) {

            if ( obj == null ) {
                return;
            }

            if ( UnityEditor.EditorUtility.IsPersistent(this) && !UnityEditor.EditorUtility.IsPersistent(obj) ) {
                Logger.Log("This Graph is an asset. The dragged object is a scene reference. The reference will not persist!", LogTag.EDITOR);
            }

            var targetType = obj.GetType();
            var menu = new UnityEditor.GenericMenu();
            menu = AppendDragAndDropObjectMenu(menu, obj, "", mousePos);
            menu.AddSeparator("/");
            menu = this.AppendTypeReflectionNodesMenu(menu, targetType, "", mousePos, null, obj);
            if ( obj is GameObject ) {
                foreach ( var component in ( obj as GameObject ).GetComponents<Component>().Where(c => c.hideFlags == 0) ) {
                    var cType = component.GetType();
                    menu = AppendDragAndDropObjectMenu(menu, component, cType.Name + "/", mousePos);
                    menu = this.AppendTypeReflectionNodesMenu(menu, cType, "", mousePos, null, component);
                }
            }

            menu.ShowAsBrowser("Add Node For Drag & Drop Instance");
            Event.current.Use();
        }

        //Used above for convenience
        UnityEditor.GenericMenu AppendDragAndDropObjectMenu(UnityEditor.GenericMenu menu, UnityEngine.Object o, string category, Vector2 mousePos) {
            foreach ( var _wrapperType in NodeCanvas.Editor.GraphEditorUtility.GetDropedReferenceNodeTypes<IDropedReferenceNode>(o) ) {
                var wrapperType = _wrapperType;
                if ( baseNodeType.IsAssignableFrom(wrapperType) ) {
                    menu.AddItem(new GUIContent(string.Format(category + "Add Node ({0})", wrapperType.FriendlyName())), false, (x) =>
                    {
                        ( AddFlowNode(wrapperType, mousePos, null, x) as IDropedReferenceNode ).SetTarget((UnityEngine.Object)x);
                    }, o);
                }
            }

            if ( o is IExternalImplementedNode ) {
                menu.AddItem(new GUIContent(category + "Add Implemented Node"), false, (x) =>
                {
                    AddFlowNode<ExternalImplementedNodeWrapper>(mousePos, null, x).SetTarget((IExternalImplementedNode)x);
                }, o);
            }

            var targetType = o.GetType();
            menu.AddItem(new GUIContent(string.Format(category + "Make Variable ({0})", targetType.FriendlyName())), false, (x) => { this.AddVariableGet(targetType, null, null, mousePos, null, x); }, o);
            return menu;
        }

        ///<summary>Show Get/Set variable menu</summary>
        protected override void OnVariableDropInGraph(IBlackboard bb, Variable variable, Vector2 mousePos) {
            if ( variable != null ) {
                var menu = new UnityEditor.GenericMenu();
                menu.AddItem(new GUIContent("Get " + variable.name), false, () => { this.AddVariableGet(variable.varType, bb, variable, mousePos, null, null); });
                menu.AddItem(new GUIContent("Set " + variable.name), false, () => { this.AddVariableSet(variable.varType, bb, variable, mousePos, null, null); });
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        [UnityEditor.MenuItem("Tools/ParadoxNotion/FlowCanvas/Create/FlowScript Asset", false, 1)]
        public static void CreateFlowScript() {
            var fs = ParadoxNotion.Design.EditorUtils.CreateAsset<FlowScript>();
            UnityEditor.Selection.activeObject = fs;
        }

#endif
        ///----------------------------------------------------------------------------------------------

    }
}
