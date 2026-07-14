# character-gameplay-pipeline-closure Specification

## MODIFIED Requirements

### Requirement: 角色 Gameplay 管线必须形成 ActionInstance 事实闭环

系统 MUST让输入适配、compiled Graph/StateMachine、portable ActionRuntime、compiled Timeline、Motion resolve、World Solver、SimulationState、Presentation Committer 和 model facts通过同一 `CharacterSimulationProgram -> SimulationKernel` 主线形成闭环。系统 MUST不保留旧 CharacterPipeline object interpreter、第二套 deterministic node、第二套 Timeline gameplay runtime、demo 专用角色规则或网络临时桥接。

#### Scenario: 本地预测攻击闭环

- **WHEN** 本地输入适配器产生 Attack request
- **THEN** compiled Graph MUST创建 ActionInstance并推进 Attack Timeline
- **AND** Kernel MUST产生 window、motion、cue command和facts
- **AND** World Solver、Driver 与 Presentation MUST消费正式输出

### Requirement: Authoring 装配必须从 CharacterPipelineDefinition 汇入 runtime

系统 MUST继续使用 CharacterPipelineDefinition 作为唯一角色 authoring 聚合入口，并由正式 Compiler 生成 CharacterSimulationProgram 与 Presentation projection。Runtime Host MUST只加载与当前 source revision一致的 compiled artifact；MUST不直接从 RootTree、ActionProfile、GameplayEffect profile 或 Timeline asset创建 gameplay runtime clone。

#### Scenario: 创建可玩的角色

- **WHEN** CharacterPipelineHost 启动 Corin
- **THEN** Host MUST绑定 compiled Program、input adapter、Driver actor和Presentation resources
- **AND** MUST不在运行时解释 authoring asset

### Requirement: Graph 和 Timeline 必须只输出 gameplay facts

Graph、StateMachine 与 Timeline compiled operation MUST只改变 SimulationState并输出 gameplay facts、motion request和带EventId的presentation command。它们 MUST不直接写 Transform、调用 solver、裁决命中、发送 packet或播放客户端资源。Animation selection/visual sampling MUST通过 Presentation projection/Committer处理，MUST不进入 portable Program gameplay state。

#### Scenario: Timeline 输出动作位移

- **WHEN** motion segment 在当前 SimulationTick active
- **THEN** Kernel MUST产生portable MotionContribution/request
- **AND** 当前 World Solver MUST产生唯一body result

### Requirement: Motion 闭环必须依赖正式仲裁而不是直接移动

所有 gameplay motion MUST按 contribution resolve、modifier、portable request、World Solver、SimulationState body result 的唯一顺序执行。ServerAuthoritative reconciliation 与 Rollback restore/replay MUST位于 model Driver，MUST不作为额外 MotionStage correction contribution。所有 request/result MUST可按 Program operation、ActorId、SimulationTick和solver id诊断。

#### Scenario: 权威结果与预测不同

- **WHEN** Model Driver 发现 authoritative state 差异
- **THEN** Driver MUST通过正式 history/restore/reconciliation处理
- **AND** MUST不在 Motion resolver前直接设置 Transform或注入误差贡献

### Requirement: SyncFacts 必须成为 demo 同步和 debug 的唯一事实出口

SimulationKernel MUST输出稳定 gameplay facts和state observations，model-owned Driver/adapter MUST按当前 ModelId解析并构造 packet/history。CharacterSimulationProgram、Kernel、World Solver和Presentation MUST不引用 model packet、endpoint或policy。旧Character NetworkSend/Receive双写入口 MUST删除。

#### Scenario: ActionWindow 进入模型

- **WHEN** compiled Timeline projection产生ActionWindow fact
- **THEN** fact MUST先进入正式 simulation output
- **AND** 当前 model adapter MUST按自己的协议处理

## ADDED Requirements

### Requirement: 正式 Demo 必须只暴露完整 Model 与 Endpoint 组合

完成本 change 后，正式可运行 Network Model MUST为 ServerAuthoritativeHybrid 与 DeterministicRollback。ServerAuthoritativeHybrid MUST提供 Fantasy endpoint，并可连接 Unity authoritative 或 DotRecast authoritative server deployment；DeterministicRollback MUST提供自己的完整 endpoint/protocol。Disconnected 与 LocalLoopback MAY保留为明确状态/调试 endpoint，但 MUST不被称为模型，也 MUST不作为连接失败 fallback。

#### Scenario: 查看完整模型与 Endpoint

- **WHEN** 作者查看 SessionHost 和 model definitions
- **THEN** MUST能选择两个完整 model definition
- **AND** ServerAuthoritative MUST能配置正式 Fantasy endpoint
- **AND** 未安装或不完整选项 MUST不出现

### Requirement: 网络模型对比 Demo 必须限制为双角色业务纵切

本 change 的网络纵切 MUST覆盖两个本地客户端、两个 Corin actor、输入、动作事务、motion、window、GameplayEffect、Attribute、cue、Owner prediction、Remote presentation和三种后端组合比较。它 MUST不宣称已经实现完整2v2vE、命中伤害、PvE、Objective、lag compensation、动态物理或断线续局。

#### Scenario: 查看当前网络能力

- **WHEN** 作者查看三个 Demo diagnostics
- **THEN** MUST明确显示当前 Model、Host、World Solver和能力限制
- **AND** MUST不把静态双人移动纵切描述为完整2v2vE产品

## REMOVED Requirements

### Requirement: 第一阶段网络后端只覆盖 None 和 LocalLoopback

**Reason**：完成本 change 后已有两个完整 Network Model 和正式 Fantasy endpoint，None/LocalLoopback 不再能表达正式后端范围。

**Migration**：SessionHost 只展示完整 ServerAuthoritativeHybrid 与 DeterministicRollback；disconnected/LocalLoopback 仅是明确状态或模型专属调试 endpoint，不是 model 也不是 fallback。

#### Scenario: 迁移模型选择

- **WHEN** 旧 Demo 使用 None 或 LocalLoopback 作为 backend 口径
- **THEN** 必须迁移为完整 ModelDefinition 与其 EndpointDefinition

### Requirement: 2v2vE demo 第一阶段只实现最小业务压力事实

**Reason**：本 change 是三种网络/求解组合的对比 Demo，不应继续用 2v2vE 产品范围命名。

**Migration**：保留两个 Corin 的输入、运动、动作、Timeline、Effect 和 Presentation 压力纵切，删除对完整 2v2vE 的暗示。

#### Scenario: 迁移 Demo 口径

- **WHEN** UI 或文档展示该 Demo
- **THEN** 必须称为双角色网络模型对比纵切
- **AND** 不得声称完整 2v2vE
