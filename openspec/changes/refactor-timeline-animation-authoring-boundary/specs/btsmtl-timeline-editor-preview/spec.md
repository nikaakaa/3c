## REMOVED Requirements

### Requirement: Timeline Editor必须只读显示Projection Foot Analysis

**Reason**：生成Foot Analysis不是Timeline作者内容，不应作为每条AnimationTrack的常驻lane，也不应要求Definition/Projection context。

#### Scenario: 删除旧Foot Analysis lane

- **WHEN** 新Timeline Animation Authoring Surface安装
- **THEN** 每Track的FOOT ANALYSIS header与四组metric lane MUST删除
- **AND** MUST不保留隐藏开关或兼容显示路径

### Requirement: Timeline Foot Analysis状态必须来自正式Build identity

**Reason**：Timeline按需工具只检查单clip artifact；Projection发布状态属于Profile/Definition Inspector，两个状态必须分开。

#### Scenario: 单Clip artifact与Projection状态不同

- **WHEN** artifact Ready但Projection Stale
- **THEN** 两个UI MUST分别表达各自状态
- **AND** Timeline MUST不把ProjectionRevision显示为artifact revision

### Requirement: Timeline重建Foot Analysis必须委托正式Definition Build

**Reason**：局部动画分析不需要Tree、Program或完整Projection；旧Rebuild入口造成无关重编译。

#### Scenario: 删除完整Definition Rebuild入口

- **WHEN** 作者在Timeline分析当前AnimationClip
- **THEN** Rebuild Selected Clip MUST只调用artifact builder
- **AND** Timeline MUST不调用CharacterSimulationProgramBuildService.Build

## ADDED Requirements

### Requirement: Timeline Animation Analysis必须是按需领域工具

Timeline窗口 MAY通过显式Character Editor provider提供Animation Analysis面板。面板 MUST默认关闭，不占Track行；打开后 MUST显式显示当前AnimationClip、Analysis Source、artifact状态、Left/Right选择与单一metric选择。生成曲线 MUST只读且不得进入Timeline selection、Undo或Curve Channel Catalog。

#### Scenario: 查看WalkLoop脚分析

- **WHEN** 作者选中WalkLoop AnimationClip、选择匹配Analysis Source并打开Analysis
- **THEN** 面板 MUST允许选择一只脚和一个metric查看
- **AND** Timeline主时间轴 MUST不增加Sole Speed、Height、Plant或Landing行

#### Scenario: 未选择Analysis Source

- **WHEN** 独立Timeline打开Analysis但没有显式Source
- **THEN** 面板 MUST显示Analysis Source Required
- **AND** MUST不搜索引用该Timeline的Definition或Graph

### Requirement: Timeline Analysis工具不得伪造Foot Placement世界

Animation Analysis面板 MUST只显示离线AnimationClip局部特征。它 MUST不执行PhysicsScene查询、Foot Lock、Ground Envelope、Pelvis、Final IK或Camera，不得把离线plant confidence显示为Gameplay contact。

#### Scenario: 预览Attack动画

- **WHEN** 作者查看Attack的plant或landing metric
- **THEN** 面板 MUST明确数据属于动画局部分析
- **AND** MUST不显示虚构地面、锁脚或运行时IK结果

### Requirement: Timeline Analysis必须显示并显式应用脚接触候选

Animation Analysis面板 MUST在artifact Ready时显示左右脚contact候选及目标frame，并在主时间轴正式Marker写入前要求作者确认。候选显示 MUST不改变Track高度、hit test或selection；Apply MUST重新校验candidate revision并使用Timeline正式mutation。

#### Scenario: 应用WalkLoop候选

- **WHEN** 作者选择完整覆盖MarkerGroup/Cyclic AnimationTrack的WalkLoop Clip并确认Apply
- **THEN** 面板 MUST把当前未过期候选写为正式LeftFootContact与RightFootContact Marker
- **AND** 现有非脚步Marker MUST保持不变

#### Scenario: 多Clip Track请求自动应用

- **WHEN** 一个AnimationTrack包含多个AnimationClip或当前Clip没有完整覆盖producer Timeline
- **THEN** 面板 MUST显示候选但禁用Apply并说明producer级映射不唯一
- **AND** MUST不选择权重最高Clip或按名称猜测Marker来源
