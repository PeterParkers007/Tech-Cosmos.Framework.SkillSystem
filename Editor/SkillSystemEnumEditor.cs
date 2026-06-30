#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// 统一管理 TriggerEvent、BuffModifyType、BuffTag 枚举的编辑器窗口。
    /// </summary>
    public class SkillSystemEnumEditor : EditorWindow
    {
        private SkillSystemEnumKind _kind = SkillSystemEnumKind.TriggerEvent;
        private List<string> _items = new();
        private string _newItemName = "";
        private string _searchFilter = "";
        private Vector2 _scrollPos;
        private bool _dirty;

        [MenuItem("Tech-Cosmos/SkillSystem/Enum Editor", priority = 13)]
        public static void OpenWindow()
        {
            OpenWindow(SkillSystemEnumKind.TriggerEvent);
        }

        public static void OpenWindow(SkillSystemEnumKind kind)
        {
            var window = GetWindow<SkillSystemEnumEditor>("技能系统枚举编辑器");
            window.minSize = new Vector2(420, 480);
            window._kind = kind;
            window.LoadItems();
            window.Show();
        }

        private void OnEnable()
        {
            LoadItems();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("技能系统枚举编辑器", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var def = SkillSystemEnumCatalog.Get(_kind);
            EditorGUILayout.HelpBox(
                "统一管理技能 TriggerEvent、Buff 事件响应与 Buff 标签 / 修改类型。\n" +
                def.Description,
                MessageType.Info);

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            var kindLabels = SkillSystemEnumCatalog.AllKinds
                .Select(k => SkillSystemEnumCatalog.Get(k).DisplayName)
                .ToArray();
            var kindValues = SkillSystemEnumCatalog.AllKinds.ToArray();
            int kindIndex = System.Array.IndexOf(kindValues, _kind);
            if (kindIndex < 0) kindIndex = 0;
            int newKindIndex = EditorGUILayout.Popup("编辑目标", kindIndex, kindLabels);
            if (newKindIndex != kindIndex)
            {
                if (_dirty && !ConfirmDiscardChanges())
                    return;

                _kind = kindValues[newKindIndex];
                LoadItems();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("添加新项", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _newItemName = EditorGUILayout.TextField(_newItemName);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("添加", GUILayout.Width(60)) && !string.IsNullOrWhiteSpace(_newItemName))
            {
                var name = SkillSystemEnumGenerator.SanitizeName(_newItemName);
                if (!_items.Contains(name))
                {
                    _items.Add(name);
                    _newItemName = "";
                    _dirty = true;
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", $"'{name}' 已存在", "确定");
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            _searchFilter = EditorGUILayout.TextField("搜索", _searchFilter, GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarSearchField);

            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? _items
                : _items.FindAll(i => i.ToLower().Contains(_searchFilter.ToLower()));

            EditorGUILayout.LabelField($"{def.DisplayName} ({_items.Count})", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(260));
            for (int i = 0; i < filtered.Count; i++)
            {
                int realIndex = _items.IndexOf(filtered[i]);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"{realIndex + 1}.", GUILayout.Width(30));
                string renamed = EditorGUILayout.TextField(_items[realIndex]);
                if (renamed != _items[realIndex])
                {
                    _items[realIndex] = SkillSystemEnumGenerator.SanitizeName(renamed);
                    _dirty = true;
                }

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    _items.RemoveAt(realIndex);
                    _dirty = true;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("生成枚举文件", GUILayout.Height(35)))
            {
                SkillSystemEnumGenerator.WriteEnum(_kind, _items);
                _dirty = false;
                EditorUtility.DisplayDialog("完成", $"{def.EnumTypeName} 已生成，共 {_items.Count} 项。", "确定");
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("从文件重新加载", GUILayout.Height(35)))
            {
                LoadItems();
                _dirty = false;
            }

            EditorGUILayout.EndHorizontal();

            if (_dirty)
                EditorGUILayout.HelpBox("有未保存的修改", MessageType.Warning);
        }

        private void OnDisable()
        {
            if (!_dirty) return;
            if (EditorUtility.DisplayDialog("未保存", "有修改未保存，要现在生成吗？", "生成", "放弃"))
                SkillSystemEnumGenerator.WriteEnum(_kind, _items);
        }

        private void LoadItems()
        {
            _items = SkillSystemEnumGenerator.LoadEnumValues(_kind);
            _dirty = false;
        }

        private bool ConfirmDiscardChanges()
        {
            return EditorUtility.DisplayDialog("未保存", "切换目标会丢失当前未保存修改，是否继续？", "继续", "取消");
        }
    }
}
#endif
