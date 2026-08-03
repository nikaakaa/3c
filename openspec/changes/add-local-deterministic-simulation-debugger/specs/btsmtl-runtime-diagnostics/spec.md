# btsmtl-runtime-diagnostics Specification

## ADDED Requirements

### Requirement: RuntimeDebugSession 不得成为本地调试执行控制器

RuntimeDebugSession MUST 继续只管理 target、interest、Trace Capture、history position 和只读 view。Pause、Step、Rate、Record、Replay、Scrub 和 ResumeFromTick MUST 通过独立 Local Simulation Debug Control Service 执行。RuntimeDebugSession MAY 共享 target identity、Trace segment key 和 capture history position 给调试窗口，但 MUST NOT 持有 Session runtime handle、debug control port、World state、Pipeline state、replay artifact mutable payload 或执行命令队列。

#### Scenario: Capture history scrub

- **WHEN** 作者在 RuntimeDebugSession 中 scrub 到旧 Trace segment
- **THEN** Graph 和 Timeline MUST 只显示记录状态
- **AND** runtime actor MUST 不因为 Trace scrub 发生 restore 或 replay

#### Scenario: 调试窗口请求 StepOne

- **WHEN** Local Simulation Debugger 对当前 target 发送 StepOne
- **THEN** 命令 MUST 进入 Local Simulation Debug Control Service
- **AND** RuntimeDebugSession MUST 只在后续 Trace 中观察该 Tick 结果
