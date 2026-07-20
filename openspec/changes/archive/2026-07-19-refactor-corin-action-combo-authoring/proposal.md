# Change: 收口 Corin Action 层级、攻击后摇与 locomotion ownership

> 历史状态：本 change 记录五段攻击与嵌套 Dodge 的前序迁移过程，其 `RushAttack`、循环 Attack5→Attack1、per-state Cancel/MoveCancel、`ResumeLocomotionThroughRunEnd` 和 Agent v8 结果已被 `refactor-action-transition-eligibility-authoring` 破坏性取代。当前实现与当前规范以该后继 change、`openspec/specs/` 和 `openspec/project.md` 为准；本目录不再作为可执行作者模板。

## Why

Corin 当前只把 `Attack1`、`Attack2` 放入 `Attack` 的内联嵌套状态机，但动画资源实际提供五段普通攻击和每段独立后摇，完整作者闭环仍有四个缺口：

- 当前只接入 `Attack_Normal_01` 与 `Attack_Normal_02`，没有接入 `Attack_Normal_03`、`04`、`05`。五段普通攻击分别拥有独立 `_End` 后摇资源，03 另有 `Explode`，05 另有 `05_B` 特殊资源。
- `Attack1` 与 `Attack2` Timeline 只放置约 0.82 秒和 0.80 秒的主攻击动画，对应 End 后摇没有进入 Timeline；主攻击 clip 目前通过 `Hold` 保持末帧直到第 80 帧，因此“没有继续攻击”不是播放后摇，而是冻结末帧后直接释放。
- Locomotion 的 `ActionOverride` 只读取 `IsDodging`。Attack 活跃时 Locomotion 仍会推进并提交 Base producer，隐藏的 `RunEnd` 可能在攻击释放时以中间时间重新露出，看起来像 RunEnd 打断攻击。
- 外层 Action StateMachine 仍将 `DodgeBack`、`DodgeForward` 平铺为动作大类，与已经层级化的 `Attack` 不一致；具体方向选择、动作组 ownership 和 leaf ActionInstance 生命周期混在同一层。

这些问题不需要新增 Action 专用 interrupt runtime，也不应由动画 priority、fallback Idle 或 Animancer 猜测修复。应在现有 StateMachine、Timeline、TreeClip、Blackboard 和统一 Runnable stop 合同内把作者数据收口。

## What Changes

- 将外层 Action StateMachine 收敛为 `None`、`Attack`、`Dodge` 三个动作大类。
- 将 `Attack` 的内联 `Attack Combo StateMachine` 完整扩展为显式循环 `Attack1 -> Attack2 -> Attack3 -> Attack4 -> Attack5 -> Attack1`，并增加独立 `RushAttack` leaf；将 `DodgeBack`、`DodgeForward` 移入 `Dodge` 的内联 `Dodge Direction StateMachine`。
- 外层 `None -> Dodge` 唯一接受 Dodge request；内层 Entry 只按当前 MoveAxis 选择 DodgeBack/DodgeForward，不得再次以瞬时 request 作为方向状态机启动条件，最终仍由目标 leaf 唯一消费 request。
- 由外层 `Attack`、`Dodge` 状态统一发布 root-owned pipeline blackboard ownership：
  - `HasActionLocomotionOwnership` 表示全身动作正在占用 locomotion 表现与运动状态入口。
  - `ResumeLocomotionThroughRunEnd` 表示动作结束且无移动输入时是否经过 RunEnd；Dodge 为 `true`，Attack 为 `false`。
- 删除 `IsDodging` declaration、读写节点和对应条件图，不保留兼容镜像。
- 将 RootTree 的 Gameplay Parallel 固定为 Action 分支先执行、Locomotion 分支后执行，使同一 logic tick 内先提交 ownership，再由 Locomotion 进入或离开 `ActionOverride`。
- Locomotion 的所有普通状态在 `HasActionLocomotionOwnership=true` 时进入无 Timeline、无 motion 的 `ActionOverride`，因此 Action 活跃期间不会推进隐藏 RunEnd/RunLoop producer。
- Action 释放 ownership 后：
  - 有移动输入直接进入 RunLoop。
  - 无移动输入且 `ResumeLocomotionThroughRunEnd=true` 进入 RunEnd。
  - 无移动输入且 `ResumeLocomotionThroughRunEnd=false` 进入 Idle。
- 以 `WithWeaponInplace` 五段普通攻击及其 End 资源为表现来源，沿用现有 Pipeline Attack1/2 的根节点初始 X/Z 归零规则，补齐规范化 PipelineInplace Attack3/4/5 主动画与 Attack1..5 End 动画。
- 以匹配的 `WithWeaponRootmotion` 主攻击与 End 资源为 gameplay motion 来源，补齐 Attack1..5 的主段和后摇 motion curve；动画根位移不得成为第二条运行时 root-motion 路径。
- 在 Attack1..5 各自同一个 AnimationTrack 中摆放“主攻击 clip + End 后摇 clip”，并在同一个 MotionCurveTrack 中摆放匹配的主段与后摇 motion clip。
- 主攻击 clip 不再 `Hold` 到 Timeline 末尾；主攻击与 End clip 使用一次明确的短重叠/ease，End clip 使用完整资源时长并成为 Timeline 自然完成边界。
- 保留 Attack1/2 现有 Hit、Cancel Decision TreeClip、Action Context 和 Cue，为 Attack3/4/5 建立同构的 leaf state、inline Timeline、Hit/Cancel TreeClip、Cue 与 motion；为 Attack1..5 增加独立 MoveCancel TreeClip。Corin 显式启用 Attack5-to-Attack1 循环，Macro 不得为未声明循环的角色隐式生成该边。
- 将 `Attack_Rush` 主段与 `Attack_Rush_End` 接入 Attack 内层独立 `RushAttack` leaf。Dodge 后摇内收到 Attack request 时进入 RushAttack；RushAttack 后段收到 Attack request 时进入 Attack1，收到移动输入时退出到 locomotion。
- 将 Dodge 现有后段窗口收敛为 `DodgeRecoveryCancel`。窗口外普通输入不能中断主段；窗口内按稳定顺序处理 Attack、再次 Dodge、移动和自然完成，同 Tick 的 Attack 优先于 Dodge，Dodge 优先于移动。
- 每段普通攻击的连段窗口与移动取消窗口均由同一 Timeline 的 Decision TreeClip 承载。同 Tick 同时存在 Attack 与移动时连段优先；没有有效取消时完整播放 End 后摇。
- `Attack_Normal_03_Explode` 与 `Attack_Normal_05_B` 记录为特殊分支候选，但在没有正式输入/资源/条件语义前不进入普通五连，不创建猜测性 transition。
- 作者完成本 change 后只需要在 Attack1..5 Timeline 中调整 Hit/Cancel 等精细窗口，不需要回到 RootTree、Locomotion 或动画表现配置补节点。
- 重新生成唯一 Semantic IR、Float32 Program 与 Presentation Projection；producer identity 继续由原 Timeline/AnimationTrack 决定，不新增第二份动画 binding。

## Impact

- 修改 Corin RootTree 内联 Action、Locomotion、Attack、Dodge StateMachine 与状态行为图。
- 修改 Corin Attack1/Attack2 inline Timeline tracks 与 clips。
- 将 Agent 旧 `two_hit_combo` / `dodge_cancel` 样例收敛为可变段数 `action_combo` 与层级化 `directional_dodge`，使 intent 可显式声明 combo loop、移动取消、Dodge 后摇取消与 RushAttack，并在 synthesis 业务覆盖中检查 Corin 五段循环 Attack、RushAttack、nested Dodge 和统一 ownership；通用 Graph Validator 不硬编码 Corin。
- 新增 Attack3/4/5 主动画与 Attack1..5 End 的正式 PipelineInplace 资产，来源为现有 WithWeaponInplace 动画并使用统一根节点归零规则。
- 新增 Attack3/4/5 主段与 Attack1..5 End 的正式 root-motion curve 数据，来源为匹配 WithWeaponRootmotion 动画。
- 删除 Corin `IsDodging` blackboard 数据及所有引用。
- 修改 `character-state-timeline-authoring-loop` 与 `character-action-authoring-closure` 当前能力口径。
- 不修改 StateMachine runtime、Runnable stop、Action lifecycle、AnimationPlaybackLifecycle 或 Animancer 混合职责。
- 不新增测试，不运行 Unity batchmode，不引入 fallback、兼容字段、旧平铺 Dodge 路径或第二套 Action 仲裁。

## Current Spec Comparison

- 当前 `character-state-timeline-authoring-loop` 明确要求外层 Action 显示 `DodgeBack`、`DodgeForward`，并要求 Locomotion 只通过 `IsDodging` 进入 `ActionOverride`；这与本 change 的动作大类层级和统一 ownership 冲突，必须通过 MODIFIED delta 替换。
- 当前 `character-action-authoring-closure` 将 `IsDodging` 定义为 Dodge 专用 locomotion ownership 真相；本 change 将其替换为外层全身 Action 统一 ownership，并保留 Dodge 的 RunEnd 返回策略。
- 当前 `character-state-interruption-authoring` 已经区分 State transition、Runnable stop、Action terminal lifecycle 和 Presentation release。本 change 不改变该合同，只修正 Corin 作者数据没有正确使用合同的问题。
- 当前 `character-animation-layer-runtime` 已禁止动画层解释 Action、State 或业务 Priority。本 change 不新增动画仲裁，并与该要求一致。
- 当前 `agent-character-controller-synthesis` 仍将 `two_hit_combo` 定义为固定二段 macro，且不能显式描述循环、移动取消、Dodge 后摇取消和 RushAttack；必须通过 MODIFIED delta 替换，且不得把 Corin 业务约束塞入通用 Validator。
