# Change: 分离 Locomotion 局部状态图与 Action lifecycle

## Why

当前实现已经把 Dodge 的 motion、animation 与 claim 接到 `ActionLifecycleFrame`，但默认 `CorinStateMachine.asset` 仍包含 `Action.Dodge` 节点、Action transition、动作动画绑定和动作运动模块。这样 Locomotion graph 仍在事实上承担全角色混合状态图职责，旧测试也继续要求状态机进入 `Action.Dodge`。

这与当前目标架构冲突：Locomotion 应是 Movement module 的局部状态图，Action lifecycle 应由 Action module 直接拥有，最终输出由 `CharacterFramePipeline` 和 Body Arbiter 仲裁。

## What Changes

- 将 Corin 默认状态图的正式语义从“统一角色状态机”改为 “Locomotion 局部 graph implementation”。
- 从 Corin Locomotion graph 中移除 `Action.*` 节点、`Locomotion.* -> Action.*` transition 和 `Action.* -> Locomotion.*` transition。
- 保留 `Action.Dodge` 作为稳定 Action id，但它不再是默认 Locomotion graph 节点。
- 将 Dodge 的持续、完成、claim 释放、动作动画请求和动作 facts 归属 Action lifecycle。
- 将 Shift 明确定义为 Dodge 触发输入，同时仍可提供 Run 输入事实；Directional Dodge 完成后的持续奔跑 MUST 依赖 Locomotion runtime 的 Run latch，而不是继续按住 Shift。
- 将 Directional Dodge 完成时的 Run latch 写入收口到 Character frame output → Locomotion output runtime，Action facts 只可作为观察事实，不得成为 Run 权威。
- 将无方向 Shift / Backstep、Directional 完成帧无移动输入、停止后 RunEnd/Idle 清 latch 的行为写成验收口径。
- 将无移动输入的 Backstep 和 Directional Dodge 退出条件收口到匹配动作动画播放完成，motion duration 只表达动作位移窗口。
- 将 `BodyClaimPolicySO` 继续作为 Action claim 规则来源，不把 full-body 重新表达成状态图 owner。
- 更新旧测试和规格口径：默认状态图测试只覆盖 `Locomotion.*`；Dodge 行为测试改为 Action lifecycle、BodyClaimPolicy、pipeline arbitration、Run latch 输出和 rollback restore。
- 将过时的 `locomotion-state-graph-config` 规格从“统一状态机”修正为“Movement 局部 Locomotion graph”。
- 退役旧 FullBody HFSM 主树 / 中心树资产规格，不再把 `/FullBody/Action/Dodge` 作为默认角色状态权威。

## Non-Goals

- 不实现 LightAttack、Jump、HitReact 或 UpperBody。
- 不把 Dodge 默认实现改成局部 action graph；Dodge 继续使用简单 lifecycle。
- 不引入 UnityHFSM、Animator Controller 或第三方状态机作为正式主线。
- 不删除所有 `FullBody*` 兼容类型名；只清理与默认状态图权威直接冲突的路径。
- 不新增 fallback 配置或 Resources/硬编码路径。
- 不把“按住 Shift 才能 Run”作为 Directional Dodge 后续奔跑的验收方式。
- 不运行 Unity batchmode。

## Dependencies

- `formalize-character-frame-module-architecture` 已经开始引入 `ActionLifecycleFrame`、`BodyClaimPolicySO` 和 `Action/FullBody` 目录迁移；本变更假定这些语义继续作为目标。
- `refactor-locomotion-fullbody-ownership` 的规格语义必须先完成或被本变更覆盖：FullBody 不是 Locomotion owner。
- `add-light-attack-combo-action` 在本变更完成前不应继续把 Attack 做成默认全局状态图叶子。

## Impact

影响规格：

- `locomotion-state-graph-config`
- `fullbody-action-framework`
- `unified-character-state-machine`
- `character-config-root`
- `fullbody-rollback-replay`
- `action-interrupt-arbiter`
- `character-frame-pipeline`
- `fullbody-hfsm-state-tree`
- `fullbody-hfsm-tree-data`

预期后续实现影响：

- `Assets/Configs/3C/StateMachine/CorinStateMachine.asset`
- `Assets/Configs/3C/StateMachine/Locomotion/Corin/`
- `Assets/Scripts/Character/Movement/Runtime/LocomotionFrameSubmitter.cs`
- `Assets/Scripts/Character/Action/Model/ActionLifecycleFrame.cs`
- `Assets/Scripts/Character/Action/Runtime/FullBodyActionFrameSubmitter.cs`
- `Assets/Scripts/Character/Action/Runtime/FullBodyActionRuntime.cs`
- `Assets/Scripts/Character/Pipeline/Model/CharacterFrameSubmission.cs`
- `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
- `Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`

## Verification

- `openspec validate refactor-locomotion-action-state-graphs --strict --no-interactive`
- `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp.csproj /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`
- `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`
- EditMode 测试覆盖 Locomotion graph 不包含 `Action.*`、Dodge lifecycle 从 Action module 推进、Dodge claim 压制 Locomotion 输出、Dodge 完成释放 claim、Shift 同时绑定 Run 与 Dodge、Directional 完成且仍有移动输入时写 Run latch、无移动输入或 Backstep 不写 Run latch、Backstep 无输入时等待匹配动作动画播放完成、rollback restore 保持 action lifecycle 与 Locomotion Run latch。
