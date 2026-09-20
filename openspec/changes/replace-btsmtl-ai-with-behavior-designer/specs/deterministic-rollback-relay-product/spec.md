## MODIFIED Requirements

### Requirement: Relay Server Runtime Manifest必须完整锁定会话身份

Rollback Build adapter MUST在Candidate中锁定CandidateId、ProductId、expected client／actor roster、Model／Protocol、TickRate、MaximumPredictionLeadTicks、GraphHash、Fixed domain layout identity、CollisionWorldHash、KCC identity／capabilities、confirmation policy、capacity和snapshot source policy。每次Run MUST另行生成绑定Candidate manifest／hash、RunId、SessionId、listen／peer endpoint和role配置hash的Relay Run Manifest。Server MUST在监听前共同校验Candidate与Run，MUST不从Unity asset、环境目录、默认值或另一Run补齐缺失事实。

Manifest MUST分别锁定真实 Peer roster、Actor roster 与每 Actor 输入所有权/来源，包含普通标量形式的 Bot 行为内容身份和所需协议版本。只有真实 Peer 具有 endpoint/连接身份；Bot MUST不伪造 PlayerId。旧一 Peer 一 Actor 的协议与名单 reader MUST随正式版本升级删除，MUST不按字段缺省猜测旧格式或从 Unity 内容解析行为。

#### Scenario: Run引用错误Candidate

- **WHEN** Relay Run Manifest的CandidateId或Candidate hash与所选Product不一致
- **THEN** Server MUST以明确退出码拒绝监听
- **AND** MUST不等待Client连接后猜测版本

#### Scenario: Manifest缺少ProgramHash

- **WHEN** Candidate静态身份缺少或包含无效GraphHash或Fixed domain identity
- **THEN** Relay MUST在读取Run endpoint前明确拒绝启动
- **AND** MUST不从Client handshake、文件名或默认值补齐

#### Scenario: Manifest缺少GraphHash

- **WHEN** Candidate静态身份缺少或包含无效GraphHash或Fixed domain identity
- **THEN** Relay MUST在读取Run endpoint前明确拒绝启动
- **AND** MUST不从Client handshake、文件名或默认值补齐

#### Scenario: Client Handshake与Manifest不一致

- **WHEN** Client提交的CandidateId、RunId、SessionId、Protocol或deterministic identity与Run Manifest不一致
- **THEN** Server MUST拒绝锁定roster
- **AND** SimulationTick MUST不开始

#### Scenario: 两份名单拥有相同Actor但不同生产者

- **WHEN** Client 与 Relay 的 Bot 输入所有权映射不同
- **THEN** 握手 MUST因身份/hash 不一致而失败
- **AND** MUST不只比较 Actor 数量后允许推进

#### Scenario: Relay装载Bot示例

- **WHEN** Relay 读取含多个 Bot 的合法正式 Manifest
- **THEN** MUST只建立对应输入权限、汇集、确认和只读诊断
- **AND** MUST不加载插件程序集、图资产、Character Program 或 Collision World
