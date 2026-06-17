## Context
`formalize-character-frame-arbitration-contract` 已经把目标架构定义为 Character frame owner 汇集 sibling submitters，再由 `BodyArbiter` 产出 `CharacterFramePlan`。当前代码也已经出现 `IBodyArbiter`、`DefaultBodyArbiter`、`BodyOccupancyDecision` 和 `CharacterFramePlan` 的雏形。

但迁移期旧身份仍留在正式路径里：
- `FullBodySubmissionBuilder` 同时实现 `ICharacterFrameRequestSubmitter` 和 `ICharacterFrameOutputSubmitter`。
- `PlayerFullBodyActionController` 仍创建并持有 `CharacterFramePipelineHost`。
- `ICharacterFrameRuntimePort` 仍继承 `IFullBodySubmissionRuntimePort` 和 `IFullBodyOutputRuntimePort`。
- `CharacterFrameSubmissionSource` 仍只有 `FullBody` 作为有效来源。
- `CharacterFrameOutputComposer` 仍接收单个 `CharacterFrameSubmission`，再把它转换为 plan/output。
- `fullbody-action-framework` 当前 spec 仍有“FullBody 主调度入口”和“Locomotion 作为 FullBody 子职责”的历史目标描述。

这些形态是可运行的迁移 Implementation，不是长期 Interface。若不退役，新的身体域会继续沿 FullBody 集成路径扩张。

## Goals
- 把 FullBody 集成路径从正式目标降级为迁移 Adapter。
- 明确哪些旧正式入口需要删除，哪些只能作为兼容 Adapter 保留。
- 让角色级 `CharacterFramePlan` 成为 output composer/applier 的正式合同。
- 让 `ICharacterFrameRuntimePort` 回到 Character 层 Interface，不再暴露 FullBody 继承面。
- 让 `PlayerFullBodyActionController` 降级为 Unity 装配/兼容入口。
- 保持 Corin 当前 playable 主线行为不变。

## Non-Goals
- 不在本 change 中实现新的 UpperBody runtime。
- 不改变 Dodge、TurnBack、Locomotion 的行为数值。
- 不重建状态机 runner。
- 不绕过 `CharacterFramePipeline` 或 output applier。
- 不删除第三方 Ref/Art 示例。

## Retirement Matrix

| 当前身份 | 问题 | 目标状态 | 顺序 |
| --- | --- | --- | --- |
| `FullBodySubmissionBuilder` 作为 request/output submitter | 一个 Module 同时集成 Locomotion、FullBody Action、状态机推进和 frame output | 降级为迁移期 integrated adapter；最终由 Locomotion submitter、FullBody Action submitter 和 plan composer 替代 | 先加 characterization tests，再拆 submitter，最后移除正式注入 |
| `ICharacterFrameRuntimePort : IFullBodySubmissionRuntimePort, IFullBodyOutputRuntimePort` | Character 层 Interface 泄露 FullBody 操作面板 | 替换为 Character runtime port + 专用 adapter seam；FullBody ports 只在 FullBody adapter 内部存在 | 先新增窄 Interface/fake tests，再迁移 pipeline/applier，最后删除继承 |
| `PlayerFullBodyActionController.FramePipelineHost` | FullBody MonoBehaviour 仍像角色帧 owner | 降级为 Unity 装配和旧 Tick 兼容 Adapter；正式 owner 迁到 Character runtime host | 先引入 Character host，再让旧 Tick 转发，最后禁止直接创建 host |
| `CharacterFrameSubmissionSource.FullBody` | 单源来源像最终 output authority | 如保留只能作为迁移诊断；正式 output authority 来自 `CharacterFramePlan` | 先让 result/test 观测 plan，再移除对 FullBody source 的正式判断 |
| `CharacterFrameOutputComposer.Compose(CharacterFrameSubmission)` | 仍以单 submission 为主要入口 | 降级为 legacy adapter；正式 composer 消费 sibling submissions 或 `CharacterFramePlan` | 先保证 plan path 测试通过，再迁移调用点 |
| `fullbody-action-framework` 中 FullBody 拥有 Locomotion 的要求 | 规格把历史 Implementation 写成目标架构 | 删除或改写为迁移期说明；目标架构改为 sibling submitters | 先更新 spec，再用静态测试阻止回归 |

## Decisions
### Decision: 先降级身份，再物理删除
如果先删 `FullBodySubmissionBuilder` 或 FullBody runtime port 继承，会直接切断 Corin 当前主线。实施必须先建立替代 Interface 和测试，再迁移调用点，最后删除旧正式身份。

### Decision: CharacterFramePlan 是正式 output 合同
`CharacterFrameSubmission` 可以短期作为 legacy input，但 output composer/applier 的正式目标必须是 `CharacterFramePlan`。运动、动画、输入消费、runtime facts 和 diagnostics 应从 plan 读取最终选择。

### Decision: FullBody ports 只留在 FullBody Adapter 内部
FullBody 领域仍可以有自己的 narrow ports，但 Character 层 Interface 不应通过继承 FullBody ports 暴露所有 FullBody 操作。这样可以提升 Interface Depth，也能让 UpperBody/HitReact 等未来 Module 不需要学习 FullBody 内部面板。

### Decision: 旧 Tick 入口保留兼容，正式身份降级
`PlayerFullBodyActionController.Tick` 可继续作为兼容入口转发到 Character runtime host，但它不得被新身体域视为上级 owner，也不得直接构造正式 pipeline。

## Sequencing
1. 完成并归档 `formalize-character-frame-arbitration-contract`。
2. 添加静态测试锁住旧身份：FullBody 集成路径只能标记为 legacy/integrated adapter。
3. 添加角色级 host 与 plan path characterization tests。
4. 引入窄 Character runtime Interface，并让 pipeline/output applier 先通过新 Interface 测试 fake。
5. 拆分 Locomotion submitter 与 FullBody Action submitter。
6. 将 `FullBodySubmissionBuilder` 从正式注入降级为 legacy adapter。
7. 迁出 `PlayerFullBodyActionController` 的 pipeline host ownership。
8. 移除或降级 `CharacterFrameSubmissionSource.FullBody` 的正式判断。
9. 删除 pass-through 或 legacy overload，保留必要诊断兼容层。

## Risks / Trade-offs
- 风险：退役过早会破坏 Corin playable 主线。Mitigation：每一步都要求旧主线 characterization tests 通过。
- 风险：为了保留兼容而继续扩大 FullBody Adapter。Mitigation：静态测试要求新增身体域不得依赖 FullBody 私有状态。
- 风险：plan path 和 legacy submission path 同时存在太久。Mitigation：tasks 中要求每个 legacy path 都有删除条件和追踪测试。

## Open Questions
- 无阻塞问题。当前假设是：本 change 只定义退役和降级方案，实际代码删除在 apply 阶段按任务顺序执行。
