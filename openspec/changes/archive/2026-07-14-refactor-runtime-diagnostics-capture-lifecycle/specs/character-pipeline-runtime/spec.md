# character-pipeline-runtime Specification

## MODIFIED Requirements

### Requirement: CharacterPipeline 必须作为显式 diagnostics target

每个 active `CharacterPipeline` MUST 能通过 `CharacterPipelineHost` 注册为独立 diagnostics target，并提供 session identity、source/program revision、严格 Source Map、按需 diagnostics store 和只读 target metadata。target 注册时 MUST 默认没有 active diagnostics interest；Host 或 Pipeline MUST NOT 向 editor 暴露 runtime Graph/Node/Timeline 对象作为正式调试 API。

#### Scenario: Host 激活 Pipeline

- **WHEN** `CharacterPipelineHost` 激活一个有效 Pipeline
- **THEN** diagnostics target registry MUST 注册该 runtime target
- **AND** target MUST 提供稳定 session identity 和 definition/source revision
- **AND** target MUST 在没有 Editor interest 时保持 diagnostics collection 关闭

#### Scenario: Host 禁用或销毁

- **WHEN** Host deactivate 或 dispose Pipeline
- **THEN** diagnostics target MUST 注销并终止其 active diagnostics interest
- **AND** attached Debug Session MUST 收到正式 detach lifecycle
- **AND** editor MUST 不继续持有 Pipeline runtime 对象或可写 diagnostics store

### Requirement: Pipeline domain debug 必须进入统一 Trace

Action、Blackboard、Motion、Timeline、Animation selection、producer sample、playback lifecycle、Animancer fade、Presentation 与 Camera runtime debug MUST投影到统一 diagnostics data plane。该 data plane MUST 将 current Live State 与显式 Capture history 分离；CharacterPipelineHostEditor MUST消费 shared provider 的 current summary 或 Capture view，不得遍历 runtime service 私有集合形成平行调试链。Trace MUST不包含已删除的 Driver、ExecutionLineage、causal component、Arbitrator 或 LayerPlan。

#### Scenario: 查看 Pipeline Inspector

- **WHEN** 用户选择附着 active Debug Session 的 Host
- **THEN** Inspector MUST 从 shared provider 显示当前 Action、Blackboard、Motion、selection、playback lifecycle 与 Camera snapshot
- **AND** Graph/Timeline/Presentation 窗口 MUST引用同一 source/runtime instance identity
- **AND** Inspector MUST 不扫描完整 historical event list

#### Scenario: 只有 Capture 请求连续细节

- **WHEN** 作者开始 Continuous Capture 并请求 Animation/Timeline channel
- **THEN** Pipeline MUST 将正式 sample/fade/time 记录到该 Capture
- **AND** 未开始 Capture 时这些连续细节 MUST 只更新需要它们的 Live State
- **AND** Pipeline MUST 不创建第二套 animation 或 Timeline 运行结果
