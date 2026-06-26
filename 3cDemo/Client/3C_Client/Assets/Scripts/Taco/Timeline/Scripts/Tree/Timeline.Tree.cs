using System;
using System.Collections.Generic;
using UnityEngine;
using TreeDesigner;

namespace Taco.Timeline
{
    [TrackGroup("Base"), ScriptGuid("31085f11443fe1347b871c5d69db3774"), IconGuid("e28acf5dc5b2e3d4a97920bf4e831c87"), Ordered(3), Color(201, 060, 032)]
    public class TreeTrack : Track
    {
#if UNITY_EDITOR

        public override Type ClipType => typeof(TreeClip);

        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            TreeClip clip = new TreeClip(referenceObject as TimelineRunningTree, this, frame);
            m_Clips.Add(clip);
            return clip;
        }
        public override bool DragValid()
        {
            return UnityEditor.DragAndDrop.objectReferences.Length == 1 && UnityEditor.DragAndDrop.objectReferences[0] is TimelineRunningTree;
        }

#endif
    }

    [ScriptGuid("31085f11443fe1347b871c5d69db3774"), ClipInspectorView("TreeClipInspectorView"), Color(201, 060, 032)]
    public partial class TreeClip : Clip
    {
        [ShowInInspector, OnValueChanged("OnClipChanged", "ReInit", "RebindTimeline", "RepaintInspector"), HorizontalGroup("TreePrefab")]
        public TimelineRunningTree TreePrefab;
        [ShowInInspector, ReadOnly, HorizontalGroup("TreeInstance"), ShowIf("ShowIf")]
        public TimelineRunningTree TreeInstance;

        [SerializeReference]
        List<TreeProperty> m_Properties = new List<TreeProperty>();
        public List<TreeProperty> Properties => m_Properties;

        public override void Init(Track track)
        {
            base.Init(track);
            TreePrefab?.InitTree(null);
            for (int i = m_Properties.Count - 1; i >= 0; i--)
            {
                TreeProperty property = m_Properties[i];
                property.Init(this);
                if(property.ExposedProperty == null)
                    m_Properties.RemoveAt(i);
            }
        }
        public override void Bind()
        {
            base.Bind();

            Instantiate();

#if UNITY_EDITOR
            if (TreeInstance)
                TreeInstance.OnModified += OnTreeModified;
#endif

        }
        public override void Unbind()
        {
            base.Unbind();

#if UNITY_EDITOR
            if (TreeInstance)
                TreeInstance.OnModified -= OnTreeModified;
#endif

            Destroy();
        }
        public override void Evaluate(float deltaTime)
        {
            base.Evaluate(deltaTime);
            if (TreeInstance && Active)
            {
                TreeInstance.UpdateTree(deltaTime);
            }
        }
        public override void OnEnable()
        {
            TreeInstance?.OnTreeEnable();
        }
        public override void OnDisable()
        {
            TreeInstance?.OnTreeDisable();
        }


        void Instantiate()
        {
            if (TreePrefab)
            {
                if (Application.isPlaying)
                {
                    TreeInstance = UnityEngine.Object.Instantiate(TreePrefab);
                    TreeInstance.InitTree(this);
                }
                else
                {
                    TreeInstance = TreePrefab;
                    TreeInstance.InitTree(this);
                }

                foreach (var property in m_Properties)
                {
                    property.UpdateValue();
                }
            }
        }
        void Destroy()
        {
            if (!TreeInstance) return;

            if (Application.isPlaying)
            {
                if (TreeInstance)
                {
                    TreeInstance.OnTreeDestroy();
                    TreeInstance.ResetTree();
                    TreeInstance.DisposeTree();
                    UnityEngine.Object.Destroy(TreeInstance);
                    TreeInstance = null;
                }
            }
            else
            {
                if (TreeInstance.IsValid)
                {
                    TreeInstance.OnTreeDestroy();
                    TreeInstance.ResetTree();
                    TreeInstance.DisposeTree();
                    TreeInstance = null;
                }
            }
        }

#if UNITY_EDITOR

        public override string Name => TreePrefab ? TreePrefab.name : base.Name;
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable;

        public TreeClip(Track track, int frame) : base(track, frame)
        {
        }
        public TreeClip(TimelineRunningTree tree, Track track, int frame) : base(track, frame)
        {
            TreePrefab = tree;
        }

        public void AddProperty(BaseExposedProperty exposedProperty)
        {
            Type type = exposedProperty.ValueType;
            var genericType = typeof(TreeProperty<>).MakeGenericType(type);
            TreeProperty treeProperty = Activator.CreateInstance(genericType) as TreeProperty;
            treeProperty.TargetGUID = exposedProperty.GUID;
            treeProperty.Init(this);

            m_Properties.Add(treeProperty);
        }


        void ReInit()
        {
            Init(Track);
        }
        void OnTreeModified()
        {
            Timeline.RebindTrack(Track);
        }
        void OnClipChanged()
        {
            OnNameChanged?.Invoke();
        }
        [Button("Open"), HorizontalGroup("TreePrefab")]
        void OpenTreePrefab()
        {
            if (TreePrefab)
            {
                TreeDesigner.Editor.TreeWindowUtility.OpenTree(TreePrefab);
            }
        }
        [Button("Open"), HorizontalGroup("TreeInstance"), ShowIf("ShowIf")]
        void OpenTreeInstance()
        {
            if (TreeInstance)
            {
                TreeDesigner.Editor.TreeWindowUtility.OpenTree(TreeInstance);
            }
        }
        bool ShowIf()
        {
            return Application.isPlaying;
        }
#endif
    }

    [Serializable]
    public class TreeProperty
    {
        [HideInInspector]
        public string TargetGUID;

        [NonSerialized]
        public TreeClip TreeClip;
        [NonSerialized]
        public BaseExposedProperty ExposedProperty;

        public void Init(TreeClip treeClip)
        {
            TreeClip = treeClip;
            ExposedProperty = null;
            if (treeClip.TreePrefab)
            {
                if (treeClip.TreePrefab.GUIDExposedPropertyMap.TryGetValue(TargetGUID, out BaseExposedProperty exposedProperty))
                {
                    ExposedProperty = exposedProperty;
                }
            }
        }
        public void UpdateValue()
        {
            if (TreeClip.TreeInstance)
            {
                if (TreeClip.TreeInstance.GUIDExposedPropertyMap.TryGetValue(TargetGUID, out BaseExposedProperty exposedProperty))
                {
                    ExposedProperty = exposedProperty;
                }
            }
            ExposedProperty?.SetValue(GetValue());
        }

        public virtual object GetValue()
        {
            return null;
        }

    }

    [Serializable]
    public class TreeProperty<T> : TreeProperty
    {
        [SerializeField]
        protected T m_Value;
        public T Value { get => m_Value; set => m_Value = value; }

        public override object GetValue()
        {
            return m_Value;
        }
    }
}