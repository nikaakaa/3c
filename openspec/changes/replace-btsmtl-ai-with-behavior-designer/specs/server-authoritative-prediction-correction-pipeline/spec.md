## MODIFIED Requirements

### Requirement: Authority输入缺失策略必须区分连续值与离散请求

Authority Tick Schedule MUST使用显式、进入PipelineHash的missing-input policy。连续move/facing MAY在有界hold window内沿用最后accepted sample；Attack、Dodge、Combo等离散request MUST永不重复；超过hold window后MUST使用显式neutral input。Room或worker MUST不按未声明默认值猜测输入。

上述缺失输入策略 MUST仅处理正式远端输入来源的网络缺失，不得用来掩盖本地 Bot 任务异常或非法输出。Bot 每个新权威输入 Tick MUST生产一次完整输入；等待或重试同 Tick 时使用已冻结帧。

#### Scenario: 某Actor当前Authority Tick无新输入

- **WHEN** 上一accepted sample仍在hold window内
- **THEN** continuous move MAY保持
- **AND** discrete requests MUST为空

#### Scenario: 本地Bot任务异常

- **WHEN** Authority 内的 Bot 决策失败
- **THEN** 当前新输入批次 MUST不部分交付且会话 MUST按正式失败策略终止
- **AND** MUST不借连续输入保持或 neutral 规则继续该 Bot

### Requirement: Authority Pipeline必须独立执行Canonical Gameplay

系统 MUST提供显式`ServerAuthoritativeAuthorityPipelineDefinition`，装配Accepted Input Ingress、Authority Tick Schedule、标准Float32 Evaluate/WorldSolve/Finalize Step和Authority Replication Egress。Authority Pipeline MUST对完整canonical roster按稳定ActorId执行一次World batch，MUST不消费client applied displacement或prediction state作为权威真值。

唯一 Authority Source MUST根据锁定输入所有权，将真实客户端 accepted 输入与权威本地生产的 Bot 输入合成同一权威 Tick 的完整 roster。输入准备 MUST在任何角色 Evaluate 前完成，不增设第二个权威输入 writer、AI 角色执行循环或 WorldSolver。Bot 不等待客户端输入 ACK/连接，角色和动作结果仍由唯一正式 Egress 发送。

#### Scenario: 两Actor Authority Tick

- **WHEN** Authority Schedule产生Actor A/B的Authoritative step
- **THEN** Program/Kernel MUST独立产生两ActorWorldRequest
- **AND** Unity Solver MUST在同一batch返回canonical Body results

#### Scenario: 两个玩家与三个Bot进入权威Tick

- **WHEN** 玩家网络输入与本地 Bot 输入均满足各自正式来源合同
- **THEN** Authority MUST为完整五 Actor roster 生成同一 Authoritative step
- **AND** MUST不因 Bot 没有客户端连接而持续 Pending

#### Scenario: 客户端接收Bot攻击结果

- **WHEN** Bot 请求经角色规则和 Action 执行后产生已提交结果
- **THEN** Replication MUST沿现有 Actor/事件身份分发结果
- **AND** MUST不同步行为树游标、黑板或插件 TaskStatus

### Requirement: Prediction与Authority失败必须保持Session事务边界

任一baseline decode、restore、Replay step、WorldSolve、Finalize、history capture或OutputDisposition失败时，当前outer transaction MUST不发布部分Character/World/Pipeline state或外部output。Authority worker、Fantasy connection或reliable queue失败时Session MUST fail-stop，MUST不切换Local Pipeline、旧Driver或client pose authority。

声明为 ExternalSource 的已冻结输入及原请求身份属于不可改写输入事实，不属于待提交 Character/World/输出候选。模拟事务恢复 MUST不重新执行插件或回退输入生产 frontier；发送/消费重试只能复用原帧。该分责 MUST进入正式 Source 合同，不得以跳过 checkpoint 的未声明特例实现。

#### Scenario: Replay中WorldResult不匹配

- **WHEN** Replay 102收到错误Solver identity
- **THEN** Backend MUST拒绝整个outer transaction
- **AND** Replay 101产生的表现与网络输出 MUST不被提交

#### Scenario: 权威Bot输入冻结后模拟失败

- **WHEN** Bot 输入已经冻结而当前 WorldSolve/Finalize/提交失败
- **THEN** 玩法结果与状态 MUST按原事务失败规则撤销
- **AND** 原输入身份 MUST保留；原失败策略允许的重试 MUST复用它而不重新运行 BT
