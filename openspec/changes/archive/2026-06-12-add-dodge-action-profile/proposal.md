# Change: 增加 Shift FullBody 冲刺/后闪动作和可替换动作动画 Profile

## Why
当前项目已经具备基础移动、输入缓冲和 Action 打断仲裁地基，但还没有一个可演示的 Shift FullBody 动作闭环。这个动作按一次 Shift 触发：有方向输入时向当前移动方向冲刺，无方向输入时向后闪避。它需要输入方向判定、优先级仲裁、动作位移、动画替换，以及方向冲刺结束后进入 Run 档位。

同时当前角色动画来源不稳定，可琳部分动画不适合作为长期固定实现。需要把“动作语义”和“具体动画资源”分离，让不同角色或同一角色的不同动画套件能替换 Dodge 动画，而不修改 Dodge 逻辑。

## What Changes
- 将 Shift 绑定为这个 FullBody 动作的输入来源，不再把 Shift held 直接作为基础移动 Run 输入。
- 新增最小 FullBody 动作能力：有移动方向输入时执行方向冲刺，无移动方向输入时执行后闪。
- 方向冲刺完成后进入基础移动 Run 档位，不需要继续按住 Shift；无方向后闪不强制进入 Run。
- 角色完全停下并回到 Idle 后重置 Run latch，下次普通移动回到 Walk。
- 本变更不实现 cooldown；动作结束并回到 `Action.None` 后，再次按下 Shift 必须能重新触发该 FullBody 动作。
- 该动作通过现有 `InputRequestBuffer`、`ActionInterruptArbiter` 和 `ActionRuntimeStateTracker` 地基进入，不新增绕过当前系统的第二角色控制路径。
- 最终架构 MUST 收束到同一个 FullBody 行为域：基础 Locomotion 局部状态图可以作为模块存在，但不能和 Dodge/FullBody Action 形成两套平级、同时争夺 base layer 或角色位移的状态路径。
- 当前只实现 FullBody 层级主树：FullBody 主层负责 Idle/MoveStart/MoveLoop/MoveStop 与 Dodge 等全身行为；UpperBody、Facial、IK 等并行表现层不在本变更范围内，后续必须另开 OpenSpec。
- 新增动作动画 Profile 能力，用稳定 action animation key 解析具体动画表现，第一版至少支持 `Action.Dodge.Directional` 和 `Action.Dodge.Backstep`。
- 最终编辑入口 SHOULD 收束为明确的 FullBody 装配闭环：动作逻辑入口负责运动参数和打断策略，动作动画绑定入口负责 `ActionStateId -> ActionAnimationProfile`，角色 FullBody 主调度入口显式引用二者；设计者不应该被迫在多个互不关联的游离配置里拼出一个 Dodge。
- 动作位移必须通过统一运动出口或等价运动执行端口提交，不允许动画 Presenter、Animancer 回调或 Root Motion 直接移动角色。
- 保留现有 Locomotion `Idle / MoveStart / MoveLoop / MoveStop` 局部状态图职责，该 FullBody 动作不成为基础移动 phase 或 Walk/Run gait。
- 不允许通过错误复用 `ActionRuntimeStateTracker` resistance、step 或 input buffer 过期造成“只能 Shift 一次”的隐式锁死。

## Impact
- Affected specs:
  - `dodge-action`
  - `action-animation-profile`
  - `basic-locomotion-animation`
  - 关联现有 `action-interrupt-arbiter`
  - 关联现有 `action-runtime-state-tracker`
  - 关联现有 `local-preinput-buffer`
  - 关联现有 `wasd-locomotion-pipeline`
- Affected code:
  - `Assets/Scripts/Character/Action`
  - `Assets/Scripts/Character/Animation`
  - `Assets/Scripts/Character/Movement`
  - `Assets/Scripts/Input`
  - `Assets/Tests/Editor`
- Not in scope:
  - 完整攻击连招、cancel window、hitbox、IK、VFX/SFX、网络同步和预测回滚。
  - 把该 FullBody 动作塞进基础 Locomotion 状态图。
  - 新增独立 `Action.Sprint` 或第二个 Sprint 变更。
  - 新增第二套 FullBody/WASD base-layer 状态机来绕过现有 Locomotion、Action 仲裁或运动出口。
  - 让完整 Animator Root Motion 或 Animancer 事件直接驱动角色位移。
