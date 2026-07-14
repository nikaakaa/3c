# network-model-comparison-demo Specification

## ADDED Requirements

### Requirement: 三个 Demo 必须复用同一 gameplay Program

系统 MUST提供 Unity authoritative、DotRecast authoritative 和 DeterministicRollback 三个独立本地双客户端 Demo。三个 Demo MUST使用同一 Corin ProgramHash、input schema、地图 identity、Action/Effect catalog 和 gameplay authoring source；它们 MUST只在 Network Model、Host、World Solver 和对应协议策略上不同。

#### Scenario: 比较三个 Demo 配置

- **WHEN** 作者查看三个 launch definition
- **THEN** 三者 MUST引用相同 Corin ProgramHash
- **AND** Unity/DotRecast MUST都使用 ServerAuthoritativeHybrid model id
- **AND** 第三个 MUST使用 DeterministicRollback model id

### Requirement: Unity 权威 Demo 必须由服务端独立模拟

Unity authoritative Demo MUST运行独立 Unity server process，服务端从 accepted simulation input 和 action state执行同一 Program，并通过 Unity CharacterController World Solver 生成 canonical pose。客户端 resolved displacement MUST只用于 prediction comparison，MUST不成为服务端 canonical input。

#### Scenario: Owner 发送移动输入

- **WHEN** Owner client 预测本 Tick 位移并发送 simulation input
- **THEN** Unity server MUST从自己的旧 body state独立执行 Program 和 solver
- **AND** MUST向 Owner/Remote 发布权威结果

### Requirement: DotRecast 权威 Demo 必须诚实限制为导航表面约束

DotRecast authoritative Demo MUST运行纯 .NET server host，读取同一 Program，并使用 DotRecast navigation-surface solver 约束静态 NavMesh 上的移动和高度。该 Demo MUST复用 ServerAuthoritativeHybrid 协议与客户端模型，但 MUST不声明通用 KCC、动态物理或 Deterministic capability。

#### Scenario: Actor 移动到 NavMesh 边界

- **WHEN** canonical motion request 超出可行走表面
- **THEN** DotRecast solver MUST返回表面约束后的 portable body result
- **AND** ServerAuthoritative Driver MUST把它作为 canonical pose
- **AND** MUST不调用 Unity server 或 Deterministic KCC fallback

### Requirement: DeterministicRollback Demo 必须按输入与世界状态重演

DeterministicRollback Demo MUST让服务端和两个客户端使用相同 Program、canonical input bundle、deterministic KCC 和 world state。客户端 MUST能因迟到输入 restore/replay，并使用 state hash 与权威 snapshot recovery 收口。

#### Scenario: 人工网络延迟导致迟到输入

- **WHEN** canonical input 在 history 范围内迟到
- **THEN** Demo metrics MUST显示 restore tick、replayed tick count 和最终 hash
- **AND** 角色 animation/Cue MUST不因 replay 重复提交

### Requirement: 三个 Demo 必须提供统一只读比较指标

三个 Demo MUST通过同一 model-neutral metrics contract 展示 RTT、带宽、queue health、prediction error、correction、rollback count、replayed ticks、history occupancy、solver id/capability 和 state hash。指标 MUST只读正式 model/solver facts，MUST不参与输入接受、运动求解、correction、replay 或 presentation commit。

#### Scenario: 切换独立 Demo 启动入口

- **WHEN** 作者分别运行三个 Demo
- **THEN** HUD/Inspector MUST使用相同字段和单位展示指标
- **AND** 不适用字段 MUST明确显示为不适用
- **AND** MUST不通过运行中切换 model 实现比较

