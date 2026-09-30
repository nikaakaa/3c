## MODIFIED Requirements

### Requirement: 表现运行时必须执行唯一原生Pose链

SimulationCommitter与唯一 `CharacterPresentationDomainRuntime` MUST 共同构成 Unity animation application seam。表现运行时 MUST 唯一持有该 Actor 的 Pose 域实例，提供同一表现帧的 frameInput、动作命令及表现时钟，并消费整帧最终结果。DomainSession MUST 将完整帧执行交给 FrameCoordinator；FrameCoordinator MUST 单点执行准备、求值、校验、提交或丢弃的阶段门：preparation 为 Prepared 才能继续 PrepareEvaluation，evaluation 为 Evaluated 才能继续 ValidatePending 与 Commit。任何不满足阶段门的结果 MUST 在协调点按 FailureCode 终止后续阶段，Barrier 前关闭并丢弃 Pending 帧，Barrier 开始后的失败 MUST 按既有 Fault 合同处理，不得伪装物理回滚。外层表现运行时 MUST NOT 再维护一套逐阶段驱动或重复解析同一阶段结果。整帧 lease 与最终成败由 FrameCoordinator 协调，根图及子图仅保留各自实际执行所需的局部状态；正式调用链为 PresentationDomainRuntime → Pose DomainInstance/Session → FrameCoordinator → RoleRuntime（唯一驱动 Source Module 帧生命周期）→ 根 GraphRuntime（FlowCanvas 原生图，状态机子图经 Subgraph 边界派生并共享同一帧谱与模块租约）→ 节点 Handler。只有正式发布成功才允许进入其后的 Constraint 历史提升、Source 提交及动作／时钟确认；失败结果 MUST NOT 继续走成功提交尾部。

正式运行 MUST 由唯一原生 FlowCanvas Pose Graph 实例执行 PoseStateMachine、Player、ActionPlaybackInput lifecycle、AnimationSlot、Local/Component Pose、Constraint 与 Output；唯一 `CharacterPoseSourceModule` 负责 source sample、Animancer/Playable 与物理 source 生命周期，其帧打开、收帧与丢弃 MUST 由 RoleRuntime 单点驱动，模块开帧失败 MUST 丢弃已开图帧并上抛；唯一 `CharacterPoseConstraintRuntime` 负责 Foot Placement、PoseBone Goal、Goal Contribution、Assembler、唯一 Goal Set、FBBIK 与 BendHistory；唯一 `CharacterFinalPosePublication` 负责唯一 Committed/Pending Final Pose 物理页与 Physical Writer。图实例 MUST 按固定阶段调用各 Constraint 入口一次，Constraint Module MUST 不扫描 Graph 或维护第二份 Schedule。Module 间 MUST 只交换同 Frame、Completion、Graph、Rig 与 Tuning Generation lineage 的 typed Result。外层 Runtime、ScenePlay 与 Diagnostics MUST 不创建第二 Graph 实例、第二 Action lifecycle、第二 Constraint 事务、第二 Goal Set、第二 FBBIK、第二 Final Pose 页或第二 Writer。

Constraint外部Owner变化 MUST整体保留指定提交ad3527e103cc3235a63e8a1c1dbd26df5155e0ba的动画时钟／混合、Foot、Pelvis、Goal与FBBIK实现、公式、配置和数值顺序；成功Reset MUST保留第一阶段通过后的正式结果，第一阶段批准的Reset差异单独引用证据；本次Goal MUST先完成并验证IK维护重构，再以其通过提交串行接入；第一阶段结构变化与独立Reset修正的证据 MUST保留，总基线不变。其它Foot待办或未归档不构成前置。本change不得恢复旧中央Foot状态机、已撤除业务Reach硬夹紧／末端夹脚、已撤销SmoothKnee或恢复第一阶段已删除的结构，不得接管其它未实施IK行为任务。

#### Scenario: 正常执行Foot Placement与FBBIK

- **WHEN** Native Pose Graph Runtime执行到Foot Placement和PoseBone Goal Operation并由Constraint Module形成合法Goal Set与FBBIK结果
- **THEN** 同一Frame MUST只发布一个Constraint Result、一个Pose Output和一个Final Publication Result
- **AND** 外层Runtime MUST不理解Foot Context、Goal workspace或BendHistory

#### Scenario: Source target Pending

- **WHEN** Pose Graph Runtime发布候选target Demand且Source Module返回Pending
- **THEN** 是否保持当前合法State MUST只由Graph节点语义决定并在Barrier前关闭Pending帧
- **AND** Source Module MUST不选择State，外层Runtime MUST不使用旧Timeline或默认Idle补洞

#### Scenario: Goal Slot重复

- **WHEN** 两个Goal Contribution尝试写入同一FBBIK Effector Slot
- **THEN** Constraint Module MUST在Physical Writer前使同一Frame typed Invalid
- **AND** Runtime MUST不按连接顺序覆盖、创建第二Goal Set或绕过FBBIK

#### Scenario: 整帧入口遇到准备失败

- **WHEN** 表现运行时提交一帧且原生 Pose 准备未返回 Prepared
- **THEN** 帧协调点 MUST 按正式失败结果关闭已经取得的图帧和模块租约，并返回最终结果
- **AND** 外层 MUST NOT 再次推进该帧求值或以另一条阶段调用路径继续执行

#### Scenario: 最终发布失败

- **WHEN** 同一 Pose 帧最终发布失败
- **THEN** 系统 MUST NOT 将该帧作为成功帧提升 Constraint 历史、提交 Source 或确认动作与表现时钟
- **AND** 已跨过 Animancer Evaluate Barrier 的失败 MUST 进入正式 Fault 状态并保留故障上下文，MUST NOT 宣称已经回滚骨骼写入

### Requirement: Dense状态与稀疏生命周期必须使用不同暂存策略

每帧完整生成的Pose、velocity、weight、parameter、Value、Operation completion、Inertialization next state、Constraint Result和Final Pose MUST直接写各Owner固定Committed/Pending页。PoseState、Player、ActionPlaybackInput lifecycle、Slot与Transition的小型状态 MUST使用原生图实例固定布局的节点小型 Pending 状态。Action command cursor、source ownership、usage、retirement与release handshake MUST使用固定容量mutation journal或prepared/deferred resource命令。在线调参 MUST使用Frame外的actor-local Graph/Source/Constraint Candidate Snapshot并一次提升Tuning Generation，不得写入原生图资产或混入Frame journal。系统 MUST不为了统一Interface复制完整Registry，也 MUST不把Dense Pose、Goal或Operation结果降低为逐项托管mutation。

#### Scenario: 本帧只有一个source release

- **WHEN** 当前Frame只释放一个旧source而其它source ownership不变
- **THEN** Source Module MUST只记录对应预验证release mutation与deferred command
- **AND** MUST不复制完整Physical Source Registry或把release字段写进原生图资产

#### Scenario: Program产生下一帧Pose

- **WHEN** 原生Pose图实例执行当前Frame全部活跃Pose节点
- **THEN** MUST把Value与completion直接写入原生Pose图实例自有Frame Pending页并只向根Transaction返回typed lease/result
- **AND** MUST不先复制上一Committed Value Workspace或通过旧Native Program持有两种寿命

#### Scenario: 本帧只有一个Action生命周期变化

- **WHEN** 当前帧只新增或推进一个Action playback而其它Registry entry不变
- **THEN** Runtime MUST只在固定journal中记录对应mutation
- **AND** MUST不复制完整Action registry或全部source ownership集合

#### Scenario: Pose Graph生成下一帧结果

- **WHEN** Native Pose Graph为当前帧求值全部PoseBone
- **THEN** Job MUST把结果直接写入Pending Native/Pose页
- **AND** MUST不先把Committed Pose页复制为Pending页

#### Scenario: Pose Graph产生下一帧Pose

- **WHEN** 原生Pose图实例在当前Frame求值全部活跃Pose节点
- **THEN** Job MUST把结果直接写入Pending Native/Pose页
- **AND** MUST不先把Committed Pose页复制为Pending页

## ADDED Requirements

### Requirement: 表现外围业务提交与释放必须遵守同一故障归属

角色表现 owner MUST 将 Pose 不可逆求值及其后的 Timeline、桥、时钟和 Camera 业务收尾纳入同一 Actor 表现故障归属。Pose 返回成功不表示整个表现帧已经完成；之后的业务提交失败 MUST 保留已提交事实、实际失败阶段和原因，并阻止该 Actor 继续下一表现帧，不得以丢弃残余 Pending 宣称物理回滚。Pose 局部阶段与资源所有权 MUST 保持，不得另建外层 Pose 逐阶段执行路径或让多个独立故障状态各自决定恢复。

已提交结果的纯观察输出 MUST 与业务提交区分；观察错误按正式诊断错误通道报告，不得将已完成业务事实改写成回滚成功。停止与故障清理 MUST 按实际资源 owner 尝试释放全部已取得资源；一个释放失败不得跳过其他 owner 或覆盖最初的业务故障，MUST NOT 吞掉失败并报告成功。

#### Scenario: Pose成功后Timeline业务收尾失败

- **WHEN** Pose 和动作确认已经提交，而 Timeline 表现业务收尾失败
- **THEN** 角色表现 owner MUST 保留该阶段失败并使 Actor 表现不可继续
- **AND** MUST NOT 因 Pose 局部 RunFrame 已成功而继续下一帧，或宣称已经回滚骨骼和动作确认

#### Scenario: 已完成结果的观察发布失败

- **WHEN** 已完成业务结果的纯诊断观察发布失败
- **THEN** 系统 MUST 按正式诊断通道记录观察失败并保留真实业务提交状态
- **AND** MUST NOT 把观察异常记录成业务帧已成功回滚

#### Scenario: Session释放失败而Services仍待释放

- **WHEN** 表现 owner 正在释放已取得的 Session 与 Services，而先释放的 owner 抛出异常
- **THEN** 系统 MUST 继续尝试释放剩余 owner，并保留首故障与清理失败信息
- **AND** MUST NOT 因第一个释放异常而保留其余租约或以默认成功结束
