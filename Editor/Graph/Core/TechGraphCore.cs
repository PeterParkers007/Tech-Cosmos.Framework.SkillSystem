#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor.Graph
{
    internal static class GraphPortTypes
    {
        public static readonly Type Flow = typeof(bool);
        public static readonly Type Data = typeof(float);
    }

    public abstract class TechGraphNode : Node
    {
        public string NodeId { get; }
        public Port InputFlow { get; protected set; }
        public Port OutputFlow { get; protected set; }

        protected TechGraphNode(string nodeId, string title, Vector2 position)
        {
            NodeId = nodeId;
            title = title;
            SetPosition(new Rect(position, Vector2.zero));
            viewDataKey = nodeId;

            style.borderTopWidth = 2;
            style.borderBottomWidth = 2;
            style.borderLeftWidth = 2;
            style.borderRightWidth = 2;
            style.borderTopColor = new Color(0.35f, 0.55f, 0.85f);
            style.borderBottomColor = new Color(0.35f, 0.55f, 0.85f);
            style.borderLeftColor = new Color(0.35f, 0.55f, 0.85f);
            style.borderRightColor = new Color(0.35f, 0.55f, 0.85f);
            style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
        }

        protected Port CreateFlowPort(Orientation orientation, Direction direction, Port.Capacity capacity, string portName)
        {
            var port = InstantiatePort(orientation, direction, capacity, GraphPortTypes.Flow);
            port.portName = portName;
            return port;
        }

        public void RefreshLayoutFromPosition(Rect rect) => SetPosition(rect);
    }

    public abstract class TechGraphView : GraphView
    {
        public event Action GraphChanged;

        protected TechGraphView()
        {
            style.flexGrow = 1;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = OnGraphViewChanged;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var result = new List<Port>();
            foreach (var port in ports)
            {
                if (port == startPort || port.node == startPort.node) continue;
                if (port.direction == startPort.direction) continue;
                if (port.portType != startPort.portType) continue;
                result.Add(port);
            }
            return result;
        }

        protected GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null || change.elementsToRemove != null || change.movedElements != null)
                GraphChanged?.Invoke();
            return change;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendSeparator();
            BuildGraphContextMenu(evt);
        }

        protected abstract void BuildGraphContextMenu(ContextualMenuPopulateEvent evt);

        protected void PersistNodePositions(IEnumerable<TechGraphNode> nodes, GraphEditorLayout layout)
        {
            if (layout == null) return;
            foreach (var node in nodes)
                layout.SetPosition(node.NodeId, node.GetPosition().position);
        }

        protected Edge Connect(Port output, Port input)
        {
            if (output == null || input == null) return null;
            var edge = output.ConnectTo(input);
            AddElement(edge);
            return edge;
        }
    }
}
#endif
