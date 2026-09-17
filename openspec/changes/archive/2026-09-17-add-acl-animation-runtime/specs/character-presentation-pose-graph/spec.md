## MODIFIED Requirements

### Requirement: Pose参数必须通过typed页面和显式解析传播

Pose Graph MUST 声明稳定 ParameterId、类型、单位、默认值与允许来源。ProgramParameterInput MUST 只读取 committed parameter page；source-local curve 参数 MUST 随 Pose Value 传播；PoseParameterResolve MUST 按显式 Base、Overlay、Weighted、Max、Min 规则合成。节点 MUST 不按字符串、GameplayTag 或 State 显示名查找参数。

Graph 参数声明 MUST 显式区分既有控制用途与动画属性用途；Presentation Profile MUST 只将动画属性参数映射到明确 Renderer/Mesh/BlendShape，不复制参数默认值或曲线内容作为第二 authoring 真相。Compiler MUST 将同一声明、source scalar binding、属性 target 与容量编入同一 Projection/Program。运行时属性 MUST 使用现有 typed 参数页和混合 owner；动画属性 MUST 不自动成为 Gameplay、Foot 或 Phase 输入。

Layered Bone Blend 与 Additive MUST 保留现有 Base 参数传播，骨骼 Mask MUST 不自动修改参数。需要不同曲线来源时 MUST 显式使用 Parameter Resolve；不得自动补 ALS 式末端覆盖节点或由 decoder 决定参数来源。状态过渡、Blend Space 和 BlendStack MUST 继续按各自既有参数语义处理数值。

#### Scenario: Blend权重读取Program参数

- **WHEN** BlendPose 权重连接 ProgramParameterInput
- **THEN** Compiler MUST 校验 ParameterId、类型和 page layout
- **AND** Runtime MUST 不读取 Gameplay 对象

#### Scenario: 为动画片段声明面部形变参数

- **WHEN** Graph 声明一个动画属性参数，Profile 将它映射到明确的模型形变
- **THEN** Build MUST 将参数、source 轨道和目标 binding 降低为同一 dense 合同
- **AND** 参数曲线仍只由正式 AnimationClip 拥有，Runtime MUST 不读取作者显示名

#### Scenario: 骨骼分层后需要保留另一分支曲线

- **WHEN** 作者显式选择另一份 parameter-source-pose 和 Overlay 规则
- **THEN** Parameter Resolve MUST 保留 base-pose 的骨骼，仅按规则解析参数值
- **AND** MUST 不重新混合骨骼或创建另一条动画播放链
