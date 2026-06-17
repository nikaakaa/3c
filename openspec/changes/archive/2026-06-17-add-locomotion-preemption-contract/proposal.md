# Change: 建立 FullBody 抢占 Locomotion transient 的生命周期契约

## Why
当前 FullBody Action 通过 body claim 压制 Locomotion 输出，但压制不等于结束 Locomotion 生命周期。角色在 `Locomotion.TurnBack` 中被 `Action.Dodge` 打断时，TurnBack 位移会在 Dodge 期间被压住，但 Dodge 结束后旧 TurnBack timeline / motion source 仍可能继续采样，导致角色被 TurnBack 位移曲线拉回。

现有规格只分别描述了 FullBody claim 压制 Locomotion 输出、TurnBack 自然结束后回 MoveLoop/Idle，没有定义“FullBody action 抢占 Locomotion transient motion source”时应如何取消被抢占的 Locomotion transient。

## What Changes
- 新增一个纯数据、一次性消费的 Locomotion preemption contract：当 FullBody Action 以 full-body claim 抢占当前 Locomotion transient motion source 时，系统 MUST 产出可被 Locomotion graph 消费的抢占事实。
- `TurnBack` 被 FullBody Action 抢占时 MUST 正式结束当前 TurnBack motion source，而不是只在输出层 suppress。
- `TurnBack` 抢占后根据当前移动输入和 Run latch 进入 `MoveLoop` 或 `Idle`；`Run` 仍由 Locomotion gait / Run latch 决定，不作为硬编码状态目标。
- 抢占事实 MUST 由 Character frame pipeline / submitter / plan / runtime facts 的正式数据边界传递，不得通过 Dodge 特判脚本、fallback 配置或 `TurnBackMotionResolver` 内部读取 Action 状态实现。
- 实施时必须补自动测试，覆盖 TurnBack 中 Dodge 抢占、有输入恢复移动、无输入回 Idle、正常 TurnBack 自然结束不回退。

## Non-Goals
- 不新增 `Action.Dodge` 到 Locomotion graph。
- 不恢复旧 `FullBody/Action/Dodge` 或 `FullBody/Locomotion/*` 状态树路径。
- 不修改 Shift 输入绑定语义。
- 不修改 TurnBack 动画资源、motion profile 或 Dodge 动画资源。
- 不实现 Attack、HitReact、Knockback 等后续动作，只保证契约可扩展到这些动作。
- 不引入 fallback 配置；所有新增配置必须是正式配置或正式状态图规则。

## Impact
- Affected specs:
  - `character-frame-pipeline`
  - `fullbody-action-framework`
  - `locomotion-state-graph-config`
  - `locomotion-turnback-root-motion`
- Affected code:
  - `Assets/Scripts/Character/Pipeline/Model|Runtime`
  - `Assets/Scripts/Character/Action/Runtime`
  - `Assets/Scripts/Character/Movement/Runtime|Solver`
  - `Assets/Scripts/Character/StateMachine/Model|Solver`
  - `Assets/Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset`
  - `Assets/Tests/Editor`

## Validation
- OpenSpec: `openspec validate add-locomotion-preemption-contract --strict --no-interactive`
- Unity EditMode: 定向运行覆盖 `CharacterFramePipeline`、FullBody Action arbitration、Locomotion state graph、TurnBack root motion 的相关测试集合。
- 用户验收方式：在 Play Mode 中让角色进入 TurnBack，中途点按 Shift 触发 Dodge；有方向输入时 Dodge 结束后应进入移动/奔跑且不被 TurnBack 曲线拉回，无方向输入时 Dodge 完整播放后回 Idle 且不恢复 TurnBack 位移。
