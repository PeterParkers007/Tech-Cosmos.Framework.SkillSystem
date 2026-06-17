#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// 技能 Timeline 的编辑器绘制工具，支持片段编排与属性编辑。
    /// </summary>
    public static class SkillTimelineEditor
    {
        private const float TrackHeight = 48f;
        private const float RulerHeight = 22f;
        private static Vector2 _scroll;
        private static int _selectedClip = -1;

        /// <summary>在 Inspector 或技能编辑器窗口中绘制 Timeline 区域。</summary>
        public static void Draw(SerializedObject serializedObject)
        {
            var timelineProp = serializedObject.FindProperty("Timeline");
            if (timelineProp == null) return;

            var enabledProp = timelineProp.FindPropertyRelative("enabled");
            var durationProp = timelineProp.FindPropertyRelative("totalDuration");
            var clipsProp = timelineProp.FindPropertyRelative("clips");

            EditorGUILayout.PropertyField(enabledProp, new GUIContent("启用 Timeline"));
            if (!enabledProp.boolValue)
            {
                EditorGUILayout.HelpBox("启用后可在时间轴上编排机制与事件标记。", MessageType.Info);
                return;
            }

            EditorGUILayout.PropertyField(durationProp, new GUIContent("总时长 (秒)"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 机制片段", GUILayout.Height(22)))
                AddClip(clipsProp, SkillTimelineClipType.Mechanism);
            if (GUILayout.Button("+ 事件标记", GUILayout.Height(22)))
                AddClip(clipsProp, SkillTimelineClipType.EventMarker);
            if (GUILayout.Button("+ 阶段标签", GUILayout.Height(22)))
                AddClip(clipsProp, SkillTimelineClipType.PhaseLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            DrawTimelineRuler(durationProp.floatValue);
            DrawTimelineTrack(serializedObject, clipsProp, durationProp.floatValue);
            EditorGUILayout.Space(6);
            DrawClipInspector(clipsProp);
        }

        private static void DrawTimelineRuler(float totalDuration)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(RulerHeight));
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            if (totalDuration <= 0f) return;

            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f);
            int stepCount = Mathf.Max(1, Mathf.CeilToInt(totalDuration));
            for (int i = 0; i <= stepCount; i++)
            {
                float t = i / totalDuration;
                float x = rect.x + rect.width * Mathf.Clamp01(t);
                Handles.DrawLine(new Vector3(x, rect.y + 12f), new Vector3(x, rect.yMax));
                GUI.Label(new Rect(x + 2f, rect.y, 40f, 16f), i.ToString("0.##"), EditorStyles.miniLabel);
            }
            Handles.EndGUI();
        }

        private static void DrawTimelineTrack(SerializedObject serializedObject, SerializedProperty clipsProp, float totalDuration)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(TrackHeight));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.14f, 0.18f));

            if (clipsProp == null || totalDuration <= 0f) return;

            for (int i = 0; i < clipsProp.arraySize; i++)
            {
                var clipProp = clipsProp.GetArrayElementAtIndex(i);
                float start = clipProp.FindPropertyRelative("startTime").floatValue;
                float duration = clipProp.FindPropertyRelative("duration").floatValue;
                string label = clipProp.FindPropertyRelative("label").stringValue;
                var clipType = (SkillTimelineClipType)clipProp.FindPropertyRelative("clipType").enumValueIndex;

                float xMin = rect.x + rect.width * (start / totalDuration);
                float xMax = rect.x + rect.width * ((start + duration) / totalDuration);
                var clipRect = new Rect(xMin, rect.y + 6f, Mathf.Max(8f, xMax - xMin), TrackHeight - 12f);

                var color = clipType switch
                {
                    SkillTimelineClipType.Mechanism => new Color(0.3f, 0.65f, 1f, 0.85f),
                    SkillTimelineClipType.EventMarker => new Color(1f, 0.75f, 0.2f, 0.85f),
                    _ => new Color(0.6f, 0.6f, 0.6f, 0.85f)
                };

                if (_selectedClip == i)
                    color = Color.Lerp(color, Color.white, 0.25f);

                EditorGUI.DrawRect(clipRect, color);
                GUI.Label(clipRect, label, EditorStyles.miniLabel);

                if (Event.current.type == EventType.MouseDown && clipRect.Contains(Event.current.mousePosition))
                {
                    _selectedClip = i;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }

                if (_selectedClip == i && Event.current.type == EventType.MouseDrag)
                {
                    float normalized = (Event.current.mousePosition.x - rect.x) / rect.width;
                    clipProp.FindPropertyRelative("startTime").floatValue =
                        Mathf.Clamp(normalized * totalDuration, 0f, totalDuration);
                    serializedObject.ApplyModifiedProperties();
                    Event.current.Use();
                }
            }
        }

        private static void DrawClipInspector(SerializedProperty clipsProp)
        {
            if (clipsProp == null || _selectedClip < 0 || _selectedClip >= clipsProp.arraySize)
            {
                EditorGUILayout.HelpBox("点击时间轴片段以编辑详情。", MessageType.None);
                return;
            }

            var clipProp = clipsProp.GetArrayElementAtIndex(_selectedClip);
            EditorGUILayout.LabelField($"片段 #{_selectedClip}", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(clipProp.FindPropertyRelative("label"));
            EditorGUILayout.PropertyField(clipProp.FindPropertyRelative("clipType"));
            EditorGUILayout.PropertyField(clipProp.FindPropertyRelative("startTime"));
            EditorGUILayout.PropertyField(clipProp.FindPropertyRelative("duration"));

            var clipType = (SkillTimelineClipType)clipProp.FindPropertyRelative("clipType").enumValueIndex;
            if (clipType == SkillTimelineClipType.Mechanism)
                EditorGUILayout.PropertyField(clipProp.FindPropertyRelative("mechanism"), new GUIContent("机制"), true);
            else if (clipType == SkillTimelineClipType.EventMarker)
                EditorGUILayout.PropertyField(clipProp.FindPropertyRelative("eventName"), new GUIContent("事件名"));

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("删除片段", GUILayout.Height(22)))
            {
                clipsProp.DeleteArrayElementAtIndex(_selectedClip);
                _selectedClip = -1;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private static void AddClip(SerializedProperty clipsProp, SkillTimelineClipType clipType)
        {
            int index = clipsProp.arraySize;
            clipsProp.InsertArrayElementAtIndex(index);
            var clipProp = clipsProp.GetArrayElementAtIndex(index);
            clipProp.FindPropertyRelative("label").stringValue = clipType.ToString();
            clipProp.FindPropertyRelative("clipType").enumValueIndex = (int)clipType;
            clipProp.FindPropertyRelative("startTime").floatValue = 0f;
            clipProp.FindPropertyRelative("duration").floatValue = 0.2f;
            clipsProp.serializedObject.ApplyModifiedProperties();
            _selectedClip = index;
        }
    }
}
#endif
