## ADDED Requirements

### Requirement: Pose Graph必须显式支持BlendSpacePlayer

Character Presentation Pose Graph的正式Runtime节点目录 MUST加入`BlendSpacePlayer`。该节点 MUST位于Selection/Parameter与Pose composition之间，并由Compiler降低到SourceAndNativePose阶段。GraphInput/GraphOutput仍只用于subgraph编译边界，Runtime MUST不在图外创建隐藏Blend Space player。

#### Scenario: 编译Locomotion表现分支

- **WHEN** 图包含`AnimationSelectionInput -> MarkerSync -> BlendSpacePlayer -> Inertialization`
- **THEN** Compiler MUST生成固定Selection、source sample和local continuity执行顺序
- **AND** Runtime MUST不追加隐藏SelectedPosePlayer或BlendStack

### Requirement: BlendSpacePlayer必须使用typed参数端口

BlendSpacePlayer MUST使用现有typed Program Parameter体系读取一维X或二维X/Y输入。Compiler MUST根据全部可达BlendSpace source的编译轴合同确定端口数量、ParameterId、类型与单位；未知、缺失、多余或不一致连接 MUST失败。节点 MUST不按显示名、Gameplay State、Blackboard key或字符串查询参数。

#### Scenario: 二维节点缺少Y参数

- **WHEN** 可达资产为FreeformDirectional2D但节点只连接X Parameter
- **THEN** Validator MUST拒绝图并定位NodeId与缺失ParameterId

### Requirement: Pose Graph diagnostics必须解释Blend Space贡献

Pose Graph Preview、Pose Watch与Live Details MUST从匹配Projection revision的正式source map显示BlendSpacePlayer的AssetId、axis value、active SampleId、normalized weight、canonical phase、effective time、Pose availability和feature contribution。Diagnostics MUST不从authoring asset或Animancer状态重新计算这些值。

#### Scenario: 运行时观察BlendSpacePlayer

- **WHEN** 作者选择正在运行的BlendSpacePlayer节点
- **THEN** Live Details MUST显示Runtime Snapshot中的实际样本贡献
- **AND** revision不匹配时 MUST明确标记Unavailable

