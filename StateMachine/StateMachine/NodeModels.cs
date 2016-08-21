using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMDataBridge;

namespace StateMachine
{
    [NodeName("SetCurrentState")]
    [NodeCategory("FiniteStateMachine")]
    [IsDesignScriptCompatible]
    public class SetCurrentState : NodeModel
    {
        public static string currentState { get; private set; }
        public Action Executed;

        public SetCurrentState()
        {
            Executed = new Action(() => { });
            InPortData.Add(new PortData("state", "the name of the state to switch to"));

            RegisterAllPorts();
            CanUpdatePeriodically = true;

        }
        //when this node is executed it needs to schedule other nodes in the graph
        //we don't actually care about the values generated here...
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            return new AssociativeNode[]
            {
                AstFactory.BuildAssignment(
                    AstFactory.BuildIdentifier(AstIdentifierBase + "_dummy"),
                    DataBridge.GenerateBridgeDataAst(GUID.ToString(), inputAstNodes.FirstOrDefault()))
            };
        }

        public override void Dispose()
        {
            base.Dispose();
            DataBridge.Instance.UnregisterCallback(GUID.ToString());
        }

        protected override void OnBuilt()
        {
            base.OnBuilt();
            DataBridge.Instance.RegisterCallback(GUID.ToString(), DataBridgeCallback);
        }

        private void DataBridgeCallback(object data)
        {
            SetCurrentState.currentState = data as string;
            this.Executed();
        }
    }


    [NodeName("OnCurrentState")]
    [NodeCategory("FiniteStateMachine")]
    [IsDesignScriptCompatible]
    public class OnCurrentState : NodeModel
    {
        private object output;
        public OnCurrentState()
        {
            InPortData.Add(new PortData("state", "the name of the state to activate on"));
            InPortData.Add(new PortData("data", " data to pass through if state is active"));

            OutPortData.Add(new PortData("data", "pass through data"));

            RegisterAllPorts();
            CanUpdatePeriodically = true;

        }
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            return new AssociativeNode[]
            {
                //return the data if we execute
                  AstFactory.BuildAssignment(
                    GetAstIdentifierForOutputIndex(0),inputAstNodes[1])

            };
        }

        //called from the UI thread using the engineMirror... may need to be scheduled...
        public void SetFreezeState(string thisState)
        {
            try
            {
                //if the current state is the same as the one referenced by this node then pass the value out
                //else return nothing...
                if (SetCurrentState.currentState == thisState)
                {
                    this.IsFrozen = false;
                }
                else
                {
                    //freeze the node now.
                    this.IsFrozen = true;
                }
            }
            catch
            {

            }
        }
    }
}
