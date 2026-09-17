## Purpose

定义项目在不修改 UnityPlayer 的前提下保存、加载、解码和播放 ACL 动画数据的正式边界，使压缩动画能够进入现有唯一 Pose Source 与表现帧事务，同时保持资源身份、质量和生命周期可验证。

本次范围包含片段自带骨骼与 BlendShape；属性运输、混合及发布合同由同 change 的 `character-animation-scalar-presentation` 规定，复用现有 Program/Pose Plan，不增加独立表情行为系统。

## ADDED Requirements

### Requirement: ACL 动画资源必须拥有完整且不可混淆的身份

每个 ACL 动画资源 MUST 分别声明 Transform、可选 Scalar、数据库头和各 quality tier 的 bulk，固定各块存在标记、长度、版本与 hash，以及源身份、Rig、完整骨骼/标量绑定、reference/default 值身份、正式播放范围、采样网格、压缩设置、误差度量和构建版本。声明必须存在的数据、绑定或身份缺失、损坏或不匹配时 MUST 为 Invalid，不得用其它资源或未声明默认值补全。明确声明零标量轨道、未使用数据库或 tier 长度为零 MUST 不被误判为缺块；原始 bulk MUST 不被误当作数据库头。

#### Scenario: 有效 ACL 资源进入运行时

- **WHEN** Projection 引用的 ACL 资源所有数据块、Rig、绑定、版本和哈希均匹配
- **THEN** Source MUST 为该资源建立唯一可追踪的 source identity
- **AND** 该 identity MUST 可关联到对应的 Projection、Frame、SourceGeneration 与资源版本

#### Scenario: ACL 资源身份不匹配

- **WHEN** ACL 数据的格式版本、Rig、绑定或内容哈希与 Projection 不一致
- **THEN** 资源 MUST 返回 Invalid 及稳定失败原因
- **AND** Runtime MUST 不开始采样或使用上一资源的资源句柄继续播放

### Requirement: ACL 构建结果必须由正式素材和可复现质量合同产生

Editor-only 构建 MUST 从正式 AnimationClip 及明确的绑定/轨道覆盖合同生成项目不可变 ACL 资源，保存源依赖、压缩设置、误差度量与输出 hash。ZZZ 私有 payload MUST 不被直接交给项目格式解码器或仅修改版本号后使用。构建 MUST 验证全部必须变换和标量轨道、Rig、播放范围、重采样误差与压缩误差；缺失或超限 MUST 阻止发布，不得静默提高误差、删除轨道或生成替代数据。

质量报告 MUST 分别说明项目压缩相对正式素材的误差，以及正式素材相对原始 ZZZ 的还原证明。原始 Scalar 未解码、绑定未解析或数据库不完整时，MUST 不声称已经完整还原原始动画。

#### Scenario: 构建 ACL 资源

- **WHEN** 正式素材、Rig、绑定、采样范围和压缩设置均合法
- **THEN** 构建 MUST 发布带完整身份和质量记录的 ACL 资源
- **AND** 资源 MUST 能在相同输入下得到稳定的内容哈希

#### Scenario: 构建误差超过上限

- **WHEN** 压缩结果相对规范参考采样的误差超过声明上限
- **THEN** 构建 MUST 失败并定位资源、轨道与误差度量
- **AND** MUST 不发布该资源或降低上限要求后继续

### Requirement: ACL Source 必须只负责按正式时间产生 Pose

ACL Source MUST 接收编译产物确定的资源、source identity、effective time、采样策略和 Frame lineage，并发布匹配的 source sample。Program MUST 唯一推进时钟并计算 effective time；ACL Source MUST 不重复应用 play rate、选择 PoseState、Transition、Slot、Action winner、Foot Placement、IK、Goal 或 Final Pose，MUST 不读取作者字符串、AssetDatabase 或运行时 AnimationClip 作者曲线。

ACL 输入 MUST 保留既有 Root/Scale policy、Virtual Bone 派生、source velocity、continuity 和 completion 语义。正式 Phase/Foot 注册曲线 MUST 继续使用既有 Build 与消费者；原始 Motion/Root 标量 MUST 不直接驱动 Gameplay 位移或绕过唯一模拟移动链。

同一动画源的骨骼与动画属性 MUST 使用同一 effective time、资源 generation、Frame lineage 和 readiness。NativeClip 与 ACL 的属性采样 MUST 输出同一 typed 参数合同；backend MUST 不选择属性的跨 source 混合规则或直接写 Renderer。

#### Scenario: 持续 Pose 使用 ACL Source

- **WHEN** PoseStateMachine 选择一个绑定 ACL source 的状态并提交合法 demand
- **THEN** ACL Source MUST 按该 demand 的有效时间解码并发布一个匹配 generation 和 frame lease 的 Pose sample
- **AND** PoseStateMachine、Transition 与 source-local 权重 MUST 保持其既有 owner

#### Scenario: ACL Source 收到过期请求

- **WHEN** sample 请求的 Projection、SourceGeneration、Frame lease、Rig 或资源 identity 已过期
- **THEN** ACL Source MUST 拒绝该请求并返回 Invalid
- **AND** MUST 不把过期 Pose 写入当前表现帧

### Requirement: ACL 资源准备和释放必须服从唯一 Source 生命周期

ACL 资源 MUST 在其声明的全部质量数据、绑定和 Context 准备完成并取得使用租约前返回 Pending，完整匹配后才 Ready。资源加载 MUST 通过项目正式资源体系在表现帧外推进，不能依赖该帧成功 Seal 才完成准备。预期 Pending MUST 通过正式 outcome 处理，不得借异常恢复。Source usage、retention、retirement、deferred release 和 release completion MUST 与唯一 Source 生命周期一致；表现帧 MUST 不同步等待 I/O。

可见 source 的资源与质量数据 MUST 被使用租约保留，当前解码结果 MUST 不因 LRU 或中途流入改变。共享数据库和 payload MUST 等待全部 Actor 的租约与在途请求结束后才可回收；不得因一个 source 退休而释放其它 source 正在使用的数据。

#### Scenario: 资源尚未准备完成

- **WHEN** ACL payload 或解码 Context 仍在准备
- **THEN** Source MUST 返回 Pending
- **AND** Program MUST 按既有语义继续采样当前合法 source；Entry Pending MUST 不发布 Final Pose，不得伪造 target sample 或改用其它 backend

#### Scenario: 资源获得释放许可

- **WHEN** Program consumer 已发布匹配 source identity 的 retirement permission 且当前 Frame 成功 Seal
- **THEN** Source Module MUST 执行唯一 deferred release 并发布匹配 generation 的 completion
- **AND** MUST 不在 release completion 前复用该资源的逻辑或物理槽位

### Requirement: ACL Source 必须接入现有唯一表现执行链

ACL backend MUST 复用当前唯一 Source Module、Pose Plan、Evaluate Barrier、PlayableGraph、Goal Assembly、FBBIK 和 Final Publication。图内采样节点 MUST 使用既有 capture 与结果页合同，不得创建第二个 Graph、第二个逻辑 Player、平行 Pose buffer、第二个 Final writer 或独立 Preview 链。ACL 结果 MUST 经过同一 source policy、Virtual Bone、Velocity 与 completion 边界。ACL-backed source MUST 不同时驱动展开 `.anim`，运行时资源闭包 MUST 不保留该素材的备用 Clip 引用。

#### Scenario: ACL 与其它 source 共同过渡

- **WHEN** 编译后的 Transition 要求 ACL source 与另一个合法 source 同时可见
- **THEN** Program Runtime MUST 发布两份正常 source demand，Source Module MUST 在同一 Evaluate Barrier 提供两份 capture
- **AND** Transition weight、clock、Slot、blend 和 release MUST 仍由既有 owner 计算

#### Scenario: ACL Source 试图创建第二播放链

- **WHEN** ACL backend 请求独立 PlayableGraph、第二 Final Pose 或直接写 Physical Transform
- **THEN** Runtime 构造或验证 MUST 失败
- **AND** 当前 Actor MUST 保持既有 Committed 结果，不得建立旁路播放链

### Requirement: ACL 解码质量和时间语义必须可复现

同一平台/native artifact、ACL 资源、Rig/reference、固定质量数据集合、采样策略和 effective time MUST 产生一致的采样数值。正式 Clip 播放范围与压缩数据覆盖范围 MUST 分别保存；有限动作末端、循环端点和任意时刻采样 MUST 遵守已编译时间合同，不得只按压缩样本数推导 Clip 时长。质量验证 MUST 覆盖重采样和压缩两段误差，MUST 不通过最终 Pose 低通、Foot Placement、IK 或匿名校正掩盖问题。

#### Scenario: 相同输入重复采样

- **WHEN** 相同资源在相同平台、Rig/reference、固定质量集合、effective time 和解码设置下重复采样
- **THEN** 变换与标量数值 MUST 一致，completion、Frame lease 与 source identity MUST 各自匹配本次请求
- **AND** Source MUST 不因诊断开关、采样顺序或缓存命中改变数值；不同帧或 Actor MUST 不因此复用过期生命周期身份

#### Scenario: 正式播放范围短于压缩样本覆盖范围

- **WHEN** 某资源的正式 stop time 早于压缩数据最后一个样本
- **THEN** source MUST 以正式时间合同定位末帧或循环边界
- **AND** MUST 不延长 Action、改变 Transition 时机或把尾部额外样本当成正式播放时间

#### Scenario: 资源版本支持范围外

- **WHEN** 运行时遇到未声明支持的 ACL 格式版本或轨道类型
- **THEN** Source MUST 返回 Invalid 及格式原因
- **AND** MUST 不尝试按另一版本或另一轨道布局解释数据

### Requirement: 默认子轨道必须有明确来源且覆盖完整输出

资源 MUST 为 default 或 constant 子轨道声明完整值来源，reference/default 身份 MUST 与 Rig 和 binding 一起验证。解码 MUST 为每个必需分量生成本次合法值，MUST 不依赖输出页中的上一帧残留。显式默认值属于压缩格式合同，MUST 不被当成资源错误时的备用姿态。

#### Scenario: 一个关节的平移使用reference默认值

- **WHEN** 资源声明该平移子轨道为 default，且 reference identity 匹配
- **THEN** 解码 MUST 使用声明的 reference 平移并正常完成该分量
- **AND** reference 缺失或不匹配时 MUST Invalid，不得改用零平移或历史值

### Requirement: 质量流入必须在采样前形成稳定租约

资源 MUST 声明所需质量 tier/chunk；只有该集合完整并在当前采样期间被保留时才可 Ready。底层解码器允许缺数据库或少 tier 时解码的能力，MUST 不成为项目的隐式降质路径。资源管理 MUST 在安全边界推进流入和流出；stream-out MUST 不与使用相同数据的解码并发。

#### Scenario: 所需bulk只完成部分加载

- **WHEN** 低层解码器能够用已有关键帧采样，但正式所需 bulk 尚未完整到位
- **THEN** 项目 source MUST 保持 Pending
- **AND** MUST 不把较低质量的 Pose 标记为正式 Ready

#### Scenario: 两个Actor共享同一数据库

- **WHEN** Actor A 的 source 获得退休许可，而 Actor B 仍持有相同数据库的使用租约
- **THEN** 资源管理 MUST 只释放 A 的 source 使用关系
- **AND** 数据库、streamer 和 B 需要的 payload MUST 保持有效

### Requirement: ACL 运行事实必须只读且不改变正式播放

Runtime MAY 发布 ACL 资源版本、格式、压缩字节、resident 字节、stream 状态、Context identity、effective time、解码耗时、采样轨道数量、completion 和错误身份。Diagnostics 与性能采集 MUST 只读取已提交的 Source 结果，不得参与资源选择、采样、Transition、Foot、IK、Goal 或释放决策；关闭采集 MUST 不改变正式结果。

#### Scenario: 采集 ACL 资源事实

- **WHEN** 当前 Frame 成功 Seal 且存在匹配 diagnostics interest
- **THEN** Projector MUST 发布与同一 Frame、Projection、Rig 和 SourceGeneration 对齐的 ACL 只读事实
- **AND** 事实 MUST 不包含 Pending Context 或下一帧的未提交数据

#### Scenario: 关闭 ACL 诊断

- **WHEN** ACL 诊断 interest 被关闭
- **THEN** Runtime MUST 跳过对应的诊断复制和事件发布
- **AND** ACL 资源准备、解码 Pose、Transition、IK 与 Final Publication MUST 保持不变
