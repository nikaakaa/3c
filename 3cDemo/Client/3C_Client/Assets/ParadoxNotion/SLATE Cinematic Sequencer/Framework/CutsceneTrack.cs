using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Slate
{

    ///<summary>Tracks are contained within CutsceneGroups and contain ActionsClips within</summary>
    abstract public class CutsceneTrack : MonoBehaviour, IDirectable
    {

        [SerializeField]
        private string _name;
        [SerializeField]
        private Color _color = Color.white;
        [SerializeField, HideInInspector]
        private bool _active = true;
        [SerializeField, HideInInspector]
        private bool _isLocked = false;
        [SerializeField, HideInInspector]
        private List<ActionClip> _actionClips = new List<ActionClip>();

        ///<summary>The actor to be used in the track taken from it's parent group</summary>
        public GameObject actor {
            get { return parent != null ? parent.actor : null; }
        }

        ///<summary>The name...</summary>
        new public string name {
            get { return string.IsNullOrEmpty(_name) ? GetType().Name.SplitCamelCase() : _name; }
            set
            {
                if ( _name != value ) {
                    _name = value;
                    base.name = value;
                }
            }
        }

        ///<summary>Coloring of clips within this track</summary>
        public Color color {
            get { return _color.a > 0.1f ? _color : Color.white; }
        }

        ///<summary>All action clips of this track</summary>
        public List<ActionClip> clips {
            get { return _actionClips; }
            set { _actionClips = value; }
        }

        ///<summary>Display info</summary>
        virtual public string info {
            get { return string.Empty; }
        }

        ///<summary>Children</summary>
        IEnumerable<IDirectable> IDirectable.children {
            get { return clips.Cast<IDirectable>(); }
        }

        ///<summary>Type-based order of track</summary>
        public int layerOrder { get; private set; }

        ///<summary>Root director</summary>
        public IDirector root { get { return parent != null ? parent.root : null; } }
        ///<summary>Parent directable</summary>
        public IDirectable parent { get; private set; }

        ///<summary>Editor is collapsed?</summary>
        virtual public bool isCollapsed {
            get { return parent != null ? parent.isCollapsed : false; }
        }

        ///<summary>Is active and used?</summary>
        virtual public bool isActive {
            get { return parent != null ? parent.isActive && _active : false; }
            set
            {
                if ( _active != value ) {
                    _active = value;
                    if ( root != null ) {
                        root.Validate();
                    }
                }
            }
        }

        ///<summary>Editor is locked?</summary>
        virtual public bool isLocked {
            get { return parent != null ? parent.isLocked || _isLocked : false; }
            set { _isLocked = value; }
        }

        ///<summary>Start time, usually parent.startTime</summary>
        virtual public float startTime {
            get { return parent != null ? parent.startTime : 0; }
            set { }
        }

        ///<summary>Start time, usually parent.endTime</summary>
        virtual public float endTime {
            get { return parent != null ? parent.endTime : 0; }
            set { }
        }

        ///<summary>Blend in</summary>
        virtual public float blendIn {
            get { return 0f; }
            set { }
        }

        ///<summary>Blend out</summary>
        virtual public float blendOut {
            get { return 0f; }
            set { }
        }

        ///<summary>Able to cross-blend?</summary>
        public bool canCrossBlend {
            get { return false; }
        }

        //when the cutscene init (once, awake)
        bool IDirectable.Initialize() { return OnInitialize(); }
        //when the cutscene starts
        void IDirectable.Enter() { OnEnter(); }
        //when the cutscene is updated
        void IDirectable.Update(float time, float previousTime) { OnUpdate(time, previousTime); }
        //when the cutscene stops
        void IDirectable.Exit() { OnExit(); }
        //when the cutscene enters backwards
        void IDirectable.ReverseEnter() { OnReverseEnter(); }
        //when the cutscene is reversed/rewinded
        void IDirectable.Reverse() { OnReverse(); }

        //when root is enabled/started
        void IDirectable.RootEnabled() { OnRootEnabled(); }
        //when root is disabled/finished
        void IDirectable.RootDisabled() { OnRootDisabled(); }
        //when root is updated
        void IDirectable.RootUpdated(float time, float previousTime) { OnRootUpdated(time, previousTime); }
        //when root is destroyed
        void IDirectable.RootDestroyed() { OnRootDestroyed(); }

#if UNITY_EDITOR
        //Gizmos selected
        void IDirectable.DrawGizmos(bool selected) { if ( selected ) OnDrawGizmosSelected(); }
        //Scene GUI stuff
        void IDirectable.SceneGUI(bool selected) { OnSceneGUI(); }
#endif

        ///<summary>After creation</summary>
        public void PostCreate(IDirectable parent) {
            this.parent = parent;
            OnCreate();
        }

        ///<summary>Validate the track and it's clips</summary>
        public void Validate(IDirector root, IDirectable parent) {
            this.parent = parent;
            clips = GetComponents<ActionClip>().OrderBy(a => a.startTime).ToList();
            layerOrder = parent.children.Where(t => t.GetType() == this.GetType() && t.isActive).Reverse().ToList().IndexOf(this); // O.o
            OnAfterValidate();
        }

        ///----------------------------------------------------------------------------------------------
        virtual protected void OnCreate() { }
        virtual protected void OnAfterValidate() { }
        virtual protected bool OnInitialize() { return true; }
        virtual protected void OnEnter() { }
        virtual protected void OnUpdate(float time, float previousTime) { }
        virtual protected void OnExit() { }
        virtual protected void OnReverseEnter() { }
        virtual protected void OnReverse() { }
        virtual protected void OnDrawGizmosSelected() { }
        virtual protected void OnSceneGUI() { }
        virtual protected void OnRootEnabled() { }
        virtual protected void OnRootDisabled() { }
        virtual protected void OnRootUpdated(float time, float previousTime) { }
        virtual protected void OnRootDestroyed() { }
        ///----------------------------------------------------------------------------------------------

        ///----------------------------------------------------------------------------------------------
        public float GetTrackWeight() { return this.GetWeight(root.currentTime - this.startTime, this.blendIn, this.blendOut); }
        public float GetTrackWeight(float time) { return this.GetWeight(time, this.blendIn, this.blendOut); }
        public float GetTrackWeight(float time, float blendInOut) { return this.GetWeight(time, blendInOut, blendInOut); }
        public float GetTrackWeight(float time, float blendIn, float blendOut) { return this.GetWeight(time, blendIn, blendOut); }
        ///----------------------------------------------------------------------------------------------

        ///----------------------------------------------------------------------------------------------
#if !UNITY_EDITOR //runtime add/delete action
        ///<summary>Add an ActionClip to this Track</summary>
        public T AddAction<T>(float time) where T : ActionClip { return (T)AddAction(typeof(T), time); }
        public ActionClip AddAction(System.Type type, float time) {
            var newAction = gameObject.AddComponent(type) as ActionClip;
            newAction.startTime = time;
            clips.Add(newAction);
            newAction.PostCreate(this);

            var nextAction = clips.FirstOrDefault(a => a.startTime > newAction.startTime);
            if ( nextAction != null ) { newAction.endTime = Mathf.Min(newAction.endTime, nextAction.startTime); }

            root.Validate();
            return newAction;
        }

        ///<summary>Remove an ActionClip from this Track</summary>
        public void DeleteAction(ActionClip action) {
            clips.Remove(action);
            DestroyImmediate(action);
            root.Validate();
        }
#endif
        ///----------------------------------------------------------------------------------------------


        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        private const float PARAMS_TOP_MARGIN = 5f;
        private const float PARAMS_LINE_HEIGHT = 18f;
        private const float PARAMS_LINE_MARGIN = 2f;
        private const float BOX_WIDTH = 30f;

        [SerializeField, HideInInspector]
        private float _customHeight = 300f;

        private bool isResizingHeight = false;
        private float proposedHeight = 0f;
        private int inspectedParameterIndex = -1;
        private object _icon;
        private bool _showAnimationCurves;
        private ActionClip _showCurvesClip;

        [UnityEditor.InitializeOnLoadMethod]
        static void Editor_Init() {
            CutsceneUtility.onSelectionChange += OnDirectableSelectionChange;
        }

        //basically store last clip selected so that we can show multiple clip curves from different tracks at the same time
        static void OnDirectableSelectionChange(IDirectable directable) {
            if ( directable is IKeyable && directable.parent is CutsceneTrack ) {
                ( directable.parent as CutsceneTrack ).showCurvesClip = (IKeyable)directable;
            }
        }

        ///<summary>The expansion height</summary>
        private float customHeight {
            get { return _customHeight; }
            set { _customHeight = Mathf.Clamp(value, proposedHeight, 500); }
        }

        ///<summary>The final shown height</summary>
        public float finalHeight {
            get
            {
                if ( showCurves ) {
                    return inspectedParameterIndex == -1 ? Mathf.Max(proposedHeight, defaultHeight + 50) : Mathf.Max(proposedHeight, customHeight);
                }
                return defaultHeight;
            }
        }

        //are curves shown?
        virtual public bool showCurves {
            get { return _showAnimationCurves; }
            set { _showAnimationCurves = value; }
        }

        private IKeyable showCurvesClip {
            get
            {
                if ( _showCurvesClip == null || Equals(_showCurvesClip, null) ) {
                    return null;
                }
                return _showCurvesClip;
            }
            set { _showCurvesClip = (ActionClip)value; }
        }

        ///<summary>The default track height when not expanded</summary>
        virtual public float defaultHeight {
            get { return 32f; }
        }

        ///<summary>Icon shown on left if any</summary>
        virtual public Texture icon {
            get
            {
                if ( _icon == null ) {
                    var att = this.GetType().RTGetAttribute<IconAttribute>(true);
                    if ( att != null ) {
                        _icon = Resources.Load(att.iconName) as Texture;
                        if ( _icon == null && !string.IsNullOrEmpty(att.iconName) ) {
                            _icon = UnityEditor.EditorGUIUtility.FindTexture(att.iconName) as Texture;
                        }
                        if ( _icon == null && att.fromType != null ) {
                            _icon = UnityEditor.AssetPreview.GetMiniTypeThumbnail(att.fromType);
                        }
                    }

                    if ( _icon == null ) {
                        _icon = new object();
                    }
                }

                return _icon as Texture;
            }
        }

        ///----------------------------------------------------------------------------------------------

        ///<summary>Add an ActionClip to this Track</summary>
        public T AddAction<T>(float time) where T : ActionClip { return (T)AddAction(typeof(T), time); }
        public ActionClip AddAction(System.Type type, float time) {

            var catAtt = type.GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute;
            if ( catAtt != null && clips.Count == 0 ) {
                name = catAtt.category + " Track";
            }

            bool recordUndo = CutsceneEditor.ShouldRecordUndoFor(root as Cutscene);
            var newAction = recordUndo
                ? UnityEditor.Undo.AddComponent(gameObject, type) as ActionClip
                : gameObject.AddComponent(type) as ActionClip;
            if ( recordUndo ) { UnityEditor.Undo.RegisterCompleteObjectUndo(this, "New Action"); }
            newAction.startTime = time;
            clips.Add(newAction);
            newAction.PostCreate(this);

            var nextAction = clips.FirstOrDefault(a => a.startTime > newAction.startTime);
            if ( nextAction != null ) {
                newAction.endTime = Mathf.Min(newAction.endTime, nextAction.startTime);
            }

            root.Validate();
            CutsceneUtility.selectedObject = newAction;
            return newAction;
        }

        ///<summary>Remove an ActionClip from this Track</summary>
        public void DeleteAction(ActionClip action) {
            bool recordUndo = CutsceneEditor.ShouldRecordUndoFor(root as Cutscene);
            if ( recordUndo ) { UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Remove Action"); }
            clips.Remove(action);
            if ( ReferenceEquals(CutsceneUtility.selectedObject, action) ) {
                CutsceneUtility.selectedObject = null;
            }
            if ( recordUndo ) { UnityEditor.Undo.DestroyObjectImmediate(action); }
            else { UnityEngine.Object.DestroyImmediate(action); }
            root.Validate();
        }

        ///----------------------------------------------------------------------------------------------

        ///<summary>The Editor GUI in the track info on the left</summary>
        virtual public void OnTrackInfoGUI(Rect trackRect) {
            var e = Event.current;
            DoDefaultInfoGUI(e, trackRect);
            if ( showCurves ) {
                var wasEnable = GUI.enabled;
                GUI.enabled = true;
                DoParamsInfoGUI(e, trackRect, showCurvesClip, showCurvesClip is ActionClips.AnimateProperties);
                GUI.enabled = wasEnable;
            }

            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
        }


        //default track info gui 
        protected void DoDefaultInfoGUI(Event e, Rect trackRect) {
            TrackEditorGUI.DrawDefaultInfoGUI(
                trackRect,
                name,
                info,
                icon,
                ReferenceEquals(CutsceneUtility.selectedObject, this),
                isActive,
                isLocked,
                showCurves,
                value => isActive = value,
                value => isLocked = value,
                value => showCurves = value);
        }

        //show selected clip animated parameters list info
        protected void DoParamsInfoGUI(Event e, Rect trackRect, IKeyable keyable, bool showAddPropertyButton) {
            TrackEditorGUI.DrawParametersInfoGUI(
                e,
                trackRect,
                keyable,
                showAddPropertyButton,
                defaultHeight,
                () => finalHeight,
                () => customHeight,
                value => customHeight = value,
                () => keyable == null || ReferenceEquals(keyable.parent, this),
                ref isResizingHeight,
                ref proposedHeight,
                ref inspectedParameterIndex);
        }



        ///<summary>The Editor GUI within the timeline rectangle</summary>
        virtual public void OnTrackTimelineGUI(Rect posRect, Rect timeRect, float cursorTime, System.Func<float, float> TimeToPos) {
            TrackEditorGUI.DrawTimelineGUI(
                Event.current,
                posRect,
                timeRect,
                cursorTime,
                TimeToPos,
                showCurves,
                showCurvesClip,
                ref inspectedParameterIndex,
                DoTrackContextMenu,
                OnCanAcceptDrop,
                OnAcceptDrop);
        }

        //...
        virtual protected bool OnCanAcceptDrop(UnityEngine.Object obj) { return false; }
        //...
        virtual protected bool OnAcceptDrop(UnityEngine.Object obj, float cursorTime) { return false; }

        void DoTrackContextMenu(Event e, Rect clipsPosRect, float cursorTime) {
            if ( e.type == EventType.ContextClick && clipsPosRect.Contains(e.mousePosition) ) {

                var attachableTypeInfos = new List<EditorTools.TypeMetaInfo>();

                var existing = clips.FirstOrDefault();
                var existingCatAtt = existing != null ? existing.GetType().GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute : null;
                foreach ( var info in EditorTools.GetTypeMetaDerivedFrom(typeof(ActionClip)) ) {

                    if ( !info.attachableTypes.Contains(this.GetType()) ) {
                        continue;
                    }

                    if ( existingCatAtt != null ) {
                        if ( existingCatAtt.category == info.category ) {
                            attachableTypeInfos.Add(info);
                        }
                    } else {
                        attachableTypeInfos.Add(info);
                    }
                }

                if ( attachableTypeInfos.Count > 0 ) {
                    var menu = new UnityEditor.GenericMenu();
                    foreach ( var _info in attachableTypeInfos ) {
                        var info = _info;
                        var category = string.IsNullOrEmpty(info.category) ? string.Empty : ( info.category + "/" );
                        var tName = info.name;
                        menu.AddItem(new GUIContent(category + tName), false, () => { AddAction(info.type, cursorTime); });
                    }

                    var copyType = CutsceneUtility.GetCopyType();
                    if ( copyType != null && attachableTypeInfos.Select(i => i.type).Contains(copyType) ) {
                        menu.AddSeparator("/");
                        menu.AddItem(new GUIContent(string.Format("Paste Clip ({0})", copyType.Name)), false, () => { CutsceneUtility.PasteClip(this, cursorTime); });
                    }

                    menu.ShowAsContext();
                    e.Use();
                }
            }
        }

        protected void DoClipCurves(Event e, Rect posRect, Rect timeRect, System.Func<float, float> TimeToPos, IKeyable keyable) {
            TrackEditorGUI.DrawClipCurves(
                e,
                posRect,
                timeRect,
                TimeToPos,
                keyable,
                () => keyable == null || ReferenceEquals(keyable.parent, this),
                ref inspectedParameterIndex);
        }

#endif
    }
}
