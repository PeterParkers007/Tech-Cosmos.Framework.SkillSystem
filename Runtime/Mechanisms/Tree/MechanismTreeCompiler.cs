using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 机制树编译器：将序列化机制树转换为运行时 <see cref="Mechanism{T}"/> 实例。
    /// </summary>
    public static class MechanismTreeCompiler
    {
        public static List<MechanismBase> CompileToMechanismList<T>(
            MechanismTreeNodeBase root,
            IList<MechanismBase> legacyMechanisms) where T : class, IUnit<T>
        {
            var result = new List<MechanismBase>();
            var visitedPresets = new HashSet<CompositeMechanismSO>();

            if (root != null)
            {
                var compiled = Compile<T>(root, visitedPresets);
                if (compiled != null)
                    result.Add(compiled);
                return result;
            }

            if (legacyMechanisms == null) return result;

            for (int i = 0; i < legacyMechanisms.Count; i++)
            {
                if (legacyMechanisms[i] is Mechanism<T> typed)
                    result.Add(typed);
            }

            return result;
        }

        public static Mechanism<T> Compile<T>(
            MechanismTreeNodeBase node,
            HashSet<CompositeMechanismSO> visitedPresets = null) where T : class, IUnit<T>
        {
            if (node == null) return null;
            visitedPresets ??= new HashSet<CompositeMechanismSO>();

            switch (node)
            {
                case MechanismTreeLeaf leaf:
                    return leaf.mechanism as Mechanism<T>;

                case MechanismTreeSequence sequence:
                    return CompileComposite<T>(sequence.children, items =>
                    {
                        if (items.Count == 0) return null;
                        if (items.Count == 1) return items[0];
                        return new SequenceMechanism<T>(items.ToArray());
                    }, visitedPresets);

                case MechanismTreeParallel parallel:
                    return CompileComposite<T>(parallel.children, items =>
                    {
                        if (items.Count == 0) return null;
                        if (items.Count == 1) return items[0];
                        return new ParallelMechanism<T>(items.ToArray());
                    }, visitedPresets);

                case MechanismTreeRef reference:
                    if (reference.preset == null || reference.preset.mechanismTreeRoot == null)
                        return null;
                    if (!visitedPresets.Add(reference.preset))
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[MechanismTreeCompiler] 检测到循环引用: preset '{reference.preset.name}'");
                        return null;
                    }
                    return Compile<T>(reference.preset.mechanismTreeRoot, visitedPresets);

                default:
                    return null;
            }
        }

        private static Mechanism<T> CompileComposite<T>(
            List<MechanismTreeNodeBase> children,
            System.Func<List<Mechanism<T>>, Mechanism<T>> build,
            HashSet<CompositeMechanismSO> visitedPresets) where T : class, IUnit<T>
        {
            if (children == null || children.Count == 0) return null;

            var compiledChildren = new List<Mechanism<T>>(children.Count);
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
