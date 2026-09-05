using System.Collections;
using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{

    [Name("While True")]
    [Category("Flow Controllers/Iterators")]
    [Description("Once called, will continuously call 'Do' while the input boolean condition is true. Once condition becomes or is false, 'Done' is called")]
    [ContextDefinedInputs(typeof(bool))]
    public class While : FlowControlNode
    {

        private IEnumerator coroutine;

        public override void OnGraphStarted() {
            coroutine = null;
        }

        public override void OnGraphStoped() {
            coroutine = null;
        }

        protected override void RegisterPorts() {
            var c = AddValueInput<bool>("Condition");
            var fUpdate = AddFlowOutput("Do");
            var fFinish = AddFlowOutput("Done");
            AddFlowInput("In", (f) =>
            {
                if ( coroutine == null ) {
                    coroutine = StartSyncedCoroutine(DoWhile(fUpdate, fFinish, f, c));
                }
            });
        }

        IEnumerator DoWhile(FlowOutput fUpdate, FlowOutput fFinish, Flow f, ValueInput<bool> condition) {
            SetStatus(NodeCanvas.Framework.Status.Running);
            var active = true;
            var ff = f; //copy for finish
            f.BeginBreakBlock(() => { active = false; });
            while ( active && condition.value ) {
                fUpdate.Call(f);
                yield return null;
            }
            coroutine = null;
            f.EndBreakBlock();
            SetStatus(NodeCanvas.Framework.Status.Resting);
            fFinish.Call(ff);
        }
    }
}