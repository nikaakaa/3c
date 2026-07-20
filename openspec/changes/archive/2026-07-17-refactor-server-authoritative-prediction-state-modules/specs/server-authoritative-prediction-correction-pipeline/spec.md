## ADDED Requirements

### Requirement: Prediction State必须由唯一aggregate root协调内部模块

`IServerAuthoritativePredictionStatePort` MUST只暴露一个`ServerAuthoritativePredictionState` aggregate root。该root MUST唯一拥有Confirmation/Request、Prediction History、EventId Disposition Journal与Reconciliation内部模块，并负责跨模块操作顺序。Correction Schedule、History Egress和Output Disposition Pass MUST不分别创建状态、直接持有子模块或维护重复cursor与集合。

#### Scenario: 三个Prediction Pass绑定同一Source port

- **WHEN** Prediction Pipeline创建Correction、History与Disposition Pass runtime
- **THEN** 三者 MUST取得同一个Prediction State aggregate root
- **AND** 每个可变字段 MUST只有一个内部模块owner

### Requirement: Prediction内部模块必须保持明确状态所有权

Confirmation/Request模块 MUST唯一拥有confirmed input/event cursor、authority ack/baseline/clock cursor与pending request；History模块 MUST唯一拥有history record、replay查询和history capacity；Disposition Journal模块 MUST唯一拥有EventId entry、journal cursor、confirmation/rejection与journal capacity；Reconciler MUST只验证identity、计算decision并构造restore plan，不得拥有这些可变集合。

#### Scenario: Authority Ack推进确认

- **WHEN** Prediction State收到合法Authority Ack
- **THEN** Journal模块 MUST计算EventId重分类，Confirmation模块 MUST推进ack与confirmed cursor
- **AND** Reconciler与History MUST不保存第二份confirmation cursor

### Requirement: Prediction跨模块转换必须原子提交

Ack、Authority Baseline与Restore构造 MUST先完成全部identity、horizon、history和capacity验证，再提交Confirmation、History与Journal变化。任一prepare或restore store失败 MUST不得留下部分模块已推进的活动状态；outer Pipeline transaction的checkpoint/rollback MUST继续覆盖三个正式SnapshotParticipant。

#### Scenario: Baseline identity在restore前失败

- **WHEN** Authority Baseline的Program、Solver、Actor或World identity不匹配
- **THEN** Prediction State MUST拒绝该Baseline
- **AND** confirmed cursor、history、journal与pending request MUST全部保持调用前状态

### Requirement: Prediction State模块化不得改变Snapshot身份与字节

Correction Schedule、History Egress与Output Disposition MUST继续使用现有StateOwner、StateSchemaId、SchemaVersion和SnapshotParticipant顺序。Correction v3、History v1与Journal当前schema的magic、字段顺序、排序、count上限、nested World/Pipeline bytes和canonical hash MUST保持不变；系统 MUST不增加兼容reader、schema升级、双写payload或第四份Prediction状态。

#### Scenario: 相同Prediction状态在模块化前后Capture

- **WHEN** Confirmation、History与Journal包含相同逻辑状态
- **THEN** 三个Participant产生的canonical payload与StateHash MUST exact-byte相同
- **AND** 既有Network checkpoint与restore transaction MUST无需转换即可消费

### Requirement: Prediction容量与淘汰策略必须保持单一实现

History模块 MUST继续只淘汰confirmed input record，并在容量不足且最早record仍未确认时明确失败；Journal模块 MUST继续保留live predicted event并使用现有capacity上界。Aggregate root、Pass、Source与Endpoint MUST不复制容量判断、扩大容量、吞掉异常或增加fallback淘汰路径。

#### Scenario: History最早输入仍未确认

- **WHEN** 新history record到达且HistoryCapacity已满，而最早input sequence大于confirmed sequence
- **THEN** History模块 MUST以包含firstTick、firstSequence、confirmedSequence、lastAckTick与lastBaselineTick的结构化上下文失败
- **AND** MUST不删除未确认record或改用另一个history容器
