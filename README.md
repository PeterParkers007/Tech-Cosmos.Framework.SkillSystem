# TechCosmos Skill System Framework

一个高度可扩展的泛型技能系统框架，支持自定义条件、机制和多层技能架构，适用于各类Unity游戏开发。

## 特性

### 🏗️ 分层架构
- **基础层**: 处理技能触发机制（主动/被动）
- **条件层**: 灵活的条件判断系统，支持逻辑运算符组合
- **信息层**: 技能名称、描述等显示信息
- **机制层**: 技能效果和执行逻辑
- **数据层**: 技能数值和公式计算
- **执行层**: 统一的技能执行流程

### 🔧 高度可扩展
- 泛型设计，与具体游戏逻辑完全解耦
- 易于添加自定义条件和机制
- 支持运算符重载的条件组合系统

### 🛡️ 类型安全
- 编译时类型检查
- 避免运行时类型转换错误
- 完整的泛型约束

## 安装

### 通过 Unity Package Manager
1. 打开 Unity Editor
2. 进入 Window > Package Manager
3. 点击 "+" 按钮，选择 "Add package from git URL"
4. 输入: `https://github.com/PeterParkers007/Tech-Cosmos.Framework.SkillSystem.git`

### 手动安装
1. 下载最新 release
2. 将 `TechCosmosSkillSystem` 文件夹放入项目的 `Assets` 目录

## 快速开始

### 1. 定义你的单位类
```csharp
using TechCosmos.SkillSystem.Runtime;
using UnityEngine;

public class GameCharacter : MonoBehaviour, IUnit<GameCharacter>
{
    private SkillHolder<GameCharacter> skillHolder;
    private UnitEvent<GameCharacter> unitEvent;
    
    [SerializeField] private string[] supportedEvents = new[] { "OnAttack", "OnBeingHit" };

    private void Start()
    {
        SkillSystemConfig.Initialize<GameCharacter>();
        unitEvent = new UnitEvent<GameCharacter>(supportedEvents);
        skillHolder = new SkillHolder<GameCharacter>(unitEvent);
        
        // 添加技能
        InitializeSkills();
    }

    public string[] GetSupportedEvents() => supportedEvents;
    public void TriggerEvent(string eventName, SkillContext<GameCharacter> context) 
        => unitEvent.Trigger(eventName, context);
    public void AddSkill(ISkill<GameCharacter> skill) => skillHolder.AddSkill(skill);
    public void RemoveSkill(ISkill<GameCharacter> skill) => skillHolder.RemoveSkill(skill);
    
    private void InitializeSkills()
    {
        // 技能初始化代码
    }
}
```

### 2. 创建技能数据
```csharp
var skillData = new SkillData<GameCharacter>
{
    SkillType = SkillType.Passive,
    TriggerEvent = "OnBeingHit",
    SkillName = "反击",
    SkillDescription = "受到攻击时有一定几率反击",
    
    Conditions = new List<Condition<GameCharacter>>
    {
        new CooldownCondition<GameCharacter>(2.0f, skillData),
        new FuncCondition<GameCharacter>(ctx => UnityEngine.Random.value > 0.7f)
    },
    
    Mechanisms = new List<Action<SkillContext<GameCharacter>>>
    {
        ctx => Debug.Log($"{ctx.caster.name} 发动了反击!"),
        ctx => ctx.target.TakeDamage(new Damage<GameCharacter> { owner = ctx.caster, damage = 10 })
    }
};

var skill = SkillFactory<GameCharacter>.CreateSkill(skillData);
GetComponent<GameCharacter>().AddSkill(skill);
```

### 3. 创建自定义条件
```csharp
public class HealthCondition<T> : Condition<T> where T : IUnit<T>
{
    private float minHealthPercent;
    
    public HealthCondition(float minHealthPercent)
    {
        this.minHealthPercent = minHealthPercent;
    }
    
    public override bool IsEligible(SkillContext<T> context)
    {
        // 假设你的单位类有 GetHealthPercent 方法
        var unit = context.caster as GameCharacter;
        return unit != null && unit.GetHealthPercent() >= minHealthPercent;
    }
}
```

### 4. 使用条件组合
```csharp
// 组合条件：冷却完成 AND (生命值高于50% OR 有护盾)
var combinedCondition = 
    new CooldownCondition<GameCharacter>(5.0f, skillData) & 
    (new HealthCondition<GameCharacter>(0.5f) | new HasShieldCondition<GameCharacter>());
```

## 核心概念

### 技能层 (Skill Layers)
框架将技能分为六个独立的层，每层负责特定的功能：

- **IBaseLayer**: 技能触发基础（主动/被动）
- **IConditionLayer**: 技能释放条件判断
- **IInformationLayer**: 技能描述信息
- **IMechanismLayer**: 技能效果机制
- **IDataLayer**: 技能数值数据
- **IExecuteLayer**: 技能执行流程

### 条件系统 (Condition System)
条件系统支持复杂的逻辑组合：
```csharp
// 使用运算符重载创建复杂条件
var complexCondition = 
    (conditionA & conditionB) | 
    (!conditionC & conditionD);
```

### 事件系统 (Event System)
基于委托的事件系统，支持动态订阅和触发：
```csharp
// 订阅事件
unitEvent.Subscribe("OnAttack", OnAttackHandler);

// 触发事件
unitEvent.Trigger("OnAttack", skillContext);
```

## API 文档

### 核心接口
- `IUnit<T>`: 单位接口，需要游戏中的单位类实现
- `ISkill<T>`: 技能接口
- `SkillContext<T>`: 技能执行上下文

### 主要类
- `SkillFactory<T>`: 技能创建工厂
- `SkillHolder<T>`: 技能持有者管理
- `UnitEvent<T>`: 单位事件系统

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

MIT License - 详见 [LICENSE](LICENSE) 文件

## 支持

如有问题请：
- 发送邮件至: 3427463164@qq.com