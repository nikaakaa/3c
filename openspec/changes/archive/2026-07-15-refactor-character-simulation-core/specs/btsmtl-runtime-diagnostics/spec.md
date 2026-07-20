# btsmtl-runtime-diagnostics Specification

## MODIFIED Requirements

### Requirement: Runtime diagnostics 必须与执行对象布局解耦

Runtime diagnostics MUST只依赖稳定 source identity、Program revision、operation handle、Actor/activation identity、SimulationTick、Debug Source Map 和 structured Trace。Editor MUST不持有或轮询 Character/World state mutable view、pending evaluation、runtime clone 或 WorldSolver object。

#### Scenario: Graph Editor 跟随 Runtime

- **WHEN** Editor 显示 compiled operation 的当前状态
- **THEN** MUST通过 Source Map 和 Trace 反查 authoring element

### Requirement: Debug Source Map 必须严格映射执行元素到 authoring source

Compiler MUST为 operation、state slot、scope、Timeline segment、TreeClip、Action/Effect definition 和 presentation producer 生成严格 Source Map。断裂、歧义或 duplicate identity MUST使 Program build 失败。

#### Scenario: 定位 Timeline Window

- **WHEN** Trace 包含 ActionWindow EventId
- **THEN** Source Map MUST唯一定位原 Timeline/TreeClip/declaration

### Requirement: Runtime producer 必须在正式生命周期边界发布 Trace

SimulationKernel、SessionRuntime、Driver、WorldSolver adapter 和 Committer MUST分别在自己的正式边界发布 Trace。成功、失败、restore、replay 与 OutputPlan disposition MUST都进入只读 diagnostics sink；Driver MUST不能通过 Publish、Replace、Retire 或 Suppress 隐藏 Trace。Trace MUST不反向驱动 Character/World state、Driver policy 或 Presentation result。

#### Scenario: 一次 Motion 执行

- **WHEN** Kernel 生成 request 且 Solver 返回 result
- **THEN** Trace MUST区分 operation、request、solver result、published body sample 和 OutputPlan disposition
