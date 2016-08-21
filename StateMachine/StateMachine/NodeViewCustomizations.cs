using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Scheduler;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateMachine
{
    public class StatemachineCustomization : INodeViewCustomization<SetCurrentState>
    {
        private DynamoViewModel dynamoViewModel;
        private NodeModel model;

        public void Dispose()
        {
            //unhook handler
            (this.model as SetCurrentState).Executed -= onExecuted;
        }

        public void CustomizeView(SetCurrentState model, NodeView nodeView)
        {
            dynamoViewModel = nodeView.ViewModel.DynamoViewModel;
            this.model = model;
            model.Executed += onExecuted;
        }

        private void onExecuted()
        {
            //find all nodes of type OnState
            var nodesToExecute = dynamoViewModel.CurrentSpace.Nodes.OfType<OnCurrentState>();
            var engine = dynamoViewModel.EngineController;
            var stateString = "default";

            nodesToExecute.ToList().ForEach(node => {
                if (node.HasConnectedInput(0))
                {
                    //TODO(this needs to be made robust, what if data is not a string!?)
                    var stateNode = node.InPorts[0].Connectors[0].Start.Owner;
                    var stateIndex = node.InPorts[0].Connectors[0].Start.Index;
                    var startId = stateNode.GetAstIdentifierForOutputIndex(stateIndex).Name;
                    var stateMirror = engine.GetMirror(startId);
                    stateString = stateMirror.GetData().Data as string;
                }
                var copy = stateString;
                var task = new DelegateBasedAsyncTask(dynamoViewModel.Model.Scheduler, () => { node.SetFreezeState(copy); });
                dynamoViewModel.Model.Scheduler.ScheduleForExecution(task);
            });

        }
    }
}
