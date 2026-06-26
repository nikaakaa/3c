using System;
using UnityEngine;

namespace Taco.Timeline
{
    [TrackGroup("Base"), ScriptGuid("c76545f3ff0961e4ba4395d075f7c341"), IconGuid("76c9ab52db448f24ba098ad7d930fd96"), Ordered(1), Color(165, 032, 025)]
    public class GameObjectTrack : Track
    {
#if UNITY_EDITOR

        public override Type ClipType => typeof(GameObjectClip);
        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            GameObjectClip clip = new GameObjectClip(referenceObject as GameObject, this, frame);
            m_Clips.Add(clip);
            return clip;
        }
        public override bool DragValid()
        {
            return UnityEditor.DragAndDrop.objectReferences.Length == 1 && UnityEditor.DragAndDrop.objectReferences[0] is GameObject gameObject && UnityEditor.EditorUtility.IsPersistent(gameObject);
        }
#endif
    }

    [ScriptGuid("c76545f3ff0961e4ba4395d075f7c341"), Color(165, 032, 025)]
    public class GameObjectClip : Clip
    {
        [ShowInInspector, OnValueChanged("OnClipChanged", "RebindTimeline")]
        public GameObject GameObjectPrefab;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string SocketName;

        public GameObject GameObjectInstance { get; private set; }


        public override void Unbind()
        {
            base.Unbind();
            Destroy();
        }
        public override void OnEnable()
        {
            Instantiate();
        }
        public override void OnDisable()
        {
            Destroy();
        }     

        void Instantiate()
        {
            if (GameObjectPrefab)
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

                GameObjectInstance = UnityEngine.Object.Instantiate(GameObjectPrefab, socketTransform, false);
            }
        }
        void Destroy()
        {
            if (GameObjectInstance)
            {
                UnityEngine.Object.DestroyImmediate(GameObjectInstance);
                GameObjectInstance = null;
            }
        }

#if UNITY_EDITOR

        public override string Name => GameObjectPrefab ? GameObjectPrefab.name : base.Name;
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable;
        public GameObjectClip(Track track, int frame) : base(track, frame) { }
        public GameObjectClip(GameObject gameObject, Track track, int frame) : base(track, frame)
        {
            GameObjectPrefab = gameObject;
        }

        void OnClipChanged()
        {
            OnNameChanged?.Invoke();
        }
#endif
    }
}