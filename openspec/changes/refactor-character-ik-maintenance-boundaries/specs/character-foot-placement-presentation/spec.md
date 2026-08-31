## RENAMED Requirements

- FROM: `### Requirement: Pelvis必须只消费Resolved Foot Pair`
- TO: `### Requirement: Pelvis必须只消费typed脚需求并返回可达裁决`

## MODIFIED Requirements

### Requirement: Resolved Foot必须形成紧凑下游合同

`CharacterResolvedFootResult` MUST只表示当前Foot流程完成既有Landing判定与Reach限制后的最终Goal输入。它 MUST发布下游实际消费的Frame、Completion、Rig、Side、Final Sole/Ankle/Rotation、有效Sole/Ankle/Rotation、最终Correction、作者位置/旋转权重、Contact Reference与Ownership、Support Eligibility、Support Intent与Weight、Support Error、Event lineage、typed Reach Outcome和Outcome。未完成Pelvis及Foot Reach裁决的需求 MUST使用不同的内部typed请求，不得复用最终Resolved类型或名字。

最终Resolved Pair MUST只组合同Frame、Completion与Rig的两脚结果，不重新选择State、Support、Reach或Goal。内部State、Transition Decision、Path、Anchor历史与Interpolation过程 MUST不进入最终下游合同。Primary Support与Pelvis MUST只消费本模块内部的typed请求视图；Goal编码 MUST只读取最终Resolved与Pelvis Result，不得在编码后再夹紧业务目标。

最终Sole、Ankle、Rotation、有效目标与Correction MUST来自同一最终目标，复用正式Foot/Heel/Toe几何和权重规则。未加权Goal、加权目标与实际Solved/Physical Pose MUST保持不同含义，不得把最终Goal输入称为已写入的物理脚底。原请求因Reach不可达而被限制时 MUST保留请求和裁决证据，不以最终合法位置掩盖原请求失败。

#### Scenario: 脚需求尚未完成骨盆裁决

- **WHEN** Foot已完成本帧目标与Interpolation但Pelvis及Reach尚未处理
- **THEN** Foot MUST只产生内部typed脚需求和完成凭据，不发布最终Resolved
- **AND** 根Runtime与Goal消费者 MUST不能取得这份未完成结果作为正式输出

#### Scenario: 原目标不可达而最终输出被夹紧

- **WHEN** 既有政策要求保护Primary Support并限制另一脚的输出位置
- **THEN** 唯一Foot收口 MUST完成限制并由同一目标生成最终Resolved的关联几何字段
- **AND** Goal编码 MUST直接表达这个最终目标，不再修改它
- **AND** 原请求未满足的Outcome MUST继续阻止该脚进入Full Lock

#### Scenario: 正常输出保持

- **WHEN** 原Foot请求已被既有Pelvis和Reach政策满足
- **THEN** 分型迁移 MUST保持Goal的位置、旋转、权重和原连续性处理
- **AND** MUST不新增一次Interpolation、Pelvis响应或FBBIK

### Requirement: Pelvis必须只消费typed脚需求并返回可达裁决

Primary Support MUST只读取同Frame、Completion、Rig与Side的typed请求中正式Support Eligibility、Support Intent、Support Error、Event lineage与Pelvis Reach Reference。正式Support为零或Reference无效时 MUST按现有业务发布不可用，不得按相对权重归一制造支撑。Contact Reference、Pelvis Reach Reference和Landing Reach Request MUST保持独立含义。

Pelvis MUST只消费请求中所需的目标与Reach视图、Primary Support Result、同帧动画/Body输入和显式设置，不得读取Foot State、Lock Mode、Anchor历史、Path Residual、Interpolation内部状态或Diagnostics。请求的未加权与有效目标 MUST明确分型，权重不得重复应用。

Pelvis MUST继续使用用户接受的唯一目标、姿态偏好、双腿硬区间和一次Response实现，返回Pelvis Result与每脚typed Reach Outcome。最终Foot Lifecycle仍 MUST拥有状态写入，Foot收口仍 MUST拥有既有脚目标限制；Pelvis不得直接修改Foot状态或提前发布最终Resolved。已采用的Primary安全政策、不可达处理、作者权重与Full Lock准入 MUST保持不变，不增加第二种响应或自动调参。

Reach Outcome MUST只作为现有Transition Resolver的准入输入，State仍由唯一Transition Runtime更新。Pelvis和Hard Constraint MUST不能直接反写离散State或形成第二份Transition判定。

#### Scenario: 下游选择Support

- **WHEN** Primary Support收到合法的两脚请求
- **THEN** 它 MUST仅按请求的正式Support与Event字段执行原有获取/保留选择
- **AND** MUST不读取Foot State、Lock Mode或Interpolation历史

#### Scenario: 双腿可达裁决返回Foot

- **WHEN** 唯一Pelvis处理完成本帧双腿可达性判断
- **THEN** 它 MUST返回与原请求身份一致的每脚Outcome，区分请求满足与请求不可达
- **AND** Foot Lifecycle MUST按原政策消费该Outcome完成准入，随后发布最终结果

#### Scenario: 请求身份混杂

- **WHEN** 请求、Primary Result或Reach Outcome来自不同Frame、Completion、Rig、Side或Event
- **THEN** 本帧 MUST在正式发布前拒绝
- **AND** MUST不借用上一帧结果、默认脚需求或另一只脚的裁决补全

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime运行历史、内部typed请求、最终结果与只读过程证据 MUST严格分型。运行Owner MUST在计算时捕获本帧实际发生的证据；Diagnostics MUST在唯一根事务内从同一Pending的Observation、请求、最终Foot/Pelvis结果与后续阶段结果完成固定容量冻结和验证。Writer成功时 MUST仅补入同Completion的实际写入事实，Seal后消费者 MUST只读取Committed页，不延迟重算Foot业务。

响应、Contact、Support与Reach过程证据 MAY按业务分组保存，但 MUST不在多份记录中维护同义平铺真相。Gizmo、CSV、Trace与Pose Watch MUST不得查询世界、选择Support、生成Goal、执行FBBIK或改写运行历史。Diagnostics布局与显示兴趣 MUST不改变Runtime输出，公开Diagnostics不得被读取为下一帧状态。

#### Scenario: 捕获正式Foot事实

- **WHEN** Foot、Pelvis、Goal、FBBIK和Pending Pose准备进入正式Writer
- **THEN** 已完成阶段的输入、请求、Transition、连续Correction、Reach、最终Resolved与Solved证据 MUST属于同一Pending帧并已完成冻结校验
- **AND** Writer完成后Physical事实 MUST补入同一页，Seal后才作为Committed结果发布

#### Scenario: 增加响应解释字段

- **WHEN** 仅增加本帧响应原因或前后数值的诊断记录
- **THEN** 运行状态、脚目标、Pelvis、Bend与最终骨骼 MUST保持不变
- **AND** MUST不要求修改Goal Assembler、Solver算法或质量评分政策

## ADDED Requirements

### Requirement: Foot运行历史不得借用过程证据保存

下一帧必须读取的方向、响应、残差和有效性 MUST保存在固定布局typed运行状态中，每项字段具有唯一写入Owner及明确初始化/Reset语义。过程Fact MUST只表达本帧前值、采用值、结果与理由，不能成为隐藏的跨帧状态容器。

全部状态和证据 MUST仍属于同一根Bank；拆分不得创建独立Committed/Pending生命周期、全局缓存、字符串状态Key或新的外部可变Context。Pending事务开放与Committed结果可读 MUST分别判断，不得以已经关闭的Pending标志否定正式历史。其它active拥有的连续性参考不得由本change新增旁路消费。

#### Scenario: 从过程记录移出上一帧方向

- **WHEN** Interpolation需要上一帧实际应用方向限制本帧方向变化
- **THEN** 它 MUST读取唯一正式方向历史并由同一Owner写入Pending新值
- **AND** 诊断Fact只能单向记录该变化，删除诊断投影不得改变方向计算

#### Scenario: 后续阶段丢弃本帧

- **WHEN** Pending Foot历史已更新但完整帧未成功Seal
- **THEN** 新历史与过程证据 MUST共同丢弃，Committed历史保持上一成功帧
- **AND** 任一内部记录 MUST不能单独提交或从未提交Fact恢复状态

### Requirement: Foot采样读写必须由唯一typed列绑定描述

当前Foot采样格式 MUST由Editor唯一有序typed列绑定声明名称、类型、单位、业务分组、有效性和读写映射；Header、写行、Analyzer读取和必需列校验 MUST使用同一绑定。相同列名不得重复声明，位置写入与列名解释不得分别维护互不关联的清单。格式identity MUST来自唯一正式定义。

绑定及索引 MUST在明确初始化阶段验证和缓存，不在OnInspectorGUI进行重操作。Runtime MUST不读取列名、反射或采样Dictionary。原始主行与大几何表 MUST继续沿唯一采样链分别发布；搬运映射不得执行第二份Foot数学或生成评分。

字段布局或含义变化 MUST显式升级版本，缺列、重复列、非法类型或不匹配版本 MUST拒绝，不建立旧reader、别名或默认值补全。历史原包及其旧结果 MUST保留为证据，不自动覆盖或用新语义重新解释。现有评分维度、权重、分母和Unavailable规则 MUST保持原Owner。

#### Scenario: 新增普通证据列

- **WHEN** 当前版本新增一个正式响应证据字段
- **THEN** 列名、写值位置、typed读取和必需列校验 MUST由同一绑定得到
- **AND** 不改变质量规则时 MUST不新增评分Target或修改Publisher业务规则

#### Scenario: 列绑定不完整或重复

- **WHEN** 当前格式存在重名、缺失typed读写绑定或类型不一致
- **THEN** 初始化 MUST明确失败，不开始生成看似合法的采样文件
- **AND** MUST不靠空值、零值或忽略该列继续运行

#### Scenario: 读取旧语义Resolved列

- **WHEN** 原包的Resolved字段表示Reach裁决前请求，而当前合同要求裁决后最终目标
- **THEN** 当前读取器 MUST报告版本或语义不匹配
- **AND** MUST不把旧列改名补成最终结果，原包保持不变
