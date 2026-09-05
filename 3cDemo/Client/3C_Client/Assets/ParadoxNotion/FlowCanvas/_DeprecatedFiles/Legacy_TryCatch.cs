using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{

    [System.Obsolete]
    [Description("Similar to Try/Catch/Finally in code")]
    public class TryCatch : FlowControlNode
    {

        protected override void RegisterPorts() {
            var fTry = AddFlowOutput("Try");
            var fCatch = AddFlowOutput("Catch");
            var fFinally = AddFlowOutput("Finally");
            AddFlowInput("In", (f) =>
                {
                    try {
                        fTry.Call(f);
                    }
                    catch {
                        fCatch.Call(f);
                    }
                    finally {
                        fFinally.Call(f);
                    }
                }
            );
        }
    }
}