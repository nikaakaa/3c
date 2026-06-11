# Change: 新增动作打断策略数据源

## Why

`add-action-interrupt-arbiter` 已经把“能不能打断”的裁决核心拆成纯逻辑模块，但目前策略仍只能由代码临时构造。后续如果要接攻击、闪避、受击、死亡、输入缓冲、预测回滚和编辑器，需要先有一个稳定、可校验、可序列化的策略集合数据源，而不是把打断关系散落在状态类、动画配置或测试 helper 里。

本变更只补最小数据来源：让项目可以配置一组 `ActionInterruptPolicy`，并把它们编译成仲裁器可消费的纯数据列表。它不做完整动作状态库，不接运行时状态机，不接 Animancer，不做 Timeline 编辑器。

## What Changes

- 新增 `action-interrupt-policy-data` 能力，用序列化数据表达一组动作打断策略。
- 提供最小 `ActionInterruptPolicySet` 或等价数据结构，持有多条策略定义。
- 提供 Unity Inspector 可编辑的 ScriptableObject 包装或等价配置入口，但不做自定义编辑器窗口。
- 策略定义使用稳定状态 ID、最小优先级、时间规则、窗口和 force 标记，不引用 Unity 场景对象、AnimationClip 或 Animancer 类型。
- 提供编译/解析步骤，把序列化定义转换成现有 `ActionInterruptPolicy` 列表。
- 扩展或复用 `ActionInterruptPolicyValidator`，校验策略集合中的非法 ID、非法窗口、负优先级和重复规则。
- 增加 EditMode 测试，证明策略集合可校验、可转换、可被现有仲裁器消费。
- 明确不接入 `PlayerLocomotionController`、不修改 `Idle / MoveStart / MoveLoop / MoveStop` 状态图、不改变 `MoveStop -> MoveStart`。

## Non-Goals

- 不新增完整 `ActionStateDefinition`、`ActionStateCatalog`、动作层级或动作状态机。
- 不接输入缓冲，不把输入直接转成动作请求。
- 不调用 `ActionInterruptArbiter` 来驱动实际状态切换。
- 不播放动画，不接 Animancer、Animator、TransitionAsset、AnimationClip 或 clip length。
- 不迁移 `MoveStop/RunEnd` 逻辑。
- 不实现 FullBody / UpperBody / LowerBody 多层仲裁。
- 不实现 Timeline cancel window 编辑器。
- 不复制 BBB 代码或依赖 `Ref/BBB-Nexus`。

## Impact

- Affected specs:
  - `action-interrupt-policy-data`
- Related active changes:
  - `add-action-interrupt-arbiter`
- Affected code planned:
  - `Assets/Scripts/Character/Action/Config/ActionInterruptPolicySetSO.cs`
  - `Assets/Scripts/Character/Action/Model/ActionInterruptPolicyDefinition.cs`
  - `Assets/Scripts/Character/Action/Model/ActionInterruptPolicySet.cs`
  - `Assets/Scripts/Character/Action/Solver/ActionInterruptPolicySetCompiler.cs`
  - `Assets/Scripts/Character/Action/Solver/ActionInterruptPolicyValidator.cs`
  - `Assets/Tests/Editor/ActionInterruptPolicyDataTests.cs`

## Assumptions

- 当前 `add-action-interrupt-arbiter` 先作为依赖存在；如果它先被归档，本变更继续追加在正式 spec 之上。
- 第一版只要求 Inspector 能编辑 ScriptableObject 字段，不要求专用窗口、Timeline 轨道或可视化图。
- 第一版允许使用字符串状态 ID；是否引入完整 state catalog 另起 proposal。
