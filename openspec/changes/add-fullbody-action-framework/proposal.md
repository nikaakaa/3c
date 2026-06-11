# Change: 增加 FullBody Action 框架主入口

## Why
当前 Dodge 变更已经把 Shift 动作、Action 仲裁、输入缓冲、动作动画 Profile 和统一运动出口串成了最小闭环，但运行时形态仍以 `PlayerLocomotionController` 加 `PlayerDodgeActionController` 两个 MonoBehaviour 协作为主。这个形态适合验证一条动作，不适合继续扩展 Roll、Jump、Vault、Attack、Hit、Death 等全身动作，否则每个动作都会新增自己的调度点和压制逻辑，最终又回到分裂路径。

需要先把 FullBody 行为域框架接出来：基础 Locomotion 是 FullBody 主层下的子职责，全身 Action 是同一主层下的可注册模块，最终每帧只有一个 base layer 行为和一个平面位移权威。动画转换问题、具体 Dodge 手感、cooldown 和网络同步可以在框架稳定后分别小步处理。

## What Changes
- 新增 FullBody Action 框架能力，提供单一 FullBody 主调度入口或等价 coordinator。
- 将现有基础 Locomotion 视为 FullBody 主层下的 Locomotion 子图/模块，而不是与 Dodge 平级争夺 base layer 的独立状态权威。
- 定义 FullBody Action module 端口，使 Dodge、Roll、Jump、Vault 等全身动作能通过统一输入请求、Action 仲裁、生命周期、运动命令和动画命令协作。
- 定义每帧固定调度顺序：输入事实/请求 -> Locomotion 意图事实 -> Action 仲裁 -> FullBody 行为选择 -> 运动命令 -> 动画命令 -> 相机 Look/Resolve。
- 定义输出权威规则：每帧平面位移只能由当前 FullBody 行为提交到统一 motion executor；base layer 动画只能由当前 FullBody 行为提交到对应 presenter。
- 定义配置入口规则：FullBody Action 逻辑集只聚合各 Action 的逻辑定义、运动参数和打断策略；动作动画通过独立的 `ActionStateId -> ActionAnimationProfile` 绑定集进入 FullBody 主调度入口；Locomotion Walk/Run 状态图和 alias 配置仍属于 Locomotion 配置入口。
- 规划把当前 `PlayerDodgeActionController` 收束为 FullBody Action module 或迁移适配器，不再作为长期独立动作调度入口。
- 增加自动测试、静态边界验证和手动验证任务，证明框架没有复制 BBB 主控、没有新增第二运动路径、没有让动画 Presenter 接管业务状态或位移。

## Impact
- Affected specs:
  - `fullbody-action-framework`
  - `wasd-locomotion-pipeline`
  - 关联现有 `unityhfsm-locomotion`
  - 关联现有 `action-interrupt-arbiter`
  - 关联现有 `action-runtime-state-tracker`
  - 关联活跃变更 `add-dodge-action-profile`
- Affected code:
  - `Assets/Scripts/Character/Action`
  - `Assets/Scripts/Character/Movement`
  - `Assets/Scripts/Character/Animation`
  - `Assets/Scripts/Input`
  - `Assets/Tests/Editor`
- Not in scope:
  - 不调 Dodge 动画转换 bug、clip 混合、具体过渡时机或 8 向动画。
  - 不实现 cooldown、耐力、cost、cancel window、hitbox、IK、VFX/SFX 或连招。
  - 不引入完整 Root Motion 位移权威；如必须改变运动权威，需要另开 proposal。
  - 不实现网络同步、预测、回滚或 Fantasy 协议修改。
  - 不复制 BBB 的 `BBBCharacterController`、`PlayerStateRegistry`、`PlayerBaseState`、`OverrideState` 或运行时命名空间依赖。
