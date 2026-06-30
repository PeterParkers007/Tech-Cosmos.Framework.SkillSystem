using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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

        /// <summary>HasAllBuff 要求每个 tag 在至少一个 Buff 上出现。</summary>
        [Test]
        public void HasAllBuff_RequiresEachTagAcrossBuffs()
        {
            var target = new MockTarget();
            var system = new BuffSystem<MockTarget>(target);
            system.AddBuff(new SimpleBuff<MockTarget>(
                target, "StunBuff", 5f, new[] { "Stun" }, null, null, BuffStackPolicy.ExtendDuration, 1));
            system.AddBuff(new SimpleBuff<MockTarget>(
                target, "SlowBuff", 5f, new[] { "Slow" }, null, null, BuffStackPolicy.ExtendDuration, 1));

            Assert.IsTrue(system.HasAllBuff("Stun", "Slow"));
            Assert.IsFalse(system.HasAllBuff("Stun", "Poison"));
        }

        /// <summary>ClearBuff 应对每个 Buff 触发 OnBuffRemoved。</summary>
        [Test]
        public void ClearBuff_InvokesOnBuffRemovedForEachBuff()
        {
            var target = new MockTarget();
            var system = new BuffSystem<MockTarget>(target);
            system.AddBuff(new SimpleBuff<MockTarget>(
                target, "A", 5f, null, null, null, BuffStackPolicy.ExtendDuration, 1));
            system.AddBuff(new SimpleBuff<MockTarget>(
                target, "B", 5f, null, null, null, BuffStackPolicy.ExtendDuration, 1));

            int removedCount = 0;
            system.OnBuffRemoved += _ => removedCount++;

            system.ClearBuff();
            Assert.AreEqual(2, removedCount);
            Assert.AreEqual(0, system.BuffCount);
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

        /// <summary>循环 Ref 应记录警告并返回 null。</summary>
        [Test]
        public void CompileRefCondition_CircularReference_ReturnsNull()
        {
            var presetA = ScriptableObject.CreateInstance<CompositeConditionSO>();
            var presetB = ScriptableObject.CreateInstance<CompositeConditionSO>();
            presetA.conditionTreeRoot = new ConditionTreeRef { preset = presetB };
            presetB.conditionTreeRoot = new ConditionTreeRef { preset = presetA };

            LogAssert.Expect(LogType.Warning, new Regex(".*循环引用.*"));
            var compiled = ConditionTreeCompiler.Compile<MockUnit>(new ConditionTreeRef { preset = presetA });
            Assert.IsNull(compiled);

            Object.DestroyImmediate(presetA);
            Object.DestroyImmediate(presetB);
        }

        /// <summary>NOT 节点应反转子条件结果。</summary>
        [Test]
        public void CompileNotCondition_InvertsChild()
        {
            var tree = new ConditionTreeNot
            {
                child = new ConditionTreeLeaf { condition = new FuncCondition<MockUnit>(_ => true) }
            };
            var compiled = ConditionTreeCompiler.Compile<MockUnit>(tree);
            Assert.IsFalse(compiled.IsEligible(default, null));
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

        /// <summary>循环 Ref 应记录警告并返回 null。</summary>
        [Test]
        public void CompileRefMechanism_CircularReference_ReturnsNull()
        {
            var presetA = ScriptableObject.CreateInstance<CompositeMechanismSO>();
            var presetB = ScriptableObject.CreateInstance<CompositeMechanismSO>();
            presetA.mechanismTreeRoot = new MechanismTreeRef { preset = presetB };
            presetB.mechanismTreeRoot = new MechanismTreeRef { preset = presetA };

            LogAssert.Expect(LogType.Warning, new Regex(".*循环引用.*"));
            var compiled = MechanismTreeCompiler.Compile<MockUnit>(new MechanismTreeRef { preset = presetA });
            Assert.IsNull(compiled);

            Object.DestroyImmediate(presetA);
            Object.DestroyImmediate(presetB);
        }
    }

    /// <summary>SkillExecutionController 施法流程测试。</summary>
    public class SkillExecutionControllerTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        private static ISkill<MockUnit> CreateSkill(
            SkillProfile profile,
            System.Collections.Generic.List<Condition<MockUnit>> conditions = null)
        {
            var data = new SkillData<MockUnit>
            {
                SkillName = "TestSkill",
                SkillType = SkillType.Active,
                TriggerEvents = new System.Collections.Generic.List<string> { "OnAttack" },
                Profile = profile,
                Conditions = conditions ?? new System.Collections.Generic.List<Condition<MockUnit>>()
            };
            return SkillFactory<MockUnit>.CreateSkill(data);
        }

        [SetUp]
        public void SetUp()
        {
            SkillSystemServices.Clock = new FixedSkillClock(0f, 0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            SkillSystemServices.Clock = new UnitySkillClock();
        }

        /// <summary>读条成功完成后应触发 OnCastCompleted。</summary>
        [Test]
        public void CompleteCast_FiresOnCastCompletedWhenPipelineSucceeds()
        {
            var skill = CreateSkill(new SkillProfile { castTime = 1f });
            var controller = new SkillExecutionController<MockUnit>();
            int completed = 0;
            controller.OnCastCompleted += (_, __) => completed++;

            Assert.IsTrue(controller.TryExecute(skill, new SkillContext<MockUnit>(new MockUnit())));
            controller.Tick();
            controller.Tick();

            Assert.AreEqual(1, completed);
        }

        /// <summary>读条完成后条件失败时不应触发 OnCastCompleted。</summary>
        [Test]
        public void CompleteCast_SkipsOnCastCompletedWhenPipelineFails()
        {
            bool eligible = false;
            var skill = CreateSkill(
                new SkillProfile { castTime = 1f },
                new System.Collections.Generic.List<Condition<MockUnit>>
                {
                    new FuncCondition<MockUnit>(_ => eligible)
                });
            var controller = new SkillExecutionController<MockUnit>();
            int completed = 0;
            controller.OnCastCompleted += (_, __) => completed++;

            Assert.IsTrue(controller.TryExecute(skill, new SkillContext<MockUnit>(new MockUnit())));
            controller.Tick();
            controller.Tick();

            Assert.AreEqual(0, completed);
        }

        /// <summary>打断施法时不应触发 OnCastCompleted。</summary>
        [Test]
        public void Interrupt_DoesNotFireOnCastCompleted()
        {
            var skill = CreateSkill(new SkillProfile { castTime = 2f, canBeInterrupted = true });
            var controller = new SkillExecutionController<MockUnit>();
            int completed = 0;
            int interrupted = 0;
            controller.OnCastCompleted += (_, __) => completed++;
            controller.OnCastInterrupted += (_, __) => interrupted++;

            Assert.IsTrue(controller.TryExecute(skill, new SkillContext<MockUnit>(new MockUnit())));
            Assert.IsTrue(controller.TryInterrupt(InterruptReason.Manual));

            Assert.AreEqual(0, completed);
            Assert.AreEqual(1, interrupted);
        }
    }

    /// <summary>SkillTimelineService 时间轴测试。</summary>
    public class SkillTimelineServiceTests
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

        [TearDown]
        public void TearDown()
        {
            SkillTimelineService.StopAll();
        }

        /// <summary>StopForOwner 只停止指定单位的时间轴。</summary>
        [Test]
        public void StopForOwner_OnlyStopsMatchingOwner()
        {
            var ownerA = new MockUnit();
            var ownerB = new MockUnit();
            var counterA = new CounterMechanism();
            var counterB = new CounterMechanism();

            var timelineA = new SkillTimelineData
            {
                enabled = true,
                totalDuration = 1f,
                clips = new System.Collections.Generic.List<SkillTimelineClip>
                {
                    new SkillTimelineClip
                    {
                        clipType = SkillTimelineClipType.Mechanism,
                        startTime = 0f,
                        mechanism = counterA
                    }
                }
            };

            var timelineB = new SkillTimelineData
            {
                enabled = true,
                totalDuration = 1f,
                clips = new System.Collections.Generic.List<SkillTimelineClip>
                {
                    new SkillTimelineClip
                    {
                        clipType = SkillTimelineClipType.Mechanism,
                        startTime = 0f,
                        mechanism = counterB
                    }
                }
            };

            var skillData = new SkillData<MockUnit>
            {
                SkillName = "TimelineSkill",
                SkillType = SkillType.Active,
                TriggerEvents = new System.Collections.Generic.List<string> { "OnAttack" }
            };
            var skill = SkillFactory<MockUnit>.CreateSkill(skillData);

            SkillTimelineService.Play(skill, new SkillContext<MockUnit>(ownerA), timelineA);
            SkillTimelineService.Play(skill, new SkillContext<MockUnit>(ownerB), timelineB);

            SkillTimelineService.StopForOwner(ownerA);
            SkillTimelineService.Tick(1f);

            Assert.AreEqual(0, counterA.Count);
            Assert.AreEqual(1, counterB.Count);
        }

        /// <summary>大 delta 仍应触发所有已到时间的 clip。</summary>
        [Test]
        public void Tick_LargeDelta_ExecutesAllDueClipsOnce()
        {
            var owner = new MockUnit();
            var first = new CounterMechanism();
            var second = new CounterMechanism();
            var timeline = new SkillTimelineData
            {
                enabled = true,
                totalDuration = 2f,
                clips = new System.Collections.Generic.List<SkillTimelineClip>
                {
                    new SkillTimelineClip
                    {
                        clipType = SkillTimelineClipType.Mechanism,
                        startTime = 0f,
                        mechanism = first
                    },
                    new SkillTimelineClip
                    {
                        clipType = SkillTimelineClipType.Mechanism,
                        startTime = 0.5f,
                        mechanism = second
                    }
                }
            };

            var skillData = new SkillData<MockUnit>
            {
                SkillName = "TimelineSkill",
                SkillType = SkillType.Active,
                TriggerEvents = new System.Collections.Generic.List<string> { "OnAttack" },
                Timeline = timeline
            };
            var skill = SkillFactory<MockUnit>.CreateSkill(skillData);

            SkillTimelineService.Play(skill, new SkillContext<MockUnit>(owner), timeline);
            SkillTimelineService.Tick(2f);

            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(1, second.Count);
        }
    }
}
