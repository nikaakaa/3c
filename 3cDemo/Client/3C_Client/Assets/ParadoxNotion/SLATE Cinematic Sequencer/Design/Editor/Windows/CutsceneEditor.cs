#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Slate
{

    public class CutsceneEditor : EditorWindow
    {
        CutsceneEditorSurface m_Surface;

        public static CutsceneEditor current;

        public static event System.Action OnStopInEditor
        {
            add => CutsceneEditorSurface.OnStopInEditor += value;
            remove => CutsceneEditorSurface.OnStopInEditor -= value;
        }

        public static event System.Action<Cutscene, string> OnEditTransactionBegin
        {
            add => CutsceneEditorSurface.OnEditTransactionBegin += value;
            remove => CutsceneEditorSurface.OnEditTransactionBegin -= value;
        }

        public static event System.Action<Cutscene> OnEditTransactionCommit
        {
            add => CutsceneEditorSurface.OnEditTransactionCommit += value;
            remove => CutsceneEditorSurface.OnEditTransactionCommit -= value;
        }

        public static event System.Action<Cutscene> OnEditTransactionCancel
        {
            add => CutsceneEditorSurface.OnEditTransactionCancel += value;
            remove => CutsceneEditorSurface.OnEditTransactionCancel -= value;
        }

        public static event System.Action OnEditorClosed
        {
            add => CutsceneEditorSurface.OnEditorClosed += value;
            remove => CutsceneEditorSurface.OnEditorClosed -= value;
        }

        public static event System.Action<ActionClip> OnActionDoubleClick
        {
            add => CutsceneEditorSurface.OnActionDoubleClick += value;
            remove => CutsceneEditorSurface.OnActionDoubleClick -= value;
        }

        public static System.Func<Cutscene, bool> RecordUndoForCutscene
        {
            get => CutsceneEditorSurface.RecordUndoForCutscene;
            set => CutsceneEditorSurface.RecordUndoForCutscene = value;
        }

        public static System.Func<Cutscene, bool> AllowPlaybackForCutscene
        {
            get => CutsceneEditorSurface.AllowPlaybackForCutscene;
            set => CutsceneEditorSurface.AllowPlaybackForCutscene = value;
        }

        public Cutscene cutscene => m_Surface != null ? m_Surface.cutscene : null;
        public float length
        {
            get => m_Surface != null ? m_Surface.length : 0f;
            set
            {
                if (m_Surface != null)
                    m_Surface.length = value;
            }
        }

        public float viewTimeMin
        {
            get => m_Surface != null ? m_Surface.viewTimeMin : 0f;
            set
            {
                if (m_Surface != null)
                    m_Surface.viewTimeMin = value;
            }
        }

        public float viewTimeMax
        {
            get => m_Surface != null ? m_Surface.viewTimeMax : 0f;
            set
            {
                if (m_Surface != null)
                    m_Surface.viewTimeMax = value;
            }
        }

        public static bool ShouldRecordUndoFor(Cutscene target)
        {
            return CutsceneEditorSurface.ShouldRecordUndoFor(target);
        }

        public static bool IsPlaybackAllowedFor(Cutscene target)
        {
            return CutsceneEditorSurface.IsPlaybackAllowedFor(target);
        }

        public static void ShowWindow()
        {
            ShowWindow(null);
        }

        public static void ShowWindow(Cutscene newCutscene)
        {
            CutsceneEditor window = GetWindow<CutsceneEditor>();
            window.InitializeWindow(newCutscene);
            window.Show();
        }

        public void InitializeEmbedded(Cutscene newCutscene, System.Action repaint)
        {
            EnsureSurface();
            m_Surface.InitializeEmbedded(newCutscene, repaint);
        }

        public void InitializeEmbedded(
            IEmbeddedTimelineBinding binding,
            System.Action repaint,
            System.Action beginWindowsCallback = null,
            System.Action endWindowsCallback = null)
        {
            EnsureSurface();
            m_Surface.InitializeEmbedded(binding, repaint, beginWindowsCallback, endWindowsCallback);
        }

        public void DrawEmbeddedGUI(float width, float height)
        {
            m_Surface?.DrawEmbeddedGUI(width, height);
        }

        public void RequestEmbeddedRepaint()
        {
            m_Surface?.RequestEmbeddedRepaint();
        }

        public void ClearEmbedded()
        {
            m_Surface?.ClearEmbedded();
        }

        public void Play(Cutscene.WrapMode wrapMode = Cutscene.WrapMode.Loop, System.Action callback = null)
        {
            m_Surface?.Play(wrapMode, callback);
        }

        public void PlayReverse()
        {
            m_Surface?.PlayReverse();
        }

        public void Pause()
        {
            m_Surface?.Pause();
        }

        public void Stop(bool forceRewind)
        {
            m_Surface?.Stop(forceRewind);
        }

        public static void ClearCutscene(Cutscene target)
        {
            CutsceneEditorSurface.ClearCutscene(target);
        }

        void OnEnable()
        {
            titleContent = new GUIContent("SLATE", Styles.cutsceneIconOpen);
            wantsMouseMove = true;
            autoRepaintOnSceneChange = false;
            minSize = new Vector2(500, 500);
            current = this;
            EnsureSurface();
            m_Surface.InitializeStandalone(
                null,
                Repaint,
                ShowNotification,
                RemoveNotification,
                content => titleContent = content,
                BeginWindowsHost,
                EndWindowsHost);
        }

        void OnDisable()
        {
            if (m_Surface != null)
            {
                DestroyImmediate(m_Surface);
                m_Surface = null;
            }
            if (ReferenceEquals(current, this))
                current = null;
        }

        void OnGUI()
        {
            m_Surface?.DrawGUI(position.width, position.height);
        }

        void InitializeWindow(Cutscene newCutscene)
        {
            EnsureSurface();
            m_Surface.InitializeStandalone(
                newCutscene,
                Repaint,
                ShowNotification,
                RemoveNotification,
                content => titleContent = content,
                BeginWindowsHost,
                EndWindowsHost);
        }

        void BeginWindowsHost()
        {
            BeginWindows();
        }

        void EndWindowsHost()
        {
            EndWindows();
        }

        void EnsureSurface()
        {
            if (m_Surface == null)
                m_Surface = CreateInstance<CutsceneEditorSurface>();
        }
    }
    public class CutsceneEditorSurface : ScriptableObject
    {

        enum EditorPlaybackState
        {
            Stoped,
            PlayingForwards,
            PlayingBackwards
        }

        struct GuideLine
        {
            public float time;
            public Color color;
            public GuideLine(float time, Color color) {
                this.time = time;
                this.color = color;
            }
        }

        ///----------------------------------------------------------------------------------------------

        public static CutsceneEditorSurface current;
        public static event System.Action OnStopInEditor;
        public static event System.Action<Cutscene, string> OnEditTransactionBegin;
        public static event System.Action<Cutscene> OnEditTransactionCommit;
        public static event System.Action<Cutscene> OnEditTransactionCancel;
        public static event System.Action OnEditorClosed;
        public static event System.Action<ActionClip> OnActionDoubleClick;
        public static System.Func<Cutscene, bool> RecordUndoForCutscene;
        public static System.Func<Cutscene, bool> AllowPlaybackForCutscene;

        public static bool ShouldRecordUndoFor(Cutscene target)
        {
            return RecordUndoForCutscene == null || RecordUndoForCutscene(target);
        }

        public static bool IsPlaybackAllowedFor(Cutscene target)
        {
            return AllowPlaybackForCutscene == null || AllowPlaybackForCutscene(target);
        }

        private Cutscene _cutscene;
        [System.NonSerialized] private bool embeddedSurface;
        [System.NonSerialized] private float embeddedWidth;
        [System.NonSerialized] private float embeddedHeight;
        [System.NonSerialized] private System.Action embeddedRepaint;
        [System.NonSerialized] private System.Action standaloneRepaint;
        [System.NonSerialized] private System.Action<GUIContent> showNotification;
        [System.NonSerialized] private System.Action removeNotification;
        [System.NonSerialized] private System.Action<GUIContent> setTitle;
        [System.NonSerialized] private System.Action beginWindows;
        [System.NonSerialized] private System.Action endWindows;
        [System.NonSerialized] private System.Func<int> embeddedFrameRate;
        [System.NonSerialized] private System.Func<float> embeddedLength;
        [System.NonSerialized] private System.Func<int> embeddedCurrentFrame;
        [System.NonSerialized] private System.Action<int> embeddedSetCurrentFrame;
        [System.NonSerialized] private System.Func<float> embeddedViewTimeMin;
        [System.NonSerialized] private System.Action<float> embeddedSetViewTimeMin;
        [System.NonSerialized] private System.Func<float> embeddedViewTimeMax;
        [System.NonSerialized] private System.Action<float> embeddedSetViewTimeMax;
        [System.NonSerialized] private System.Func<float?> embeddedRuntimeTime;
        [System.NonSerialized] private System.Func<float?> embeddedHistoryTime;
        [System.NonSerialized] private System.Func<string, bool> embeddedRuntimeTrackActive;
        [System.NonSerialized] private System.Func<string, string> embeddedRuntimeClipStatus;
        [System.NonSerialized] private System.Action embeddedAddTrack;
        [System.NonSerialized] private System.Action<ActionClip> embeddedCopyClip;
        [System.NonSerialized] private IEmbeddedTimelineBinding embeddedTimeline;
        internal IEmbeddedTimelineBinding EmbeddedTimeline => embeddedTimeline;
#if UNITY_6000_5_OR_NEWER
        private EntityId _cutsceneEntityID;
#else
        private int _cutsceneID;
#endif

        //Layout variables
        private static float LEFT_MARGIN { //caps for consistency. margin on the left side. The width of the group/tracks list.
            get { return Prefs.trackListLeftMargin; }
            set { Prefs.trackListLeftMargin = Mathf.Clamp(value, 230, 400); }
        }
        private const float RIGHT_MARGIN = 16; //margin on the right side
        private const float TOOLBAR_HEIGHT = 21; //the height of the toolbar
        private const float TOP_MARGIN = 40; //top margin AFTER the toolbar
        private const float GROUP_HEIGHT = 22; //height of group headers
        private const float TRACK_MARGINS = 4;  //margin between tracks of same group (top/bottom)
        private const float GROUP_RIGHT_MARGIN = 4;  //margin at the right side of groups
        private const float TRACK_RIGHT_MARGIN = 4;  //margin at the right side of tracks
        private const float FIRST_GROUP_TOP_MARGIN = 22; //initial top margin

        private static readonly Color LIST_SELECTION_COLOR = new Color(0.5f, 0.5f, 1, 0.3f);
        private static readonly Color GROUP_COLOR = new Color(0f, 0f, 0f, 0.25f);
        private Color HIGHLIGHT_COLOR { get { return isProSkin ? new Color(0.65f, 0.65f, 1) : new Color(0.1f, 0.1f, 0.1f); } }
        private float MAGNET_SNAP_INTERVAL { get { return viewTime * 0.01f; } }

        //Layout Rects
        private Rect topLeftRect;   //for playback controls
        private Rect topMiddleRect; //for time info
        private Rect leftRect;      //for group/track list
        private Rect centerRect;    //for timeline


        [System.NonSerialized] private Dictionary<int, ActionClipWrapper> clipWrappers;
        [System.NonSerialized] private Dictionary<ActionClip, ActionClipWrapper> clipWrappersMap;
        [System.NonSerialized] private EditorPlaybackState editorPlaybackState = EditorPlaybackState.Stoped;
        [System.NonSerialized] private Cutscene.WrapMode editorPlaybackWrapMode = Cutscene.WrapMode.Loop;
        [System.NonSerialized] private ActionClipWrapper interactingClip;
        [System.NonSerialized] private bool isMovingScrubCarret;
        [System.NonSerialized] private bool isMovingEndCarret;
        [System.NonSerialized] private bool isMovingLoopRegionMin;
        [System.NonSerialized] private bool isMovingLoopRegionMax;
        [System.NonSerialized] private bool isMouseButton2Down;
        [System.NonSerialized] private Vector2 scrollPos;
        [System.NonSerialized] private float totalHeight;
        [System.NonSerialized] private CutsceneTrack pickedTrack;
        [System.NonSerialized] private CutsceneGroup pickedGroup;
        [System.NonSerialized] private float lastStartPlayTime;
        [System.NonSerialized] private float editorPreviousTime;

        [System.NonSerialized] private Vector2? multiSelectStartPos;
        [System.NonSerialized] private List<ActionClipWrapper> multiSelection;
        [System.NonSerialized] private Rect preMultiSelectionRetimeMinMax;
        [System.NonSerialized] private int multiSelectionScaleDirection;

        [System.NonSerialized] private Vector2 mousePosition;
        [System.NonSerialized] private Section draggedSection;
        [System.NonSerialized] private bool willRepaint;
        [System.NonSerialized] private bool willDirty;
        [System.NonSerialized] private bool willResample;
        [System.NonSerialized] private bool editTransactionActive;
        [System.NonSerialized] private int editTransactionButton = -1;
        [System.NonSerialized] private System.Action onDoPopup;
        [System.NonSerialized] private bool isResizingLeftMargin;
        [System.NonSerialized] private bool isAboutButtonPressed;
        [System.NonSerialized] private bool showDragDropInfo;
        [System.NonSerialized] private string searchString;
        [System.NonSerialized] private float[] magnetSnapTimesCache;
        [System.NonSerialized] private List<GuideLine> pendingGuides;
        [System.NonSerialized] private System.Action postWindowsGUI;
        [System.NonSerialized] private Dictionary<string, string> formalInspectedParameters;
        [System.NonSerialized] private IEmbeddedTimelineTrackBinding formalPickedTrack;
        [System.NonSerialized] private IEmbeddedTimelineTrackBinding formalResizingTrack;
        [System.NonSerialized] private IEmbeddedTimelineSectionBinding formalDraggedSection;
        [System.NonSerialized] private bool formalSelectionHandled;

        [System.NonSerialized] private CutsceneTrack copyTrack;


        [System.NonSerialized] private float timeInfoStart;
        [System.NonSerialized] private float timeInfoEnd;
        [System.NonSerialized] private float timeInfoInterval;
        [System.NonSerialized] private float timeInfoHighMod;

        ///----------------------------------------------------------------------------------------------

        //The current cutscene reference
        public Cutscene cutscene {
            get
            {
                if ( _cutscene == null ) {
#if UNITY_6000_5_OR_NEWER
                    _cutscene = EditorUtility.EntityIdToObject(_cutsceneEntityID) as Cutscene;
#else
                    _cutscene = EditorUtility.InstanceIDToObject(_cutsceneID) as Cutscene;
#endif
                }
                return _cutscene;
            }
            private set
            {
                _cutscene = value;
                if ( value != null ) {
#if UNITY_6000_5_OR_NEWER
                    _cutsceneEntityID = value.GetEntityId();
#else
                    _cutsceneID = value.GetInstanceID();
#endif
                }
            }
        }

        ///----------------------------------------------------------------------------------------------

        //The length of the cutscene reference
        public float length {
            get { return embeddedLength != null ? Mathf.Max(0f, embeddedLength()) : cutscene.length; }
            set { cutscene.length = value; }
        }

        //The min view time
        public float viewTimeMin {
            get { return embeddedViewTimeMin != null ? embeddedViewTimeMin() : cutscene.viewTimeMin; }
            set
            {
                if (embeddedSetViewTimeMin != null)
                    embeddedSetViewTimeMin(value);
                else
                    cutscene.viewTimeMin = value;
            }
        }

        //The max view time
        public float viewTimeMax {
            get { return embeddedViewTimeMax != null ? embeddedViewTimeMax() : cutscene.viewTimeMax; }
            set
            {
                if (embeddedSetViewTimeMax != null)
                    embeddedSetViewTimeMax(value);
                else
                    cutscene.viewTimeMax = value;
            }
        }

        public Vector2 EmbeddedScrollPosition {
            get => scrollPos;
            set => scrollPos = value;
        }

        //The max time currently in view
        public float maxTime {
            get { return Mathf.Max(viewTimeMax, length); }
        }

        //The "length" of the currently viewing time
        public float viewTime {
            get { return viewTimeMax - viewTimeMin; }
        }

        ///----------------------------------------------------------------------------------------------

        //SHORTCUTS//

        //Is Unity editor pro skin?
        private static bool isProSkin {
            get { return EditorGUIUtility.isProSkin; }
        }

        //A white texture
        private static Texture2D whiteTexture {
            get { return Slate.Styles.whiteTexture; }
        }

        //Is cutscene reference an asset in project?
        private bool isCutsceneAsset {
            get { return cutscene != null && UnityEditor.EditorUtility.IsPersistent(cutscene); }
        }

        //Screen Width. Handles retina.
        private float screenWidth {
            get { return embeddedSurface ? embeddedWidth : Screen.width / EditorGUIUtility.pixelsPerPoint; }
        }

        //Screen Height. Hanldes retina.
        private float screenHeight {
            get { return embeddedSurface ? embeddedHeight : Screen.height / EditorGUIUtility.pixelsPerPoint; }
        }

        //The color used in scruber
        private Color scruberColor {
            get { return embeddedSurface || cutscene.isActive ? Color.yellow : new Color(1, 0.3f, 0.3f); }
        }

        ///----------------------------------------------------------------------------------------------

        //UTILITIES

        //Convert time to position
        float TimeToPos(float time) {
            return ( time - viewTimeMin ) / viewTime * centerRect.width;
        }

        //Convert position to time
        float PosToTime(float pos) {
            return ( pos - LEFT_MARGIN ) / centerRect.width * viewTime + viewTimeMin;
        }

        //Round time to nearest working snap interval
        float SnapTime(float time) {
            //holding control for precision (ignore snap intervals)
            if ( Event.current.control ) { return time; }
            if ( embeddedSurface ) {
                var frameRate = Mathf.Max(1, embeddedFrameRate != null ? embeddedFrameRate() : Prefs.frameRate);
                return Mathf.Round(time * frameRate) / frameRate;
            }
            return ( Mathf.Round(time / Prefs.snapInterval) * Prefs.snapInterval );
        }

        internal float EmbeddedCurrentTime()
        {
            if (embeddedCurrentFrame != null)
                return embeddedCurrentFrame() / (float)Mathf.Max(1, embeddedFrameRate != null ? embeddedFrameRate() : Prefs.frameRate);
            return cutscene.currentTime;
        }

        internal void SetEmbeddedCurrentTime(float time)
        {
            if (embeddedSetCurrentFrame != null)
            {
                int frame = Mathf.RoundToInt(Mathf.Max(0f, time) * Mathf.Max(1, embeddedFrameRate != null ? embeddedFrameRate() : Prefs.frameRate));
                embeddedSetCurrentFrame(frame);
                return;
            }
            cutscene.currentTime = time;
        }

        internal void BeginEmbeddedEdit(string undoName)
        {
            BeginEditTransaction(undoName, 0);
        }

        internal void CommitEmbeddedEdit()
        {
            CommitEditTransaction();
        }

        internal void BeginEmbeddedTrackResize(IEmbeddedTimelineTrackBinding track)
        {
            if (embeddedTimeline == null || embeddedTimeline.IsReadOnly || track == null)
                return;
            formalResizingTrack = track;
            BeginEmbeddedEdit("Resize Timeline Track");
        }

        internal bool IsEmbeddedTrackResizing(IEmbeddedTimelineTrackBinding track)
        {
            return ReferenceEquals(formalResizingTrack, track);
        }

        internal void EndEmbeddedTrackResize()
        {
            if (formalResizingTrack == null)
                return;
            formalResizingTrack = null;
            CommitEmbeddedEdit();
        }

        public static float CurrentSnapInterval {
            get
            {
                if ( current != null && current.embeddedSurface )
                    return 1f / Mathf.Max(1, current.embeddedFrameRate != null ? current.embeddedFrameRate() : Prefs.frameRate);
                return Prefs.snapInterval;
            }
        }

        readonly struct SurfaceLayout
        {
            public SurfaceLayout(Rect topLeft, Rect topMiddle, Rect left, Rect center)
            {
                TopLeft = topLeft;
                TopMiddle = topMiddle;
                Left = left;
                Center = center;
            }

            public Rect TopLeft { get; }
            public Rect TopMiddle { get; }
            public Rect Left { get; }
            public Rect Center { get; }
        }

        //Do action safely (stop cutscene, do, resample)
        void SafeDoAction(System.Action call) {
            bool ownsTransaction = !editTransactionActive;
            if ( ownsTransaction ) { BeginEditTransaction("Cutscene Change", 0); }
            var time = cutscene.currentTime;
            try {
                Stop(true);
                call();
                cutscene.currentTime = time;
            } finally {
                if ( ownsTransaction ) { CommitEditTransaction(); }
            }
        }

        bool ShouldRecordUndo() {
            if (embeddedTimeline != null)
                return !embeddedTimeline.IsReadOnly;
            return ShouldRecordUndoFor(cutscene);
        }

        void BeginEditTransaction(string undoName, int button) {
            if ( editTransactionActive ) { return; }
            editTransactionActive = true;
            editTransactionButton = button;
            if (embeddedTimeline != null)
            {
                embeddedTimeline.BeginEdit(undoName);
                return;
            }
            OnEditTransactionBegin?.Invoke(cutscene, undoName);
        }

        void CommitEditTransaction() {
            if ( !editTransactionActive ) { return; }
            editTransactionActive = false;
            editTransactionButton = -1;
            if (embeddedTimeline != null)
            {
                embeddedTimeline.CommitEdit();
                return;
            }
            OnEditTransactionCommit?.Invoke(cutscene);
        }

        void CancelEditTransaction() {
            if ( !editTransactionActive ) { return; }
            formalResizingTrack = null;
            editTransactionActive = false;
            editTransactionButton = -1;
            if (embeddedTimeline != null)
            {
                embeddedTimeline.CancelEdit();
                return;
            }
            OnEditTransactionCancel?.Invoke(cutscene);
        }

        //Is directable filtered out by search string?
        bool IsFilteredOutBySearch(IDirectable directable, string search) {
            if ( string.IsNullOrEmpty(search) ) { return false; }
            if ( string.IsNullOrEmpty(directable.name) ) { return true; }
            return !directable.name.ToLower().Contains(search.ToLower());
        }

        //Draw a vertical guide line at time with color
        void DrawGuideLine(float time, Color color) {
            if ( time >= viewTimeMin && time <= viewTimeMax ) {
                var xPos = TimeToPos(time);
                var guideRect = new Rect(xPos + centerRect.x - 1, centerRect.y, 2, centerRect.height);
                GUI.color = color;
                GUI.DrawTexture(guideRect, whiteTexture);
                GUI.color = Color.white;
            }
        }

        //Add a cursor type at rect
        void AddCursorRect(Rect rect, MouseCursor type) {
            EditorGUIUtility.AddCursorRect(rect, type);
            willRepaint = true;
        }

        //Pop action GUI calls in popup
        void DoPopup(System.Action call) {
            onDoPopup = call;
        }

        //Cache an array of snap times for clip (clip times are excluded)
        //Saved in property .magnetSnapTimesCache
        void CacheMagnetSnapTimes(IClipEditorBinding excluded = null) {
            var result = new List<float>();
            result.Add(0);
            result.Add(length);
            result.Add(EmbeddedCurrentTime());
            if ( cutscene != null && cutscene.directorGroup != null ) {
                result.AddRange(cutscene.directorGroup.sections.Select(s => s.time));
            }
            foreach ( var cw in clipWrappers ) {
                IClipEditorBinding binding = cw.Value.editorBinding;
                bool sameTrack = excluded == null ||
                    (binding.Track != null && excluded.Track != null
                        ? ReferenceEquals(binding.Track, excluded.Track)
                        : binding.NativeAction != null && excluded.NativeAction != null &&
                          binding.NativeAction.parent.parent == excluded.NativeAction.parent.parent);
                if (sameTrack && !ReferenceEquals(binding, excluded)) {
                    result.Add(binding.StartTime);
                    result.Add(binding.EndTime);
                }
            }
            magnetSnapTimesCache = result.Distinct().ToArray();
        }

        //Find best snap time (closest)
        float? MagnetSnapTime(float time, float[] snapTimes) {
            if ( snapTimes == null ) { return null; }
            var bestDistance = float.PositiveInfinity;
            var bestTime = float.PositiveInfinity;
            for ( var i = 0; i < snapTimes.Length; i++ ) {
                var snapTime = snapTimes[i];
                var distance = Mathf.Abs(snapTime - time);
                if ( distance < bestDistance ) {
                    bestDistance = distance;
                    bestTime = snapTime;
                }
            }
            if ( Mathf.Abs(bestTime - time) <= MAGNET_SNAP_INTERVAL ) {
                return bestTime;
            }
            return null;
        }

        ///----------------------------------------------------------------------------------------------

        public void InitializeEmbedded(Cutscene newCutscene, System.Action repaint)
        {
            InitializeEmbedded(newCutscene, repaint, null, null, null);
        }

        SurfaceLayout CalculateLayout()
        {
            var timelineTop = embeddedSurface ? TOP_MARGIN : TOOLBAR_HEIGHT + TOP_MARGIN;
            var timeInfoTop = embeddedSurface ? 0f : TOOLBAR_HEIGHT;
            float contentWidth = screenWidth - LEFT_MARGIN - RIGHT_MARGIN;
            float contentHeight = screenHeight - timelineTop + scrollPos.y;
            return new SurfaceLayout(
                new Rect(0, embeddedSurface ? 0 : TOOLBAR_HEIGHT, LEFT_MARGIN, TOP_MARGIN),
                new Rect(LEFT_MARGIN, timeInfoTop, contentWidth, TOP_MARGIN),
                new Rect(0, timelineTop, LEFT_MARGIN, contentHeight),
                new Rect(LEFT_MARGIN, timelineTop, contentWidth, contentHeight));
        }

        public void InitializeEmbedded(
            Cutscene newCutscene,
            System.Action repaint,
            System.Func<int> frameRate,
            System.Action addTrack,
            System.Action<ActionClip> copyClip,
            System.Func<int> currentFrame = null,
            System.Action<int> setCurrentFrame = null,
            System.Func<float> timelineLength = null,
            System.Func<float> viewMin = null,
            System.Action<float> setViewMin = null,
            System.Func<float> viewMax = null,
            System.Action<float> setViewMax = null)
        {
            embeddedSurface = true;
            embeddedRepaint = repaint;
            embeddedFrameRate = frameRate;
            embeddedLength = timelineLength;
            embeddedCurrentFrame = currentFrame;
            embeddedSetCurrentFrame = setCurrentFrame;
            embeddedViewTimeMin = viewMin;
            embeddedSetViewTimeMin = setViewMin;
            embeddedViewTimeMax = viewMax;
            embeddedSetViewTimeMax = setViewMax;
            embeddedAddTrack = addTrack;
            embeddedCopyClip = copyClip;
            Styles.Load();
            showDragDropInfo = false;
            willRepaint = true;
            pendingGuides = new List<GuideLine>();
            InitializeAll(newCutscene);
        }

        public void InitializeEmbedded(
            IEmbeddedTimelineBinding binding,
            System.Action repaint,
            System.Action beginWindowsCallback = null,
            System.Action endWindowsCallback = null)
        {
            embeddedSurface = true;
            embeddedTimeline = binding ?? throw new System.ArgumentNullException(nameof(binding));
            embeddedRepaint = repaint;
            embeddedFrameRate = () => binding.FrameRate;
            embeddedLength = () => binding.Length;
            embeddedCurrentFrame = () => binding.CurrentFrame;
            embeddedSetCurrentFrame = value => binding.CurrentFrame = value;
            embeddedViewTimeMin = () => binding.ViewTimeMin;
            embeddedSetViewTimeMin = value => binding.ViewTimeMin = value;
            embeddedViewTimeMax = () => binding.ViewTimeMax;
            embeddedSetViewTimeMax = value => binding.ViewTimeMax = value;
            embeddedAddTrack = binding.AddTrack;
            embeddedCopyClip = null;
            beginWindows = beginWindowsCallback;
            endWindows = endWindowsCallback;
            Styles.Load();
            showDragDropInfo = false;
            willRepaint = true;
            pendingGuides = new List<GuideLine>();
            clipWrappers = new Dictionary<int, ActionClipWrapper>();
            clipWrappersMap = null;
            interactingClip = null;
            multiSelection = null;
            formalInspectedParameters = new Dictionary<string, string>(System.StringComparer.Ordinal);
            formalPickedTrack = null;
            formalResizingTrack = null;
            formalDraggedSection = null;
            cutscene = null;
        }

        internal void ApplyEmbeddedCommand(System.Action command, string undoName)
        {
            if (embeddedTimeline == null || embeddedTimeline.IsReadOnly)
                return;
            try
            {
                BeginEditTransaction(undoName, 0);
                command?.Invoke();
                CommitEditTransaction();
            }
            catch (System.Exception exception)
            {
                CancelEditTransaction();
                ShowNotification(new GUIContent(exception.Message));
            }
        }

        public void ConfigureEmbeddedRuntimeTime(System.Func<float?> runtimeTime)
        {
            embeddedRuntimeTime = runtimeTime;
        }

        public void ConfigureEmbeddedHistoryTime(System.Func<float?> historyTime)
        {
            embeddedHistoryTime = historyTime;
        }

        public void ConfigureEmbeddedRuntimeState(
            System.Func<string, bool> trackActive,
            System.Func<string, string> clipStatus)
        {
            embeddedRuntimeTrackActive = trackActive;
            embeddedRuntimeClipStatus = clipStatus;
        }

        public void InitializeStandalone(
            Cutscene newCutscene,
            System.Action repaint,
            System.Action<GUIContent> notification,
            System.Action removeNotificationCallback,
            System.Action<GUIContent> titleCallback,
            System.Action beginWindowsCallback,
            System.Action endWindowsCallback)
        {
            embeddedSurface = false;
            embeddedRepaint = null;
            standaloneRepaint = repaint;
            showNotification = notification;
            removeNotification = removeNotificationCallback;
            setTitle = titleCallback;
            beginWindows = beginWindowsCallback;
            endWindows = endWindowsCallback;
            showDragDropInfo = true;
            InitializeAll(newCutscene);
        }

        public void DrawGUI(float width, float height)
        {
            CutsceneEditorSurface previous = current;
            current = this;
            formalSelectionHandled = false;
            try
            {
                OnGUI();
            }
            finally
            {
                if (ReferenceEquals(current, this))
                    current = previous;
            }
        }

        public void DrawEmbeddedGUI(float width, float height)
        {
            DrawEmbeddedGUI(width, height, null, null);
        }

        public void DrawEmbeddedGUI(
            float width,
            float height,
            System.Action beginWindowsCallback,
            System.Action endWindowsCallback)
        {
            if (!embeddedSurface)
                throw new System.InvalidOperationException("Slate editor is not initialized as an embedded surface.");
            embeddedWidth = Mathf.Max(1f, width);
            embeddedHeight = Mathf.Max(1f, height);
            beginWindows = beginWindowsCallback;
            endWindows = endWindowsCallback;
            formalSelectionHandled = false;
            CutsceneEditorSurface previous = current;
            current = this;
            try
            {
                OnGUI();
            }
            finally
            {
                if (ReferenceEquals(current, this))
                    current = previous;
            }
        }

        public void RequestEmbeddedRepaint()
        {
            if (embeddedSurface)
                embeddedRepaint?.Invoke();
            else
                standaloneRepaint?.Invoke();
        }

        public void ClearEmbedded()
        {
            if (!embeddedSurface)
                return;
            CancelEditTransaction();
            if (cutscene != null && !embeddedSurface && !Application.isPlaying)
                Stop(true);
            if (cutscene != null &&
                (ReferenceEquals(Selection.activeObject, cutscene.gameObject) ||
                 Selection.activeObject is Component component && component.transform.IsChildOf(cutscene.transform)))
            {
                Selection.activeObject = null;
            }
            CutsceneUtility.selectedObject = null;
            cutscene = null;
            clipWrappers = null;
            clipWrappersMap = null;
            embeddedRepaint = null;
            standaloneRepaint = null;
            showNotification = null;
            removeNotification = null;
            setTitle = null;
            beginWindows = null;
            endWindows = null;
            embeddedFrameRate = null;
            embeddedLength = null;
            embeddedCurrentFrame = null;
            embeddedSetCurrentFrame = null;
            embeddedViewTimeMin = null;
            embeddedSetViewTimeMin = null;
            embeddedViewTimeMax = null;
            embeddedSetViewTimeMax = null;
            embeddedRuntimeTime = null;
            embeddedHistoryTime = null;
            embeddedRuntimeTrackActive = null;
            embeddedRuntimeClipStatus = null;
            embeddedAddTrack = null;
            embeddedCopyClip = null;
            CurveEditor.ClearEmbeddedCache(this);
            DopeSheetEditor.ClearEmbeddedCache(this);
            embeddedTimeline = null;
            formalInspectedParameters = null;
            formalPickedTrack = null;
            formalResizingTrack = null;
            formalDraggedSection = null;
            embeddedSurface = false;
            if (ReferenceEquals(current, this))
                current = null;
        }

        public static void ClearCutscene(Cutscene target)
        {
            if (current == null || !ReferenceEquals(current.cutscene, target))
                return;
            current.CancelEditTransaction();
            if (!Application.isPlaying)
                current.Stop(true);
            current.cutscene = null;
            current.clipWrappers = null;
            current.clipWrappersMap = null;
            CutsceneUtility.selectedObject = null;
            current.willRepaint = true;
        }

        void SyncEmbeddedCurveTrack()
        {
            if (!embeddedSurface || cutscene == null)
                return;
            CutsceneTrack selectedTrack = (CutsceneUtility.selectedObject as ActionClip)?.parent as CutsceneTrack;
            bool changed = false;
            foreach (CutsceneGroup group in cutscene.groups)
            {
                foreach (CutsceneTrack track in group.tracks)
                {
                    bool shouldShow = selectedTrack != null && ReferenceEquals(track, selectedTrack);
                    if (track.showCurves == shouldShow)
                        continue;
                    track.showCurves = shouldShow;
                    changed = true;
                }
            }
            if (changed)
                willRepaint = true;
        }

        void OnEmbeddedSelectionChanged(IDirectable _)
        {
            if (!embeddedSurface)
                return;
            SyncEmbeddedCurveTrack();
            RequestEmbeddedRepaint();
        }

        void ShowNotification(GUIContent content)
        {
            showNotification?.Invoke(content);
        }

        void RemoveNotification()
        {
            removeNotification?.Invoke();
        }

        void SetTitle(GUIContent content)
        {
            setTitle?.Invoke(content);
        }

        void BeginWindows()
        {
            beginWindows?.Invoke();
        }

        void EndWindows()
        {
            endWindows?.Invoke();
        }

        //...
        void OnEnable() {
            Styles.Load();

            UnityEditor.SceneManagement.PrefabStage.prefabStageClosing += (stage) => { if ( !embeddedSurface && cutscene != null && stage.IsPartOfPrefabContents(cutscene.gameObject) ) { Stop(true); } };
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnWillSaveScene;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnWillSaveScene;

#pragma warning disable 618
            EditorApplication.playmodeStateChanged -= InitializeAll;
            EditorApplication.playmodeStateChanged += InitializeAll;
#pragma warning restore

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            Tools.hidden = false;
            willRepaint = true;
            showDragDropInfo = true;
            pendingGuides = new List<GuideLine>();

            current = this;
            CutsceneUtility.onSelectionChange -= OnEmbeddedSelectionChanged;
            CutsceneUtility.onSelectionChange += OnEmbeddedSelectionChanged;
            InitializeAll();
        }

        //...
        void OnDisable() {
            CutsceneUtility.onSelectionChange -= OnEmbeddedSelectionChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnWillSaveScene;
            CancelEditTransaction();

#pragma warning disable 618
            EditorApplication.playmodeStateChanged -= InitializeAll;
#pragma warning restore

            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
            Tools.hidden = false;
            if ( cutscene != null && !embeddedSurface && !Application.isPlaying ) {
                Stop(true);
            }
            OnEditorClosed?.Invoke();
            current = null;
        }


        //Set a new view when a script is selected in Unity's tab
        void OnSelectionChange() {
            if ( Selection.activeGameObject != null ) {
                var cut = Selection.activeGameObject.GetComponent<Cutscene>();
                if ( cut != null && cutscene != cut ) {
                    InitializeAll(cut);
                }
            }
        }

        //Before scene is saved we need to stop so that cutscene changes are reverted.
        void OnWillSaveScene(UnityEngine.SceneManagement.Scene scene, string path) {
            if ( cutscene != null && !embeddedSurface && cutscene.currentTime > 0 ) {
                Stop(true);
                Debug.LogWarning("Scene Saved while a cutscene was in preview mode. Cutscene was reverted before saving the scene along with changes it affected.");
            }
        }

        ///<summary>Initialize everything</summary>
        void InitializeAll() { InitializeAll(cutscene); }
        void InitializeAll(Cutscene newCutscene) {

            //first stop current cut if any
            if ( cutscene != null ) {
                if ( !embeddedSurface && !Application.isPlaying ) {
                    Stop(true);
                }
            }

            //set the new
            if ( newCutscene != null ) {
                cutscene = newCutscene;
                CutsceneUtility.selectedObject = null;
                multiSelection = null;
                InitClipWrappers();
                if ( !embeddedSurface && !Application.isPlaying ) {
                    Stop(true);
                }
            }

            willRepaint = true;
        }

        //initialize the action clip wrappers
        void InitClipWrappers() {

            if ( cutscene == null ) {
                return;
            }

            multiSelection = null;
            var lastTime = cutscene.currentTime;

            if ( !embeddedSurface && !Application.isPlaying ) {
                Stop(true);
            }

            cutscene.Validate();
            clipWrappers = new Dictionary<int, ActionClipWrapper>();
            clipWrappersMap = new Dictionary<ActionClip, ActionClipWrapper>();
            for ( int g = 0; g < cutscene.groups.Count; g++ ) {
                for ( int t = 0; t < cutscene.groups[g].tracks.Count; t++ ) {
                    for ( int a = 0; a < cutscene.groups[g].tracks[t].clips.Count; a++ ) {
                        var id = UID(g, t, a);
                        if ( clipWrappers.ContainsKey(id) ) {
                            Debug.LogError("Collided UIDs. This should really not happen but it did!");
                            continue;
                        }
                        var clip = cutscene.groups[g].tracks[t].clips[a];
                        var wrapper = new ActionClipWrapper(clip);
                        clipWrappers[id] = wrapper;
                        clipWrappersMap[clip] = wrapper;
                    }
                }
            }

            if ( lastTime > 0 ) {
                cutscene.currentTime = lastTime;
            }
        }

        //An integer UID out of list indeces (group, track, action clip)
        int UID(int g, int t, int a) {
            var A = g.ToString("D3");
            var B = t.ToString("D3");
            var C = a.ToString("D4");
            return int.Parse(A + B + C);
        }

        //Play button pressed or otherwise started
        public void Play(Cutscene.WrapMode wrapMode = Cutscene.WrapMode.Loop, System.Action callback = null) {

            SetTitle(new GUIContent("SLATE", Styles.cutsceneIconClose));

            if ( Application.isPlaying ) {
                var temp = cutscene.currentTime == length ? 0 : cutscene.currentTime;
                cutscene.Play(0, length, cutscene.defaultWrapMode, callback, Cutscene.PlayingDirection.Forwards);
                cutscene.currentTime = temp;
                return;
            }

            editorPlaybackWrapMode = wrapMode;
            editorPlaybackState = EditorPlaybackState.PlayingForwards;
            editorPreviousTime = Time.realtimeSinceStartup;
            lastStartPlayTime = cutscene.currentTime;
            OnStopInEditor = callback != null ? callback : OnStopInEditor;
        }

        //Play reverse button pressed
        public void PlayReverse() {

            SetTitle(new GUIContent("SLATE", Styles.cutsceneIconClose));

            if ( Application.isPlaying ) {
                var temp = cutscene.currentTime == 0 ? length : cutscene.currentTime;
                cutscene.Play(0, length, cutscene.defaultWrapMode, null, Cutscene.PlayingDirection.Backwards);
                cutscene.currentTime = temp;
                return;
            }

            editorPlaybackState = EditorPlaybackState.PlayingBackwards;
            editorPreviousTime = Time.realtimeSinceStartup;
            if ( cutscene.currentTime == 0 ) {
                cutscene.currentTime = length;
                lastStartPlayTime = 0;
            } else {
                lastStartPlayTime = cutscene.currentTime;
            }
        }

        //Pause button pressed
        public void Pause() {


            if ( Application.isPlaying ) {
                if ( cutscene.isActive ) {
                    cutscene.Pause();
                    return;
                }
            }

            editorPlaybackState = EditorPlaybackState.Stoped;
            if ( OnStopInEditor != null ) {
                OnStopInEditor();
                OnStopInEditor = null;
            }
        }

        //Stop button pressed or otherwise reset the scrubbing/previewing
        public void Stop(bool forceRewind) {

            if ( embeddedSurface ) {
                editorPlaybackState = EditorPlaybackState.Stoped;
                willRepaint = true;
                return;
            }

            if ( Application.isPlaying ) {
                if ( cutscene.isActive ) {
                    cutscene.Stop();
                    return;
                }
            }

            if ( OnStopInEditor != null ) {
                OnStopInEditor();
                OnStopInEditor = null;
            }

            //Super important to Sample instead of setting time here, so that we rewind correct if need be. 0 rewinds.
            cutscene.Sample(editorPlaybackState != EditorPlaybackState.Stoped && !forceRewind ? lastStartPlayTime : 0);
            editorPlaybackState = EditorPlaybackState.Stoped;
            willRepaint = true;
        }

        ///<summary>Steps time forward to the next key time</summary>
        void StepForward() {
            if ( embeddedSurface ) {
                if ( embeddedCurrentFrame != null && embeddedSetCurrentFrame != null ) {
                    embeddedSetCurrentFrame(embeddedCurrentFrame() + 1);
                    return;
                }
                cutscene.currentTime = Mathf.Min(cutscene.length, cutscene.currentTime + 1f / Mathf.Max(1, embeddedFrameRate != null ? embeddedFrameRate() : Prefs.frameRate));
                return;
            }
            var keyable = CutsceneUtility.selectedObject as IKeyable;
            if ( keyable != null ) {
                var time = keyable.animationData.GetKeyNext(keyable.RootTimeToLocalTimeUnclamped());
                cutscene.currentTime = time + keyable.startTime;
                return;
            }
            if ( cutscene.currentTime == cutscene.length ) {
                cutscene.currentTime = 0;
                return;
            }
            cutscene.currentTime = cutscene.GetPointerTimes().FirstOrDefault(t => t > cutscene.currentTime + 0.01f);
        }

        ///<summary>Steps time backwards to the previous key time</summary>
        void StepBackward() {
            if ( embeddedSurface ) {
                if ( embeddedCurrentFrame != null && embeddedSetCurrentFrame != null ) {
                    embeddedSetCurrentFrame(embeddedCurrentFrame() - 1);
                    return;
                }
                cutscene.currentTime = Mathf.Max(0f, cutscene.currentTime - 1f / Mathf.Max(1, embeddedFrameRate != null ? embeddedFrameRate() : Prefs.frameRate));
                return;
            }
            var keyable = CutsceneUtility.selectedObject as IKeyable;
            if ( keyable != null ) {
                var time = keyable.animationData.GetKeyPrevious(keyable.RootTimeToLocalTimeUnclamped());
                cutscene.currentTime = time + keyable.startTime;
                return;
            }
            if ( cutscene.currentTime == 0 ) {
                cutscene.currentTime = cutscene.length;
                return;
            }
            cutscene.currentTime = cutscene.GetPointerTimes().LastOrDefault(t => t < cutscene.currentTime - 0.01f);
        }

        //Sample the cutscene
        void OnEditorUpdate() {
            if ( embeddedSurface ) {
                return;
            }

            //if cutscene playmode active, it will sample and update itself.
            if ( cutscene == null || cutscene.isActive ) {
                return;
            }

            if ( EditorApplication.isCompiling ) {
                Stop(true);
                return;
            }

            var delta = ( Time.realtimeSinceStartup - editorPreviousTime ) * Time.timeScale;
            delta *= cutscene.playbackSpeed;
            editorPreviousTime = Time.realtimeSinceStartup;

            //Sample at its current time.
            cutscene.Sample();

            //Nothing.
            if ( editorPlaybackState == EditorPlaybackState.Stoped ) {
                return;
            }

            var startTime = 0f;
            var endTime = length;

            if ( Prefs.loopRegionMode ) {
                startTime = Mathf.Max(0, cutscene.playTimeMin);
                endTime = Mathf.Min(length, cutscene.playTimeMax);
            }

            //Playback.
            if ( cutscene.currentTime >= endTime && editorPlaybackState == EditorPlaybackState.PlayingForwards ) {
                if ( editorPlaybackWrapMode == Cutscene.WrapMode.Once ) {
                    Stop(true);
                    return;
                }
                if ( editorPlaybackWrapMode == Cutscene.WrapMode.Loop ) {
                    cutscene.Sample(startTime);
                    cutscene.Sample(startTime + delta);
                    return;
                }
            }

            if ( cutscene.currentTime <= startTime && editorPlaybackState == EditorPlaybackState.PlayingBackwards ) {
                Stop(true);
                return;
            }

            cutscene.currentTime += editorPlaybackState == EditorPlaybackState.PlayingForwards ? delta : -delta;
            cutscene.currentTime = Mathf.Clamp(cutscene.currentTime, startTime, endTime);
            RequestEmbeddedRepaint();
        }


        //...
        void OnSceneGUI(SceneView sceneView) {

            if ( embeddedSurface ) {
                return;
            }

            if ( cutscene == null ) {
                return;
            }

            //Shortcuts for scene gui only
            var e = Event.current;
            if ( e.type == EventType.KeyDown ) {

                if ( e.keyCode == KeyCode.Space && !e.shift ) {
                    GUIUtility.keyboardControl = 0;
                    if ( editorPlaybackState != EditorPlaybackState.Stoped ) { Stop(false); } else { Play(); }
                    e.Use();
                }

                if ( e.keyCode == KeyCode.Comma ) {
                    GUIUtility.keyboardControl = 0;
                    StepBackward();
                    e.Use();
                }

                if ( e.keyCode == KeyCode.Period ) {
                    GUIUtility.keyboardControl = 0;
                    StepForward();
                    e.Use();
                }
            }


            //Forward OnSceneGUI
            if ( cutscene.directables != null ) {
                for ( var i = 0; i < cutscene.directables.Count; i++ ) {
                    var directable = cutscene.directables[i];
                    directable.SceneGUI(CutsceneUtility.selectedObject == directable);
                }
            }
            //

            //No need to show tools of cutscene object, plus handles are shown per clip when required
            Tools.hidden = ( Selection.activeObject == cutscene || Selection.activeGameObject == cutscene.gameObject ) && CutsceneUtility.selectedObject != null;

            //Cutscene Root info and gizmos
            Handles.color = Prefs.gizmosColor;
            Handles.Label(cutscene.transform.position + new Vector3(0, 0.4f, 0), "Cutscene Root");
            Handles.DrawLine(cutscene.transform.position + cutscene.transform.forward, cutscene.transform.position + cutscene.transform.forward * -1);
            Handles.DrawLine(cutscene.transform.position + cutscene.transform.right, cutscene.transform.position + cutscene.transform.right * -1);
            Handles.color = Color.white;

            Handles.BeginGUI();

            if ( cutscene.currentTime > 0 && ( cutscene.currentTime < cutscene.length || !Application.isPlaying ) ) {
                //view frame. Red = scrubbing, yellow = active in playmode
                var cam = sceneView.camera;
                var lineWidth = 3f;
                var top = new Rect(0, 0, cam.pixelWidth, lineWidth);
                var bottom = new Rect(0, cam.pixelHeight - lineWidth - 10, cam.pixelWidth, lineWidth + 10);
                var left = new Rect(0, 0, lineWidth, cam.pixelHeight);
                var right = new Rect(cam.pixelWidth - lineWidth, 0, lineWidth, cam.pixelHeight);
                var texture = whiteTexture;
                GUI.color = cutscene.isActive ? Color.green : Color.red;
                GUI.DrawTexture(top, texture);
                GUI.DrawTexture(bottom, texture);
                GUI.DrawTexture(left, texture);
                GUI.DrawTexture(right, texture);
                //

                //Info
                GUI.color = Color.black.WithAlpha(0.7f);
                if ( cutscene.isActive ) {
                    GUI.Label(bottom, string.Format(" Active '{0}'", cutscene.name), GUIStyle.none);
                } else {
                    GUI.Label(bottom, string.Format(" Previewing '{0}'. Non animatable changes made to actor components will be reverted.", cutscene.name), GUIStyle.none);
                }
            }

            GUI.color = Color.white;
            Handles.EndGUI();
        }

        //...
        void OnGUI() {

            GUI.skin.label.richText = true;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            EditorStyles.label.richText = true;
            EditorStyles.textField.wordWrap = true;
            EditorStyles.foldout.richText = true;
            var e = Event.current;
            mousePosition = e.mousePosition;
            current = this;

            if ( (cutscene == null && embeddedTimeline == null) || isAboutButtonPressed ) {
                ShowWelcome();
                return;
            }

            //avoid edit when compiling
            if ( EditorApplication.isCompiling ) {
                if ( !embeddedSurface )
                    Stop(true);
                ShowNotification(new GUIContent("Compiling\n...Please wait..."));
                return;
            }

            //handle undo/redo shortcuts
            if ( e.type == EventType.ValidateCommand && e.commandName == "UndoRedoPerformed" ) {
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                multiSelection = null;
                if (cutscene != null)
                {
                    cutscene.Validate();
                    InitClipWrappers();
                }
                else
                {
                    embeddedTimeline?.RequestRepaint();
                }
                e.Use();
                return;
            }

            if (embeddedSurface && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && editTransactionActive)
            {
                CancelEditTransaction();
                interactingClip?.ResetInteraction();
                interactingClip = null;
                formalDraggedSection = null;
                e.Use();
                return;
            }

            //prefab editing is not allowed
            if ( isCutsceneAsset ) {
                ShowNotification(new GUIContent("Editing Prefab Assets is not allowed\nPlease add an instance in the scene, or open the prefab for editing"));
                return;
            }

            //remove notifications quickly
            if ( e.type == EventType.MouseDown ) { RemoveNotification(); }

            //button 2 seems buggy
            if ( e.button == 2 && e.type == EventType.MouseDown ) { isMouseButton2Down = true; }
            if ( e.button == 2 && e.rawType == EventType.MouseUp ) { isMouseButton2Down = false; }

            //Record Undo and dirty? This is an overal fallback. Certain actions register undo as well.
            var doRecordUndo = e.rawType == EventType.MouseDown && ( e.button == 0 || e.button == 1 );
            doRecordUndo |= e.type == EventType.DragPerform;
            if ( doRecordUndo && embeddedTimeline == null ) {
                BeginEditTransaction("Cutscene Change", e.button);
                if (ShouldRecordUndo() && cutscene != null) {
                    Undo.RegisterFullObjectHierarchyUndo(cutscene.groupsRoot.gameObject, "Cutscene Change");
                    Undo.RecordObject(cutscene, "Cutscene Change");
                    willDirty = true;
                }
            }

            //reorder clips lists for better UI. This is strictly a UI thing.
            if ( cutscene != null && interactingClip == null && e.type == EventType.Layout ) {
                foreach ( var group in cutscene.groups ) {
                    foreach ( var track in group.tracks ) {
                        track.clips = track.clips.OrderBy(a => a.startTime).ToList();
                    }
                }
            }

            //make the layout rects
            SurfaceLayout layout = CalculateLayout();
            topLeftRect = layout.TopLeft;
            topMiddleRect = layout.TopMiddle;
            leftRect = layout.Left;
            centerRect = layout.Center;

            //...
            DoKeyboardShortcuts();
            if (!embeddedSurface)
            {
                bool guiEnabled = GUI.enabled;
                GUI.enabled = guiEnabled && IsPlaybackAllowedFor(cutscene);
                ShowPlaybackControls(topLeftRect);
                GUI.enabled = guiEnabled;
            }
            else
            {
                ShowEmbeddedAuthoringToolbar(topLeftRect);
            }
            ShowTimeInfo(topMiddleRect);
            if (!embeddedSurface)
                ShowToolbar();
            DoScrubControls();
            DoZoomAndPan();


            //Timelines
            var scrollRect1 = Rect.MinMaxRect(0, centerRect.yMin, screenWidth, screenHeight - 5);
            var scrollRect2 = Rect.MinMaxRect(0, centerRect.yMin, screenWidth, totalHeight + 150);
            scrollPos = GUI.BeginScrollView(scrollRect1, scrollPos, scrollRect2);
            ShowGroupsAndTracksList(leftRect);
            ShowTimeLines(centerRect);
            GUI.EndScrollView();
            ///---

            DrawRuntimeOverlay();
            DrawHistoryOverlay();
            DrawGuides();
            AcceptDrops();

            if ( e.rawType == EventType.MouseUp && editTransactionActive &&
                 (editTransactionButton < 0 || e.button == editTransactionButton) ) {
                if ( ShouldRecordUndo() ) {
                    willDirty = true;
                    willResample = true;
                }
                CommitEditTransaction();
            }


            //Final stuff...

            //clean selection and hotcontrols
            if ( e.type == EventType.MouseDown && e.button == 0 && GUIUtility.hotControl == 0 ) {
                if ( centerRect.Contains(mousePosition) && !formalSelectionHandled ) {
                    if (embeddedTimeline != null)
                        embeddedTimeline.Select(null);
                    else
                        CutsceneUtility.selectedObject = null;
                    multiSelection = null;
                }
                GUIUtility.keyboardControl = 0;
                showDragDropInfo = false;
            }

            //just some info for the user to drag/drop gameobject in editor
            if ( showDragDropInfo && cutscene != null && cutscene.groups.Find(g => g.GetType() == typeof(ActorGroup)) == null ) {
                var label = "Drag & Drop GameObjects or Prefabs in this window to create Actor Groups";
                var size = new GUIStyle("label").CalcSize(new GUIContent(label));
                var notificationRect = new Rect(0, 0, size.x, size.y);
                notificationRect.center = new Vector2(( screenWidth / 2 ) + ( LEFT_MARGIN / 2 ), ( screenHeight / 2 ) + TOP_MARGIN);
                GUI.Label(notificationRect, label);
            }

            //repaint?
            if ( e.type == EventType.MouseDrag || e.type == EventType.MouseUp || GUI.changed ) {
                willRepaint = true;
            }

            //dirty?
            if ( willDirty && ShouldRecordUndo() && cutscene != null ) {
                willDirty = false;
                EditorUtility.SetDirty(cutscene);
                foreach ( var o in cutscene.GetComponentsInChildren(typeof(IDirectable), true).Cast<UnityEngine.Object>() ) {
                    EditorUtility.SetDirty(o);
                }
            }

            //resample?
            if ( willResample ) {
                willResample = false;
                if ( !embeddedSurface ) {
                    //delaycall so that other gui controls are finalized before resample.
                    EditorApplication.delayCall += () => { if ( cutscene != null ) cutscene.ReSample(); };
                }
            }

            //hack to show modal popup windows
            if ( onDoPopup != null ) {
                var temp = onDoPopup;
                onDoPopup = null;
                QuickPopup.Show(temp);
            }

            //if a prefab darken whole UI
            if ( isCutsceneAsset ) {
                GUI.color = Color.black.WithAlpha(0.5f);
                GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), whiteTexture);
                GUI.color = Color.white;
            }

            //cheap ver/hor seperators
            Handles.color = Color.black;
            Handles.DrawLine(new Vector2(0, centerRect.y + 1), new Vector2(centerRect.xMax, centerRect.y + 1));
            Handles.DrawLine(new Vector2(centerRect.x, centerRect.y + 1), new Vector2(centerRect.x, centerRect.yMax));
            Handles.color = Color.white;

            //repaint
            if ( willRepaint ) {
                willRepaint = false;
                RequestEmbeddedRepaint();
            }

            //cleanup
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.skin = null;

            if ( viewTimeMax == 0 ) { GUI.Label(centerRect, "<size=40>:-)</size>", Styles.centerLabel); }
        }

        void DrawRuntimeOverlay()
        {
            if (!embeddedSurface || embeddedRuntimeTime == null)
                return;
            float? runtimeTime = embeddedRuntimeTime();
            if (!runtimeTime.HasValue || runtimeTime.Value < viewTimeMin || runtimeTime.Value > viewTimeMax)
                return;
            float x = TimeToPos(runtimeTime.Value) + centerRect.x;
            GUI.color = new Color(0.25f, 0.85f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(x - 1f, centerRect.y, 2f, centerRect.height), whiteTexture);
            GUI.Label(new Rect(x + 4f, centerRect.y + 2f, 64f, 18f), "Runtime", EditorStyles.label);
            GUI.color = Color.white;
        }

        void DrawHistoryOverlay()
        {
            if (!embeddedSurface || embeddedHistoryTime == null)
                return;
            float? historyTime = embeddedHistoryTime();
            if (!historyTime.HasValue || historyTime.Value < viewTimeMin || historyTime.Value > viewTimeMax)
                return;
            float x = TimeToPos(historyTime.Value) + centerRect.x;
            GUI.color = new Color(0.85f, 0.45f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(x - 1f, centerRect.y, 2f, centerRect.height), whiteTexture);
            GUI.Label(new Rect(x + 4f, centerRect.y + 20f, 64f, 18f), "History", EditorStyles.label);
            GUI.color = Color.white;
        }

        void ShowEmbeddedAuthoringToolbar(Rect rect)
        {
            GUI.Box(rect, string.Empty, EditorStyles.toolbar);
            GUI.BeginGroup(rect);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("+ Track", EditorStyles.toolbarButton, GUILayout.Width(62)))
                embeddedAddTrack?.Invoke();
            if (GUILayout.Button("‹", EditorStyles.toolbarButton, GUILayout.Width(24)))
                StepBackward();
            if (GUILayout.Button("›", EditorStyles.toolbarButton, GUILayout.Width(24)))
                StepForward();
            if (GUILayout.Button("Fit", EditorStyles.toolbarButton, GUILayout.Width(36)))
            {
                viewTimeMin = 0f;
                viewTimeMax = Mathf.Max(length, 1f / Mathf.Max(1, embeddedFrameRate != null ? embeddedFrameRate() : Prefs.frameRate));
            }
            int authoringFrame = embeddedCurrentFrame != null
                ? embeddedCurrentFrame()
                : Mathf.RoundToInt(cutscene.currentTime * Mathf.Max(1, embeddedFrameRate != null ? embeddedFrameRate() : Prefs.frameRate));
            if (embeddedCurrentFrame != null && embeddedSetCurrentFrame != null)
            {
                GUILayout.Label("Edit", EditorStyles.miniLabel);
                int requestedFrame = EditorGUILayout.IntField(authoringFrame, GUILayout.Width(52));
                if (requestedFrame != authoringFrame)
                    embeddedSetCurrentFrame(requestedFrame);
                GUILayout.Label("F", EditorStyles.miniLabel);
            }
            else
                GUILayout.Label($"Edit  {authoringFrame}F", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
            GUI.EndGroup();
        }

        ///----------------------------------------------------------------------------------------------

        //...		
        void DoKeyboardShortcuts() {

            var e = Event.current;
            if ( e.type == EventType.KeyDown && GUIUtility.keyboardControl == 0 && !e.control && !e.shift ) {

                //play
                if ( e.keyCode == KeyCode.Space ) {
                    if ( embeddedSurface ) {
                        e.Use();
                        return;
                    }
                    if ( editorPlaybackState != EditorPlaybackState.Stoped ) { Stop(false); } else { Play(); }
                    e.Use();
                }

                //step forw
                if ( e.keyCode == KeyCode.Period ) {
                    StepForward();
                    e.Use();
                }

                //step back
                if ( e.keyCode == KeyCode.Comma ) {
                    StepBackward();
                    e.Use();
                }

                //key at scrubber
                if ( e.keyCode == KeyCode.K ) {
                    if (embeddedTimeline != null && embeddedTimeline.Selected is IEmbeddedTimelineClipBinding formalClip)
                        ApplyEmbeddedCommand(() => formalClip.AddIdentityKey(Mathf.Clamp(EmbeddedCurrentTime() - formalClip.StartTime, 0f, formalClip.Length)), "Key Clip");
                    else if ( CutsceneUtility.selectedObject is IKeyable keyable )
                        SafeDoAction(() => keyable.TryAddIdentityKey(keyable.RootTimeToLocalTime()));
                    e.Use();
                }

                //split at scrubber
                if ( e.keyCode == KeyCode.S ) {
                    if (embeddedTimeline != null && embeddedTimeline.Selected is IEmbeddedTimelineClipBinding formalClip)
                        embeddedTimeline.SplitClip(formalClip, Mathf.RoundToInt(EmbeddedCurrentTime() * embeddedTimeline.FrameRate));
                    else if (CutsceneUtility.selectedObject is ActionClip clip) {
                        var wrapper = clipWrappersMap[clip];
                        SafeDoAction(() => wrapper?.Split(cutscene.currentTime));
                    }
                    e.Use();
                }

                //strech fit
                if ( e.keyCode == KeyCode.F ) {
                    if (embeddedTimeline != null && embeddedTimeline.Selected is IEmbeddedTimelineClipBinding formalClip)
                        ApplyEmbeddedCommand(formalClip.StretchFit, "Fit Clip");
                    else if (CutsceneUtility.selectedObject is ActionClip clip) {
                        var wrapper = clipWrappersMap[clip];
                        SafeDoAction(() => wrapper?.StretchFit());
                    }
                    e.Use();
                }

                //clean off range keys
                if ( e.keyCode == KeyCode.C ) {
                    if (embeddedTimeline != null && embeddedTimeline.Selected is IEmbeddedTimelineClipBinding formalClip)
                        ApplyEmbeddedCommand(formalClip.CleanKeysOffRange, "Clean Keys");
                    else if (CutsceneUtility.selectedObject is ActionClip clip) {
                        var wrapper = clipWrappersMap[clip];
                        SafeDoAction(() => wrapper?.CleanKeysOffRange());
                    }
                    e.Use();
                }

                //delete
                if ( e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace ) {
                    if (embeddedTimeline != null && multiSelection != null)
                    {
                        var selectedFormalClips = multiSelection
                            .Select(value => value.editorBinding.FormalClip)
                            .Where(value => value != null)
                            .ToArray();
                        if (selectedFormalClips.Length != 0)
                            embeddedTimeline.DeleteClips(selectedFormalClips);
                        multiSelection = null;
                        e.Use();
                    }
                    else if (embeddedTimeline != null && embeddedTimeline.Selected is IEmbeddedTimelineClipBinding formalClip)
                    {
                        embeddedTimeline.DeleteClip(formalClip);
                        e.Use();
                    }
                    else if ( multiSelection != null ) {
                        SafeDoAction(() =>
                           {
                               foreach ( var act in multiSelection.Select(b => b.action).ToArray() ) {
                                   ( act.parent as CutsceneTrack ).DeleteAction(act);
                               }
                               InitClipWrappers();
                               multiSelection = null;
                           });
                        e.Use();
                    } else {
                        var clip = CutsceneUtility.selectedObject as ActionClip;
                        if ( clip != null ) {
                            SafeDoAction(() => { ( clip.parent as CutsceneTrack ).DeleteAction(clip); InitClipWrappers(); });
                            e.Use();
                        }
                    }
                }
            }
        }

        //...
        void DrawGuides() {

            //draw a vertical line at 0 time
            DrawGuideLine(0, isProSkin ? Color.white : Color.black);

            //draw a vertical line at length time
            DrawGuideLine(length, isProSkin ? Color.white : Color.black);

            //draw a vertical line at current time
            var embeddedCurrentTime = EmbeddedCurrentTime();
            if ( embeddedCurrentTime > 0 ) {
                DrawGuideLine(embeddedCurrentTime, scruberColor);
            }

            //draw a vertical line at dragging clip start/end time
            if ( interactingClip != null ) {
                if ( interactingClip.isDragging || interactingClip.isScalingStart ) {
                    DrawGuideLine(interactingClip.editorBinding.StartTime, Color.white.WithAlpha(0.05f));
                }
                if ( interactingClip.isDragging || interactingClip.isScalingEnd ) {
                    DrawGuideLine(interactingClip.editorBinding.EndTime, Color.white.WithAlpha(0.05f));
                }
            }

            //draw a vertical line at dragging section
            if ( draggedSection != null ) {
                DrawGuideLine(draggedSection.time, draggedSection.color);
            }

            //draw guide at cutscene runtime play min/max
            if ( cutscene != null && cutscene.isActive ) {
                if ( cutscene.playTimeMin > 0 ) {
                    DrawGuideLine(cutscene.playTimeMin, Color.red);
                }
                if ( cutscene.playTimeMax < length ) {
                    DrawGuideLine(cutscene.playTimeMax, Color.red);
                }
            }

            //draw guide when moving loop region start/end
            if ( cutscene != null && (isMovingLoopRegionMax || isMovingLoopRegionMin) ) {
                DrawGuideLine(cutscene.playTimeMax, Color.white.WithAlpha(0.05f));
                DrawGuideLine(cutscene.playTimeMin, Color.white.WithAlpha(0.05f));
            }

            //draw other "subscribed" guidelines
            for ( var i = 0; i < pendingGuides.Count; i++ ) { DrawGuideLine(pendingGuides[i].time, pendingGuides[i].color); }
            pendingGuides.Clear();
        }

        //...
        void AcceptDrops() {

            if (embeddedTimeline != null)
                return;

            if ( cutscene.currentTime > 0 ) {
                return;
            }

            var e = Event.current;
            if ( e.type == EventType.DragUpdated && DragAndDrop.objectReferences?[0] is GameObject ) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            }

            if ( e.type == EventType.DragPerform ) {
                for ( int i = 0; i < DragAndDrop.objectReferences.Length; i++ ) {
                    var o = DragAndDrop.objectReferences[i];
                    if ( o is GameObject go ) {
                        if ( go.GetComponent<DirectorCamera>() != null ) {
                            ShowNotification(new GUIContent("The 'DIRECTOR' group is already used for the 'DirectorCamera' object"));
                            continue;
                        }

                        if ( cutscene.GetAffectedActors().Contains(go) ) {
                            ShowNotification(new GUIContent(string.Format("GameObject '{0}' is already in the cutscene", o.name)));
                            continue;
                        }

                        DragAndDrop.AcceptDrag();
                        var newGroup = cutscene.AddGroup<ActorGroup>(go);
                        newGroup.AddTrack<ActorActionTrack>("Action Track");
                        CutsceneUtility.selectedObject = newGroup;
                    }
                }
            }
        }

        //The toolbar...
        void ShowToolbar() {

            if ( !isProSkin ) { GUI.contentColor = Color.black.WithAlpha(0.7f); }

            GUI.enabled = cutscene.currentTime <= 0;

            var e = Event.current;

            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if ( GUILayout.Button(string.Format("[{0}]", cutscene.name), EditorStyles.toolbarDropDown, GUILayout.Width(100)) ) {
                static void SelectCutscene(object cut) {
                    Selection.activeObject = (Cutscene)cut;
                    EditorGUIUtility.PingObject((Cutscene)cut);
                }

                var cutscenes = UnityObjectUtility.FindObjectsByType<Cutscene>();
                var menu = new GenericMenu();
                foreach ( Cutscene cut in cutscenes ) {
                    menu.AddItem(new GUIContent(string.Format("[{0}]", cut.name)), cut == cutscene, SelectCutscene, cut);
                }
                menu.ShowAsContext();
            }

            if ( GUILayout.Button("Select", EditorStyles.toolbarButton, GUILayout.Width(60)) ) {
                Selection.activeObject = cutscene;
                EditorGUIUtility.PingObject(cutscene);
            }

            if ( GUILayout.Button("Render", EditorStyles.toolbarButton, GUILayout.Width(60)) ) {
                RenderWindow.Open();
            }

            if ( GUILayout.Button("Snap: " + Prefs.snapInterval.ToString(), EditorStyles.toolbarDropDown, GUILayout.Width(90)) ) {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("0.001"), false, () => { Prefs.timeStepMode = Prefs.TimeStepMode.Seconds; Prefs.frameRate = 1000; });
                menu.AddItem(new GUIContent("0.01"), false, () => { Prefs.timeStepMode = Prefs.TimeStepMode.Seconds; Prefs.frameRate = 100; });
                menu.AddItem(new GUIContent("0.1"), false, () => { Prefs.timeStepMode = Prefs.TimeStepMode.Seconds; Prefs.frameRate = 10; });
                menu.AddItem(new GUIContent("24 FPS"), false, () => { Prefs.timeStepMode = Prefs.TimeStepMode.Frames; Prefs.frameRate = 24; });
                menu.AddItem(new GUIContent("30 FPS"), false, () => { Prefs.timeStepMode = Prefs.TimeStepMode.Frames; Prefs.frameRate = 30; });
                menu.AddItem(new GUIContent("60 FPS"), false, () => { Prefs.timeStepMode = Prefs.TimeStepMode.Frames; Prefs.frameRate = 60; });
                menu.ShowAsContext();
            }

            GUILayout.Space(10);

            Prefs.magnetSnapping = GUILayout.Toggle(Prefs.magnetSnapping, new GUIContent(Styles.magnetIcon, "Clips Magnet Snapping"), EditorStyles.toolbarButton);
            Prefs.rippleMode = GUILayout.Toggle(Prefs.rippleMode, new GUIContent(Styles.rippleIcon, "Ripple moving of next clips and keyframes (Shift)"), EditorStyles.toolbarButton);
            Prefs.retimeMode = GUILayout.Toggle(Prefs.retimeMode, new GUIContent(Styles.retimeIcon, "Retiming of keyframes when scaling clips"), EditorStyles.toolbarButton);

            GUILayout.Space(10);

            Prefs.loopRegionMode = GUILayout.Toggle(Prefs.loopRegionMode, new GUIContent(Styles.loopIcon, "Loop Region"), EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            if ( !Prefs.autoKey ) {
                var wasEnabled = GUI.enabled;
                GUI.enabled = true;
                var changedParams = CutsceneUtility.changedParameterCallbacks;
                var hasChangedParams = changedParams != null && changedParams.Count > 0;
                GUI.color = hasChangedParams ? Color.white : Color.clear;
                GUILayout.BeginHorizontal();
                if ( hasChangedParams ) {
                    GUI.backgroundColor = Color.clear;
                    GUI.color = Color.green;
                    var b1 = GUILayout.Button(Styles.keyIcon, EditorStyles.toolbarButton);
                    GUI.color = Color.white;
                    var b2 = GUILayout.Button(string.Format("Key ({0}) Changed Parameters", changedParams.Count), EditorStyles.toolbarButton);
                    GUI.backgroundColor = Color.white;
                    if ( b1 || b2 ) {
                        foreach ( var pair in changedParams ) {
                            pair.Value.Commit();
                        }
                    }
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUI.enabled = wasEnabled;
            }
            GUILayout.FlexibleSpace();


            GUI.color = Color.white.WithAlpha(0.4f);
            if ( GUILayout.Button(string.Format("SLATE Cinematic Sequencer v{0}", Cutscene.VERSION_NUMBER.ToString("0.00")), EditorStyles.toolbarButton) ) {
                Help.BrowseURL("https://slate.paradoxnotion.com");
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUI.color = Color.white;

            if ( GUILayout.Button(Slate.Styles.gearIcon, EditorStyles.toolbarButton, GUILayout.Width(26)) ) {
                PreferencesWindow.Show(new Rect(screenWidth - 5 - 400, TOOLBAR_HEIGHT + 5, 400, screenHeight - TOOLBAR_HEIGHT - 50));
            }

            isAboutButtonPressed = GUILayout.Toggle(isAboutButtonPressed, Styles.helpIcon, EditorStyles.toolbarButton);

            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            GUI.enabled = true;
            GUI.contentColor = Color.white;
        }

        //Scrubing....
        void DoScrubControls() {

            if ( !embeddedSurface && !IsPlaybackAllowedFor(cutscene) ) {
                return;
            }

            if ( !embeddedSurface && cutscene.isActive ) { //no scrubbing if playing in runtime
                return;
            }

            //
            var e = Event.current;
            if ( e.type == EventType.MouseDown && topMiddleRect.Contains(mousePosition) ) {
                var endCarretPos = TimeToPos(length) + leftRect.width;
                var loopRegionMaxPos = !embeddedSurface ? TimeToPos(cutscene.playTimeMax) + leftRect.width : 0f;
                var loopRegionMinPos = !embeddedSurface ? TimeToPos(cutscene.playTimeMin) + leftRect.width : 0f;

                var isEndCarret = embeddedLength == null && (Mathf.Abs(mousePosition.x - endCarretPos) < 10 || e.control);
                var isRegionMax = !embeddedSurface && Prefs.loopRegionMode && Mathf.Abs(mousePosition.x - loopRegionMaxPos) < 10;
                var isRegionMin = !embeddedSurface && Prefs.loopRegionMode && Mathf.Abs(mousePosition.x - loopRegionMinPos) < 10;

                if ( isEndCarret || isRegionMax || isRegionMin ) {
                    CacheMagnetSnapTimes();
                }

                if ( e.button == 0 ) {
                    isMovingLoopRegionMax = isRegionMax;
                    isMovingLoopRegionMin = isRegionMin && !isMovingLoopRegionMax;
                    isMovingEndCarret = isEndCarret && !isMovingLoopRegionMax;
                    isMovingScrubCarret = !isMovingEndCarret && !isMovingLoopRegionMax && !isMovingLoopRegionMin;
                    if ( isMovingScrubCarret && !embeddedSurface ) {
                        Pause();
                    }
                }

                if ( e.button == 1 && isEndCarret && cutscene.directables != null ) {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Set To Last Clip Time"), false, () =>
                        {
                            var lastClip = cutscene.directables.Where(d => d is ActionClip).OrderBy(d => d.endTime).LastOrDefault();
                            if ( lastClip != null ) {
                                length = lastClip.endTime;
                            }
                        });
                    menu.ShowAsContext();
                }

                e.Use();
            }

            if ( e.button == 0 && e.rawType == EventType.MouseUp ) {
                isMovingScrubCarret = false;
                isMovingEndCarret = false;
                isMovingLoopRegionMax = false;
                isMovingLoopRegionMin = false;
            }

            var pointerTime = PosToTime(mousePosition.x);
            if ( isMovingScrubCarret ) {
                var scrubTime = Mathf.Clamp(SnapTime(pointerTime), Mathf.Max(viewTimeMin, 0) + float.Epsilon, length - float.Epsilon);
                SetEmbeddedCurrentTime(scrubTime);
            }

            if ( isMovingEndCarret ) {
                length = SnapTime(pointerTime);
                var magnetSnap = MagnetSnapTime(length, magnetSnapTimesCache);
                length = magnetSnap != null ? magnetSnap.Value : length;
                length = Mathf.Clamp(length, viewTimeMin + float.Epsilon, viewTimeMax - float.Epsilon);
            }

            if ( isMovingLoopRegionMax ) {
                cutscene.playTimeMax = SnapTime(pointerTime);
                var magnetSnap = MagnetSnapTime(cutscene.playTimeMax, magnetSnapTimesCache);
                cutscene.playTimeMax = magnetSnap != null ? magnetSnap.Value : cutscene.playTimeMax;
                cutscene.playTimeMax = Mathf.Clamp(cutscene.playTimeMax, viewTimeMin + float.Epsilon, viewTimeMax - float.Epsilon);
            }

            if ( isMovingLoopRegionMin ) {
                cutscene.playTimeMin = SnapTime(pointerTime);
                var magnetSnap = MagnetSnapTime(cutscene.playTimeMin, magnetSnapTimesCache);
                cutscene.playTimeMin = magnetSnap != null ? magnetSnap.Value : cutscene.playTimeMin;
                cutscene.playTimeMin = Mathf.Clamp(cutscene.playTimeMin, viewTimeMin + float.Epsilon, viewTimeMax - float.Epsilon);
            }
        }

        //...
        void DoZoomAndPan() {

            if ( !centerRect.Contains(mousePosition) ) {
                return;
            }

            var e = Event.current;
            //Zoom or scroll down/up if prefs is set to scrollwheel
            if ( ( e.type == EventType.ScrollWheel && Prefs.scrollWheelZooms ) || ( e.alt && !e.shift && e.button == 1 ) ) {
                this.AddCursorRect(centerRect, MouseCursor.Zoom);
                if ( e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel ) {
                    var pointerTimeA = PosToTime(mousePosition.x);
                    var delta = e.alt ? -e.delta.x * 0.1f : e.delta.y;
                    var t = ( Mathf.Abs(delta * 25) / centerRect.width ) * viewTime;
                    viewTimeMin += delta > 0 ? -t : t;
                    viewTimeMax += delta > 0 ? t : -t;
                    var pointerTimeB = PosToTime(mousePosition.x + e.delta.x);
                    var diff = pointerTimeA - pointerTimeB;
                    viewTimeMin += diff;
                    viewTimeMax += diff;
                    e.Use();
                }
            }

            //pan left/right, up/down
            if ( isMouseButton2Down || ( e.alt && !e.shift && e.button == 0 ) ) {
                this.AddCursorRect(centerRect, MouseCursor.Pan);
                if ( e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp ) {
                    var t = ( Mathf.Abs(e.delta.x) / centerRect.width ) * viewTime;
                    viewTimeMin += e.delta.x > 0 ? -t : t;
                    viewTimeMax += e.delta.x > 0 ? -t : t;
                    scrollPos.y -= e.delta.y;
                    e.Use();
                }
            }
        }

        void ShowPlaybackControls(Rect topLeftRect) {

            var autoKeyRect = new Rect(topLeftRect.xMin + 10, topLeftRect.yMin + 4, 32, 32);
            AddCursorRect(autoKeyRect, MouseCursor.Link);
            GUI.backgroundColor = Prefs.autoKey ? Color.black.WithAlpha(0.5f) : Color.grey.WithAlpha(0.5f);
            GUI.Box(autoKeyRect, string.Empty, Styles.clipBoxStyle);
            GUI.color = Prefs.autoKey ? new Color(1, 0.4f, 0.4f) : Color.white;
            GUI.backgroundColor = Color.clear;
            if ( GUI.Button(autoKeyRect, Styles.keyIcon, (GUIStyle)"box") ) {
                Prefs.autoKey = !Prefs.autoKey;
                ShowNotification(new GUIContent(string.Format("AutoKey {0}", Prefs.autoKey ? "Enabled" : "Disabled"), Styles.keyIcon));
            }
            var autoKeyLabelRect = autoKeyRect;
            autoKeyLabelRect.yMin += 16;
            GUI.backgroundColor = Color.white;
            GUI.Label(autoKeyLabelRect, "<color=#AAAAAA>Auto</color>", Styles.centerLabel);
            GUI.color = Color.white;


            if ( !isProSkin ) { GUI.contentColor = Color.black.WithAlpha(0.7f); }

            //Cutscene shows the gui
            GUILayout.BeginArea(topLeftRect);

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Rect lastRect;
            if ( GUILayout.Button(Styles.stepReverseIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20)) ) {
                StepBackward();
                Event.current.Use();
            }
            lastRect = GUILayoutUtility.GetLastRect();
            if ( lastRect.Contains(Event.current.mousePosition) ) { AddCursorRect(lastRect, MouseCursor.Link); }


            var isStoped = Application.isPlaying ? ( cutscene.isPaused || !cutscene.isActive ) : editorPlaybackState == EditorPlaybackState.Stoped;
            if ( isStoped ) {
                if ( GUILayout.Button(Styles.playReverseIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20)) ) {
                    PlayReverse();
                    Event.current.Use();
                }
                lastRect = GUILayoutUtility.GetLastRect();
                if ( lastRect.Contains(Event.current.mousePosition) ) { AddCursorRect(lastRect, MouseCursor.Link); }
                if ( GUILayout.Button(Styles.playIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20)) ) {
                    Play();
                    Event.current.Use();
                }
                lastRect = GUILayoutUtility.GetLastRect();
                if ( lastRect.Contains(Event.current.mousePosition) ) { AddCursorRect(lastRect, MouseCursor.Link); }
            } else {
                if ( GUILayout.Button(Styles.pauseIcon, (GUIStyle)"box", GUILayout.Width(44), GUILayout.Height(20)) ) {
                    Pause();
                    Event.current.Use();
                }
                lastRect = GUILayoutUtility.GetLastRect();
                if ( lastRect.Contains(Event.current.mousePosition) ) { AddCursorRect(lastRect, MouseCursor.Link); }
            }


            if ( GUILayout.Button(Styles.stopIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20)) ) {
                Stop(false);
                Event.current.Use();
            }
            lastRect = GUILayoutUtility.GetLastRect();
            if ( lastRect.Contains(Event.current.mousePosition) ) { AddCursorRect(lastRect, MouseCursor.Link); }

            if ( GUILayout.Button(Styles.stepIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20)) ) {
                StepForward();
                Event.current.Use();
            }
            lastRect = GUILayoutUtility.GetLastRect();
            if ( lastRect.Contains(Event.current.mousePosition) ) { AddCursorRect(lastRect, MouseCursor.Link); }

            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndArea();

            GUI.contentColor = Color.white;
        }


        //top mid - viewTime selection and time info
        void ShowTimeInfo(Rect topMiddleRect) {

            GUI.color = Color.white.WithAlpha(0.2f);
            GUI.Box(topMiddleRect, string.Empty, EditorStyles.toolbarButton);
            GUI.color = Color.black.WithAlpha(0.2f);
            GUI.Box(topMiddleRect, string.Empty, Styles.timeBoxStyle);
            GUI.color = Color.white;

            timeInfoInterval = 1000000f;
            timeInfoHighMod = timeInfoInterval;
            var lowMod = 0.01f;
            var doFrames = embeddedSurface || Prefs.timeStepMode == Prefs.TimeStepMode.Frames;
            var frameRate = Mathf.Max(1, embeddedFrameRate != null ? embeddedFrameRate() : Prefs.frameRate);
            if ( embeddedSurface ) {
                var frameIntervals = new[] { 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1200, 3000, 6000 };
                var selectedInterval = frameIntervals[frameIntervals.Length - 1];
                var selectedHighInterval = selectedInterval;
                for ( var i = 0; i < frameIntervals.Length; i++ ) {
                    var count = viewTime * frameRate / frameIntervals[i];
                    if ( centerRect.width / Mathf.Max(1f, count) > 50 ) {
                        selectedInterval = frameIntervals[i];
                        selectedHighInterval = i < frameIntervals.Length - 1 ? frameIntervals[i + 1] : selectedInterval;
                        break;
                    }
                }
                timeInfoInterval = selectedInterval / (float)frameRate;
                timeInfoHighMod = selectedHighInterval / (float)frameRate;
                lowMod = 1f / frameRate;
            }
            else {
                var modulos = new float[] { 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 50000, 100000, 250000, 500000 }; //... O.o
                for ( var i = 0; i < modulos.Length; i++ ) {
                    var count = viewTime / modulos[i];
                    if ( centerRect.width / count > 50 ) { //50 is approx width of label
                        timeInfoInterval = modulos[i];
                        lowMod = i > 0 ? modulos[i - 1] : lowMod;
                        timeInfoHighMod = i < modulos.Length - 1 ? modulos[i + 1] : timeInfoHighMod;
                        break;
                    }
                }
            }

            var timeStep = doFrames ? ( 1f / frameRate ) : lowMod;

            if ( embeddedSurface ) {
                timeInfoStart = Mathf.Floor(viewTimeMin * frameRate / (timeInfoInterval * frameRate)) * timeInfoInterval;
                timeInfoEnd = Mathf.Ceil(viewTimeMax * frameRate / (timeInfoInterval * frameRate)) * timeInfoInterval;
            }
            else {
                timeInfoStart = (float)Mathf.FloorToInt(viewTimeMin / timeInfoInterval) * timeInfoInterval;
                timeInfoEnd = (float)Mathf.CeilToInt(viewTimeMax / timeInfoInterval) * timeInfoInterval;
                timeInfoStart = Mathf.Round(timeInfoStart * 10) / 10;
                timeInfoEnd = Mathf.Round(timeInfoEnd * 10) / 10;
            }

            GUI.BeginGroup(topMiddleRect);
            {
                //the minMax slider
                var _timeMin = viewTimeMin;
                var _timeMax = viewTimeMax;
                var sliderRect = new Rect(5, 0, topMiddleRect.width - 10, 18);
                EditorGUI.MinMaxSlider(sliderRect, ref _timeMin, ref _timeMax, 0, maxTime);
                viewTimeMin = _timeMin;
                viewTimeMax = _timeMax;
                if ( sliderRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2 ) {
                    viewTimeMin = 0;
                    viewTimeMax = length;
                }

                GUI.color = Color.white.WithAlpha(0.1f);
                GUI.DrawTexture(Rect.MinMaxRect(0, TOP_MARGIN - 1, topMiddleRect.xMax, TOP_MARGIN), Styles.whiteTexture);
                GUI.color = Color.white;

                //the step interval
                if ( centerRect.width / ( viewTime / timeStep ) > 6 ) {
                    for ( var i = timeInfoStart; i <= timeInfoEnd; i += timeStep ) {
                        var posX = TimeToPos(i);
                        var frameRect = Rect.MinMaxRect(posX - 1, TOP_MARGIN - 2, posX + 1, TOP_MARGIN - 1);
                        GUI.color = isProSkin ? Color.white : Color.black;
                        GUI.DrawTexture(frameRect, whiteTexture);
                        GUI.color = Color.white;
                    }
                }

                //the time interval
                for ( var i = timeInfoStart; i <= timeInfoEnd; i += timeInfoInterval ) {

                    var posX = TimeToPos(i);
                    var rounded = embeddedSurface ? Mathf.Round(i * frameRate) / frameRate : Mathf.Round(i * 10) / 10;

                    GUI.color = isProSkin ? Color.white : Color.black;
                    var markRect = Rect.MinMaxRect(posX - 2, TOP_MARGIN - 3, posX + 2, TOP_MARGIN - 1);
                    GUI.DrawTexture(markRect, whiteTexture);
                    GUI.color = Color.white;

                    var text = doFrames
                        ? Mathf.RoundToInt(rounded * frameRate).ToString("0") + "F"
                        : rounded.ToString("0.00");
                    var size = GUI.skin.GetStyle("label").CalcSize(new GUIContent(text));
                    var stampRect = new Rect(0, 0, size.x, size.y);
                    stampRect.center = new Vector2(posX, TOP_MARGIN - size.y + 2);
                    var isMajor = doFrames
                        ? Mathf.RoundToInt(rounded * frameRate) % Mathf.Max(1, Mathf.RoundToInt(timeInfoHighMod * frameRate)) == 0
                        : rounded % timeInfoHighMod == 0;
                    GUI.color = isMajor ? Color.white : Color.white.WithAlpha(0.5f);
                    GUI.Box(stampRect, text, (GUIStyle)"label");
                    GUI.color = Color.white;
                }

                //the number showing current time when scubing
                var embeddedCurrentTime = EmbeddedCurrentTime();
                if ( embeddedCurrentTime > 0 ) {
                    var label = doFrames
                        ? Mathf.RoundToInt(embeddedCurrentTime * frameRate).ToString("0") + "F"
                        : embeddedCurrentTime.ToString("0.00");
                    var text = "<b><size=17>" + label + "</size></b>";
                    var size = Styles.headerBoxStyle.CalcSize(new GUIContent(text));
                    var posX = TimeToPos(embeddedCurrentTime);
                    var stampRect = new Rect(0, 0, size.x, size.y);
                    stampRect.center = new Vector2(posX, TOP_MARGIN - size.y / 2);

                    GUI.backgroundColor = isProSkin ? Color.black.WithAlpha(0.4f) : Color.black.WithAlpha(0.7f);
                    GUI.color = scruberColor;
                    GUI.Box(stampRect, text, Styles.headerBoxStyle);
                }

                //the length position carret texture and pre-exit length indication
                var lengthPos = TimeToPos(length);
                var lengthRect = new Rect(0, 0, 16, 16);
                lengthRect.center = new Vector2(lengthPos, TOP_MARGIN - 2);
                GUI.color = isProSkin ? Color.white : Color.black;
                GUI.DrawTexture(lengthRect, Styles.carretIcon);
                GUI.color = Color.white;

                if ( !embeddedSurface && Prefs.loopRegionMode && !Application.isPlaying ) {
                    //the loop region min
                    var lrMinPos = TimeToPos(cutscene.playTimeMin);
                    var lrMinRect = new Rect(0, 0, 16, 16);
                    lrMinRect.center = new Vector2(lrMinPos, TOP_MARGIN);
                    GUI.color = isProSkin ? Color.white : Color.black;
                    GUI.DrawTexture(lrMinRect, Styles.carretInIcon);
                    GUI.color = Color.white;

                    //the loop region max
                    var lrMaxPos = TimeToPos(cutscene.playTimeMax);
                    var lrMaxRect = new Rect(0, 0, 16, 16);
                    lrMaxRect.center = new Vector2(lrMaxPos, TOP_MARGIN);
                    GUI.color = isProSkin ? Color.white : Color.black;
                    GUI.DrawTexture(lrMaxRect, Styles.carretOutIcon);
                    GUI.color = Color.white;

                    GUI.DrawTexture(Rect.MinMaxRect(lrMinRect.center.x, lrMinRect.center.y - 1, lrMaxRect.center.x, lrMinRect.center.y), Texture2D.whiteTexture);
                }

            }
            GUI.EndGroup();
        }


        //left - the groups and tracks info and option per group/track
        void ShowGroupsAndTracksList(Rect leftRect) {

            if (embeddedTimeline != null)
            {
                ShowGroupsAndTracksList(leftRect, embeddedTimeline);
                return;
            }

            var e = Event.current;

            //allow resize list width
            var scaleRect = new Rect(leftRect.xMax - 4, leftRect.yMin, 4, leftRect.height);
            AddCursorRect(scaleRect, MouseCursor.ResizeHorizontal);
            if ( e.type == EventType.MouseDown && e.button == 0 && scaleRect.Contains(e.mousePosition) ) { isResizingLeftMargin = true; e.Use(); }
            if ( isResizingLeftMargin ) { LEFT_MARGIN = e.mousePosition.x + 2; }
            if ( e.rawType == EventType.MouseUp ) { isResizingLeftMargin = false; }

            GUI.enabled = EmbeddedCurrentTime() <= 0;

            //starting height && search.
            var nextYPos = FIRST_GROUP_TOP_MARGIN;
            var wasEnabled = GUI.enabled;
            GUI.enabled = true;
            var collapseAllRect = Rect.MinMaxRect(leftRect.x + 5, leftRect.y + 4, 20, leftRect.y + 20 - 1);
            var searchRect = Rect.MinMaxRect(leftRect.x + 20, leftRect.y + 4, leftRect.xMax - 18, leftRect.y + 20 - 1);
            var searchCancelRect = Rect.MinMaxRect(searchRect.xMax, searchRect.y, leftRect.xMax - 4, searchRect.yMax);
            var anyExpanded = cutscene.groups.Any(g => !g.isCollapsed);
            AddCursorRect(collapseAllRect, MouseCursor.Link);
            GUI.color = Color.white.WithAlpha(0.5f);
            if ( GUI.Button(collapseAllRect, anyExpanded ? "▼" : "►", (GUIStyle)"label") ) {
                foreach ( var group in cutscene.groups ) {
                    group.isCollapsed = anyExpanded;
                }
            }

            GUI.color = Color.white;
            var searchFieldText = (GUIStyle)"ToolbarSearchTextField";
            var searchFieldCancel = (GUIStyle)"ToolbarSearchCancelButton";
            searchString = EditorGUI.TextField(searchRect, searchString, searchFieldText);
            if ( GUI.Button(searchCancelRect, string.Empty, searchFieldCancel) ) {
                searchString = string.Empty;
                GUIUtility.keyboardControl = 0;
            }
            GUI.enabled = wasEnabled;


            //begin area for left Rect
            GUI.BeginGroup(leftRect);
            ShowListGroups(e, ref nextYPos);
            GUI.EndGroup();

            //store total height required
            totalHeight = nextYPos;


            //Simple button to add empty group for convenience
            if ( !embeddedSurface ) {
                var addButtonY = totalHeight + TOP_MARGIN + TOOLBAR_HEIGHT + 20;
                var addRect = Rect.MinMaxRect(leftRect.xMin + 10, addButtonY, leftRect.xMax - 10, addButtonY + 20);
                GUI.color = Color.white.WithAlpha(0.5f);
                if ( GUI.Button(addRect, "Add Actor Group") ) {
                    var newGroup = cutscene.AddGroup<ActorGroup>(null).AddTrack<ActorActionTrack>();
                    CutsceneUtility.selectedObject = newGroup;
                }
            }

            //clear picks
            if ( e.rawType == EventType.MouseUp ) {
                pickedGroup = null;
                pickedTrack = null;
            }

            GUI.enabled = true;
            GUI.color = Color.white;
        }

        void DrawGroupListHeaderFrame(
            Rect groupRect,
            string title,
            bool active,
            bool selected,
            bool collapsed,
            Color activeColor,
            Action<bool> setCollapsed)
        {
            GUI.color = selected ? LIST_SELECTION_COLOR : GROUP_COLOR;
            GUI.Box(groupRect, string.Empty, Styles.headerBoxStyle);
            GUI.color = active ? activeColor : Color.grey;
            Rect foldRect = new Rect(groupRect.x + 2, groupRect.y + 1, 20, groupRect.height);
            setCollapsed(!EditorGUI.Foldout(foldRect, !collapsed, title));
            GUI.color = Color.white;
        }

        void DrawTrackListRowFrame(
            Rect trackRect,
            bool active,
            bool selected,
            Color trackColor,
            Action drawInfo)
        {
            GUI.color = ColorUtility.Grey(isProSkin ? (active ? 0.25f : 0.2f) : (active ? 0.9f : 0.8f));
            GUI.DrawTexture(trackRect, whiteTexture);
            GUI.color = Color.white.WithAlpha(0.25f);
            GUI.Box(trackRect, string.Empty, (GUIStyle)"flow node 0");
            if (selected)
            {
                GUI.color = LIST_SELECTION_COLOR;
                GUI.DrawTexture(trackRect, whiteTexture);
            }
            if (active && trackColor != Color.white && trackColor.a > 0.2f)
            {
                GUI.color = trackColor;
                GUI.DrawTexture(new Rect(trackRect.xMax + 1, trackRect.yMin, 2, trackRect.height), whiteTexture);
            }
            GUI.color = Color.white;
            GUI.BeginGroup(trackRect);
            drawInfo?.Invoke();
            GUI.EndGroup();
        }

        void HandleTrackListInput(
            Event e,
            Rect trackRect,
            MouseCursor cursor,
            Action selectAndBeginDrag,
            Func<bool> canDrop,
            Action drawDropMarker,
            Action completeDrop)
        {
            AddCursorRect(trackRect, cursor);
            if (e.type == EventType.MouseDown && e.button == 0 && trackRect.Contains(e.mousePosition))
            {
                selectAndBeginDrag();
                e.Use();
            }
            if (!canDrop())
                return;
            if (trackRect.Contains(e.mousePosition))
                drawDropMarker();
            if (e.rawType == EventType.MouseUp && e.button == 0 && trackRect.Contains(e.mousePosition))
            {
                completeDrop();
                e.Use();
            }
        }

        void HandleGroupListInput(
            Event e,
            Rect groupRect,
            MouseCursor cursor,
            Action selectAndBeginDrag,
            Func<bool> canDrop,
            Action drawDropMarker,
            Action completeDrop)
        {
            AddCursorRect(groupRect, cursor);
            if (e.type == EventType.MouseDown && e.button == 0 && groupRect.Contains(e.mousePosition))
            {
                selectAndBeginDrag();
                e.Use();
            }
            if (!canDrop())
                return;
            if (groupRect.Contains(e.mousePosition))
                drawDropMarker();
            if (e.rawType == EventType.MouseUp && e.button == 0 && groupRect.Contains(e.mousePosition))
            {
                completeDrop();
                e.Use();
            }
        }

        void ShowGroupsAndTracksList(Rect leftRect, IEmbeddedTimelineBinding timeline)
        {
            Event e = Event.current;
            Rect scaleRect = new Rect(leftRect.xMax - 4, leftRect.yMin, 4, leftRect.height);
            AddCursorRect(scaleRect, MouseCursor.ResizeHorizontal);
            if (e.type == EventType.MouseDown && e.button == 0 && scaleRect.Contains(e.mousePosition))
            {
                isResizingLeftMargin = true;
                e.Use();
            }
            if (isResizingLeftMargin)
                LEFT_MARGIN = e.mousePosition.x + 2;
            if (e.rawType == EventType.MouseUp)
                isResizingLeftMargin = false;

            IReadOnlyList<IEmbeddedTimelineGroupBinding> groups = embeddedTimeline.Groups;
            bool anyExpanded = groups.Any(group => !group.IsCollapsed);
            Rect collapseAllRect = Rect.MinMaxRect(leftRect.x + 5, leftRect.y + 4, leftRect.x + 20, leftRect.y + 19);
            Rect searchRect = Rect.MinMaxRect(leftRect.x + 20, leftRect.y + 4, leftRect.xMax - 18, leftRect.y + 19);
            Rect searchCancelRect = Rect.MinMaxRect(searchRect.xMax, searchRect.y, leftRect.xMax - 4, searchRect.yMax);
            AddCursorRect(collapseAllRect, MouseCursor.Link);
            GUI.color = Color.white.WithAlpha(0.5f);
            if (GUI.Button(collapseAllRect, anyExpanded ? "▼" : "►", (GUIStyle)"label"))
                for (int index = 0; index < groups.Count; index++)
                    groups[index].IsCollapsed = anyExpanded;
            GUI.color = Color.white;
            searchString = EditorGUI.TextField(searchRect, searchString, (GUIStyle)"ToolbarSearchTextField");
            if (GUI.Button(searchCancelRect, string.Empty, (GUIStyle)"ToolbarSearchCancelButton"))
            {
                searchString = string.Empty;
                GUIUtility.keyboardControl = 0;
            }

            float nextY = FIRST_GROUP_TOP_MARGIN;
            GUI.BeginGroup(leftRect);
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                IEmbeddedTimelineGroupBinding group = groups[groupIndex];
                bool matches = string.IsNullOrEmpty(searchString) ||
                    group.DisplayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    group.Tracks.Any(track => track.DisplayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0);
                if (!matches)
                {
                    group.IsCollapsed = true;
                    continue;
                }

                Rect groupRect = new Rect(4, nextY, leftRect.width - GROUP_RIGHT_MARGIN - 4, GROUP_HEIGHT - 3);
                nextY += GROUP_HEIGHT;
                bool groupSelected = ReferenceEquals(embeddedTimeline.Selected, group);
                DrawGroupListHeaderFrame(
                    groupRect,
                    string.Format("<b>{0}</b>", group.DisplayName),
                    group.IsActive,
                    groupSelected,
                    group.IsCollapsed,
                    Color.white,
                    value => group.IsCollapsed = value);

                HandleGroupListInput(
                    e,
                    groupRect,
                    formalPickedTrack == null ? MouseCursor.Link : MouseCursor.MoveArrow,
                    () => embeddedTimeline.Select(group),
                    () => false,
                    () => { },
                    () => { });
                if (e.type == EventType.ContextClick && groupRect.Contains(e.mousePosition))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Add Track"), false, embeddedTimeline.AddTrack);
                    menu.ShowAsContext();
                    e.Use();
                }

                if (group.IsCollapsed)
                    continue;

                    IReadOnlyList<IEmbeddedTimelineTrackBinding> tracks = group.Tracks;
                for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
                {
                    IEmbeddedTimelineTrackBinding track = tracks[trackIndex];
                    Rect trackRect = new Rect(10, nextY, leftRect.width - TRACK_RIGHT_MARGIN - 10, track.FinalHeight);
                    nextY += track.FinalHeight + TRACK_MARGINS;
                    bool runtimeActive = embeddedRuntimeTrackActive == null || embeddedRuntimeTrackActive(track.AuthoringId);
                    string inspectionKey = FormalInspectionKey(track);
                    string inspected = string.Empty;
                    if (formalInspectedParameters != null)
                        formalInspectedParameters.TryGetValue(inspectionKey, out inspected);
                    bool selected = ReferenceEquals(embeddedTimeline.Selected, track);
                    DrawTrackListRowFrame(
                        trackRect,
                        track.IsActive && runtimeActive,
                        selected,
                        track.Color,
                        () => TrackEditorGUI.DrawParametersInfoGUI(
                            e,
                            new Rect(0f, 0f, trackRect.width, trackRect.height),
                            track,
                            selected,
                            ref inspected));
                    if (formalInspectedParameters != null)
                        formalInspectedParameters[inspectionKey] = inspected ?? string.Empty;

                    if (e.type == EventType.ContextClick && trackRect.Contains(e.mousePosition))
                    {
                        int frame = Mathf.Max(0, Mathf.RoundToInt(PosToTime(mousePosition.x) * embeddedTimeline.FrameRate));
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Add Clip"), false, () => embeddedTimeline.AddClip(track, frame));
                        if (embeddedTimeline.CanPasteClip)
                            menu.AddItem(new GUIContent("Paste Clip"), false, () => embeddedTimeline.PasteClip(track, frame));
                        menu.AddItem(new GUIContent("Delete Track"), false, () => embeddedTimeline.DeleteTrack(track));
                        menu.ShowAsContext();
                        e.Use();
                    }
                    HandleTrackListInput(
                        e,
                        trackRect,
                        formalPickedTrack == null ? MouseCursor.Link : MouseCursor.MoveArrow,
                        () =>
                        {
                            embeddedTimeline.Select(track);
                            formalPickedTrack = track;
                        },
                        () => formalPickedTrack != null &&
                              !ReferenceEquals(formalPickedTrack, track) &&
                              tracks.Any(value => value.AuthoringId == formalPickedTrack.AuthoringId),
                        () =>
                        {
                            int pickedIndex = 0;
                            while (pickedIndex < tracks.Count &&
                                   tracks[pickedIndex].AuthoringId != formalPickedTrack.AuthoringId)
                                pickedIndex++;
                            var markRect = new Rect(
                                trackRect.x,
                                pickedIndex < trackIndex ? trackRect.yMax - 2 : trackRect.y,
                                trackRect.width,
                                2);
                            GUI.color = Color.grey;
                            GUI.DrawTexture(markRect, Styles.whiteTexture);
                            GUI.color = Color.white;
                        },
                        () =>
                        {
                            embeddedTimeline.MoveTrack(formalPickedTrack, trackIndex);
                            formalPickedTrack = null;
                        });
                }
            }
            GUI.EndGroup();
            totalHeight = nextY;
            if (e.rawType == EventType.MouseUp)
                formalPickedTrack = null;
            GUI.color = Color.white;
            GUI.enabled = true;
        }

        //...
        void ShowListGroups(Event e, ref float nextYPos) {

            //GROUPS
            for ( int g = 0; g < cutscene.groups.Count; g++ ) {
                var group = cutscene.groups[g];

                if ( IsFilteredOutBySearch(group, searchString) ) {
                    group.isCollapsed = true;
                    continue;
                }

                var groupRect = new Rect(4, nextYPos, leftRect.width - GROUP_RIGHT_MARGIN - 4, GROUP_HEIGHT - 3);
                nextYPos += GROUP_HEIGHT;

                //highligh?
                var groupSelected = ( ReferenceEquals(group, CutsceneUtility.selectedObject) || group == pickedGroup );
                var isVirtual = group.referenceMode == CutsceneGroup.ActorReferenceMode.UseInstanceHideOriginal;
                DrawGroupListHeaderFrame(
                    groupRect,
                    string.Format("<b>{0} {1}</b>", group.name, isVirtual ? "(Ref)" : string.Empty),
                    group.isActive,
                    groupSelected,
                    group.isCollapsed,
                    isProSkin ? Color.yellow : Color.white,
                    value => group.isCollapsed = value);


                //GROUP CONTROLS
                var plusClicked = false;
                GUI.color = isProSkin ? Color.white.WithAlpha(0.5f) : new Color(0.2f, 0.2f, 0.2f);
                var plusRect = new Rect(groupRect.xMax - 14, groupRect.y + 5, 8, 8);
                if ( GUI.Button(plusRect, Slate.Styles.plusIcon, GUIStyle.none) ) { plusClicked = true; }
                if ( !group.isActive ) {
                    var disableIconRect = new Rect(plusRect.xMin - 20, groupRect.y + 1, 16, 16);
                    if ( GUI.Button(disableIconRect, Styles.hiddenIcon, GUIStyle.none) ) { group.isActive = true; }
                }
                if ( group.isLocked ) {
                    var lockIconRect = new Rect(plusRect.xMin - ( group.isActive ? 20 : 36 ), groupRect.y + 1, 16, 16);
                    if ( GUI.Button(lockIconRect, Styles.lockIcon, GUIStyle.none) ) { group.isLocked = false; }
                }

                //Actor Object Field
                if ( !embeddedSurface && group.actor == null ) {
                    var oRect = Rect.MinMaxRect(groupRect.xMin + 20, groupRect.yMin + 1, groupRect.xMax - 20, groupRect.yMax - 1);
                    group.actor = (GameObject)UnityEditor.EditorGUI.ObjectField(oRect, group.actor, typeof(GameObject), true);
                }
                ///---

                //CONTEXT
                if ( ( e.type == EventType.ContextClick && groupRect.Contains(e.mousePosition) ) || plusClicked ) {
                    if ( embeddedSurface && embeddedAddTrack != null ) {
                        embeddedAddTrack();
                    }
                    else {
                        var menu = new GenericMenu();
                        foreach ( var _info in EditorTools.GetTypeMetaDerivedFrom(typeof(CutsceneTrack)) ) {
                            var info = _info;
                            if ( info.attachableTypes == null || !info.attachableTypes.Contains(group.GetType()) ) {
                                continue;
                            }

                            var canAdd = !info.isUnique || ( group.tracks.Find(track => track.GetType() == info.type) == null );
                            var finalPath = string.IsNullOrEmpty(info.category) ? info.name : info.category + "/" + info.name;
                            if ( canAdd ) {
                                menu.AddItem(new GUIContent("Add Track/" + finalPath), false, () => { group.AddTrack(info.type); });
                            } else {
                                menu.AddDisabledItem(new GUIContent("Add Track/" + finalPath));
                            }
                        }
                        if ( group.CanAddTrack(copyTrack) ) {
                            menu.AddItem(new GUIContent("Paste Track"), false, () => { group.DuplicateTrack(copyTrack); });
                        } else {
                            menu.AddDisabledItem(new GUIContent("Paste Track"));
                        }
                        menu.AddItem(new GUIContent("Disable Group"), !group.isActive, () => { group.isActive = !group.isActive; });
                        menu.AddItem(new GUIContent("Lock Group"), group.isLocked, () => { group.isLocked = !group.isLocked; });

                        if ( !( group is DirectorGroup ) ) {
                            menu.AddItem(new GUIContent("Select Actor (Double Click)"), false, () => { Selection.activeObject = group.actor; });
                            menu.AddItem(new GUIContent("Replace Actor"), false, () => { group.actor = null; });
                            menu.AddItem(new GUIContent("Duplicate"), false, () =>
                                {
                                    cutscene.DuplicateGroup(group);
                                    InitClipWrappers();
                                });
                            menu.AddSeparator("/");
                            menu.AddItem(new GUIContent("Delete Group"), false, () =>
                                {
                                    if ( EditorUtility.DisplayDialog("Delete Group", "Are you sure?", "YES", "NO!") ) {
                                        cutscene.DeleteGroup(group);
                                        InitClipWrappers();
                                    }
                                });
                        }
                        menu.ShowAsContext();
                    }
                    e.Use();
                }


                HandleGroupListInput(
                    e,
                    groupRect,
                    pickedGroup == null ? MouseCursor.Link : MouseCursor.MoveArrow,
                    () =>
                    {
                        CutsceneUtility.selectedObject = group;
                        if (!(group is DirectorGroup))
                            pickedGroup = group;
                        if (e.clickCount == 2 && !embeddedSurface)
                            Selection.activeGameObject = group.actor;
                    },
                    () => pickedGroup != null &&
                          pickedGroup != group &&
                          !(group is DirectorGroup),
                    () =>
                    {
                        var markRect = new Rect(
                            groupRect.x,
                            cutscene.groups.IndexOf(pickedGroup) < g ? groupRect.yMax - 2 : groupRect.y,
                            groupRect.width,
                            2);
                        GUI.color = Color.grey;
                        GUI.DrawTexture(markRect, Styles.whiteTexture);
                        GUI.color = Color.white;
                    },
                    () =>
                    {
                        cutscene.groups.Remove(pickedGroup);
                        cutscene.groups.Insert(g, pickedGroup);
                        cutscene.Validate();
                        pickedGroup = null;
                    });

                //SHOW TRACKS (?)
                if ( !group.isCollapsed ) {
                    ShowListTracks(e, group, ref nextYPos);
                    //draw vertical graphic on left side of nested track rects
                    GUI.color = groupSelected ? LIST_SELECTION_COLOR : GROUP_COLOR;
                    var verticalRect = Rect.MinMaxRect(groupRect.x, groupRect.yMax, groupRect.x + 3, nextYPos - 2);
                    GUI.DrawTexture(verticalRect, Styles.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        //...
        void ShowListTracks(Event e, CutsceneGroup group, ref float nextYPos) {

            //TRACKS
            for ( int t = 0; t < group.tracks.Count; t++ ) {
                var track = group.tracks[t];
                var yPos = nextYPos;

                var trackRect = new Rect(10, yPos, leftRect.width - TRACK_RIGHT_MARGIN - 10, track.finalHeight);
                nextYPos += track.finalHeight + TRACK_MARGINS;

                DrawTrackListRowFrame(
                    trackRect,
                    track.isActive,
                    ReferenceEquals(track, CutsceneUtility.selectedObject) || track == pickedTrack,
                    track.color,
                    () => track.OnTrackInfoGUI(trackRect));

                //CONTEXT
                if ( e.type == EventType.ContextClick && trackRect.Contains(e.mousePosition) ) {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Disable Track"), !track.isActive, () => { track.isActive = !track.isActive; });
                    menu.AddItem(new GUIContent("Lock Track"), track.isLocked, () => { track.isLocked = !track.isLocked; });
                    if ( !embeddedSurface ) {
                        menu.AddItem(new GUIContent("Copy"), false, () => { copyTrack = track; });
                        if ( track.GetType().RTGetAttribute<UniqueElementAttribute>(true) == null ) {
                            menu.AddItem(new GUIContent("Duplicate"), false, () =>
                                {
                                    group.DuplicateTrack(track);
                                    InitClipWrappers();
                                });
                        } else {
                            menu.AddDisabledItem(new GUIContent("Duplicate"));
                        }
                    }
                    menu.AddSeparator("/");
                    menu.AddItem(new GUIContent("Delete Track"), false, () =>
                        {
                            if ( EditorUtility.DisplayDialog("Delete Track", "Are you sure?", "YES", "NO!") ) {
                                group.DeleteTrack(track);
                                InitClipWrappers();
                            }
                        });
                    menu.ShowAsContext();
                    e.Use();
                }

                HandleTrackListInput(
                    e,
                    trackRect,
                    pickedTrack == null ? MouseCursor.Link : MouseCursor.MoveArrow,
                    () =>
                    {
                        CutsceneUtility.selectedObject = track;
                        pickedTrack = track;
                    },
                    () => pickedTrack != null &&
                          pickedTrack != track &&
                          ReferenceEquals(pickedTrack.parent, group),
                    () =>
                    {
                        var markRect = new Rect(
                            trackRect.x,
                            group.tracks.IndexOf(pickedTrack) < t ? trackRect.yMax - 2 : trackRect.y,
                            trackRect.width,
                            2);
                        GUI.color = Color.grey;
                        GUI.DrawTexture(markRect, Styles.whiteTexture);
                        GUI.color = Color.white;
                    },
                    () =>
                    {
                        group.tracks.Remove(pickedTrack);
                        group.tracks.Insert(t, pickedTrack);
                        cutscene.Validate();
                        pickedTrack = null;
                    });
            }
        }


        ///----------------------------------------------------------------------------------------------


        //middle - the actual timeline tracks
        string FormalInspectionKey(IEmbeddedTimelineTrackBinding track)
        {
            return string.Concat(
                track?.AuthoringId ?? string.Empty,
                ":",
                track?.SelectedClip?.AuthoringId ?? string.Empty);
        }

        void ShowTimeLines(Rect centerRect) {

            if (embeddedTimeline != null)
            {
                ShowTimeLines(centerRect, embeddedTimeline);
                return;
            }

            var e = Event.current;

            //bg graphic
            var bgRect = Rect.MinMaxRect(centerRect.xMin, centerRect.yMin, centerRect.xMax, screenHeight + scrollPos.y);
            GUI.color = Color.black.WithAlpha(0.1f);
            GUI.DrawTexture(bgRect, whiteTexture);
            GUI.color = Color.black.WithAlpha(0.03f);
            GUI.DrawTextureWithTexCoords(bgRect, Styles.stripes, new Rect(0, 0, bgRect.width / -7, bgRect.height / -7));
            GUI.color = Color.white;

            // draw guides based on time info stored
            for ( var _i = timeInfoStart; _i <= timeInfoEnd; _i += timeInfoInterval ) {
                var i = Mathf.Round(_i * 10) / 10;
                DrawGuideLine(i, Color.black.WithAlpha(0.05f));
                if ( i % timeInfoHighMod == 0 ) {
                    DrawGuideLine(i, Color.black.WithAlpha(0.05f));
                }
            }


            //Begin Group
            GUI.BeginGroup(centerRect);

            //starting height
            var nextYPos = FIRST_GROUP_TOP_MARGIN;

            //master sections
            var sectionsRect = Rect.MinMaxRect(Mathf.Max(TimeToPos(viewTimeMin), TimeToPos(0)), 3, TimeToPos(viewTimeMax), 18);
            if ( cutscene.directorGroup != null ) { //it never should
                ShowGroupSections(cutscene.directorGroup, sectionsRect);
            }

            //Begin Windows
            BeginWindows();

            //GROUPS
            for ( int g = 0; g < cutscene.groups.Count; g++ ) {
                var group = cutscene.groups[g];

                if ( IsFilteredOutBySearch(group, searchString) ) {
                    group.isCollapsed = true;
                    continue;
                }

                var groupRect = Rect.MinMaxRect(Mathf.Max(TimeToPos(viewTimeMin), TimeToPos(0)), nextYPos, TimeToPos(viewTimeMax), nextYPos + GROUP_HEIGHT);
                nextYPos += GROUP_HEIGHT;

                //if collapsed, just show a heat minimap of clips.
                if ( group.isCollapsed ) {
                    GUI.color = Color.black.WithAlpha(0.15f);
                    var collapseRect = Rect.MinMaxRect(groupRect.xMin + 2, groupRect.yMin + 2, groupRect.xMax, groupRect.yMax - 4);
                    GUI.DrawTexture(collapseRect, Styles.whiteTexture);
                    GUI.color = Color.grey.WithAlpha(0.5f);
                    foreach ( var track in group.tracks ) {
                        foreach ( var clip in track.clips ) {
                            var start = TimeToPos(clip.startTime);
                            var end = TimeToPos(clip.endTime);
                            GUI.DrawTexture(Rect.MinMaxRect(start + 0.5f, collapseRect.y + 2, end - 0.5f, collapseRect.yMax - 2), Styles.whiteTexture);
                        }
                    }
                    GUI.color = Color.white;
                    continue;
                }


                //TRACKS
                for ( int t = 0; t < group.tracks.Count; t++ ) {
                    var track = group.tracks[t];
                    var yPos = nextYPos;
                    var trackPosRect = Rect.MinMaxRect(Mathf.Max(TimeToPos(viewTimeMin), TimeToPos(track.startTime)), yPos, TimeToPos(viewTimeMax), yPos + track.finalHeight);
                    var trackTimeRect = Rect.MinMaxRect(Mathf.Max(viewTimeMin, track.startTime), 0, viewTimeMax, 0);
                    nextYPos += track.finalHeight + TRACK_MARGINS;

                    //GRAPHICS
                    GUI.color = Color.black.WithAlpha(isProSkin ? 0.06f : 0.1f);
                    GUI.DrawTexture(trackPosRect, whiteTexture);
                    Handles.color = ColorUtility.Grey(isProSkin ? 0.15f : 0.4f);
                    Handles.DrawLine(new Vector2(TimeToPos(viewTimeMin), trackPosRect.y + 1), new Vector2(trackPosRect.xMax, trackPosRect.y + 1));
                    Handles.DrawLine(new Vector2(TimeToPos(viewTimeMin), trackPosRect.yMax), new Vector2(trackPosRect.xMax, trackPosRect.yMax));
                    if ( track.showCurves ) {
                        Handles.DrawLine(new Vector2(trackPosRect.x, trackPosRect.y + track.defaultHeight), new Vector2(trackPosRect.xMax, trackPosRect.y + track.defaultHeight));
                    }
                    Handles.color = Color.white;
                    if ( viewTimeMin < 0 ) { //just visual clarity
                        GUI.Box(Rect.MinMaxRect(TimeToPos(viewTimeMin), trackPosRect.yMin, TimeToPos(0), trackPosRect.yMax), string.Empty);
                    }
                    if ( track.startTime > track.parent.startTime || track.endTime < track.parent.endTime ) {
                        Handles.color = Color.white;
                        GUI.color = Color.black.WithAlpha(0.2f);
                        if ( track.startTime > track.parent.startTime ) {
                            var tStart = TimeToPos(track.startTime);
                            var r = Rect.MinMaxRect(TimeToPos(0), yPos, tStart, yPos + track.finalHeight);
                            GUI.DrawTexture(r, whiteTexture);
                            GUI.DrawTextureWithTexCoords(r, Styles.stripes, new Rect(0, 0, r.width / 7, r.height / 7));
                            var a = new Vector2(tStart, trackPosRect.yMin);
                            var b = new Vector2(a.x, trackPosRect.yMax);
                            Handles.DrawLine(a, b);
                        }
                        if ( track.endTime < track.parent.endTime ) {
                            var tEnd = TimeToPos(track.endTime);
                            var r = Rect.MinMaxRect(tEnd, yPos, TimeToPos(length), yPos + track.finalHeight);
                            GUI.DrawTexture(r, whiteTexture);
                            GUI.DrawTextureWithTexCoords(r, Styles.stripes, new Rect(0, 0, r.width / 7, r.height / 7));
                            var a = new Vector2(tEnd, trackPosRect.yMin);
                            var b = new Vector2(a.x, trackPosRect.yMax);
                            Handles.DrawLine(a, b);
                        }
                        GUI.color = Color.white;
                        Handles.color = Color.white;
                    }
                    GUI.backgroundColor = Color.white;

                    //highlight selected track
                    if ( ReferenceEquals(CutsceneUtility.selectedObject, track) ) {
                        GUI.color = Color.grey;
                        GUI.Box(trackPosRect.ExpandBy(0, 2), string.Empty, Styles.hollowFrameHorizontalStyle);
                        GUI.color = Color.white;
                    }
                    //

                    if ( track.isLocked ) {
                        if ( e.isMouse && trackPosRect.Contains(e.mousePosition) ) {
                            e.Use();
                        }
                    }

                    //...
                    var cursorTime = SnapTime(PosToTime(mousePosition.x));
                    track.OnTrackTimelineGUI(trackPosRect, trackTimeRect, cursorTime, TimeToPos);
                    //...

                    if ( !track.isActive || track.isLocked ) {

                        postWindowsGUI += () =>
                        {
                            //overlay dark stripes for disabled tracks
                            if ( !track.isActive ) {
                                GUI.color = Color.black.WithAlpha(0.2f);
                                GUI.DrawTexture(trackPosRect, whiteTexture);
                                GUI.DrawTextureWithTexCoords(trackPosRect, Styles.stripes, new Rect(0, 0, ( trackPosRect.width / 5 ), ( trackPosRect.height / 5 )));
                                GUI.color = Color.white;
                            }

                            //overlay light stripes for locked tracks
                            if ( track.isLocked ) {
                                GUI.color = Color.black.WithAlpha(0.15f);
                                GUI.DrawTextureWithTexCoords(trackPosRect, Styles.stripes, new Rect(0, 0, trackPosRect.width / 20, trackPosRect.height / 20));
                                GUI.color = Color.white;
                            }

                            if ( isProSkin ) {
                                string overlayLabel = null;
                                if ( !track.isActive && track.isLocked ) {
                                    overlayLabel = "DISABLED & LOCKED";
                                } else {
                                    if ( !track.isActive ) { overlayLabel = "DISABLED"; }
                                    if ( track.isLocked ) { overlayLabel = "LOCKED"; }
                                }
                                var size = Styles.centerLabel.CalcSize(new GUIContent(overlayLabel));
                                var bgLabelRect = new Rect(0, 0, size.x, size.y);
                                bgLabelRect.center = trackPosRect.center;
                                GUI.Label(trackPosRect, string.Format("<b>{0}</b>", overlayLabel), Styles.centerLabel);
                                GUI.color = Color.white;
                            }
                        };
                    }


                    var nativeClipBindings = new IClipEditorBinding[track.clips.Count];
                    for (int clipIndex = 0; clipIndex < track.clips.Count; clipIndex++)
                        nativeClipBindings[clipIndex] = new NativeClipEditorBinding(track.clips[clipIndex]);
                    for (int clipIndex = 0; clipIndex < nativeClipBindings.Length; clipIndex++)
                        DrawTimelineClip(
                            centerRect,
                            yPos,
                            g,
                            t,
                            clipIndex,
                            nativeClipBindings[clipIndex],
                            clipIndex > 0 ? nativeClipBindings[clipIndex - 1] : null,
                            clipIndex + 1 < nativeClipBindings.Length ? nativeClipBindings[clipIndex + 1] : null,
                            nativeClipBindings,
                            track.isActive,
                            track.defaultHeight);
                }

                //highligh selected group
                if ( ReferenceEquals(CutsceneUtility.selectedObject, group) ) {
                    var r = Rect.MinMaxRect(groupRect.xMin, groupRect.yMin, groupRect.xMax, nextYPos);
                    GUI.color = Color.grey;
                    GUI.Box(r, string.Empty, Styles.hollowFrameHorizontalStyle);
                    GUI.color = Color.white;
                }
            }

            EndWindows();

            //call postwindow delegate
            if ( postWindowsGUI != null ) {
                postWindowsGUI();
                postWindowsGUI = null;
            }

            //this is done in the same GUI.Group
            DoMultiSelection();

            GUI.EndGroup();

            //border shadows
            GUI.color = Color.white.WithAlpha(0.2f);
            GUI.Box(bgRect, string.Empty, Styles.shadowBorderStyle);
            GUI.color = Color.white;

            //darken the time after cutscene length
            if ( viewTimeMax > length ) {
                var endPos = Mathf.Max(TimeToPos(length) + leftRect.width, centerRect.xMin);
                var darkRect = Rect.MinMaxRect(endPos, centerRect.yMin, centerRect.xMax, centerRect.yMax);
                GUI.color = Color.black.WithAlpha(0.3f);
                GUI.Box(darkRect, string.Empty, (GUIStyle)"TextField");
                GUI.color = Color.white;
            }

            //darken the time before zero
            if ( viewTimeMin < 0 ) {
                var startPos = Mathf.Min(TimeToPos(0) + leftRect.width, centerRect.xMax);
                var darkRect = Rect.MinMaxRect(centerRect.xMin, centerRect.yMin, startPos, centerRect.yMax);
                GUI.color = Color.black.WithAlpha(0.3f);
                GUI.Box(darkRect, string.Empty, (GUIStyle)"TextField");
                GUI.color = Color.white;
            }

            //ensure no interactive clip
            if ( e.rawType == EventType.MouseUp ) {
                if ( interactingClip != null ) {
                    interactingClip.ResetInteraction();
                    interactingClip.EndClipAdjust();
                    interactingClip = null;
                }
            }
        }

        void DrawTimelineClip(
            Rect centerRect,
            float y,
            int groupIndex,
            int trackIndex,
            int clipIndex,
            IClipEditorBinding currentBinding,
            IClipEditorBinding previousBinding,
            IClipEditorBinding nextBinding,
            IReadOnlyList<IClipEditorBinding> trackBindings,
            bool trackActive,
            float trackDefaultHeight)
        {
            int id = UID(groupIndex, trackIndex, clipIndex);
            ActionClipWrapper wrapper;
            if (!clipWrappers.TryGetValue(id, out wrapper) || !wrapper.Matches(currentBinding))
            {
                wrapper = currentBinding.FormalClip != null
                    ? new ActionClipWrapper(currentBinding.FormalClip)
                    : new ActionClipWrapper(currentBinding.NativeAction);
                clipWrappers[id] = wrapper;
                if (currentBinding.NativeAction != null)
                {
                    if (clipWrappersMap != null)
                        clipWrappersMap[currentBinding.NativeAction] = wrapper;
                }
            }

            wrapper.SetNeighbors(previousBinding, nextBinding);
            Rect clipRect = wrapper.rect;
            clipRect.y = y;
            clipRect.width = Mathf.Max(wrapper.editorBinding.Length / Mathf.Max(0.0001f, viewTime) * centerRect.width, 6f);
            clipRect.height = trackDefaultHeight;

            float xTime = wrapper.editorBinding.StartTime;
            float xPos = clipRect.x;
            if (ReferenceEquals(interactingClip, wrapper) && wrapper.isDragging && Event.current.type == EventType.MouseDrag)
            {
                Event e = Event.current;
                float lastTime = xTime;
                xTime = PosToTime(xPos + leftRect.width);
                xTime = SnapTime(xTime);
                xTime = Mathf.Clamp(xTime, 0f, maxTime - 0.1f);

                if (multiSelection != null && multiSelection.Count > 1)
                {
                    float delta = xTime - lastTime;
                    float boundMin = Mathf.Min(multiSelection.Select(value => value.editorBinding.StartTime).ToArray());
                    if (boundMin + delta < 0f)
                    {
                        xTime -= delta;
                        delta = 0f;
                    }
                    foreach (ActionClipWrapper value in multiSelection)
                        if (!ReferenceEquals(value, wrapper))
                            value.editorBinding.StartTime += delta;
                }

                if (multiSelection == null || multiSelection.Count < 1)
                {
                    float cursorTime = SnapTime(PosToTime(mousePosition.x));
                    IClipEditorBinding preCursorBinding = trackBindings
                        .Where(value => value.AuthoringId != wrapper.editorBinding.AuthoringId && value.StartTime < cursorTime)
                        .LastOrDefault();
                    IClipEditorBinding postCursorBinding = trackBindings
                        .Where(value => value.AuthoringId != wrapper.editorBinding.AuthoringId && value.EndTime > cursorTime)
                        .FirstOrDefault();
                    if (e.shift || Prefs.rippleMode)
                    {
                        preCursorBinding = previousBinding;
                        postCursorBinding = null;
                    }

                    float preTime = preCursorBinding != null ? preCursorBinding.EndTime : 0f;
                    float postTime = postCursorBinding != null ? postCursorBinding.StartTime : maxTime + wrapper.editorBinding.Length;
                    if (Prefs.magnetSnapping && !e.control)
                    {
                        float? snapStart = MagnetSnapTime(xTime, magnetSnapTimesCache);
                        float? snapEnd = MagnetSnapTime(xTime + wrapper.editorBinding.Length, magnetSnapTimesCache);
                        if (snapStart != null && snapEnd != null)
                        {
                            float distStart = Mathf.Abs(snapStart.Value - xTime);
                            float distEnd = Mathf.Abs(snapEnd.Value - (xTime + wrapper.editorBinding.Length));
                            bool useEnd = distEnd < distStart;
                            float bestTime = useEnd ? snapEnd.Value : snapStart.Value;
                            pendingGuides.Add(new GuideLine(bestTime, Color.white));
                            xTime = useEnd ? snapEnd.Value - wrapper.editorBinding.Length : snapStart.Value;
                        }
                        else
                        {
                            if (snapEnd != null)
                            {
                                pendingGuides.Add(new GuideLine(snapEnd.Value, Color.white));
                                xTime = snapEnd.Value - wrapper.editorBinding.Length;
                            }
                            if (snapStart != null)
                            {
                                pendingGuides.Add(new GuideLine(snapStart.Value, Color.white));
                                xTime = snapStart.Value;
                            }
                        }
                    }

                    if (wrapper.editorBinding.CanCrossBlend(preCursorBinding))
                        preTime -= Mathf.Min(wrapper.editorBinding.Length / 2f, preCursorBinding.Length / 2f);
                    if (wrapper.editorBinding.CanCrossBlend(postCursorBinding))
                        postTime += Mathf.Min(wrapper.editorBinding.Length / 2f, postCursorBinding.Length / 2f);
                    if (wrapper.editorBinding.Length > postTime - preTime)
                        xTime = lastTime;
                    if (Mathf.Abs(xTime - lastTime) > 0.0001f)
                    {
                        xTime = Mathf.Clamp(xTime, preTime, postTime - wrapper.editorBinding.Length);
                        if (e.shift || Prefs.rippleMode)
                        {
                            foreach (ActionClipWrapper value in clipWrappers.Values.Where(value =>
                                !ReferenceEquals(value, wrapper) &&
                                value.editorBinding.StartTime > lastTime &&
                                ((value.editorBinding.FormalClip != null && wrapper.editorBinding.FormalClip != null &&
                                  ReferenceEquals(value.editorBinding.Track, wrapper.editorBinding.Track)) ||
                                 (value.editorBinding.NativeAction != null && wrapper.editorBinding.NativeAction != null &&
                                  value.editorBinding.NativeAction.parent == wrapper.editorBinding.NativeAction.parent))))
                                value.editorBinding.StartTime += xTime - lastTime;
                        }
                    }
                }
                wrapper.editorBinding.StartTime = xTime;
            }
            clipRect.x = TimeToPos(xTime);

            bool isSelected = wrapper.editorBinding.FormalClip != null
                ? ReferenceEquals(embeddedTimeline?.Selected, wrapper.editorBinding.FormalClip)
                : ReferenceEquals(CutsceneUtility.selectedObject, wrapper.editorBinding.NativeAction) ||
                  (multiSelection != null && multiSelection.Contains(wrapper));
            bool isVisible = Rect.MinMaxRect(0, scrollPos.y, centerRect.width, centerRect.height).Overlaps(clipRect);
            if (!isSelected && !isVisible)
            {
                wrapper.rect = default(Rect);
                return;
            }

            if (isSelected)
            {
                GUI.color = HIGHLIGHT_COLOR;
                GUI.DrawTexture(clipRect.ExpandBy(2), Slate.Styles.whiteTexture);
                GUI.color = Color.white;
            }

            GUI.color = wrapper.editorBinding.IsValid ? Color.white : new Color(1, 0.3f, 0.3f);
            GUI.color = trackActive ? GUI.color : Color.grey;
            GUI.Box(clipRect, string.Empty, Styles.clipBoxHorizontalStyle);
            wrapper.rect = GUI.Window(id, clipRect, ActionClipWindow, string.Empty, GUIStyle.none);
            if (!isProSkin)
            {
                GUI.color = Color.white.WithAlpha(0.5f);
                GUI.Box(clipRect, string.Empty);
                GUI.color = Color.white;
            }
            if (isSelected)
            {
                GUI.color = HIGHLIGHT_COLOR;
                GUI.Box(clipRect.ExpandBy(2), string.Empty, Styles.hollowFrameHorizontalStyle);
                GUI.color = Color.white;
            }

            string runtimeStatus = wrapper.editorBinding.FormalClip != null
                ? embeddedRuntimeClipStatus?.Invoke(wrapper.editorBinding.FormalClip.AuthoringId)
                : null;
            if (!string.IsNullOrEmpty(runtimeStatus))
                GUI.Label(clipRect, runtimeStatus, Styles.centerLabel);
            float nextPosX = TimeToPos(nextBinding != null ? nextBinding.StartTime : viewTimeMax);
            float previousPosX = TimeToPos(previousBinding != null ? previousBinding.EndTime : viewTimeMin);
            wrapper.editorBinding.DrawClipGUIExternal(
                Rect.MinMaxRect(previousPosX, clipRect.yMin, clipRect.xMin, clipRect.yMax),
                Rect.MinMaxRect(clipRect.xMax, clipRect.yMin, nextPosX, clipRect.yMax));
            if (clipRect.width <= 20)
                GUI.Label(
                    Rect.MinMaxRect(clipRect.xMax, clipRect.yMin, nextPosX, clipRect.yMax).ExpandBy(-1),
                    string.Format("<size=10>{0}</size>", wrapper.editorBinding.Info));
            GUI.color = Color.white;
        }

        void ShowTimeLines(Rect centerRect, IEmbeddedTimelineBinding timeline)
        {
            Event e = Event.current;
            Rect bgRect = Rect.MinMaxRect(centerRect.xMin, centerRect.yMin, centerRect.xMax, screenHeight + scrollPos.y);
            GUI.color = Color.black.WithAlpha(0.1f);
            GUI.DrawTexture(bgRect, whiteTexture);
            GUI.color = Color.black.WithAlpha(0.03f);
            GUI.DrawTextureWithTexCoords(bgRect, Styles.stripes, new Rect(0, 0, bgRect.width / -7, bgRect.height / -7));
            GUI.color = Color.white;
            for (float time = timeInfoStart; time <= timeInfoEnd; time += timeInfoInterval)
                DrawGuideLine(Mathf.Round(time * embeddedTimeline.FrameRate) / embeddedTimeline.FrameRate, Color.black.WithAlpha(0.05f));

            GUI.BeginGroup(centerRect);
            float nextY = FIRST_GROUP_TOP_MARGIN;
            IReadOnlyList<IEmbeddedTimelineGroupBinding> groups = embeddedTimeline.Groups;
            Rect sectionsRect = Rect.MinMaxRect(Mathf.Max(TimeToPos(viewTimeMin), TimeToPos(0)), 3, TimeToPos(viewTimeMax), 18);
            ShowSections(sectionsRect, timeline);
            BeginWindows();
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                IEmbeddedTimelineGroupBinding group = groups[groupIndex];
                Rect groupRect = Rect.MinMaxRect(
                    Mathf.Max(TimeToPos(viewTimeMin), TimeToPos(0)),
                    nextY,
                    TimeToPos(viewTimeMax),
                    nextY + GROUP_HEIGHT);
                nextY += GROUP_HEIGHT;
                if (group.IsCollapsed)
                {
                    GUI.color = Color.black.WithAlpha(0.15f);
                    GUI.DrawTexture(groupRect.ExpandBy(2, -4), whiteTexture);
                    GUI.color = Color.grey.WithAlpha(0.5f);
                    for (int trackIndex = 0; trackIndex < group.Tracks.Count; trackIndex++)
                        for (int clipIndex = 0; clipIndex < group.Tracks[trackIndex].Clips.Count; clipIndex++)
                        {
                            IEmbeddedTimelineClipBinding clip = group.Tracks[trackIndex].Clips[clipIndex];
                            GUI.DrawTexture(
                                Rect.MinMaxRect(
                                    TimeToPos(clip.StartTime) + 0.5f,
                                    groupRect.y + 2,
                                    TimeToPos(clip.EndTime) - 0.5f,
                                    groupRect.yMax - 2),
                                whiteTexture);
                        }
                    GUI.color = Color.white;
                    continue;
                }

                for (int trackIndex = 0; trackIndex < group.Tracks.Count; trackIndex++)
                {
                    IEmbeddedTimelineTrackBinding track = group.Tracks[trackIndex];
                    float y = nextY;
                    Rect trackPosRect = Rect.MinMaxRect(
                        Mathf.Max(TimeToPos(viewTimeMin), TimeToPos(track.StartTime)),
                        y,
                        TimeToPos(viewTimeMax),
                        y + track.FinalHeight);
                    Rect trackTimeRect = Rect.MinMaxRect(Mathf.Max(viewTimeMin, track.StartTime), 0, viewTimeMax, 0);
                    nextY += track.FinalHeight + TRACK_MARGINS;

                    GUI.color = Color.black.WithAlpha(isProSkin ? 0.06f : 0.1f);
                    GUI.DrawTexture(trackPosRect, whiteTexture);
                    Handles.color = ColorUtility.Grey(isProSkin ? 0.15f : 0.4f);
                    Handles.DrawLine(new Vector2(TimeToPos(viewTimeMin), trackPosRect.y + 1), new Vector2(trackPosRect.xMax, trackPosRect.y + 1));
                    Handles.DrawLine(new Vector2(TimeToPos(viewTimeMin), trackPosRect.yMax), new Vector2(trackPosRect.xMax, trackPosRect.yMax));
                    Handles.color = Color.white;
                    if (ReferenceEquals(embeddedTimeline.Selected, track))
                    {
                        GUI.color = Color.grey;
                        GUI.Box(trackPosRect.ExpandBy(0, 2), string.Empty, Styles.hollowFrameHorizontalStyle);
                        GUI.color = Color.white;
                    }

                    if (track.IsLocked && e.isMouse && trackPosRect.Contains(e.mousePosition))
                        e.Use();

                    string inspectionKey = FormalInspectionKey(track);
                    string inspected = string.Empty;
                    if (formalInspectedParameters != null)
                        formalInspectedParameters.TryGetValue(inspectionKey, out inspected);
                    TrackEditorGUI.DrawClipCurves(e, trackPosRect, trackTimeRect, TimeToPos, track, ref inspected);
                    if (formalInspectedParameters != null)
                        formalInspectedParameters[inspectionKey] = inspected ?? string.Empty;

                    if (e.type == EventType.ContextClick && Rect.MinMaxRect(trackPosRect.xMin, y, trackPosRect.xMax, y + track.DefaultHeight).Contains(e.mousePosition))
                    {
                        int frame = Mathf.Max(0, Mathf.RoundToInt(PosToTime(mousePosition.x) * embeddedTimeline.FrameRate));
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Add Clip"), false, () => embeddedTimeline.AddClip(track, frame));
                        if (embeddedTimeline.CanPasteClip)
                            menu.AddItem(new GUIContent("Paste Clip"), false, () => embeddedTimeline.PasteClip(track, frame));
                        menu.AddItem(new GUIContent("Delete Track"), false, () => embeddedTimeline.DeleteTrack(track));
                        menu.ShowAsContext();
                        e.Use();
                    }

                    IReadOnlyList<IEmbeddedTimelineClipBinding> clips = track.Clips;
                    var clipBindings = new IClipEditorBinding[clips.Count];
                    for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
                        clipBindings[clipIndex] = new FormalClipEditorBinding(clips[clipIndex]);
                    for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
                        DrawTimelineClip(
                            centerRect,
                            y,
                            groupIndex,
                            trackIndex,
                            clipIndex,
                            clipBindings[clipIndex],
                            clipIndex > 0 ? clipBindings[clipIndex - 1] : null,
                            clipIndex + 1 < clips.Count ? clipBindings[clipIndex + 1] : null,
                            clipBindings,
                            track.IsActive,
                            track.DefaultHeight);
                }
            }
            EndWindows();
            DoMultiSelection();
            GUI.EndGroup();
            totalHeight = nextY;
            if (e.rawType == EventType.MouseUp && interactingClip != null)
            {
                interactingClip.ResetInteraction();
                interactingClip = null;
            }
        }

        void ShowSections(Rect rect, IEmbeddedTimelineBinding timeline)
        {
            Event e = Event.current;
            List<IEmbeddedTimelineSectionBinding> sections = embeddedTimeline.Sections
                .OrderBy(section => section.Time)
                .ToList();
            if (e.type == EventType.ContextClick && rect.Contains(e.mousePosition))
            {
                int frame = Mathf.Max(0, Mathf.RoundToInt(PosToTime(mousePosition.x) * embeddedTimeline.FrameRate));
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add Section Here"), false, () => embeddedTimeline.AddSection(frame));
                menu.ShowAsContext();
                e.Use();
            }

            float previous = 0f;
            for (int index = 0; index <= sections.Count; index++)
            {
                IEmbeddedTimelineSectionBinding section = index < sections.Count ? sections[index] : null;
                float next = section != null ? Mathf.Clamp(section.Time, previous, length) : length;
                Rect sectionRect = Rect.MinMaxRect(TimeToPos(previous), rect.y, TimeToPos(next) - 2f, rect.yMax);
                GUI.color = section != null ? section.Color : Color.black.WithAlpha(0.2f);
                GUI.DrawTexture(sectionRect, whiteTexture);
                GUI.color = section != null && section.Color.grayscale >= 0.5f ? Color.black : Color.white;
                GUI.Label(sectionRect, section != null ? $" <i>{section.Name}</i>" : " <i>Outro</i>");
                GUI.color = Color.white;
                if (section != null)
                {
                    Rect markerRect = Rect.MinMaxRect(TimeToPos(section.Time) - 5f, rect.y, TimeToPos(section.Time) + 5f, rect.yMax);
                    AddCursorRect(markerRect, MouseCursor.SlideArrow);
                    if (e.type == EventType.MouseDown && e.button == 0 && markerRect.Contains(e.mousePosition) && !embeddedTimeline.IsReadOnly)
                    {
                        formalDraggedSection = section;
                        BeginEmbeddedEdit("Move Timeline Section");
                        e.Use();
                    }
                    if (e.type == EventType.ContextClick && sectionRect.Contains(e.mousePosition))
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Move to Current Frame"), false, () =>
                        {
                            BeginEmbeddedEdit("Move Timeline Section");
                            embeddedTimeline.ConfigureSection(section, section.Name, embeddedTimeline.CurrentFrame);
                            CommitEmbeddedEdit();
                        });
                        menu.AddItem(new GUIContent("Delete Section"), false, () => embeddedTimeline.DeleteSection(section));
                        menu.ShowAsContext();
                        e.Use();
                    }
                }
                previous = next;
            }

            if (formalDraggedSection != null)
            {
                formalDraggedSection.Time = Mathf.Clamp(SnapTime(PosToTime(mousePosition.x)), 0f, length);
                if (e.rawType == EventType.MouseUp)
                {
                    CommitEmbeddedEdit();
                    formalDraggedSection = null;
                }
            }
        }


        //Group sections...
        void ShowGroupSections(CutsceneGroup group, Rect rect) {
            var e = Event.current;
            GenericMenu sectionsMenu = null;
            if ( e.type == EventType.ContextClick && rect.Contains(e.mousePosition) ) {
                var t = PosToTime(mousePosition.x);
                sectionsMenu = new GenericMenu();
                sectionsMenu.AddItem(new GUIContent("Add Section Here"), false, () => { group.sections.Add(new Section("Section", t)); });
            }

            var sections = new List<Section>(group.sections.OrderBy(s => s.time));
            if ( sections.Count == 0 ) {
                sections.Insert(0, new Section("No Sections", 0));
                sections.Add(new Section("Outro", maxTime));
            } else {
                sections.Insert(0, new Section("Intro", 0));
                sections.Add(new Section("Outro", maxTime));
            }

            for ( var i = 0; i < sections.Count - 1; i++ ) {
                var section1 = sections[i];
                var section2 = sections[i + 1];
                var pos1 = TimeToPos(section1.time);
                var pos2 = TimeToPos(section2.time);
                var y = rect.y;

                var sectionRect = Rect.MinMaxRect(pos1, y, pos2 - 2, y + GROUP_HEIGHT - 5);
                var markRect = new Rect(sectionRect.x + 2, sectionRect.y + 2, 2, sectionRect.height - 4);
                var clickRect = new Rect(0, y, 15, sectionRect.height);
                var loopRect = Rect.MinMaxRect(Mathf.Max(sectionRect.xMax - 18, sectionRect.xMin), sectionRect.yMin + 2, sectionRect.xMax - 2, sectionRect.yMax - 2);

                clickRect.center = markRect.center;

                GUI.color = section1.color;
                if ( section1.colorizeBackground ) {
                    GUI.DrawTexture(Rect.MinMaxRect(sectionRect.xMin, sectionRect.yMax + 1, sectionRect.xMax, screenHeight + scrollPos.y), whiteTexture);
                }
                GUI.DrawTexture(sectionRect, whiteTexture);
                GUI.color = Color.white.WithAlpha(0.2f);
                GUI.DrawTexture(markRect, whiteTexture);
                GUI.color = ( section1.color.grayscale >= 0.5 ? Color.black : Color.white ).WithAlpha(0.5f);
                if ( section1.exitMode == Section.ExitMode.Loop ) {
                    GUI.DrawTexture(loopRect, Styles.loopIcon);
                    if ( section1.loopCount > 0 ) {
                        var text = string.Format("<size=9>x {0}/{1}</size>", Mathf.Min(section1.currentLoopIteration, section1.loopCount), section1.loopCount);
                        var loopCountRect = Rect.MinMaxRect(sectionRect.xMin, sectionRect.yMin, loopRect.xMin - 2, sectionRect.yMax);
                        GUI.Label(loopCountRect, text, Styles.rightLabel);
                    }
                }
                GUI.color = section1.color.grayscale >= 0.5 ? Color.black : Color.white;
                GUI.Label(sectionRect, string.Format(" <i>{0}</i>", section1.name));
                GUI.color = Color.white;


                if ( sectionRect.Contains(e.mousePosition) ) {
                    if ( e.type == EventType.MouseDown && e.button == 0 ) {
                        if ( e.clickCount == 2 ) {
                            viewTimeMin = section1.time;
                            viewTimeMax = section2.time;
                            e.Use();
                        }
                    }
                    if ( i != 0 && e.type == EventType.ContextClick && sectionsMenu != null ) {
                        sectionsMenu.AddItem(new GUIContent("Edit"), false, () =>
                        {
                            DoPopup(() =>
                                {
                                    section1.name = EditorGUILayout.TextField("Name", section1.name);
                                    var previousSectionTime = sections.Last(s => s.time < section1.time && s != section1).time;
                                    var nextSectionTime = sections.First(s => s.time > section1.time && s != section1).time;
                                    section1.time = EditorGUILayout.Slider("Time", section1.time, previousSectionTime + 0.1f, nextSectionTime - 0.1f);
                                    section1.exitMode = (Section.ExitMode)EditorGUILayout.EnumPopup("Exit Mode", section1.exitMode);
                                    if ( section1.exitMode == Section.ExitMode.Loop ) {
                                        section1.loopCount = EditorGUILayout.IntField("Loops", section1.loopCount);
                                    }
                                    section1.color = EditorGUILayout.ColorField("Color", section1.color);
                                    section1.colorizeBackground = EditorGUILayout.Toggle("Colorize Background", section1.colorizeBackground);
                                });
                        });
                        sectionsMenu.AddItem(new GUIContent("Focus (Double Click)"), false, () => { viewTimeMin = section1.time; viewTimeMax = section2.time; });
                        sectionsMenu.AddSeparator("/");
                        sectionsMenu.AddItem(new GUIContent("Delete Section"), false, () => { group.sections.Remove(section1); });
                    }
                }

                if ( i != 0 && clickRect.Contains(e.mousePosition) ) {
                    this.AddCursorRect(clickRect, MouseCursor.SlideArrow);
                    if ( e.type == EventType.MouseDown && e.button == 0 ) {
                        draggedSection = section1;
                        e.Use();
                    }
                }
            }

            if ( draggedSection != null ) {
                var lastTime = draggedSection.time;
                var newTime = PosToTime(mousePosition.x);
                var previousSectionTime = sections.Last(s => s.time < lastTime).time;
                var nextSectionTime = sections.First(s => s.time > lastTime).time;
                newTime = SnapTime(newTime);
                newTime = Mathf.Clamp(newTime, previousSectionTime + 0.1f, nextSectionTime - 0.1f);
                newTime = Mathf.Clamp(newTime, 0, maxTime);
                draggedSection.time = newTime;

                //shift clips if shift.
                if ( e.shift || Prefs.rippleMode ) {
                    foreach ( var cw in clipWrappers.Values.Where(c => c.action.startTime >= lastTime) ) {
                        if ( cw.action.isLocked ) { continue; }
                        var max = cw.previousClip != null ? cw.previousClip.endTime : 0;
                        if ( cw.action.CanCrossBlend(cw.previousClip) ) { max -= Mathf.Min(cw.previousClip.length / 2, cw.action.length / 2); }
                        cw.action.startTime += newTime - lastTime;
                        cw.action.startTime = Mathf.Max(cw.action.startTime, max);
                    }

                    //This is very unoptimized but PropertyTrack will be deprecated in the future.
                    foreach ( var propTrack in cutscene.directables.OfType<PropertiesTrack>() ) {
                        if ( propTrack.isLocked ) { continue; }
                        var curves = propTrack.GetCurvesAll();
                        foreach ( var curve in curves ) {
                            for ( var i = 0; i < curve.length; i++ ) {
                                var key = curve[i];
                                if ( key.time >= lastTime ) {
                                    key.time += newTime - lastTime;
                                    curve.MoveKey(i, key);
                                }
                            }
                            curve.UpdateTangentsFromMode();
                        }
                        CutsceneUtility.RefreshAllAnimationEditorsOf(propTrack.animationData);
                    }
                    //
                }

                //shift sections if shift or control
                if ( e.shift || e.control || Prefs.rippleMode ) {
                    foreach ( var section in group.sections.Where(s => s != draggedSection && s.time > lastTime) ) {
                        section.time += newTime - lastTime;
                    }
                }

                //reset interaction and order sections
                if ( e.rawType == EventType.MouseUp ) {
                    draggedSection = null;
                    group.sections = group.sections.OrderBy(s => s.time).ToList();
                }
            }

            if ( sectionsMenu != null ) {
                sectionsMenu.ShowAsContext();
            }
        }

        //This is done in a GUILayout.Group, thus must use e.mousePosition instead of this.mousePosition
        void DoMultiSelection() {

            var e = Event.current;

            var r = new Rect();
            var bigEnough = false;
            if ( multiSelectStartPos != null ) {
                var start = (Vector2)multiSelectStartPos;
                if ( ( start - e.mousePosition ).magnitude > 10 ) {
                    bigEnough = true;
                    r.xMin = Mathf.Max(Mathf.Min(start.x, e.mousePosition.x), 0);
                    r.xMax = Mathf.Min(Mathf.Max(start.x, e.mousePosition.x), screenWidth);
                    r.yMin = Mathf.Min(start.y, e.mousePosition.y);
                    r.yMax = Mathf.Max(start.y, e.mousePosition.y);
                    GUI.color = isProSkin ? Color.white : Color.white.WithAlpha(0.3f);
                    GUI.Box(r, string.Empty, Styles.hollowFrameStyle);
                    GUI.color = Color.white.WithAlpha(0.05f);
                    GUI.DrawTexture(r, whiteTexture);
                    GUI.color = Color.white;
                    foreach ( var wrapper in clipWrappers.Values.Where(b => r.Overlaps(b.rect) && !b.editorBinding.IsLocked) ) {
                        GUI.color = new Color(0.5f, 0.5f, 1, 0.5f);
                        GUI.Box(wrapper.rect, string.Empty, Slate.Styles.clipBoxStyle);
                        GUI.color = Color.white;
                    }
                }
            }

            if ( e.rawType == EventType.MouseUp ) {
                if ( bigEnough ) {
                    multiSelection = clipWrappers.Values.Where(b => r.Overlaps(b.rect) && !b.editorBinding.IsLocked).ToList();
                    if ( multiSelection.Count == 1 ) {
                        ActionClip selectedAction = multiSelection[0].action;
                        if (embeddedTimeline != null && multiSelection[0].editorBinding.FormalClip != null)
                            embeddedTimeline.Select(multiSelection[0].editorBinding.FormalClip);
                        else
                            CutsceneUtility.selectedObject = selectedAction;
                        multiSelection = null;
                    }
                }
                multiSelectStartPos = null;
            }

            if ( multiSelection != null ) {
                var boundRect = RectUtility.GetBoundRect(multiSelection.Select(b => b.rect).ToArray()).ExpandBy(4);

                var leftDragRect = new Rect(boundRect.xMin - 6, boundRect.yMin, 4, boundRect.height);
                var rightDragRect = new Rect(boundRect.xMax + 2, boundRect.yMin, 4, boundRect.height);
                AddCursorRect(leftDragRect, MouseCursor.ResizeHorizontal);
                AddCursorRect(rightDragRect, MouseCursor.ResizeHorizontal);
                GUI.color = isProSkin ? new Color(0.7f, 0.7f, 0.7f) : Color.grey;
                GUI.DrawTexture(leftDragRect, Styles.whiteTexture);
                GUI.DrawTexture(rightDragRect, Styles.whiteTexture);
                GUI.color = Color.white;

                if ( e.type == EventType.MouseDown && ( leftDragRect.Contains(e.mousePosition) || rightDragRect.Contains(e.mousePosition) ) ) {
                    multiSelectionScaleDirection = leftDragRect.Contains(e.mousePosition) ? -1 : 1;
                    var minTime = Mathf.Min(multiSelection.Select(b => b.editorBinding.StartTime).ToArray());
                    var maxTime = Mathf.Max(multiSelection.Select(b => b.editorBinding.EndTime).ToArray());
                    preMultiSelectionRetimeMinMax = Rect.MinMaxRect(minTime, 0, maxTime, 0);
                    foreach ( var wrapper in multiSelection ) {
                        wrapper.BeginClipAdjust();
                    }
                    e.Use();
                }

                if ( e.type == EventType.MouseDrag && multiSelectionScaleDirection != 0 ) {
                    foreach ( var clipWrapper in multiSelection ) {
                        var preTimeMin = preMultiSelectionRetimeMinMax.xMin;
                        var preTimeMax = preMultiSelectionRetimeMinMax.xMax;
                        var pointerTime = SnapTime(PosToTime(mousePosition.x));

                        var lerpMin = multiSelectionScaleDirection == -1 ? Mathf.Clamp(pointerTime, 0, preTimeMax) : preTimeMin;
                        var lerpMax = multiSelectionScaleDirection == 1 ? Mathf.Max(pointerTime, preTimeMin) : preTimeMax;

                        var normIn = Mathf.InverseLerp(preTimeMin, preTimeMax, clipWrapper.preScaleStartTime);
                        clipWrapper.editorBinding.StartTime = Mathf.Lerp(lerpMin, lerpMax, normIn);

                        var normOut = Mathf.InverseLerp(preTimeMin, preTimeMax, clipWrapper.preScaleEndTime);
                        clipWrapper.editorBinding.EndTime = Mathf.Lerp(lerpMin, lerpMax, normOut);

                        clipWrapper.UpdateClipAdjustContents();
                    }
                    e.Use();
                }

                if ( e.rawType == EventType.MouseUp ) {
                    multiSelectionScaleDirection = 0;
                    foreach ( var clipWrapper in multiSelection ) {
                        clipWrapper.EndClipAdjust();
                    }
                }
            }

            if ( e.type == EventType.MouseDown && e.button == 0 && GUIUtility.hotControl == 0 ) {
                multiSelection = null;
                multiSelectStartPos = e.mousePosition;
            }

            GUI.color = Color.white;
        }


        ///----------------------------------------------------------------------------------------------

        //...
        void ShowWelcome() {

            var bgRect = Rect.MinMaxRect(0, 0, screenWidth, screenHeight);
            GUI.color = Color.black.WithAlpha(0.1f);
            GUI.DrawTexture(bgRect, whiteTexture);
            GUI.color = Color.black.WithAlpha(0.03f);
            GUI.DrawTextureWithTexCoords(bgRect, Styles.stripes, new Rect(0, 0, bgRect.width / -7, bgRect.height / -7));
            GUI.color = Color.white;

            if ( cutscene == null ) {
                isAboutButtonPressed = false;
            }

            var label = string.Format("<size=24><b>{0}</b></size>", "Welcome to SLATE Cinematic Sequencer!");
            var size = new GUIStyle("label").CalcSize(new GUIContent(label));
            var titleRect = new Rect(0, 0, size.x, size.y);
            titleRect.center = new Vector2(screenWidth / 2, size.y + 160);
            GUI.Label(titleRect, label);

            var iconRect = new Rect(0, 0, 128, 128);
            iconRect.x = titleRect.x;
            iconRect.y = titleRect.y - 128;
            EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Link);
            if ( GUI.Button(iconRect, Styles.slateIcon, GUIStyle.none) ) {
                Help.BrowseURL("https://slate.paradoxnotion.com");
            }

            GUI.color = Color.white.WithAlpha(0.8f);
            GUI.Label(new Rect(iconRect.x + 68, iconRect.yMax - 42, iconRect.width, 40), "v" + Cutscene.VERSION_NUMBER, Styles.leftLabel);
            GUI.color = Color.white;

            var boardRect = Rect.MinMaxRect(iconRect.xMax, iconRect.y + 10, titleRect.xMax, iconRect.yMax - 10);
            GUI.color = Color.black.WithAlpha(0.2f);
            GUI.Box(boardRect, string.Empty);
            GUI.color = Color.white;
            GUI.Label(boardRect.ExpandBy(-5), "从 Skill Graph 打开 Timeline 进行作者编辑。\n\n角色运行预览由 Graph Shell 管理。", EditorStyles.wordWrappedLabel);

            var buttonsRect = Rect.MinMaxRect(titleRect.xMin, titleRect.yMax + 5, titleRect.xMax, screenHeight);
            var openRect = new Rect(buttonsRect.xMax - 40, buttonsRect.yMin, 40, 40);
            if ( !isAboutButtonPressed && GUI.Button(openRect, string.Empty) ) {
                GenericMenu.MenuFunction2 SelectCutscene = (object cut) =>
                {
                    Selection.activeObject = (Cutscene)cut;
                    EditorGUIUtility.PingObject((Cutscene)cut);
                    InitializeAll((Cutscene)cut);
                };

                var cutscenes = UnityObjectUtility.FindObjectsByType<Cutscene>();
                var menu = new GenericMenu();
                foreach ( Cutscene cut in cutscenes ) {
                    menu.AddItem(new GUIContent(string.Format("[{0}]", cut.name)), cut == cutscene, SelectCutscene, cut);
                }
                menu.ShowAsContext();
                Event.current.Use();
            }

            GUILayout.BeginArea(buttonsRect);

            if ( !isAboutButtonPressed ) {
                GUI.backgroundColor = new Color(0.8f, 0.8f, 1, 1f);
                if ( GUILayout.Button("New Cutscene", GUILayout.Height(40)) ) {
                    InitializeAll(Commands.CreateCutscene());
                }
                GUI.backgroundColor = Color.white;
            }

            if ( GUILayout.Button("Documentation", GUILayout.Height(40)) ) {
                Help.BrowseURL("https://slate.paradoxnotion.com/documentation");
            }

            if ( GUILayout.Button("Downloads", GUILayout.Height(40)) ) {
                Help.BrowseURL("https://slate.paradoxnotion.com/downloads");
            }

            if ( GUILayout.Button("Support Forums", GUILayout.Height(40)) ) {
                Help.BrowseURL("https://paradoxnotion.com/forums-page/");
            }

            if ( GUILayout.Button("Discord Community", GUILayout.Height(40)) ) {
                Help.BrowseURL("https://discord.gg/97q2Rjh");
            }

            if ( !isAboutButtonPressed && GUILayout.Button("Leave a Review :-)", GUILayout.Height(40)) ) {
                Help.BrowseURL("https://u3d.as/ozt");
            }

            GUI.color = Color.white.WithAlpha(0.5f);
            GUILayout.Label("© 2016-2026 Paradox Notion. All rights reserved.");
            GUI.color = Color.white;

            GUILayout.EndArea();

            if ( !isAboutButtonPressed ) { GUI.Label(openRect, "...", Styles.centerLabel); }

            if ( isAboutButtonPressed && cutscene != null ) {
                var backRect = new Rect(0, 0, titleRect.width, 20);
                backRect.center = new Vector2(screenWidth / 2, 20);
                GUI.backgroundColor = new Color(0.8f, 0.8f, 1, 1f);
                if ( GUI.Button(backRect, "Close Panel") ) {
                    isAboutButtonPressed = false;
                }
                GUI.backgroundColor = Color.white;
            }
        }

        ///----------------------------------------------------------------------------------------------


        //ActionClip window callback. Its ID is based on the UID function that is based on the index path to the action.
        //The ID of the window is also the same as the ID to use for for clipWrappers dictionary as key to get the clipWrapper for the action that represents this window
        void ActionClipWindow(int id) {
            ActionClipWrapper wrapper = null;
            if ( clipWrappers.TryGetValue(id, out wrapper) ) {
                wrapper.OnClipGUI(id);
            }
        }


        ///----------------------------------------------------------------------------------------------


        //A wrapper of an ActionClip placed in cutscene
        class ActionClipWrapper
        {

            const float CLIP_DOPESHEET_HEIGHT = 13f;
            const float SCALE_RECT_WIDTH = 5;

            public IClipEditorBinding editorBinding;
            public ActionClip action => editorBinding.NativeAction;
            public bool isDragging;
            public bool isScalingStart;
            public bool isScalingEnd;
            public bool isControlingBlendIn;
            public bool isControlingBlendOut;
            public Dictionary<int, Keyframe[]> preScaleKeys;
            public float preScaleStartTime;
            public float preScaleEndTime;
            public int preScaleClipInFrame;
            public float preScaleSubclipOffset;
            public float preScaleSubclipSpeed;

            IClipEditorBinding previousBinding;
            IClipEditorBinding nextBinding;
            public ActionClip previousClip
            {
                get => previousBinding?.NativeAction;
                set => previousBinding = value != null ? new NativeClipEditorBinding(value) : null;
            }
            public ActionClip nextClip
            {
                get => nextBinding?.NativeAction;
                set => nextBinding = value != null ? new NativeClipEditorBinding(value) : null;
            }

            private Event e;
            private int windowID;
            private bool isWaitingMouseDrag;
            private float overlapIn;
            private float overlapOut;
            private float blendInPosX;
            private float blendOutPosX;
            private bool hasActiveParameters;
            private bool hasParameters;
            private float pointerTime;
            private float snapedPointerTime;
            private bool allowScale;

            private Rect dragRect;
            private Rect controlRectIn;
            private Rect controlRectOut;

            private CutsceneEditorSurface editor {
                get { return CutsceneEditorSurface.current; }
            }

            private List<ActionClipWrapper> multiSelection {
                get { return editor.multiSelection; }
                set { editor.multiSelection = value; }
            }

            private Rect _rect;
            public Rect rect {
                get { return editorBinding.IsCollapsed ? default(Rect) : _rect; }
                set { _rect = value; }
            }

            public ActionClipWrapper(ActionClip action) {
                editorBinding = new NativeClipEditorBinding(action);
            }

            public ActionClipWrapper(IEmbeddedTimelineClipBinding clip) {
                editorBinding = new FormalClipEditorBinding(clip);
            }

            public void SetNeighbors(IClipEditorBinding previous, IClipEditorBinding next)
            {
                previousBinding = previous;
                nextBinding = next;
            }

            public bool Matches(IClipEditorBinding candidate)
            {
                return candidate != null &&
                       (ReferenceEquals(editorBinding.FormalClip, candidate.FormalClip) ||
                        ReferenceEquals(editorBinding.NativeAction, candidate.NativeAction));
            }

            public void ResetInteraction() {
                isWaitingMouseDrag = false;
                isDragging = false;
                isControlingBlendIn = false;
                isControlingBlendOut = false;
                isScalingStart = false;
                isScalingEnd = false;
            }

            public void OnClipGUI(int windowID) {
                this.windowID = windowID;
                e = Event.current;

                overlapIn = previousBinding != null ? Mathf.Max(previousBinding.EndTime - editorBinding.StartTime, 0) : 0;
                overlapOut = nextBinding != null ? Mathf.Max(editorBinding.EndTime - nextBinding.StartTime, 0) : 0;
                blendInPosX = ( editorBinding.BlendIn / editorBinding.Length ) * rect.width;
                blendOutPosX = ( ( editorBinding.Length - editorBinding.BlendOut ) / editorBinding.Length ) * rect.width;
                hasParameters = editorBinding.HasParameters;
                hasActiveParameters = editorBinding.HasActiveParameters;

                pointerTime = editor.PosToTime(editor.mousePosition.x);
                snapedPointerTime = editor.SnapTime(pointerTime);

                allowScale = editorBinding.CanScale && editorBinding.Length > 0 && rect.width > SCALE_RECT_WIDTH * 2;
                dragRect = new Rect(0, 0, rect.width, rect.height - ( hasActiveParameters ? CLIP_DOPESHEET_HEIGHT : 0 )).ExpandBy(allowScale ? -SCALE_RECT_WIDTH : 0, 0);
                controlRectIn = new Rect(0, 0, SCALE_RECT_WIDTH, rect.height - ( hasActiveParameters ? CLIP_DOPESHEET_HEIGHT : 0 ));
                controlRectOut = new Rect(rect.width - SCALE_RECT_WIDTH, 0, SCALE_RECT_WIDTH, rect.height - ( hasActiveParameters ? CLIP_DOPESHEET_HEIGHT : 0 ));

                editor.AddCursorRect(dragRect, MouseCursor.Link);
                if ( allowScale ) {
                    editor.AddCursorRect(controlRectIn, MouseCursor.ResizeHorizontal);
                    editor.AddCursorRect(controlRectOut, MouseCursor.ResizeHorizontal);
                }

                //...
                var wholeRect = new Rect(0, 0, rect.width, rect.height);
                if (editorBinding.IsLocked &&
                    (e.type == EventType.MouseDown ||
                     e.type == EventType.MouseDrag ||
                     e.type == EventType.MouseUp ||
                     e.type == EventType.ContextClick) &&
                    wholeRect.Contains(e.mousePosition))
                    e.Use();
                editorBinding.DrawClipGUI(wholeRect);
                if ( hasActiveParameters && editorBinding.Length > 0 ) {
                    ShowClipDopesheet(wholeRect);
                }
                //...


                //set crossblend overlap properties. Do this when no clip is interacting or no clip is dragging
                //this way avoid issue when moving clip on the other side of another, but keep overlap interactive when scaling a clip at least.
                if ( editorBinding.FormalClip == null &&
                     ( editor.interactingClip == null || !editor.interactingClip.isDragging ) ) {
                        var overlap = previousBinding != null ? Mathf.Max(previousBinding.EndTime - editorBinding.StartTime, 0) : 0;
                        if ( overlap > 0 ) {
                        editorBinding.BlendIn = overlap;
                        previousBinding.BlendOut = overlap;
                        }
                }


                if ( e.type == EventType.MouseDown ) {

                    if ( e.button == 0 ) {
                        if ( dragRect.Contains(e.mousePosition) ) {
                            isWaitingMouseDrag = true;
                        }
                        editor.interactingClip = this;
                        editor.CacheMagnetSnapTimes(editorBinding);
                    }

                    if ( e.control && dragRect.Contains(e.mousePosition) ) {
                        if ( multiSelection == null ) {
                            multiSelection = new List<ActionClipWrapper>() { this };
                        }
                        if ( multiSelection.Contains(this) ) {
                            multiSelection.Remove(this);
                        } else {
                            multiSelection.Add(this);
                        }
                        if (editor.embeddedTimeline != null && editorBinding.FormalClip != null)
                            editor.formalSelectionHandled = true;
                    } else {
                        if (editor.embeddedTimeline != null && editorBinding.FormalClip != null)
                        {
                            editor.embeddedTimeline.Select(editorBinding.FormalClip);
                            editor.formalSelectionHandled = true;
                        }
                        else if (action != null)
                            CutsceneUtility.selectedObject = action;
                        if ( multiSelection != null && !multiSelection.Select(cw => cw.editorBinding).Contains(editorBinding) ) {
                            multiSelection = null;
                        }
                    }

                    if ( e.clickCount == 2 ) {
                        if (action != null)
                            OnActionDoubleClick?.Invoke(action);
                        else if (editor.embeddedTimeline != null && editorBinding.FormalClip != null)
                            editor.embeddedTimeline.OpenSource(editorBinding.FormalClip);
                        if (!editor.embeddedSurface && action != null)
                            Selection.activeObject = action.GetType().GetProperty("actor").GetValue(action, null) as UnityEngine.Object;
                    }
                }

                if ( e.type == EventType.MouseDrag && isWaitingMouseDrag ) {
                    isDragging = true;
                    isWaitingMouseDrag = false;
                    if (editorBinding.FormalClip != null)
                        editor.BeginEmbeddedEdit("Move Timeline Clip");
                }

                if ( e.rawType == EventType.ContextClick ) {
                    DoClipContextMenu();
                }


                DrawBlendGraphics();
                DoEdgeControls();


                if ( e.rawType == EventType.MouseUp ) {
                    if ( editor.interactingClip != null ) {
                        editor.interactingClip.EndClipAdjust();
                        editor.interactingClip.ResetInteraction();
                        editor.interactingClip = null;
                    }
                }

                if ( e.button == 0 ) {
                    GUI.DragWindow(dragRect);
                }

                //Draw info text if big enough
                if ( rect.width > 20 ) {
                    var r = new Rect(1, 1, rect.width - 2, rect.height - 2);
                    if ( overlapIn > 0 ) { r.xMin = blendInPosX; }
                    if ( overlapOut > 0 ) { r.xMax = blendOutPosX; }
                    var label = string.Format("<size=10>{0}</size>", editorBinding.Info);
                    GUI.color = Color.black;
                    GUI.Label(r, label);
                    GUI.color = Color.white;
                }
            }

            //blend graphics
            void DrawBlendGraphics() {
                ClipEditorGUI.DrawBlendGraphics(
                    rect,
                    blendInPosX,
                    blendOutPosX,
                    editorBinding.BlendIn,
                    editorBinding.BlendOut,
                    overlapIn,
                    overlapOut);
            }

            //clip scale/blend in/out controls
            void DoEdgeControls() {

                var canBlendIn = editorBinding.CanBlendIn && editorBinding.Length > 0;
                var canBlendOut = editorBinding.CanBlendOut && editorBinding.Length > 0;
                if ( !isScalingStart && !isScalingEnd && !isControlingBlendIn && !isControlingBlendOut ) {
                    if ( allowScale || canBlendIn ) {
                        if ( controlRectIn.Contains(e.mousePosition) ) {
                            GUI.BringWindowToFront(windowID);
                            GUI.DrawTexture(controlRectIn.ExpandBy(0, -2), whiteTexture);
                            if ( e.type == EventType.MouseDown && e.button == 0 ) {
                                if ( allowScale && !e.control ) { isScalingStart = true; }
                                if ( canBlendIn && e.control ) { isControlingBlendIn = true; }
                                BeginClipAdjust();
                                e.Use();
                            }
                        }
                    }

                    if ( allowScale || canBlendOut ) {
                        if ( controlRectOut.Contains(e.mousePosition) ) {
                            GUI.BringWindowToFront(windowID);
                            GUI.DrawTexture(controlRectOut.ExpandBy(0, -2), whiteTexture);
                            if ( e.type == EventType.MouseDown && e.button == 0 ) {
                                if ( allowScale && !e.control ) { isScalingEnd = true; }
                                if ( canBlendOut && e.control ) { isControlingBlendOut = true; }
                                BeginClipAdjust();
                                e.Use();
                            }
                        }
                    }
                }

                if ( isControlingBlendIn ) { editorBinding.BlendIn = Mathf.Clamp(pointerTime - editorBinding.StartTime, 0, editorBinding.Length - editorBinding.BlendOut); }
                if ( isControlingBlendOut ) { editorBinding.BlendOut = Mathf.Clamp(editorBinding.EndTime - pointerTime, 0, editorBinding.Length - editorBinding.BlendIn); }

                if ( isScalingStart ) {
                    var prevTime = previousBinding != null ? previousBinding.EndTime : 0;
                    //magnet snap
                    if ( Prefs.magnetSnapping && !e.control ) {
                        var snapStart = editor.MagnetSnapTime(snapedPointerTime, editor.magnetSnapTimesCache);
                        if ( snapStart != null ) {
                            snapedPointerTime = snapStart.Value;
                            editor.pendingGuides.Add(new GuideLine(snapedPointerTime, Color.white));
                        }
                    }

                    if ( editorBinding.CanCrossBlend(previousBinding) ) { prevTime -= Mathf.Min(editorBinding.Length / 2, previousBinding.Length / 2); }

                    editorBinding.StartTime = snapedPointerTime;
                    editorBinding.StartTime = Mathf.Clamp(editorBinding.StartTime, prevTime, preScaleEndTime);
                    editorBinding.EndTime = preScaleEndTime;

                    UpdateClipAdjustContents();
                }

                if ( isScalingEnd ) {
                    var nextTime = nextBinding != null ? nextBinding.StartTime : editor.maxTime;
                    //magnet snap
                    if ( Prefs.magnetSnapping && !e.control ) {
                        var snapEnd = editor.MagnetSnapTime(snapedPointerTime, editor.magnetSnapTimesCache);
                        if ( snapEnd != null ) {
                            snapedPointerTime = snapEnd.Value;
                            editor.pendingGuides.Add(new GuideLine(snapedPointerTime, Color.white));
                        }
                    }

                    if ( editorBinding.CanCrossBlend(nextBinding) ) { nextTime += Mathf.Min(editorBinding.Length / 2, nextBinding.Length / 2); }

                    editorBinding.EndTime = snapedPointerTime;
                    editorBinding.EndTime = Mathf.Clamp(editorBinding.EndTime, 0, nextTime);

                    UpdateClipAdjustContents();
                }
            }


            //store pre adjust values
            public void BeginClipAdjust() {
                preScaleStartTime = editorBinding.StartTime;
                preScaleEndTime = editorBinding.EndTime;
                preScaleClipInFrame = editorBinding.FormalClip?.ClipInFrame ?? 0;

                preScaleKeys = new Dictionary<int, Keyframe[]>();
                var curves = editorBinding.Curves;
                for ( var i = 0; i < curves.Length; i++ ) {
                    preScaleKeys[i] = curves[i].keys;
                }

                if ( action is ISubClipContainable ) {
                    preScaleSubclipOffset = ( action as ISubClipContainable ).subClipOffset;
                    preScaleSubclipSpeed = ( action as ISubClipContainable ).subClipSpeed;
                }
                editor.CacheMagnetSnapTimes(editorBinding);
                if (action == null)
                    editor.BeginEmbeddedEdit("Adjust Timeline Clip");
            }

            //retime keys lerp between start/end time.
            public void UpdateClipAdjustContents() {

                if ( preScaleKeys == null ) { return; }

                var retime = Event.current.control || Prefs.retimeMode;
                var trim = !Event.current.shift && !Prefs.rippleMode && !retime;

                ClipEditorGUI.UpdateScaledCurves(
                    editorBinding.Curves,
                    preScaleKeys,
                    preScaleStartTime,
                    preScaleEndTime,
                    editorBinding.StartTime,
                    editorBinding.Length,
                    retime,
                    trim);

                if (editorBinding.FormalClip != null &&
                    editorBinding.FormalClip.CanClipIn &&
                    trim &&
                    isScalingStart)
                {
                    int deltaFrame = Mathf.RoundToInt(
                        (preScaleStartTime - editorBinding.StartTime) * editor.embeddedTimeline.FrameRate);
                    editorBinding.FormalClip.ClipInFrame = preScaleClipInFrame + deltaFrame;
                }

                if (editorBinding.FormalClip is IEmbeddedTimelineSourceRangeBinding sourceRange &&
                    trim &&
                    (isScalingStart || isScalingEnd))
                {
                    sourceRange.AdjustSourceRange(
                        Mathf.RoundToInt(preScaleStartTime * editor.embeddedTimeline.FrameRate),
                        Mathf.RoundToInt(preScaleEndTime * editor.embeddedTimeline.FrameRate),
                        Mathf.RoundToInt(editorBinding.StartTime * editor.embeddedTimeline.FrameRate),
                        Mathf.RoundToInt(editorBinding.EndTime * editor.embeddedTimeline.FrameRate),
                        isScalingStart);
                }

                if (action != null)
                    CutsceneUtility.RefreshAllAnimationEditorsOf(action.animationData);

                if ( action is ISubClipContainable ) {
                    if ( trim ) {
                        var subClip = (ISubClipContainable)action;
                        var delta = preScaleStartTime - editorBinding.StartTime;
                        var newOffset = preScaleSubclipOffset + delta;
                        subClip.subClipOffset = newOffset;
                    }
                }
            }

            //flush pre adjust values
            public void EndClipAdjust() {
                preScaleKeys = null;
                if ( Prefs.autoCleanKeysOffRange ) {
                    CleanKeysOffRange();
                }
                if (editorBinding.FormalClip != null)
                    editor.CommitEmbeddedEdit();
            }



            ///<summary>Split the clip in two, at specified local time</summary>
            public ActionClip Split(float time) {

                if ( !action.IsTimeWithinClip(time) ) {
                    return null;
                }

                if ( hasParameters ) {
                    foreach ( var param in action.animationData.animatedParameters ) {
                        if ( param.HasAnyKey() ) { param.TryKeyIdentity(action.ToLocalTime(time)); }
                    }
                }

                CutsceneUtility.CopyClip(action);
                var copy = CutsceneUtility.PasteClip((CutsceneTrack)action.parent, time);
                copy.startTime = time;
                copy.endTime = action.endTime;
                action.endTime = time;
                copy.blendIn = 0;
                action.blendOut = 0;
                CutsceneUtility.selectedObject = null;
                CutsceneUtility.FlushCopy();

                var delta = action.length;
                if ( hasParameters ) {
                    foreach ( var curve in copy.GetCurvesAll() ) {
                        curve.OffsetCurveTime(-delta);
                        curve.RemoveNegativeKeys();
                    }
                    CutsceneUtility.RefreshAllAnimationEditorsOf(action.animationData);
                }

                if ( copy is ISubClipContainable ) {
                    ( copy as ISubClipContainable ).subClipOffset -= delta;
                }

                return copy;
            }

            ///<summary>Scale clip to fit previous and next</summary>
            public void StretchFit() {
                var wasStartTime = action.startTime;
                var wasEndTime = action.endTime;
                var targetStart = previousClip != null ? previousClip.endTime : action.parent.startTime;
                var targetEnd = nextClip != null ? nextClip.startTime : action.parent.endTime;
                if ( previousClip == null || previousClip.endTime < action.startTime ) {
                    action.startTime = targetStart;
                    action.endTime = wasEndTime;
                }
                if ( nextClip == null || nextClip.startTime > action.endTime ) {
                    action.endTime = targetEnd;
                }

                var delta = wasStartTime - action.startTime;
                if ( hasParameters ) {
                    foreach ( var curve in action.GetCurvesAll() ) {
                        curve.OffsetCurveTime(delta);
                    }
                    CutsceneUtility.RefreshAllAnimationEditorsOf(action.animationData);
                }

                if ( action is ISubClipContainable ) {
                    ( action as ISubClipContainable ).subClipOffset += delta;
                }
            }

            ///<summary>Clean keys off clip range after adding a key at 0 and length if there is any key outside that range</summary>
            public void CleanKeysOffRange() {
                if (editorBinding.FormalClip != null)
                {
                    editorBinding.FormalClip.CleanKeysOffRange();
                    return;
                }
                if ( hasParameters ) {
                    foreach ( var param in action.animationData.animatedParameters ) {
                        if ( param.HasAnyKey() ) {
                            if ( param.GetKeyPrevious(0) < 0 ) {
                                param.TryKeyIdentity(0);
                            }
                            if ( param.GetKeyNext(action.length) > action.length ) {
                                param.TryKeyIdentity(action.length);
                            }
                        }
                    }
                    foreach ( var curve in action.GetCurvesAll() ) {
                        curve.RemoveKeysOffRange(0, action.length);
                        curve.UpdateTangentsFromMode();
                    }
                    CutsceneUtility.RefreshAllAnimationEditorsOf(action.animationData);
                }
            }

            //Show the clip dopesheet
            void ShowClipDopesheet(Rect rect) {
                var dopeRect = new Rect(0, rect.height - CLIP_DOPESHEET_HEIGHT, rect.width, CLIP_DOPESHEET_HEIGHT);
                GUI.color = isProSkin ? new Color(0, 0.2f, 0.2f, 0.5f) : new Color(0, 0.8f, 0.8f, 0.5f);
                GUI.Box(dopeRect, string.Empty, Slate.Styles.clipBoxHorizontalStyle);
                GUI.color = Color.white;
                if (editorBinding.FormalClip != null)
                {
                    TrackEditorGUI.DrawClipDopeSheet(editorBinding.FormalClip, dopeRect);
                    return;
                }
                DopeSheetEditor.DrawDopeSheet(action.animationData, action, dopeRect, 0, action.length, false);
            }

            //CONTEXT
            void DoClipContextMenu() {
                var menu = new GenericMenu();
                IEmbeddedTimelineClipBinding formalClip = editorBinding.FormalClip;
                if ( multiSelection != null && multiSelection.Contains(this) ) {
                    var selectedFormalClips = multiSelection
                        .Select(value => value.editorBinding.FormalClip)
                        .Where(value => value != null)
                        .ToArray();
                    if (selectedFormalClips.Length != 0 && editor.embeddedTimeline != null)
                    {
                        menu.AddItem(new GUIContent("Delete Clips"), false, () =>
                        {
                            editor.embeddedTimeline.DeleteClips(selectedFormalClips);
                            multiSelection = null;
                        });
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("Delete Clips"), false, () =>
                        {
                            editor.SafeDoAction(() =>
                            {
                                foreach (var act in multiSelection.Select(b => b.action).ToArray())
                                    (act.parent as CutsceneTrack).DeleteAction(act);
                                editor.InitClipWrappers();
                                multiSelection = null;
                            });
                        });
                    }

                    menu.ShowAsContext();
                    e.Use();
                    return;
                }

                if ( !editor.embeddedSurface ) {
                    menu.AddItem(new GUIContent("Copy Clip"), false, () => { CutsceneUtility.CopyClip(action); });
                    menu.AddItem(new GUIContent("Cut Clip"), false, () => { CutsceneUtility.CutClip(action); });
                }
                else if ( formalClip != null && editor.embeddedTimeline != null ) {
                    menu.AddItem(new GUIContent("Copy Clip"), false, () => { editor.embeddedTimeline.CopyClip(formalClip); });
                }
                else if ( editor.embeddedCopyClip != null && action != null ) {
                    menu.AddItem(new GUIContent("Copy Clip"), false, () => { editor.embeddedCopyClip(action); });
                }

                if ( allowScale ) {
                    menu.AddItem(new GUIContent("Fit Clip (F)"), false, () =>
                    {
                        if (formalClip != null)
                            editor.ApplyEmbeddedCommand(formalClip.StretchFit, "Fit Clip");
                        else
                            StretchFit();
                    });
                    if ( !editor.embeddedSurface && action != null && action.length > 0 ) {
                        menu.AddItem(new GUIContent("Split At Cursor"), false, () => { Split(snapedPointerTime); });
                        menu.AddItem(new GUIContent("Split At Scrubber (S)"), false, () => { Split(editor.cutscene.currentTime); });
                    }
                }

                if ( hasParameters ) {
                    menu.AddItem(new GUIContent("Key At Cursor"), false, () =>
                    {
                        if (formalClip != null)
                            editor.ApplyEmbeddedCommand(() => formalClip.AddIdentityKey(Mathf.Clamp(snapedPointerTime - formalClip.StartTime, 0f, formalClip.Length)), "Key Clip");
                        else
                            action.TryAddIdentityKey(action.ToLocalTime(snapedPointerTime));
                    });
                    menu.AddItem(new GUIContent("Key At Scrubber (K)"), false, () =>
                    {
                        if (formalClip != null)
                            editor.ApplyEmbeddedCommand(() => formalClip.AddIdentityKey(Mathf.Clamp(editor.EmbeddedCurrentTime() - formalClip.StartTime, 0f, formalClip.Length)), "Key Clip");
                        else
                            action.TryAddIdentityKey(action.RootTimeToLocalTime());
                    });
                }

                menu.AddSeparator("/");

                if ( hasActiveParameters ) {
                    menu.AddItem(new GUIContent("Clean Keys Off-Range (C)"), false, () =>
                    {
                        if (formalClip != null)
                            editor.ApplyEmbeddedCommand(formalClip.CleanKeysOffRange, "Clean Keys");
                        else
                            CleanKeysOffRange();
                    });
                    menu.AddItem(new GUIContent("Remove Animation"), false, () =>
                    {
                        if ( EditorUtility.DisplayDialog("Remove Animation", "All Animation Curve keys of all animated parameters for this clip will be removed.\nAre you sure?", "Yes", "No") ) {
                            if (formalClip != null)
                                editor.ApplyEmbeddedCommand(formalClip.ResetAnimation, "Remove Animation");
                            else
                                editor.SafeDoAction(() => { action.ResetAnimatedParameters(); });
                        }
                    });
                }

                menu.AddItem(new GUIContent("Delete Clip"), false, () =>
                {
                    if (formalClip != null)
                    {
                        editor.embeddedTimeline.DeleteClip(formalClip);
                    }
                    else
                    {
                        editor.SafeDoAction(() =>
                        {
                            ( action.parent as CutsceneTrack ).DeleteAction(action);
                            editor.InitClipWrappers();
                        });
                    }
                });

                menu.ShowAsContext();
                e.Use();
            }
        }

    }
}

#endif
