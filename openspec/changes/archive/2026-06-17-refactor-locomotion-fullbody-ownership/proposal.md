# Change: 先清理 Locomotion 与 FullBody 归属旧规格

## Why

当前规格里仍有两类过时口径：一类把角色状态写成 FullBody 分层/HFSM 主树，另一类把 Locomotion 写成 FullBody 子职责。这和现有 `CharacterFramePipeline` 下 sibling submitter 的方向冲突，也会诱导后续 Attack、Jump、HitReact 或 UpperBody 继续绕过管线去扩展一个大 FullBody owner。

这次先更新规格口径，再进入实现。目标不是再做一棵统一层级状态机，而是把角色运行拆成更清楚的 module：

- Locomotion module：维护移动领域状态和移动候选输出。
- Action module：维护动作生命周期、打断、body/channel claim 和动作候选输出。
- Body/channel claim：表达动作对身体输出范围的占用，不是状态根、不是 Locomotion owner。
- Character frame pipeline：唯一合成帧输出的 module。

## What Changes

- **BREAKING**：退役“FullBody 分层/HFSM 主树”和“统一层级角色状态机”作为目标架构。
- **BREAKING**：正式状态/诊断 ID 从 `FullBody/Locomotion/...`、`FullBody/Action/...` 迁移到领域 ID，例如 `Locomotion.Idle`、`Locomotion.MoveLoop`、`Action.Dodge`。
- 将 FullBody 限定为 body/channel claim 或动画层语义，而不是状态树根、角色帧 owner 或 Locomotion 上级 owner。
- 将 Locomotion 定义为独立移动领域 module，通过 submitter 向 `CharacterFramePipeline` 提交移动事实和候选输出。
- 将 Action 定义为独立动作领域 module。Action 可以内部使用实例、时间线或局部状态图，但不要求每个 action 都成为同一棵角色树的叶子。
- 将 `FullBodyOwnerKind.Locomotion` 和 `FullBodyOwner.Locomotion` 归入遗留兼容输入，后续实现应删除正式依赖。
- 将 `FullBodyStateView` 降级为兼容诊断 view；正式可恢复状态来自 Locomotion snapshot、Action facts 和 `CharacterFramePlan`。
- 先更新过时 specs，再更新代码，避免 implementation 阶段继续按旧规格扩展分裂路径。

## Non-Goals

- 不实现 UpperBody、Facial、IK、Additive 或多身体分区混合。
- 不引入 UnityHFSM 或第三方状态机作为正式角色状态 engine。
- 不新增 fallback 配置；迁移后的配置必须是正式配置。
- 不在 proposal 阶段修改生产代码。

## Impact

影响规格：

- `fullbody-hfsm-state-tree`：从目标规格改为遗留口径退役规格。
- `unified-character-state-machine`：从统一层级状态机改为领域状态 authority + 管线合成。
- `dodge-action`：Dodge 的 full-body 语义改为 Action body/channel claim，而不是 FullBody 树叶子。
- `locomotion-state-graph-config`：Locomotion 配置归 Locomotion module，不归 FullBody 子树。
- `fullbody-action-framework`：FullBody Action 保留动作输出占用与解析职责，删除 FullBody 主树语义。
- `character-frame-pipeline`：明确多个领域 submitter 只能通过 pipeline 合成，不允许绕过。
- `action-runtime-state-tracker`：Action state tracker 记录 action facts，不依赖树路径。

预期实现会触及：

- `Assets/Scripts/Character/Pipeline/Runtime`
- `Assets/Scripts/Character/Locomotion/Runtime`
- `Assets/Scripts/Character/Action/FullBody/Runtime`
- `Assets/Tests/Editor`

活动变更风险：

- 当前 active changes 仍可能沿用“统一 runner”旧口径，实施前必须对齐。
- 后续 Attack、Skill 或 HitReact 若引用旧 `FullBody/Action/*` 路径，必须改为 `Action.*` 或批准的等价领域 ID。

## Verification

- `openspec validate refactor-locomotion-fullbody-ownership --strict --no-interactive`
- `dotnet build 3cDemo/Client/3C_Client/Assets/Scripts/Character/Character.Runtime.csproj`
- `dotnet build 3cDemo/Client/3C_Client/Assets/Tests/Editor/Character.EditorTests.csproj`
- Unity Test Runner：覆盖 `UnifiedCharacterStateMachineTests`、`CharacterFramePipelineTests`、`FullBodyActionFrameworkTests`、`LocomotionStateGraphConfigTests`
- 静态验证：生产代码、测试断言和配置资产不再把 `FullBodyOwnerKind.Locomotion`、`FullBodyOwner.Locomotion`、`FullBody/Locomotion`、`FullBody/Action` 当作正式路径或正式 owner
