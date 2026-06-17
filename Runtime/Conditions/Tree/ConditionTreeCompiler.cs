using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 条件树编译器：将序列化条件树转换为运行时 <see cref="Condition{T}"/> 实例。
    /// </summary>
    public static class ConditionTreeCompiler
    {
        /// <summary>
        /// 编译条件树或回退到旧版平铺条件列表。
        /// </summary>
        public static List<Condition<T>> CompileToConditionList<T>(
            ConditionTreeNodeBase root,
            IList<ConditionBase> legacyConditions) where T : class, IUnit<T>
        {
            var result = new List<Condition<T>>();
            var visitedPresets = new HashSet<CompositeConditionSO>();

            if (root != null)
            {
                var compiled = Compile<T>(root, visitedPresets);
                if (compiled != null)
                    result.Add(compiled);
                return result;
            }

            if (legacyConditions == null) return result;

            for (int i = 0; i < legacyConditions.Count; i++)
            {
                if (legacyConditions[i] is Condition<T> typed)
                    result.Add(typed);
            }

            return result;
        }

        /// <summary>将单个条件树节点编译为运行时条件。</summary>
        public static Condition<T> Compile<T>(
            ConditionTreeNodeBase node,
            HashSet<CompositeConditionSO> visitedPresets = null) where T : class, IUnit<T>
        {
            if (node == null) return null;
            visitedPresets ??= new HashSet<CompositeConditionSO>();

            switch (node)
            {
                case ConditionTreeLeaf leaf:
                    return leaf.condition as Condition<T>;

                case ConditionTreeAnd and:
                    return CompileComposite<T>(and.children, items =>
                    {
                        if (items.Count == 0) return null;
                        if (items.Count == 1) return items[0];
                        return new AndCondition<T>(items.ToArray());
                    }, visitedPresets);

                case ConditionTreeOr or:
                    return CompileComposite<T>(or.children, items =>
                    {
                        if (items.Count == 0) return null;
                        if (items.Count == 1) return items[0];
                        return new OrCondition<T>(items.ToArray());
                    }, visitedPresets);

                case ConditionTreeNot not:
                    var inner = Compile<T>(not.child, visitedPresets);
                    return inner == null ? null : new NotCondition<T>(inner);

                case ConditionTreeRef reference:
                    if (reference.preset == null || reference.preset.conditionTreeRoot == null)
                        return null;
                    if (!visitedPresets.Add(reference.preset))
                        return null;
                    return Compile<T>(reference.preset.conditionTreeRoot, visitedPresets);

                default:
                    return null;
            }
        }

        private static Condition<T> CompileComposite<T>(
            List<ConditionTreeNodeBase> children,
            System.Func<List<Condition<T>>, Condition<T>> build,
            HashSet<CompositeConditionSO> visitedPresets) where T : class, IUnit<T>
        {
            if (children == null || children.Count == 0) return null;

            var compiledChildren = new List<Condition<T>>(children.Count);
            for (int i = 0; i < children.Count; i++)
            {
                var compiled = Compile<T>(children[i], visitedPresets);
                if (compiled != null)
                    compiledChildren.Add(compiled);
            }

            return build(compiledChildren);
        }
    }
}
