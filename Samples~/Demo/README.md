# Skill System Demo

## 快速开始

1. 在 Unity 菜单运行 **Tech-Cosmos → SkillSystem → Generator → Generate ALL Classes**
2. 运行 **Tech-Cosmos → SkillSystem → Samples → Create Demo Scene**
3. 打开生成的 `SkillSystemDemo` 场景并 Play
4. **Space** — 玩家攻击敌人（含冷却 + Timeline 事件）
5. **H** — 治疗敌人

## 内容

| 脚本 | 说明 |
|------|------|
| `DemoCharacter` | 继承 `UnitBase`，带 `[GenerateSkillDataSO]` |
| `DemoDamageMechanism` | 演示伤害机制 |
| `DemoHealMechanism` | 演示被动治疗 |
| `DemoSceneController` | 场景入口，可纯代码或 SkillDataSO 驱动 |

## 可选：使用 SkillDataSO 资产

1. 运行代码生成后，右键 **Create → Tech-Cosmos → Skill → Demo Character**
2. 在 **技能编辑器** 中配置条件树与 Timeline
3. 将资产拖到 `DemoSceneController.skillAsset`
