using System;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 顺序执行多个子机制的运行时组合器。
    /// </summary>
    public sealed class SequenceMechanism<T> : Mechanism<T> where T : class, IUnit<T>
    {
        private readonly Mechanism<T>[] _children;

        public SequenceMechanism(params Mechanism<T>[] children)
        {
            _children = children ?? Array.Empty<Mechanism<T>>();
        }

        public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
        {
            for (int i = 0; i < _children.Length; i++)
                _children[i]?.Execute(context, dataLayer);
        }

        public override void SkillBack(ISkill<T> skill)
        {
            for (int i = _children.Length - 1; i >= 0; i--)
                _children[i]?.SkillBack(skill);
        }
    }

    /// <summary>
    /// 同批次执行多个子机制的运行时组合器。
    /// </summary>
    public sealed class ParallelMechanism<T> : Mechanism<T> where T : class, IUnit<T>
    {
        private readonly Mechanism<T>[] _children;

        public ParallelMechanism(params Mechanism<T>[] children)
        {
            _children = children ?? Array.Empty<Mechanism<T>>();
        }

        public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
        {
            for (int i = 0; i < _children.Length; i++)
                _children[i]?.Execute(context, dataLayer);
        }

        public override void SkillBack(ISkill<T> skill)
        {
            for (int i = _children.Length - 1; i >= 0; i--)
                _children[i]?.SkillBack(skill);
        }
    }
}
