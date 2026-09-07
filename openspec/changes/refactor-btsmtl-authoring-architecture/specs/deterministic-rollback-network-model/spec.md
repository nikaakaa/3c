## MODIFIED Requirements

### Requirement: Rollback History 必须保存完整 Fixed SimulationWorldSnapshot

Rollback History Pass MUST保存有界 canonical input history与Fixed world snapshot history。Fixed Target MUST复用typed state schema和`Begin -> Evaluate -> Finalize -> Commit|Abort`事务生命周期形状，但 MUST实现自己的Fixed partition、numeric value、canonical codec与transaction specialization。World snapshot MUST包含SimulationTick、Fixed Program/Layout/codec identity、stable actor table、所有Actor committed SimulationState canonical bytes（含控制模块状态、ActionInstance、技能调用frame、参数及停止进度）、Deterministic KCC actor/world state、RNG、Event/Command cursor和模型必要状态，MUST不保存active transaction、mutable typed partition或Float32 State/Snapshot，也 MUST不新增平行总世界状态aggregate。

Peer的predicted completed frontier MUST不超过本地canonical contiguous frontier加`MaximumPredictionLeadTicks`。达到上限时Ingress MAY继续接收canonical并重发同一待执行Tick输入，但Schedule MUST不新增predicted history；canonical差异触发的restore/replay仍 MUST执行。`MaximumRollbackDepthTicks` MUST只用于restore/replay深度、history保护和deep recovery判定。

#### Scenario: Capture Tick T

- **WHEN** History Pass保存 Tick T snapshot
- **THEN** MUST原子 capture 全部 Actor 和 KCC/world state
- **AND** MUST不只保存 Transform 或单个 Actor

#### Scenario: 快 Peer 达到最大预测领先

- **WHEN** 下一个predicted Tick会超过canonical contiguous frontier加MaximumPredictionLeadTicks
- **THEN** Schedule MUST返回NoStep并等待canonical推进
- **AND** input history MUST不因两个进程运行速度不同而无限增长
#### Scenario: 回滚并发技能

- **WHEN** Tick T中有多个释放共享同SkillProgram
- **THEN** 完整snapshot MUST恢复各实例独立状态并复用唯一Fixed执行链


### Requirement: Rollback Model 必须严格校验 Deterministic Capability

Model MUST在创建前校验 SemanticHash、Fixed ProgramHash、Fixed ABI、Program deterministic capability、TickRate、CollisionWorldHash、KccId/capabilities、protocol version 和 actor roster 规则。任一项不满足 MUST拒绝创建，MUST不跳过 operation、加载 Float32 Program 或回退其他 Solver/Model。

#### Scenario: Program 包含 Nondeterministic Operation

- **WHEN** Fixed Program capability manifest 不满足 deterministic-compatible
- **THEN** Rollback model option MUST不可创建
#### Scenario: 控制模块或技能版本不一致

- **WHEN** Peer的代码控制合同、SkillProgram闭包或状态schema不匹配
- **THEN** 握手及Session准备 MUST拒绝
- **AND** Relay MUST不补算、改写技能数据或执行角色控制
