# Design: 纯 C# Runtime Core 与 Mono Adapter 分层

## Context
当前代码已经有角色级帧入口，但正式状态 owner 仍不统一：

- `CharacterFrameRuntimeController` 是 MonoBehaviour，当前负责解析引用、创建 `CharacterFrameRuntimeHost`、关闭子模块 auto update、驱动 tick。
- `PlayerLocomotionController` 是 MonoBehaviour，同时实现 Locomotion frame/output ports，并持有 `LocomotionRuntimeStateStore`、`CharacterRuntimeBlackboard` 和嵌套 runtime host。
- `FullBodyActionRuntime` 是 MonoBehaviour，同时持有 `CharacterStateMachineRuntime`、`ActionLifecycleRuntime`、Action output host。
- rollback / synctest tooling 正在通过 `separate-rollback-debug-rig-from-character-runtime` 拆到独立 Debug Rig；本变更不能再创建第二条 replay/runtime 管线。

这说明现在的问题不是“还缺一个 Mono 管状态机”，而是正式 runtime core 还没有从 Unity adapter 中独立出来。

## Goals
- 让正式角色运行时状态、lifecycle、runner、snapshot/restore 和 frame host 归属于纯 C# core。
- 让 MonoBehaviour 只承担 Unity 引用拼装、生命周期入口、序列化配置注入、表现/运动 adapter 绑定。
- 保持 `CharacterFramePipeline` 是唯一角色帧主线。
- 让 Locomotion 与 Action 继续作为 sibling modules 进入同一 frame plan，不合并成一个巨型状态机。
- 让 core 可用 EditMode 纯 C# fixture 测试，降低 prefab/scene 依赖。
- 为 rollback replay 提供明确目标：复用同一个 core，而不是挂一组 debug Mono 到角色上。

## Non-Goals
- 不改变 Shift 冲刺、Run latch、RunEnd、Dodge、TurnBack 的设计语义。
- 不在本变更里迁移 Rollback Debug Rig 组件；该范围属于 `separate-rollback-debug-rig-from-character-runtime`。
- 不引入新的角色控制器、第三方状态机 engine 或并行 action/locomotion 主线。
- 不一次性删除所有兼容 API；兼容 facade 可以短期存在，但不得继续作为正式状态 owner。
- 不把 `CharacterFramePipeline` 变成包含所有玩法细节的巨型类。

## Decisions

### Decision: 引入 `CharacterRuntimeCore` 或批准的等价纯 C# owner
核心对象负责组合正式 runtime host、runtime port、Locomotion runtime module、Action runtime module、snapshot/restore 和 diagnostics。它的公开面应围绕 `Tick`、phase run、capture/restore、只读观测和显式依赖注入展开。

Reasoning: 角色主线需要一个非 Unity 对象作为真实 owner，否则状态仍会被 Mono 生命周期、prefab 绑定和测试 fixture 牵着走。

### Decision: MonoBehaviour 是 adapter，不是正式 runtime state owner
`CharacterFrameRuntimeController`、`PlayerLocomotionController`、`FullBodyActionRuntime` 可以保留为 Unity adapter 或迁移期 facade，但正式状态字段必须迁到 pure runtime module。Mono 可以保存 Unity 引用，例如 input adapter、motion executor、animation presenter、config root、tick driver，但不能 new 第二套 runner 或 state store 作为正式状态。

Reasoning: Unity 层适合拼装对象和连接表现端口，不适合承载可回滚、可测试、可复用的 gameplay state。

### Decision: Core-backed runtime port 取代 controller-backed adapter
`CharacterFrameRuntimePortAdapter` 当前以 `CharacterFrameRuntimeController` 为 host。实施后正式 port 应由 core 或 core-owned adapter 提供，Unity controller 只把 Unity-facing adapter 注入 core。

Reasoning: 如果 port 继续反查 controller，再经 controller 找 Locomotion/Action Mono，依赖方向仍然是 Unity 对象驱动纯逻辑。

### Decision: Locomotion 与 Action 仍然是 sibling modules
Locomotion module 负责移动状态、移动 facts、移动候选输出和移动 snapshot。Action module 负责 request、interrupt、lifecycle、body claim、动作候选输出和动作 snapshot。Core 组合它们，Frame Pipeline 仲裁它们，不新增父子状态树关系。

Reasoning: 这符合现有 `CharacterFramePlan`、body claim 和 pipeline 权威，不会回到 FullBody 包 Locomotion 的旧结构。

### Decision: 显式引用失败要诊断失败，不做正式 fallback
正式 adapter 可在编辑期提供装配辅助，但 Play Mode/runtime 初始化必须以显式配置和显式引用为准。缺失必要引用时报告诊断失败，不能扫描第一个匹配 MonoBehaviour 继续运行。

Reasoning: fallback 会隐藏 prefab 残留和重复组件，正是现在职责漂移的来源之一。

## Migration Plan
1. 先补 characterization/static tests，锁定现有 phase 顺序、唯一 pipeline、唯一 motion/animation 出口、无 fallback 正式路径。
2. 新建 pure C# core，并先让它包装现有 `CharacterFrameRuntimeHost`，保持行为不变。
3. 将 `CharacterFrameRuntimeController` 改为 core 的 Unity composition adapter。
4. 将 `CharacterFrameRuntimePortAdapter` 改为 core-backed port，去掉 controller-backed 正式依赖。
5. 将 Locomotion state store、blackboard、frame/output runtime host 从 `PlayerLocomotionController` 迁入 pure Locomotion runtime module。
6. 将 Action state machine runtime、lifecycle runtime、output runtime host 从 `FullBodyActionRuntime` 迁入 pure Action runtime module。
7. 保留必要兼容 facade，但用静态测试禁止它们成为正式 owner。
8. 与 `separate-rollback-debug-rig-from-character-runtime` 对齐，让 replay/debug adapter 只显式引用目标 core/controller，不创建第二 runtime。

## Risks / Trade-offs
- 风险：一次迁移太大导致 Move/Run/Dodge/TurnBack 回归。缓解：按 core wrapper、port migration、Locomotion migration、Action migration 分阶段，并保留行为测试。
- 风险：兼容 facade 变成永久旧入口。缓解：任务里加入静态边界测试和删除条件。
- 风险：rollback debug tooling 和 core 迁移互相卡住。缓解：rollback rig 拆分和 runtime core 提取分别成 change，本变更只定义显式 target contract。
- 风险：为了测试方便创建并行 fake pipeline。缓解：fixture 只替换 adapter/port，不替换正式 pipeline owner。

## Open Questions
- 最终类名默认使用 `CharacterRuntimeCore`；如果实施时已有更贴近代码语义的命名，可用等价名称，但 spec 中的 owner 职责必须保留。
- `PlayerLocomotionController` 和 `FullBodyActionRuntime` 在第一阶段是否保留 public facade，由实施时按 prefab/scene 引用量决定；无论保留与否，它们都不能继续持有正式 runtime state。
