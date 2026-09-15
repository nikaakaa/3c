using System.Collections.Generic;
using UnityEngine;
using ParadoxNotion.Design;
using System.Collections;
using ParadoxNotion;

namespace FlowCanvas.Nodes
{

    [Description("Split the Flow in multiple directions. If mode is Instant, all outputs are called the same frame (in order). If mode is Timed, the outputs can be time sequenced in the inspector.")]
    [Name("Split", 90)]
    public class Split : FlowControlNode
    {
        [SerializeField, ExposeField]
        [GatherPortsCallback]
        [MinValue(2), DelayedField]
        private int _portCount = 4;

        [GatherPortsCallback]
        public Mode mode;

        public enum Mode
        {
            Instant,
            Timed,
        }

        [System.Serializable]
        public class FlowTrack : ParadoxNotion.Animation.ISequencableTrack
        {
            [SerializeField]
            private float _time;

#if UNITY_EDITOR
            public string name => flowOut.displayName;
#else
            public string name => string.Empty;
#endif
            public float time { get => _time; set => _time = value; }
            public float length => 0;
            public FlowOutput flowOut { get; set; }
        }

        [SerializeField]
        private List<FlowTrack> _tracks;
        private bool broken;

        ///----------------------------------------------------------------------------------------------

        protected override void RegisterPorts() {

            if ( mode == Mode.Instant ) {
                var outs = new List<FlowOutput>();
                for ( var i = 0; i < _portCount; i++ ) {
                    outs.Add(AddFlowOutput(i.ToString()));
                }
                AddFlowInput("In", (f) =>
                {
                    for ( var i = 0; i < _portCount; i++ ) {
                        outs[i].Call(f);
                    }
                });
            }

            //-----

            if ( mode == Mode.Timed ) {
                if ( _tracks == null ) { _tracks = new List<FlowTrack>(_portCount); }
                _tracks.Resize(_portCount);

                for ( var i = 0; i < _portCount; i++ ) {
                    if ( _tracks[i] == null ) { _tracks[i] = new FlowTrack(); }
                    _tracks[i].flowOut = AddFlowOutput(i.ToString());
                }

                AddFlowInput("In", (f) =>
                {
                    if ( status == NodeCanvas.Framework.Status.Resting ) {
                        broken = false;
                        f.BeginBreakBlock(() => { broken = true; });
                        StartSyncedCoroutine(Update(f));
                        f.EndBreakBlock();
                    }
                });
                AddFlowInput("Break", (f) => { broken = true; });
            }
        }

        public void ConfigurePortCount(int portCount)
        {
            if (portCount < 2)
                throw new System.ArgumentOutOfRangeException(nameof(portCount));
            _portCount = portCount;
            GatherPorts();
        }

        IEnumerator Update(Flow f) {
            status = NodeCanvas.Framework.Status.Running;
            var totalLength = 0f;
            foreach ( var track in _tracks ) { totalLength = Mathf.Max(totalLength, track.time); }
            var previousTime = -1f;
            while ( !broken && previousTime <= totalLength ) {
                foreach ( var track in _tracks ) {
                    if ( elapsedTime >= track.time && previousTime < track.time ) {
                        track.flowOut.Call(f);
                        if ( broken ) { break; } //in case output calls break
                    }
                }
                previousTime = elapsedTime;
                yield return null;
            }
            status = NodeCanvas.Framework.Status.Resting;
        }

        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR
        protected override void OnNodeInspectorGUI() {
            base.OnNodeInspectorGUI();
            if ( mode == Mode.Timed ) {
                SequencerEditor.ShowTracks(20, _tracks, 0, elapsedTime);
            }
        }
#endif
        ///----------------------------------------------------------------------------------------------

    }
}
