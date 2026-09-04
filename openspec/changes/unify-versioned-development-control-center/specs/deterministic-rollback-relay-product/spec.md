## MODIFIED Requirements

### Requirement: Relay Server Runtime Manifest必须完整锁定会话身份

Rollback候选 MUST固定 Server 的 Product/Model/Protocol、roster、TickRate、MaximumPredictionLeadTicks、Semantic/Fixed Program/Layout/CollisionWorld/Kcc、confirmation/capacity/snapshot策略。Development Run Host MUST通过现有产品adapter在本Run生成运行manifest，绑定CandidateId、RunId、SessionId、endpoint与静态配置hash。Server MUST在监听前核对所有事实，不从环境或缺省值补齐；运行实例值 MUST不写回候选。

#### Scenario: Manifest缺少ProgramHash

- **WHEN** 本Run的Relay运行manifest缺少或包含无效Fixed ProgramHash
- **THEN** Relay MUST在监听前以明确退出码拒绝
- **AND** MUST不等待Client连接后补齐身份

#### Scenario: Client Handshake与Manifest不一致

- **WHEN** Client握手的候选、会话、协议、roster或deterministic identity与运行manifest不同
- **THEN** Relay MUST拒绝锁定roster
- **AND** MUST不从Client消息补齐或改写正式配置

#### Scenario: Run引用错误Candidate

- **WHEN** Relay Run Manifest的CandidateId或Candidate hash与所选Product不一致
- **THEN** Server MUST以明确退出码拒绝监听
- **AND** MUST不等待Client连接后猜测版本

### Requirement: Rollback Network Test Product必须包含精确Server Closure

Rollback adapter MUST通过统一开发候选schema v4发布精确Client与Dedicated Relay artifact，并绑定独立GM工具及角色计划。Relay与Client文件闭包 MUST保持产品自己的业务边界；公共Build MUST不引用Rollback实现或按目录推断产品。实例配置 MUST只在Runs/<RunId>生成。

#### Scenario: 构建Rollback Product

- **WHEN** 作者构建DeterministicRollback候选
- **THEN** 发布器 MUST原子封存Player、Dedicated Relay与声明的GM/工具依赖
- **AND** 开发候选schema v4 MUST记录准确来源与文件闭包

#### Scenario: Server文件在Build后变化

- **WHEN** 启动前Relay executable或依赖与候选manifest/hash不符
- **THEN** Host MUST拒绝启动
- **AND** MUST不重新publish或复制另一个版本修复

#### Scenario: Candidate携带运行token

- **WHEN** Candidate闭包包含GM访问token、Relay查询token或固定RunId
- **THEN** Candidate validation MUST失败
- **AND** MUST不把构建期token继续作为多会话配置

#### Scenario: 构建包含 GM 的产品

- **WHEN** 作者执行 Rollback Candidate Build
- **THEN** MUST原子发布 Player、Relay、GM 与工具静态策略
- **AND** Run MUST另行创建并验证本次配置，不把端口、token 或 SessionId 固化进 Candidate

### Requirement: Rollback Run必须只启动一个Dedicated Relay Server与两个Unity Client

Rollback业务会话 MUST只有一个Dedicated Relay与两个Unity Client；另外的Development Run Host和开发GM是工具进程，不执行Gameplay。Host MUST按正式角色依赖先等待Relay Ready再启动两个Client，运行配置来自精确Run manifest。Run MUST不启动第三个Unity Player、不接受Client-host、不编译/publish；Relay退出 MUST使本次会话失败并由Host清理本Run，不能切换模型或影响其他Slot。

#### Scenario: 启动完整DS Demo

- **WHEN** 候选、工具、角色计划与Slot均有效
- **THEN** Host MUST启动Relay、GM及两个Client并记录自身，共五个角色进程
- **AND** 只有两个Client MUST是Unity Player

#### Scenario: Server在运行中退出

- **WHEN** 本次Dedicated Relay异常结束
- **THEN** 本Run MUST结束并保留故障记录
- **AND** MUST不由Client接管，也不停止另一Slot

#### Scenario: 两个Candidate并行运行

- **WHEN** 两个合法Rollback Candidate分别使用不同Slot启动
- **THEN** MUST形成两个Candidate/Run/Session/GM身份完全隔离的Run，每个Run由独立Host拥有Relay、GM和两个Client
- **AND** 任一GM查询或Stop MUST不命中另一Run

#### Scenario: GM 启动失败

- **WHEN** 本Run的GM Tool、endpoint、token或目标身份不合法
- **THEN** Development Run Host MUST把本Run标记Faulted并回收本Run已启动进程
- **AND** MUST不启动无匹配工具的Peer或搜索另一GM

#### Scenario: 运行中 GM 退出

- **WHEN** Gameplay Session仍在运行而独立GM退出
- **THEN** 本Run工具状态 MUST变为Unavailable且Relay与两个Client继续按原模型推进
- **AND** Player MUST不接管控制台或获得GM凭据
