# Change: 增加三段轻攻击动作连段

## Why
当前项目已经有 Attack 输入请求、Action 仲裁、FullBody Action 状态、动作动画 Profile 和回滚输入事实的基础，但还没有可执行的攻击动作状态。下一步需要先把“按攻击键播放并衔接三段轻攻击动作”做成 Character frame pipeline 下的 FullBody Action submission，伤害判定、hitbox 和受击以后再单独规划。

## What Changes
- 新增三段轻攻击动作语义：`Action.Attack01`、`Action.Attack02`、`Action.Attack03`。
- 新增三段轻攻击稳定动画 key：`Action.Attack.Light.01`、`Action.Attack.Light.02`、`Action.Attack.Light.03`。
- 使用现有 `InputRequestBuffer` 的 `Attack` 请求作为攻击输入来源，不让输入层提前决定连段结果。
- 使用统一 request submission 与 `ActionInterruptArbiter` 或等价仲裁模块判断能否从 Locomotion 进入攻击、能否从当前攻击段进入下一段。
- 新增最小连段窗口事实：只表达“当前攻击段此刻能否消费 Attack 请求进入下一段”。
- 攻击 active 时由 FullBody Action submission 成为 base layer 和平面位移的 winning submission，Locomotion 只提供移动/朝向事实。
- 攻击配置必须是正式配置，缺失配置必须校验报错，不新增 fallback 手感配置。
- 本变更不实现伤害、hitbox、hurtbox、命中停顿、击退、受击状态、锁敌、VFX/SFX/Camera event 轨道、IK、完整 Timeline 编辑器、网络协议或 Root Motion 位移权威。

## Impact
- Affected specs:
  - `light-attack-combo-action`
  - `action-animation-profile`
  - `animation-phase-timeline-facts`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Model/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Config/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/FullBody/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/StateMachine/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/*`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/*`
