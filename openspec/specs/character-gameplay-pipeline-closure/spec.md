# character-gameplay-pipeline-closure Specification

## Purpose

定义角色 Gameplay 管线闭环：输入、Graph Runtime、Control、Ability、Timeline、Effect、Character/World state、batch WorldSolver、Committer、Presentation 和 Runtime Debug 必须走同一条正式 Session/Pipeline 主线。不恢复整角色 Program、整包 Projection、旧 SO/config、对象解释器、旧播放器或 demo 临时桥接。

## Requirements

### Requirement: 角色 Gameplay 管线必须形成 ActionInstance 事实闭环

Input Adapter、Graph Runtime、Control、Ability、Timeline、CharacterSimulationState、WorldSimulationState 与 Committer MUST 通过同一 `Ingress -> Schedule -> Evaluate -> ResolveBatch -> Finalize -> Egress -> atomic Commit` 主线形成 ActionInstance 闭环。系统 MUST 不保留第二套 deterministic node 或 demo 专用业务路径。

#### Scenario: 本地 Attack1 进入 Attack2

- **WHEN** 输入进入正式 Session 并满足 Ability/Action 准入
- **THEN** Control、Ability、Graph 和 Timeline MUST 推进同一 ActionInstance 事实链
- **AND** Committer MUST 只提交经过 WorldResolve 和 Finalize 的结果

### Requirement: Authoring 装配必须从 CharacterPipelineDefinition 汇入正式领域

CharacterPipelineDefinition MUST 继续是角色 authoring 配置装配根，但 Runtime Host MUST 分别加载匹配 revision 的 Control、Ability、Timeline、Presentation、World 和 Input binding。Host MUST 不直接从 RootTree、Timeline、Action 或 Effect asset 创建 runtime clone，也 MUST 不加载整角色 Program/Projection。

#### Scenario: 装配 Corin

- **WHEN** Session 准备 Corin Actor
- **THEN** MUST 校验并绑定各领域正式 owner
- **AND** MUST 不通过 ProgramHash、LayoutHash 或整包 Projection 作为唯一启动条件

### Requirement: Graph 和 Timeline 必须只输出正式 gameplay facts

Graph、StateMachine 和 Timeline Runtime MUST 只更新 CharacterSimulationState pending evaluation，并输出 typed gameplay facts、WorldRequest 和 EventId presentation commands。它们 MUST 不直接写 Transform、调用 Solver、发送 packet、播放动画或裁决命中。

#### Scenario: Timeline 输出 Dodge Motion

- **WHEN** Timeline 在当前 Tick 产生 Motion contribution
- **THEN** contribution MUST 进入统一 Motion/World request 边界
- **AND** Pipeline Runtime MUST 在统一 batch 中取得唯一 body result

### Requirement: Motion 闭环必须依赖正式仲裁而不是直接移动

Control、Ability、Timeline 和 Motion owner MAY 产生 typed contribution，但只有统一 Motion accumulator、Body Motion Integrator 和 WorldSolver MAY 生成最终 WorldRequest/Body result。Timeline、Graph、Presentation 和 Unity Transform MUST 不拥有第二份运动真值。

### Requirement: Presentation 闭环必须只消费已提交事实

Presentation MUST 消费 committed Body/Intent、Action playback、Pose Graph、Source、Constraint 和 Final Publication 的正式结果。它 MUST 不修改 Gameplay state、World state、Ability lifecycle 或 Timeline cursor。

#### Scenario: Attack 动画播放

- **WHEN** Ability 已提交有限 Action playback
- **THEN** Presentation MUST 通过正式 AnimationSlot、原生 Pose Graph 和 Source lifecycle 播放
- **AND** Timeline UI MUST 只显示正式 playback/completion 事实

### Requirement: GameplayFacts 必须成为同步和 Debug 的唯一事实出口

Action Window、Motion、Effect、Attribute、Target、State 与完成事实 MUST 通过正式 GameplayFacts/Trace 输出。Editor MUST 不绑定 runtime clone、mutable state 或第二套诊断解释器。

### Requirement: ServerAuthoritative Gameplay 必须复用正式 domain Step

Prediction Client 与 Authority Worker MUST 复用同一 Control、Ability、Timeline、Motion、Effect、World ResolveBatch 和 Finalize 业务合同。Owner/server/remote 差异 MUST 只存在于 Session Source、Ingress/Schedule/Egress Pass 和 Presentation registration，不得进入 Graph、Timeline、Effect 或 Motion 的业务语义。

### Requirement: Local 与 Hybrid 必须是显式且互不回退的完整组合

Local、ServerAuthoritative 与 DeterministicRollback MUST 通过各自显式 Composition、Source、Backend、Pipeline 和 WorldSolver 装配。一个组合缺少能力时 MUST 明确失败，不得切换到另一个 Model、旧 Program、默认 Solver 或本地临时路径。

### Requirement: 网络复制必须只消费正式 Finalized Output

Network Model MUST 只消费正式 Finalized GameplayFacts、Body、Action playback 和 EventId disposition。Fantasy Handler、Room 和 Model Source MUST 不直接调用 Animancer、写 visual Transform 或决定 Animation transition。

### Requirement: Remote 表现必须属于正式 Committer 消费链

Remote Body sample、有限 Action producer command 和 reliable EventId facts MUST 在 Prediction Pipeline 最终 Commit 边界进入 remote presentation output，并复用既有 Body interpolation、Presentation Fact、Action lifecycle、AnimationSlot 和原生 Pose Graph。不得创建远端专用播放器或 Projection。

### Requirement: Hybrid Diagnostics 必须沿统一 Source Map 与 Session Trace 关联

Diagnostics MUST 按 ActorId、ActionInstanceId、SimulationTick、Graph source、World request/result、domain owner 和 EventId 展示输入、状态决策、Timeline window、Motion、Effect 与 committed Presentation。Diagnostics MUST 只读正式提交事实，不参与运行。
