using System.Collections.Generic;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Samples
{
    /// <summary>
    /// Demo 场景控制器，绑定玩家与敌人并处理键盘输入触发技能。
    /// </summary>
    public class DemoSceneController : MonoBehaviour
    {
        [Header("Units")]
        public DemoCharacter player;
        public DemoCharacter enemy;

        [Header("Optional Skill Asset")]
        public SkillDataSO skillAsset;

        [Header("Controls")]
        public KeyCode attackKey = KeyCode.Space;
        public KeyCode healKey = KeyCode.H;

        private void Start()
        {
            if (player == null || enemy == null)
            {
                Debug.LogError("[DemoSceneController] 请在 Inspector 中指定 player 与 enemy。");
                return;
            }

            SetupSkills();
            Debug.Log("[DemoSceneController] 演示就绪：Space 攻击，H 治疗（带 Buff）。");
        }

        private void Update()
        {
            if (player == null || enemy == null) return;

            if (Input.GetKeyDown(attackKey))
            {
                player.TriggerEvent("OnAttack", new SkillContext<DemoCharacter>(player, enemy));
            }

            if (Input.GetKeyDown(healKey))
            {
                enemy.Heal(15f);
            }
        }

        private void SetupSkills()
        {
            if (skillAsset != null)
            {
                var created = skillAsset.CreateSkill();
                if (created is ISkill<DemoCharacter> skill)
                {
                    player.AddSkill(skill);
                    return;
                }

                Debug.LogWarning("[DemoSceneController] SkillDataSO 类型与 DemoCharacter 不匹配，改用代码构建技能。");
            }

            SetupCodeDefinedSkill();
        }

        private void SetupCodeDefinedSkill()
        {
            var tree = new ConditionTreeAnd();
            tree.children.Add(new ConditionTreeLeaf
            {
                condition = new CooldownCondition<DemoCharacter> { cooldown = 0.6f }
            });

            var attackData = new SkillData<DemoCharacter>
            {
                SkillName = "DemoAttack",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" },
                Profile = new SkillProfile { executionPriority = 10 }
            };

            attackData.Conditions.Add(ConditionTreeCompiler.Compile<DemoCharacter>(tree));
            attackData.AddMechanism(new DemoDamageMechanism<DemoCharacter> { baseDamage = 15f });
            attackData.SetValue("Damage", 15f);

            attackData.Timeline = new SkillTimelineData
            {
                enabled = true,
                totalDuration = 0.5f,
                clips = new List<SkillTimelineClip>
                {
                    new SkillTimelineClip
                    {
                        label = "HitFrame",
                        clipType = SkillTimelineClipType.EventMarker,
                        startTime = 0.25f,
                        duration = 0.05f,
                        eventName = "OnTimelineHit"
                    }
                }
            };

            player.AddSkill(SkillFactory<DemoCharacter>.CreateSkill(attackData));

            var regenData = new SkillData<DemoCharacter>
            {
                SkillName = "RegenAura",
                SkillType = SkillType.Passive,
                TriggerEvents = new List<string>()
            };
            regenData.AddMechanism(new DemoHealMechanism<DemoCharacter> { healAmount = 5f });
            player.AddSkill(SkillFactory<DemoCharacter>.CreateSkill(regenData));
        }
    }
}
