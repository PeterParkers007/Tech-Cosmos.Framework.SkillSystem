using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 节点图编辑器布局数据（节点 ID 与画布坐标）。
    /// </summary>
    [Serializable]
    public class GraphEditorLayout
    {
        public List<GraphNodeLayoutEntry> nodes = new();

        public Vector2 GetPosition(string nodeId, Vector2 fallback)
        {
            if (string.IsNullOrEmpty(nodeId) || nodes == null) return fallback;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].nodeId == nodeId)
                    return nodes[i].position;
            }
            return fallback;
        }

        public void SetPosition(string nodeId, Vector2 position)
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            nodes ??= new List<GraphNodeLayoutEntry>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].nodeId == nodeId)
                {
                    nodes[i].position = position;
                    return;
                }
            }
            nodes.Add(new GraphNodeLayoutEntry { nodeId = nodeId, position = position });
        }

        public void RemoveNode(string nodeId)
        {
            if (nodes == null || string.IsNullOrEmpty(nodeId)) return;
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (nodes[i].nodeId == nodeId)
                    nodes.RemoveAt(i);
            }
        }
    }

    [Serializable]
    public class GraphNodeLayoutEntry
    {
        public string nodeId;
        public Vector2 position;
    }
}
