## MODIFIED Requirements

### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

正式窗口 MUST提供Definition-scoped Navigator、唯一原生图编辑区域、Details和可折叠观察区域，不保存第二份可编辑拓扑或选择集合。Details MUST分离Authoring、Live与References：Authoring只通过正式Presentation Mutation修改当前owner；Live只读取匹配角色实例、PoseGraphId、PoseGraphRevision与ProjectionRevision的已完成snapshot；References只读显示Source Slot、Profile binding子资产、实际资源、source map、Action producer、Rig、Policy和call site。稳定identity、GUID、revision、hash与compiled index MUST默认隐藏。Live观察模式下mutation MUST禁用，revision不匹配 MUST显示Stale并清空旧值。

#### Scenario: 查看Locomotion State
- **WHEN** 作者选中Locomotion State的Clip或BlendSpace Player
- **THEN** Authoring MUST显示类型匹配的Source Slot对象选择器
- **AND** References MUST显示解析后的Profile binding、实际资源、owner与Open Source命令
- **AND** MUST不显示BaseLocomotion Gameplay producer或可编辑Source Id

#### Scenario: Runtime revision不匹配
- **WHEN** snapshot revision与当前文档或Projection不一致
- **THEN** Live MUST显示Stale
- **AND** MUST不从authoring默认值或Animancer state伪造结果

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Pose Graph MUST继续编译为唯一不可变Program Image，正式Actor MUST使用既有Execution View、Source、Constraint、Final Publication、根Frame事务及actor-local状态。Pose窗口的Preview／Live MUST只表示观察Unity Play中已有Actor的实际结果，不创建Preview Runtime、角色、场景或时钟，也不要求独立Scene Play协调器才能观察。每帧source、Player、Action lifecycle、Transition、Slot、composition、转换、Goal、FBBIK和Writer MUST仍按正式计划执行；观察不得增加执行次数。Runtime MUST不依赖作者图对象，窗口不得启动作者图执行或自动Build。

#### Scenario: Graph修改后继续Preview
- **WHEN** 作者修改State、Slot、Rig、Pose空间、字段或Foot Placement使观察版本不匹配
- **THEN** 窗口 MUST停止旧结果叠加并提示显式Build需求，不热换或停止实际角色运行
- **AND** MUST不创建临时Program、旧ABI reader、默认空间转换或旧Projection fallback

#### Scenario: Preview缺少world context
- **WHEN** 所观察的真实Actor执行Foot Placement时精确World Context不可用
- **THEN** 正式Runtime MUST按原合同发布typed Unavailable并阻断对应Frame publication
- **AND** 窗口 MUST显示该结果，不补造地面、不跳过计算或另行执行简化Constraint

#### Scenario: 在普通游戏运行中打开窗口
- **WHEN** Unity已在Play且存在匹配的真实Actor
- **THEN** 窗口 MUST能绑定该实例观察，无需创建预览场景或再次启动Pose图

### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

Pose Graph、PoseStateMachine、Node、Port与Edge MUST只有一份正式作者数据。每个正式Node Kind MUST由唯一Definition声明Payload字段、固定／条件／动态端口、Graph Role、Execution Domain、Operation Family、Graph dependency与typed lowering；同一Capability和Port Shape MUST供原生端口注册、正式Document、Clipboard、Reconciler、Mutation预检与Validator使用。原生创建、连接、改接、删除、粘贴、名称、位置和字段修改 MUST进入同一typed Presentation Mutation及真实owner事务。Compiler MUST直接读取正式作者图，不创建旧作者图镜像或通过getter获取编译值。系统 MUST不保留第二节点目录、重复字段声明或独立Compiler binding真相；全局拓扑规则 MUST仍由唯一Topology Pass负责。

#### Scenario: 新增Pose节点能力
- **WHEN** 新Pose节点注册唯一Definition
- **THEN** 原生菜单、端口、正式Document、Clipboard、Validator、Graph Closure和Compiler MUST识别同一Capability与Payload合同
- **AND** MUST不在多个目录重复声明同一字段和端口

#### Scenario: Node Definition缺少Document投影
- **WHEN** 一个Definition无法提供完整typed字段、条件端口或Graph dependency
- **THEN** 目录或Build MUST失败并定位Node Kind
- **AND** MUST不使用通用SerializedProperty或自由文本绕过

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

UI MUST复用原生节点、端口和连接交互，保持Clip Player、Blend Space Player、Selected Pose Player、Animation State Machine、Slot、Layered Blend Per Bone、Inertialization、Locomotion Phase Group、Pose Watch和Output Pose等业务名称。序列化、Document、Mutation、Compiler source map与Diagnostics MUST使用一致的稳定身份；显示名称、颜色与布局变化不得暗改端口身份或Pose空间。MUST删除被替代的专用画布和重复端口绘制，不保留旧node kind兼容别名。

#### Scenario: 作者添加单Clip播放器
- **WHEN** 作者添加单AnimationClip state-local player
- **THEN** Capability、节点标题、Document kind和编译诊断 MUST统一显示Clip Player
- **AND** MUST不存在Clip Player兼容名称
