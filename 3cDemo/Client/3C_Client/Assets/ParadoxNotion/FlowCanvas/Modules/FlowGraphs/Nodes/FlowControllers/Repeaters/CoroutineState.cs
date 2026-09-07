using System.Collections;
using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{

    [Name("Coroutine")]
    [Category("Flow Controllers/Iterators")]
    [Description("Start a Coroutine that will repeat until Break is called")]
    public class CoroutineState : FlowControlNode
    {

        private bool active;

        public override void OnGraphStoped() {
            active = false;
        }

        protected override void RegisterPorts() {
            var fStart = AddFlowOutput("Start");
            var fUpdate = AddFlowOutput("Update");
            var fFinish = AddFlowOutput("Finish");
            AddFlowInput("Start", (f) =>
            {
                if ( !active ) {
                    active = true;
                    StartSyncedCoroutine(DoRepeat(fStart, fUpdate, fFinish, f));
                }
            });
            AddFlowInput("Break", (f) => { active = false; fFinish.Call(f); });
            AddFlowInput("Cancel", (f) => { active = false; });
        }

        IEnumerator DoRepeat(FlowOutput fStart, FlowOutput fUpdate, FlowOutput fFinish, Flow f) {
            SetStatus(NodeCanvas.Framework.Status.Running);
            var ff = f; //copy for finish
            f.BeginBreakBlock(() => { active = false; });
            fStart.Call(f);
            while ( active ) {
                fUpdate.Call(f);
                yield return null;
            }
            f.EndBreakBlock();
            SetStatus(NodeCanvas.Framework.Status.Resting);
            fFinish.Call(ff);
        }
    }
}