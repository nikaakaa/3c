# Change: 增加可配置状态打断窗口

## Why
TurnBack 已经从临时移动特判收敛为状态，但它的输入锁定、转身完成、退出和打断规则仍是局部字段，不能表达“前 20% 不可打断、20%-47% 只允许高优先级、47% 后恢复普通移动”这类动作窗口。后续攻击连招、Dodge、受击和 TurnBack 都需要同一种 timeline window、priority、resistance 和 baked motion 采样模型，否则会继续出现分裂路径。

## What Changes
- 新增状态级 timeline policy：每个 FullBody 状态可以配置 motion window、input lock window、interrupt/cancel window、exit window、priority、resistance 和窗口标签。
- 将 timeline policy 采样为纯数据 facts，供统一状态机条件、运动输出和打断仲裁消费，不让 Animancer、Animator 或 MonoBehaviour 直接决定业务窗口。
- 扩展现有 ActionInterrupt 仲裁思路为状态请求准入入口，使 TurnBack、Dodge、Attack、HitReact 等请求都能使用同一套 priority/resistance/window 规则。
- 明确逻辑 transition、自然退出窗口、打断/取消窗口和视觉 blend 的边界：transition 条件满足后立即切换逻辑状态，视觉 fade、clip、speed 和 TransitionAsset 只属于动画表现配置。
- TurnBack 第一版作为落地对象：只允许从 RunLoop 触发，视觉播放完整 inplace 动画，位移和 yaw 来自已配置的 baked motion profile，普通输入旋转和平面位移在配置窗口内被锁定。
- 预留轻量编辑器/校验入口，但第一版运行时只要求正式配置资产和自动测试，不实现完整 timeline 编辑器。

## Impact
- Affected specs: `state-timeline-policy`, `animation-phase-timeline-facts`, `action-interrupt-arbiter`, `action-interrupt-policy-data`, `unified-character-state-machine`, `basic-locomotion-animation`
- Affected code: `Assets/Scripts/Character/Action/Model/*Interrupt*`, `Assets/Scripts/Character/StateMachine/*`, `Assets/Scripts/Character/Movement/*`, `Assets/Scripts/Character/Animation/*`, `Assets/Configs/3C/Animation/Locomotion/Corin/Bake/*`
- Related changes: builds on `formalize-turnback-locomotion-state`, coordinates with `refactor-fullbody-frame-pipeline`, and provides the shared window model needed by `add-light-attack-combo-action`
