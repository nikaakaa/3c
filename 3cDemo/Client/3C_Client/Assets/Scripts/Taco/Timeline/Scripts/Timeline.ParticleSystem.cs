using System;
using UnityEngine;

namespace Taco.Timeline
{
    [TrackGroup("Base"), ScriptGuid("bd021c94b1615a64db462ca7874e19b6"), IconGuid("af6781f2578301947823eb6557b9a7cc"), Ordered(2), Color(127, 214, 253)]
    public class ParticleTrack : Track
    {
#if UNITY_EDITOR

        public override Type ClipType => typeof(ParticleClip);
        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            ParticleClip clip = new ParticleClip((referenceObject as GameObject).GetComponent<ParticleSystem>(), this, frame);
            m_Clips.Add(clip);
            return clip;
        }
        public override bool DragValid()
        {
            return UnityEditor.DragAndDrop.objectReferences.Length == 1 && UnityEditor.DragAndDrop.objectReferences[0] is GameObject gameObject && UnityEditor.EditorUtility.IsPersistent(gameObject) && gameObject.GetComponent<ParticleSystem>();
        }
#endif
    }

    [ScriptGuid("bd021c94b1615a64db462ca7874e19b6"), Color(127, 214, 253)]
    public class ParticleClip : Clip
    {
        [ShowInInspector, OnValueChanged("OnClipChanged", "RebindTimeline")]
        public ParticleSystem ParticlePrefab;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public ExtraPolationMode ExtraPolationMode;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string SocketName;
        [ShowInInspector, HideIf("UseSelfTransform"), HorizontalGroup("Position")]
        public Vector3 PositionOffset;
        [ShowInInspector, HideIf("UseSelfTransform"), HorizontalGroup("Rotation")]
        public Vector3 RotationOffset;
        [ShowInInspector(1), OnValueChanged("RebindTimeline", "RepaintInspector")]
        public bool UseSelfTransform = true;

        public ParticleSystem ParticleInstance { get; private set; }

        public override void Unbind()
        {
            base.Unbind();
            Destroy();
        }
        public override void Evaluate(float deltaTime)
        {
            base.Evaluate(deltaTime);
            if (ParticleInstance)
            {
                ParticleInstance.Simulate(OffsetTime);
            }
        }
        public override void OnEnable()
        {
            if (!ParticleInstance)
                Instantiate();
        }
        public override void OnDisable()
        {
            if(ExtraPolationMode == ExtraPolationMode.Hold && TargetTime > EndTime)
            {
                //keep
            }
            else
            {
                Destroy();
            }
        }

        void Instantiate()
        {
            if (ParticlePrefab)
            {
                Transform socketTransform = Timeline.TimelinePlayer.transform;
                var childTransforms = Timeline.TimelinePlayer.GetComponentsInChildren<Transform>();
                foreach (var childTransform in childTransforms)
                {
                    if (childTransform.name == SocketName)
                    {
                        socketTransform = childTransform;
                        break;
                    }
                }

                ParticleInstance = UnityEngine.Object.Instantiate(ParticlePrefab, socketTransform, false);
                if (!UseSelfTransform)
                {
                    ParticleInstance.transform.localPosition = PositionOffset;
                    ParticleInstance.transform.localEulerAngles = RotationOffset;
                }
            }
        }
        void Destroy()
        {
            if (ParticleInstance)
            {
                UnityEngine.Object.DestroyImmediate(ParticleInstance.gameObject);
                ParticleInstance = null;
            }
        }


#if UNITY_EDITOR

        public override string Name => ParticlePrefab ? ParticlePrefab.name : base.Name;
        public override int Length => ParticlePrefab ? Mathf.RoundToInt(ParticlePrefab.main.duration * TimelineUtility.FrameRate) : base.Length;
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.ClipInable;
        public ParticleClip(Track track, int frame) : base(track, frame) { }
        public ParticleClip(ParticleSystem particleSystem, Track track, int frame) : base(track, frame)
        {
            ParticlePrefab = particleSystem;
            EndFrame = Length + frame;
        }

        [Button("Record"), HideIf("UseSelfTransform"), ShowIf("ShowRecord"), HorizontalGroup("Position")]
        void RecordPosition()
        {
            if (ParticleInstance)
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(Track.Timeline, $"Timeline: RecordPosition");
                PositionOffset = ParticleInstance.transform.localPosition;
                UnityEditor.EditorUtility.SetDirty(Track.Timeline);                
            }
        }
        [Button("Record"), HideIf("UseSelfTransform"), ShowIf("ShowRecord"), HorizontalGroup("Rotation")]
        void RecordRotation()
        {
            if (ParticleInstance)
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(Track.Timeline, $"Timeline: RecordRotation");
                RotationOffset = ParticleInstance.transform.localEulerAngles;
                UnityEditor.EditorUtility.SetDirty(Track.Timeline);
            }
        }
        bool ShowRecord()
        {
            return ParticleInstance;
        }
        void OnClipChanged()
        {
            OnNameChanged?.Invoke();
        }
#endif
    }
}