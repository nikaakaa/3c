# character-camera-source-parity Specification

## Purpose
定义 ZZZ/Corin 相机来源资源、动作调用、3C 资产与运行消费之间的逐项证据边界；资源存在不等于动作已接通。

## Requirements

### Requirement: 相机移植必须分开记录来源和当前实现证据

移植 MUST 记录同版本资源身份/hash、类型/函数消费者、单位、空间、时间和组合规则，并区分原始配置、已证明行为、推断与未解析依赖。项目源码、作者资源、生成产物和实时运行证据 MUST 分层记录；历史取证页不代表今天的实现状态，项目写了公式也不代表该公式与来源一致。

#### Scenario: 原字段存在但消费者缺失

- **WHEN** 已解码资源包含一个尚未闭合消费者的字段
- **THEN** 该字段 MUST 保持未闭合并定位缺口
- **AND** MUST 不以名称相似、默认值或近似曲线标记完成

### Requirement: 完整目标必须覆盖单角色相机及可达依赖

完整移植目标 MUST 覆盖默认配置、球面/轨道、手动输入、跟随/阻尼、单点/双点/多点/实体取景、锁定、转场、Zoom/Stretch/Override/Shake/Shot、碰撞、时间和取消/退出行为，以及它们可达的曲线、prefab 和绑定。换人或跨角色接管不自动纳入。限定范围交付 MUST 明确列出尚未完成部分，不得借重写文档缩减完整目标。

#### Scenario: Zoom 已完成而 Shake 尚缺消费者

- **WHEN** 已闭合的 Zoom 进入正式链路
- **THEN** 该部分 MAY 单独记录交付
- **AND** 完整 change MUST 保持未完成，Shake 仍明确不可用

### Requirement: 来源资源必须落实到真实动作调用

Corin 已识别的 81 Shake、18 Zoom、18 Stretch、4 Override 及属于单角色范围的可达 Shot/曲线 MUST 逐项记录正式去向和真实调用证据。事件映射必须说明来源、时点、时间域、权重、目标、持续条件和退出行为，不得把资源数直接转换为触发点数。工程落点 MUST 按两类表达记录：绑定动作实例的一次性触发记录为 TreeClip 相机 Node，持续效果窗口记录为唯一 Timeline 效果轨道的窗口与资源引用；MUST NOT 以已删除的 CameraState/Response/Cue 触发型轨道或按类型拆分的四条效果轨道作为映射落点。无法解析的字段或依赖必须阻止对应能力发布。

#### Scenario: 资源没有已确认动作调用

- **WHEN** 导入器已读取资源但未定位调用者
- **THEN** 记录 MUST 区分资源存在与动作接通
- **AND** MUST 不虚构 TreeClip Node 或绑定事件填补数量

#### Scenario: 来源动作和工程动作名字相似

- **WHEN** 来源记录 Attack_Normal_01，而工程存在名为 Attack1 的内容
- **THEN** 映射 MUST 继续提供源事件→具体 TreeClip/Node 身份→效果类型/ResourceId→时间/持续/取消的证据，不能按名字认定对应
- **AND** 当前仅列出的 Corin_Attack_Normal_01_CamShake_A_01 MUST 保持 Shake 类型，不得用 Zoom 代替缺失资源

#### Scenario: 部分 Zoom 资源键已经对齐

- **WHEN** Attack_Counter 或 Attack_Normal_05 的来源 Zoom key 与正式资产 m_ZoomId 一致
- **THEN** 记录 MUST 只认定资源键对齐，继续补工程 TreeClip/Node 和生命周期证据
- **AND** 缺口 MUST 按具体资源记录，不要求用户凭空填写整张映射表

### Requirement: 数值语义与完整交付必须有对应去向

导入 MUST 保留曲线键、切线、边界模式、时间尺度、空间和枚举的真实语义。完成报告 MUST 对应来源→作者→编译→请求→求值→平台输出，并说明不支持能力、已删除旧路径和现有证据边界。改为 3C 自有行为必须先获得明确范围决定并更新合同，不得继续标注为已还原的原行为。

#### Scenario: 本次没有 Unity 实时证据

- **WHEN** 只有源码与磁盘资产可读
- **THEN** 报告 MUST 将动态触发、手感和画面标为未验证
- **AND** MUST 不把编译成功、资源存在或任务勾选当作端到端完成
