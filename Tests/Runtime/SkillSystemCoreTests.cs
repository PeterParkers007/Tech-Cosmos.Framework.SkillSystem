using NUnit.Framework;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Tests
{
    /// <summary>FormulaEvaluator 静态表达式求值测试。</summary>
    public class FormulaEvaluatorTests
    {
        /// <summary>验证简单加法表达式。</summary>
        [Test]
        public void EvaluateExpressionStatic_Addition_Works()
        {
            Assert.AreEqual(15f, FormulaEvaluator.EvaluateExpressionStatic("10+5"), 0.001f);
        }

        /// <summary>验证运算符优先级。</summary>
        [Test]
        public void EvaluateExpressionStatic_Precedence_Works()
        {
            Assert.AreEqual(14f, FormulaEvaluator.EvaluateExpressionStatic("2+3*4"), 0.001f);
        }

        /// <summary>验证括号改变求值顺序。</summary>
        [Test]
        public void EvaluateExpressionStatic_Parentheses_Works()
        {
            Assert.AreEqual(20f, FormulaEvaluator.EvaluateExpressionStatic("(2+3)*4"), 0.001f);
        }
    }

    /// <summary>TagContainer 标签管理测试。</summary>
    public class TagContainerTests
    {
        /// <summary>添加标签后 HasTag 应返回 true。</summary>
        [Test]
        public void HasTag_ReturnsTrueAfterAdd()
        {
            var tags = new TagContainer();
            tags.AddTag("Stun");
            Assert.IsTrue(tags.HasTag("Stun"));
        }

        /// <summary>HasAllTags 要求所有指定标签均存在。</summary>
        [Test]
        public void HasAllTags_RequiresEveryTag()
        {
            var tags = new TagContainer();
            tags.AddTag("A");
            tags.AddTag("B");
            Assert.IsTrue(tags.HasAllTags(new[] { "A", "B" }));
            Assert.IsFalse(tags.HasAllTags(new[] { "A", "C" }));
        }
    }

    /// <summary>BuffSystem 叠层与属性修正测试。</summary>
    public class BuffSystemTests
    {
        private sealed class MockTarget
        {
        }

        /// <summary>叠层策略下层数不超过 maxStacks。</summary>
        [Test]
        public void AddBuff_StacksUntilMax()
        {
            var target = new MockTarget();
            var system = new BuffSystem<MockTarget>(target);
            system.AddBuff(new SimpleBuff<MockTarget>(
                target, "Haste", 5f, null, null, null,
                BuffStackPolicy.StackAndRefresh, 3));
            system.AddBuff(new SimpleBuff<MockTarget>(
                target, "Haste", 5f, null, null, null,
                BuffStackPolicy.StackAndRefresh, 3));
            Assert.AreEqual(2, system.FindBuffByName("Haste").CurrentStacks);
        }

        /// <summary>Add 修正应正确叠加到基础属性值。</summary>
        [Test]
        public void GetModifiedValue_AppliesAddModifier()
        {
            var target = new MockTarget();
            var system = new BuffSystem<MockTarget>(target);
            system.AddBuff(new SimpleBuff<MockTarget>(
                target, "Power", 10f, null, new[]
                {
                    new StatModifier { statKey = "Attack", operation = ModifierOperation.Add, value = 5f }
                }, null, BuffStackPolicy.ExtendDuration, 1));
            Assert.AreEqual(15f, system.GetModifiedValue("Attack", 10f), 0.001f);
        }
    }

    /// <summary>CooldownCondition 冷却逻辑测试。</summary>
    public class CooldownConditionTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        /// <summary>技能执行后冷却期内 IsEligible 应返回 false。</summary>
        [Test]
        public void CooldownCondition_BlocksUntilExecuted()
        {
            var clock = new FixedSkillClock(0f);
            SkillSystemServices.Clock = clock;

            var condition = new CooldownCondition<MockUnit> { cooldown = 2f };
            var context = new SkillContext<MockUnit>(new MockUnit());
            context.meta.clock = clock;

            Assert.IsTrue(condition.IsEligible(context, null));
            condition.OnSkillExecuted(context, null);
            Assert.IsFalse(condition.IsEligible(context, null));

            SkillSystemServices.Clock = new UnitySkillClock();
        }
    }

    /// <summary>ConditionTreeCompiler 条件树编译测试。</summary>
    public class ConditionTreeCompilerTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        /// <summary>AND 节点要求所有子条件均通过。</summary>
        [Test]
        public void CompileAndCondition_RequiresAllChildren()
        {
            var tree = new ConditionTreeAnd
            {
                children = new System.Collections.Generic.List<ConditionTreeNodeBase>
                {
                    new ConditionTreeLeaf { condition = new FuncCondition<MockUnit>(_ => true) },
                    new ConditionTreeLeaf { condition = new FuncCondition<MockUnit>(_ => false) }
                }
            };

            var compiled = ConditionTreeCompiler.Compile<MockUnit>(tree);
            Assert.IsFalse(compiled.IsEligible(default, null));
        }

        /// <summary>OR 节点在任一子条件通过时即通过。</summary>
        [Test]
        public void CompileOrCondition_PassesWhenAnyChildPasses()
        {
            var tree = new ConditionTreeOr
            {
                children = new System.Collections.Generic.List<ConditionTreeNodeBase>
                {
                    new ConditionTreeLeaf { condition = new FuncCondition<MockUnit>(_ => false) },
                    new ConditionTreeLeaf { condition = new FuncCondition<MockUnit>(_ => true) }
                }
            };

            var compiled = ConditionTreeCompiler.Compile<MockUnit>(tree);
            Assert.IsTrue(compiled.IsEligible(default, null));
        }

        /// <summary>Ref 节点应内联编译被引用的复合条件。</summary>
        [Test]
        public void CompileRefCondition_InlinesPreset()
        {
            var preset = ScriptableObject.CreateInstance<CompositeConditionSO>();
            preset.conditionTreeRoot = new ConditionTreeLeaf
            {
                condition = new FuncCondition<MockUnit>(_ => true)
            };

            var tree = new ConditionTreeRef { preset = preset };
            var compiled = ConditionTreeCompiler.Compile<MockUnit>(tree);
            Assert.IsTrue(compiled.IsEligible(default, null));

            Object.DestroyImmediate(preset);
        }
    }

    /// <summary>MechanismTreeCompiler 机制树编译测试。</summary>
    public class MechanismTreeCompilerTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        private sealed class CounterMechanism : Mechanism<MockUnit>
        {
            public int Count;
            public override void Execute(SkillContext<MockUnit> context, IDataLayer<MockUnit> dataLayer)
                => Count++;
        }

        /// <summary>Sequence 节点应按顺序执行所有子机制。</summary>
        [Test]
        public void CompileSequence_ExecutesAllChildrenInOrder()
        {
            var first = new CounterMechanism();
            var second = new CounterMechanism();
            var tree = new MechanismTreeSequence
            {
                children = new System.Collections.Generic.List<MechanismTreeNodeBase>
                {
                    new MechanismTreeLeaf { mechanism = first },
                    new MechanismTreeLeaf { mechanism = second }
                }
            };

            var compiled = MechanismTreeCompiler.Compile<MockUnit>(tree) as SequenceMechanism<MockUnit>;
            Assert.IsNotNull(compiled);

            var context = new SkillContext<MockUnit>(new MockUnit());
            compiled.Execute(context, null);
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(1, second.Count);
        }

        /// <summary>Ref 节点应内联编译被引用的复合机制。</summary>
        [Test]
        public void CompileRefMechanism_InlinesPreset()
        {
            var counter = new CounterMechanism();
            var preset = ScriptableObject.CreateInstance<CompositeMechanismSO>();
            preset.mechanismTreeRoot = new MechanismTreeLeaf { mechanism = counter };

            var tree = new MechanismTreeRef { preset = preset };
            var compiled = MechanismTreeCompiler.Compile<MockUnit>(tree);
            Assert.IsNotNull(compiled);

            compiled.Execute(new SkillContext<MockUnit>(new MockUnit()), null);
            Assert.AreEqual(1, counter.Count);

            Object.DestroyImmediate(preset);
        }
    }
}
