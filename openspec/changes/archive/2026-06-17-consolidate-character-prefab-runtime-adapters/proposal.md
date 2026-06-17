# Change: 收敛角色 Prefab Runtime Adapter 装配

## Why
`extract-character-runtime-core-from-mono-adapters` 已经把正式 runtime state owner 抽到 pure C# core/module，但 Corin prefab 上仍挂着多个迁移期 MonoBehaviour facade，容易让装配看起来像还有多条 runtime 主线。

本变更规划把 prefab 上的正式 gameplay 装配收敛到少量 Unity-facing adapter，使 prefab 外观和实际 runtime ownership 对齐。

## What Changes
- 新增一个正式角色 runtime 组装 Adapter 或批准的等价入口，负责持有 Unity 序列化引用并装配 `CharacterRuntimeCore` dependencies。
- 将 `PlayerLocomotionController` 和 `FullBodyActionRuntime` 从正式 Corin prefab runtime 装配和代码面彻底删除，旧测试与旧 fixture 改用 `CharacterFrameRuntimeController` 或 pure C# module seam。
- 保留真正需要 Unity seam 的 adapter：输入、输入缓冲、motion executor、Animancer presenter、facing/camera basis、presentation interpolation 和 simulation tick adapter。
- 增加 prefab/scene 静态测试，锁定正式组件 allowlist、唯一 core、唯一 motion executor、唯一 animation presenter、无 debug tooling。
- 不改变 Run、Dodge、TurnBack、rollback、状态图和动画播放语义，只收敛装配 seam。

## Impact
- Affected specs: `character-runtime-ports`
- Affected code:
  - `Assets/Prefabs/Character/可琳.prefab`
  - `Assets/Prefabs/Character/可琳_Humanoid.prefab`
  - `Assets/Scripts/Character/Pipeline/Runtime/...`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Action/Runtime/FullBodyActionRuntime.cs`
  - `Assets/Tests/EditMode/...`
- Related changes:
  - Builds on `extract-character-runtime-core-from-mono-adapters`
  - Must not redefine `formalize-character-frame-module-architecture`
  - Must preserve `separate-rollback-debug-rig-from-character-runtime`

## Out of Scope
- 不新增 UpperBody、HitReact、Aim 或其它身体层。
- 不重写 `CharacterFramePipeline` phase 顺序。
- 不修改 Run/Dodge/TurnBack 的状态机语义。
- 不把输入、Animancer、motion executor 等必须连接 Unity 对象的 adapter 强行改成 pure C#。
- 不引入 fallback 配置或第二套 prefab runtime 路径。

## User Validation
实施完成后，用户可以在 Unity Editor 中打开两个 Corin prefab，确认正式 gameplay 根对象上只保留批准的 Unity-facing adapters，并通过现有场景运行一次移动、点按 Shift 冲刺、冲刺后 Run、无输入 Dodge/Backstep 播放完成、TurnBack 被 Dodge 打断后的位移恢复。
