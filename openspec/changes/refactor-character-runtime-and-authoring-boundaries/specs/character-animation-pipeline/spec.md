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
