## MODIFIED Requirements

### Requirement: Relay Server Runtime Manifest必须完整锁定会话身份

Rollback Build adapter MUST在Candidate中锁定CandidateId、ProductId、expected client/actor roster、Model/Protocol、TickRate、MaximumPredictionLeadTicks、SemanticHash、Fixed ProgramHash、LayoutHash、CollisionWorldHash、KCC identity/capabilities、confirmation policy、capacity和snapshot source policy。每次Run MUST另行生成绑定Candidate manifest/hash、RunId、SessionId、listen/peer endpoint和role配置hash的Relay Run Manifest。Server MUST在监听前共同校验Candidate与Run，MUST不从Unity asset、环境目录、默认值或另一Run补齐缺失事实。

#### Scenario: Run引用错误Candidate

- **WHEN** Relay Run Manifest的CandidateId或Candidate hash与所选Product不一致
- **THEN** Server MUST以明确退出码拒绝监听
- **AND** MUST不等待Client连接后猜测版本

#### Scenario: Candidate缺少ProgramHash

- **WHEN** Candidate静态身份缺少或包含无效Fixed ProgramHash
- **THEN** Relay MUST在读取Run endpoint前明确拒绝启动
- **AND** MUST不从Client handshake、文件名或默认值补齐

#### Scenario: Client Handshake与Run不一致

- **WHEN** Client提交的CandidateId、RunId、SessionId、Protocol或deterministic identity与Run Manifest不一致
- **THEN** Server MUST拒绝锁定roster
- **AND** SimulationTick MUST不开始

### Requirement: Rollback Network Test Product必须包含精确Server Closure

Rollback adapter MUST通过公共合同发布Unity Player、Dedicated Relay、独立GM、candidate-owned启动adapter和公共Orchestrator。Candidate Root MUST包含全部runtime artifacts、Tool Bundles、静态策略、Session Plan及schema v3 exact closure，但 MUST不包含本次Run的endpoint、token、RunId或运行SessionId。Player MUST不包含GM连接配置或工具凭据。公共Build workflow MUST不引用Rollback concrete type或按目录名猜测产品。

#### Scenario: 构建Rollback Candidate

- **WHEN** 作者执行DeterministicRollback Candidate Build
- **THEN** MUST原子发布Player、Relay、GM与Tool Bundles并绑定同一CandidateId
- **AND** Candidate manifest MUST证明全部artifact、工具和Session Plan hash

#### Scenario: Candidate携带运行token

- **WHEN** Candidate闭包包含GM访问token、Relay查询token或固定RunId
- **THEN** Candidate validation MUST失败
- **AND** MUST不把构建期token继续作为多会话配置

#### Scenario: Relay文件在Build后变化

- **WHEN** Run前Relay executable、依赖、Candidate静态配置或manifest hash与Candidate manifest不一致
- **THEN** Run MUST拒绝启动
- **AND** MUST不重新publish或复制文件修复产物

### Requirement: Rollback Run必须只启动一个Dedicated Relay Server与两个Unity Client

每个Rollback Run MUST由Candidate携带的Orchestrator消费显式Slot，生成Relay、Peer、GM Server与GM Console运行配置，并按Session Plan启动一个Dedicated Relay、一个独立GM和两个Unity Client。只有两个进程是Unity Player。Run MUST校验Candidate、Tool、Slot、Run与Session身份，不运行时Build/publish，不支持旧三进程、Unity Host、固定ProductRoot或StopExisting。启动失败 MUST只清理本Run；GM故障只使本Run工具不可用，Relay故障保持既有Session失败语义。

#### Scenario: 两个Candidate并行运行

- **WHEN** 两个合法Rollback Candidate分别使用不同Slot启动
- **THEN** MUST形成两个Candidate/Run/Session/GM身份完全隔离的四进程组合
- **AND** 任一GM查询或Stop MUST不命中另一Run

#### Scenario: GM启动失败

- **WHEN** 本Run的GM Tool、endpoint、token或目标身份不合法
- **THEN** Orchestrator MUST把本Run标记Faulted并回收本Run已启动进程
- **AND** MUST不启动无匹配工具的Peer或搜索另一GM

#### Scenario: Relay在运行中退出

- **WHEN** Dedicated Relay异常结束
- **THEN** 两个Client MUST结束当前Rollback Session并报告Relay unavailable
- **AND** MUST不由任一Client接管Relay或切换其它Session

#### Scenario: GM在运行中退出

- **WHEN** Gameplay Session仍在运行而独立GM退出
- **THEN** 本Run工具状态 MUST变为Unavailable且Relay与两个Client继续按原模型推进
- **AND** Player MUST不接管控制台或获得GM凭据
