#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor.Graph
{
    /// <summary>
    /// Shader Graph 风格的技能 / Buff / 复合条件 / 复合机制节点图编辑器。
    /// </summary>
    public sealed class TechCosmosGraphEditorWindow : EditorWindow
    {
        private enum GraphMode { Skill, Buff, Condition, Mechanism }

        private GraphMode _mode = GraphMode.Skill;
        private SkillDataSO _skillTarget;
        private BuffDataSO _buffTarget;
        private CompositeConditionSO _conditionTarget;
        private CompositeMechanismSO _mechanismTarget;

        private SkillGraphView _skillGraph;
        private BuffGraphView _buffGraph;
        private ConditionGraphView _conditionGraph;
        private MechanismGraphView _mechanismGraph;
        private VisualElement _graphHost;
        private Label _titleLabel;
        private Label _hintLabel;

        [MenuItem("Tech-Cosmos/SkillSystem/Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<TechCosmosGraphEditorWindow>("技能系统 Graph");
            window.minSize = new Vector2(960, 600);
            window.Show();
        }

        public static void OpenSkill(SkillDataSO skill)
        {
            var window = GetWindow<TechCosmosGraphEditorWindow>("技能系统 Graph");
            window.minSize = new Vector2(960, 600);
            window._skillTarget = skill;
            window._buffTarget = null;
            window._conditionTarget = null;
            window._mechanismTarget = null;
            window.SetMode(GraphMode.Skill);
            window.Show();
        }

        public static void OpenCondition(CompositeConditionSO preset)
        {
            if (preset == null) return;
            var window = GetWindow<TechCosmosGraphEditorWindow>("技能系统 Graph");
            window.minSize = new Vector2(960, 600);
            window._conditionTarget = preset;
            window._skillTarget = null;
            window._buffTarget = null;
            window._mechanismTarget = null;
            window.SetMode(GraphMode.Condition);
            window.Show();
        }

        public static void OpenMechanism(CompositeMechanismSO preset)
        {
            if (preset == null) return;
            var window = GetWindow<TechCosmosGraphEditorWindow>("技能系统 Graph");
            window.minSize = new Vector2(960, 600);
            window._mechanismTarget = preset;
            window._skillTarget = null;
            window._buffTarget = null;
            window._conditionTarget = null;
            window.SetMode(GraphMode.Mechanism);
            window.Show();
        }

        [MenuItem("Tech-Cosmos/SkillSystem/Open Graph Editor", true)]
        private static bool ValidateOpenFromSelection()
        {
            return Selection.activeObject is SkillDataSO or BuffDataSO or CompositeConditionSO or CompositeMechanismSO;
        }

        [MenuItem("Tech-Cosmos/SkillSystem/Open Graph Editor", priority = 2)]
        public static void OpenFromSelection()
        {
            var window = GetWindow<TechCosmosGraphEditorWindow>("技能系统 Graph");
            window.minSize = new Vector2(960, 600);
            window.BindFromSelection();
            window.Show();
        }

        private void CreateGUI()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.techcosmos.skillsystem/Editor/Graph/Styles/TechGraphStyles.uss");

            if (styleSheet == null)
            {
                var guids = AssetDatabase.FindAssets("TechGraphStyles t:StyleSheet");
                if (guids.Length > 0)
                    styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            var toolbar = new VisualElement();
            toolbar.AddToClassList("tech-graph-toolbar");

            _titleLabel = new Label("Graph Editor");
            _titleLabel.AddToClassList("tech-graph-toolbar-label");
            toolbar.Add(_titleLabel);

            toolbar.Add(new Button(() => SetMode(GraphMode.Skill)) { text = "技能图" });
            toolbar.Add(new Button(() => SetMode(GraphMode.Buff)) { text = "Buff 图" });
            toolbar.Add(new Button(() => SetMode(GraphMode.Condition)) { text = "条件图" });
            toolbar.Add(new Button(() => SetMode(GraphMode.Mechanism)) { text = "机制图" });
            toolbar.Add(new Button(SaveCurrent) { text = "保存布局" });
            toolbar.Add(new Button(RefreshCurrent) { text = "刷新" });

            _hintLabel = new Label("双击条件/机制节点可打开树编辑器 | 滚轮缩放 | 拖拽平移");
            _hintLabel.style.color = new Color(0.65f, 0.7f, 0.75f);
            _hintLabel.style.marginLeft = 16;
            toolbar.Add(_hintLabel);

            rootVisualElement.Add(toolbar);

            _graphHost = new VisualElement { style = { flexGrow = 1 } };
            rootVisualElement.Add(_graphHost);

            _skillGraph = new SkillGraphView();
            _buffGraph = new BuffGraphView();
            _conditionGraph = new ConditionGraphView();
            _mechanismGraph = new MechanismGraphView();
            _skillGraph.GraphChanged += OnGraphChanged;
            _buffGraph.GraphChanged += OnGraphChanged;
            _conditionGraph.GraphChanged += OnGraphChanged;
            _mechanismGraph.GraphChanged += OnGraphChanged;

            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();

            if (_skillTarget == null && _buffTarget == null && _conditionTarget == null && _mechanismTarget == null)
                BindFromSelection();
            else
                SetMode(_mode);
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            SaveCurrent();
        }

        private void OnSelectionChanged() => BindFromSelection();

        private void BindFromSelection()
        {
            if (Selection.activeObject is SkillDataSO skill)
            {
                _skillTarget = skill;
                _buffTarget = null;
                _conditionTarget = null;
                _mechanismTarget = null;
                SetMode(GraphMode.Skill);
                return;
            }

            if (Selection.activeObject is BuffDataSO buff)
            {
                _buffTarget = buff;
                _skillTarget = null;
                _conditionTarget = null;
                _mechanismTarget = null;
                SetMode(GraphMode.Buff);
                return;
            }

            if (Selection.activeObject is CompositeConditionSO condition)
            {
                _conditionTarget = condition;
                _skillTarget = null;
                _buffTarget = null;
                _mechanismTarget = null;
                SetMode(GraphMode.Condition);
                return;
            }

            if (Selection.activeObject is CompositeMechanismSO mechanism)
            {
                _mechanismTarget = mechanism;
                _skillTarget = null;
                _buffTarget = null;
                _conditionTarget = null;
                SetMode(GraphMode.Mechanism);
            }
        }

        private void SetMode(GraphMode mode)
        {
            _mode = mode;
            _graphHost.Clear();

            switch (mode)
            {
                case GraphMode.Skill:
                    _titleLabel.text = _skillTarget != null ? $"技能图 · {_skillTarget.name}" : "技能图 · 未选择资产";
                    _skillGraph.Bind(_skillTarget);
                    _graphHost.Add(_skillGraph);
                    break;
                case GraphMode.Buff:
                    _titleLabel.text = _buffTarget != null ? $"Buff 图 · {_buffTarget.name}" : "Buff 图 · 未选择资产";
                    _buffGraph.Bind(_buffTarget);
                    _graphHost.Add(_buffGraph);
                    break;
                case GraphMode.Condition:
                    _titleLabel.text = _conditionTarget != null
                        ? $"条件图 · {_conditionTarget.displayName}"
                        : "条件图 · 未选择 CompositeCondition 资产";
                    _conditionGraph.Bind(_conditionTarget);
                    _graphHost.Add(_conditionGraph);
                    break;
                case GraphMode.Mechanism:
                    _titleLabel.text = _mechanismTarget != null
                        ? $"机制图 · {_mechanismTarget.displayName}"
                        : "机制图 · 未选择 CompositeMechanism 资产";
                    _mechanismGraph.Bind(_mechanismTarget);
                    _graphHost.Add(_mechanismGraph);
                    break;
            }
        }

        private void RefreshCurrent()
        {
            switch (_mode)
            {
                case GraphMode.Skill: _skillGraph.Reload(); break;
                case GraphMode.Buff: _buffGraph.Reload(); break;
                case GraphMode.Condition: _conditionGraph.Reload(); break;
                case GraphMode.Mechanism: _mechanismGraph.Reload(); break;
            }
        }

        private void SaveCurrent()
        {
            switch (_mode)
            {
                case GraphMode.Skill:
                    _skillGraph.SaveLayout();
                    if (_skillTarget != null)
                    {
                        EditorUtility.SetDirty(_skillTarget);
                        AssetDatabase.SaveAssets();
                    }
                    break;
                case GraphMode.Buff:
                    _buffGraph.SaveLayout();
                    if (_buffTarget != null)
                    {
                        EditorUtility.SetDirty(_buffTarget);
                        AssetDatabase.SaveAssets();
                    }
                    break;
                case GraphMode.Condition:
                    _conditionGraph.SaveLayout();
                    if (_conditionTarget != null)
                    {
                        EditorUtility.SetDirty(_conditionTarget);
                        AssetDatabase.SaveAssets();
                    }
                    break;
                case GraphMode.Mechanism:
                    _mechanismGraph.SaveLayout();
                    if (_mechanismTarget != null)
                    {
                        EditorUtility.SetDirty(_mechanismTarget);
                        AssetDatabase.SaveAssets();
                    }
                    break;
            }
        }

        private void OnGraphChanged() => SaveCurrent();
    }
}
#endif
