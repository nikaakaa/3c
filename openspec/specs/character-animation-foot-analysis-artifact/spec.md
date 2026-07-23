# character-animation-foot-analysis-artifact Specification

## Purpose
定义单AnimationClip脚分析的Editor-only规范产物、精确缓存身份、Definition消费与Runtime Projection边界。

## Requirements

### Requirement: Animation Foot Analysis必须拥有Editor-only规范产物

系统 MUST将单AnimationClip脚分析输出写为不可变Editor-only artifact。Artifact identity MUST包含format version、AnimationClip GUID与import dependency、Analysis Source GUID/identity/version、Sampling Rig GUID/dependency、Rig Calibration identity/revision、sample rate、threshold、reduction与algorithm version。Artifact MUST写入固定`Library`存储根，不得进入Assets、Player、Addressables、YooAsset、Program、Snapshot或Network产物。

#### Scenario: 同一输入重复分析

- **WHEN** 相同AnimationClip与相同Analysis Source重复构建
- **THEN** 系统 MUST产生相同canonical payload与artifact hash
- **AND** Store MUST解析到同一规范identity

#### Scenario: AnimationClip重新导入

- **WHEN** AnimationClip GUID不变但import dependency改变
- **THEN** expected artifact identity MUST改变并把旧artifact判为Stale
- **AND** MUST不因clip名称、duration或GUID仍相同而继续使用旧数据

### Requirement: Artifact Store必须精确校验并原子发布

Artifact Store MUST使用canonical codec、payload hash、临时文件与原子替换。Reader MUST拒绝未知版本、非法长度、非法枚举、NaN、Infinity、无序key、identity不匹配与hash不匹配，并分别报告Missing、Stale或Corrupt。Store MUST不提供旧格式reader、近似匹配或内存fallback。

#### Scenario: Artifact文件损坏

- **WHEN** 文件存在但payload hash不匹配
- **THEN** Store MUST报告Corrupt并拒绝返回feature
- **AND** Definition Build MUST不把它当作Ready或Missing静默重用

#### Scenario: 发布过程中失败

- **WHEN** 新artifact写入或校验失败
- **THEN** 旧完整artifact MUST保持不变
- **AND** Store MUST不留下可被Reader识别为Ready的部分文件

### Requirement: 单Clip Analyzer不得依赖Tree或Projection

正式Analyzer MUST只接受精确AnimationClip与Analysis Source，并由Source精确解析Sampling Rig与Calibration。它 MUST不接受或读取RootTree、StateMachine、Timeline call site、CharacterPipelineDefinition、SimulationProgram或PresentationProjection。Analyzer MUST使用精确Rig/Animator/Playable采样并生成左右脚有限feature curve set。Analyzer MUST先写完全部采样帧的heel、toe、sole位置与高度，再从完整循环位置序列计算中心差分速度，不得在未来采样帧尚未写入时读取它。

#### Scenario: 从独立Timeline分析Clip

- **WHEN** 作者选择AnimationClip与合法Analysis Source并执行Rebuild Selected Clip
- **THEN** Analyzer MUST生成或更新对应artifact
- **AND** MUST不执行Authoring Discovery、Semantic compile、Numeric lowering或完整Projection Build

#### Scenario: Sampling Rig Calibration不匹配

- **WHEN** Sampling Rig引用的Calibration与Analysis Source不一致
- **THEN** Analyzer MUST拒绝并报告两端identity
- **AND** MUST不搜索其它Prefab或默认Humanoid脚骨补全

### Requirement: Analyzer必须使用统一校准地面与heel/toe接触语义

Analyzer MUST从Sampling Rig与Runtime共享的Rig Calibration绑定姿势得到唯一脚底地面参考高度，并分别采样左右脚heel与toe。每脚高度 MUST取heel/toe最低接触点相对该统一地面的高度；sole轨迹 MAY使用heel/toe中点。Plant进入/退出速度 MUST只使用sole垂直速度，不得把InPlace动画的局部水平轨迹计入Plant速度，也不得把每个AnimationClip自身最低点重新定义为地面。Algorithm identity与artifact format MUST覆盖这些语义；旧算法artifact MUST判为Stale或未知版本并拒绝，不得兼容读取。

#### Scenario: 抬脚动画自身最低点仍高于地面

- **WHEN** 一个AnimationClip全程让该脚高于Calibration地面参考
- **THEN** Analyzer MUST保留真实离地高度并保持plant confidence为非接触
- **AND** MUST不把该clip最低采样点归零为地面

#### Scenario: InPlace Run包含局部水平脚步

- **WHEN** sole在VisualRoot局部空间高速前后摆动但垂直速度与高度满足Plant条件
- **THEN** Plant classifier输入 MUST只使用垂直速度与校准高度
- **AND** 水平速度 MUST继续保存在生成轨迹中供Runtime世界接触速度合成

### Requirement: Definition Build必须精确消费Artifact并发布Projection

Definition Build MUST收集全部可达stable Timeline/Track/Clip binding，按精确`AnimationClip + Analysis Source` identity校验或生成artifact，再把feature按每个stable clip binding嵌入CharacterPresentationProjection。相同AnimationClip MAY复用一次artifact读取，但每个binding MUST保持独立producer identity。任一artifact无效 MUST阻止本次Program/Projection发布。

#### Scenario: Artifact已提前生成

- **WHEN** 所需artifact为Ready且完整identity/hash匹配
- **THEN** Definition Build MAY复用该payload而不重新采样AnimationClip
- **AND** MUST仍验证其与当前binding、Source和Calibration匹配

#### Scenario: Artifact Ready但Projection过期

- **WHEN** 单clip artifact已重建但Definition尚未重新发布
- **THEN** Timeline Analysis工具 MUST显示Artifact Ready
- **AND** Definition/Profile Inspector MUST继续显示Projection Stale

### Requirement: Player Runtime必须只消费Projection

Player Runtime MUST只从与Program和producer binding匹配的CharacterPresentationProjection读取生成feature。Runtime MUST不读取Library artifact、Analysis Source、Sampling Rig、AssetDatabase或Editor Analyzer，也不得在feature缺失时即时分析AnimationClip。

#### Scenario: Library缓存被删除

- **WHEN** Editor Library artifact在Player构建后被删除
- **THEN** 已发布Player MUST继续只使用Projection运行
- **AND** Runtime行为 MUST不依赖Editor cache存在

### Requirement: Foot Analysis必须生成可校验的接触Marker候选

Ready artifact MAY按artifact sample rate从左右脚PlantConfidence推导离散contact onset候选。一个上升沿只有在其后的Plant状态连续维持至少Analysis Source `MinimumLandingSegmentSeconds`对应的循环样本数时才是稳定接触；单帧阈值穿越 MUST不产生候选。候选 MUST携带artifact identity与content hash、AnimationClip dependency、Timeline/Track/Clip stable identity、脚侧、源动画归一化时间、目标Timeline frame与置信值。候选 MUST是Editor session中的瞬时只读数据，不得写入artifact payload、Projection或Runtime。

#### Scenario: 循环步态生成左右脚候选

- **WHEN** Ready artifact的左右脚PlantConfidence在循环边界内分别发生非接触到稳定接触的转换
- **THEN** 系统 MUST按实际采样点生成LeftFootContact与RightFootContact候选
- **AND** MUST按ClipIn与源动画cycle映射到目标Timeline frame，不得假设frame 0或半周期

#### Scenario: 候选输入过期

- **WHEN** AnimationClip import dependency、Analysis Source、Sampling Rig、Calibration、采样参数、artifact hash或Timeline映射在候选显示后改变
- **THEN** Apply MUST重新解析并把旧候选判为Stale
- **AND** MUST不按旧frame、clip名称或缓存曲线继续写入Marker
