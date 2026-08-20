# character-animation-foot-analysis-artifact Specification

## Purpose
定义单AnimationClip脚分析的Editor-only规范产物、精确缓存身份、Definition消费与Runtime Projection边界。

## Requirements

### Requirement: Animation Foot Analysis必须拥有Editor-only规范产物

Animation Foot Analysis MUST为`AnimationClip imported content + Rig Definition v3 + Sampling Rig prefab + Rig Calibration + Geometry Validation Result + Analysis Settings + Analyzer Version`生成不可变Editor-only规范Artifact。Artifact MUST保存上述输入的stable identity、revision、hash、采样域、每脚连续feature channel与接触Marker候选；artifact identity MUST包含format version、AnimationClip GUID与import dependency、Analysis Source GUID/identity/version、Rig Definition v3 identity/revision/hash、Sampling Rig GUID/dependency、Rig Calibration identity/revision、Geometry Validation identity/hash、sample rate、threshold、reduction与algorithm version。Artifact MUST写入固定`Library`存储根，不得进入Assets、Player、Addressables、YooAsset、Program、Snapshot或Network产物，也不得写回AnimationClip、Rig、Calibration、Timeline或Profile。相同输入 MUST产生相同artifact identity与规范payload。

#### Scenario: Calibration几何改变

- **WHEN** Heel、Toe、Sole Frame、Preferred Bend或Calibration Preview输入使Geometry Validation identity改变
- **THEN** 旧artifact MUST变为Stale
- **AND** Analyzer MUST不因Calibration revision字符串仍可解析而继续使用旧feature

#### Scenario: 同一合法输入重复分析

- **WHEN** 相同AnimationClip、Analysis Source和Geometry Validation identity重复构建
- **THEN** 系统 MUST产生相同canonical payload与artifact hash
- **AND** Store MUST解析到同一规范identity

#### Scenario: AnimationClip重新导入

- **WHEN** AnimationClip GUID不变但import dependency改变
- **THEN** expected artifact identity MUST改变并把旧artifact判为Stale
- **AND** MUST不因clip名称、duration或GUID仍相同而继续使用旧数据

#### Scenario: Rig腿链改变

- **WHEN** Rig v3的ankle或toe BoneId、revision或content hash改变
- **THEN** 旧artifact MUST变为Stale
- **AND** Analyzer MUST不使用Sampling Rig旧Transform映射继续发布

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

正式Analyzer MUST只接受精确AnimationClip、Rig Definition v3、Sampling Rig、Rig Calibration、Analysis Settings与Analyzer Version。它 MUST通过Rig v3 Physical BoneId绑定Sampling Rig Transform并执行独立PlayableGraph sampling，不得读取Tree、StateMachine、Timeline call site、CharacterPipelineDefinition、Profile runtime、PresentationProjection、CharacterPipelineHost、当前Scene或Transform名称。Analyzer MUST生成左右脚有限feature curve set，先写完全部采样帧的heel、toe、sole位置与高度，再从完整循环位置序列计算中心差分速度，不得在未来采样帧尚未写入时读取它。Sampling Rig、Rig与Calibration identity/revision/hash不一致 MUST明确失败。

#### Scenario: 从独立Timeline分析Clip

- **WHEN** 作者选择AnimationClip与合法Analysis Source并执行Rebuild Selected Clip
- **THEN** Analyzer MUST只使用Analysis Source提供的Rig v3、Sampling Rig与Calibration生成或更新对应artifact
- **AND** MUST不执行Authoring Discovery、Semantic compile、Numeric lowering或完整Projection Build

#### Scenario: Sampling Rig Calibration不匹配

- **WHEN** Calibration声明的rig identity与Analysis Source的Rig v3或Sampling Rig不一致
- **THEN** Analyzer MUST拒绝生成Artifact并报告Rig、Sampling Rig与Calibration三方identity/revision/hash
- **AND** MUST不尝试按骨骼名称重绑或搜索其它Prefab补全

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

Definition Build MUST收集全部可达直接Clip Binding、Blend Space Dynamic Sample与有限Action AnimationClip引用，按精确`AnimationClipAnalysisInputHash + Analysis Source + Rig + Sampling Rig + Calibration + Geometry Validation` identity读取并校验已经显式生成的Artifact，再把普通Foot Feature按stable source binding嵌入CharacterPresentationProjection。完整Clip dependency与Registered Curve Hash MUST作为Projection依赖单独校验，不得进入Analysis Input Hash。相同AnimationClip MAY复用一次artifact读取，但每个source usage MUST保持独立identity。Locomotion Sync Group成员还 MUST使用Artifact的Editor-only Phase Validation Descriptor校验Landing/Plant onset、实际source coverage和每条可达PoseState relation的脚部相容性。任一Artifact缺失、损坏、Calibration revision不匹配、Geometry Validation过期或Phase关系质量失败 MUST阻止Projection发布。Definition Build MUST不现场运行Analyzer、不生成Artifact，也不得把Sampling Rig、Preview Clip对象、Phase Validation samples或pairwise warp payload发布进Projection。

#### Scenario: Artifact Ready但Phase接触侧相反

- **WHEN** Clip的Phase整数inverse时间没有与右脚Landing/Plant onset对齐，或对应时刻左右脚接触顺序相反
- **THEN** Definition Build MUST报告精确Clip、Phase、onset时间与接触语义冲突
- **AND** MUST不发布Phase plan或修正Curve

#### Scenario: Artifact完整匹配

- **WHEN** Artifact payload、Calibration revision、Geometry Validation identity与Phase关系质量全部匹配
- **THEN** Definition Build MAY复用该payload而不重新采样AnimationClip
- **AND** Projection MUST发布精确validation identity供Runtime create核对

### Requirement: Player Runtime必须只消费Projection

Player Runtime MUST只从与Program和producer binding匹配的CharacterPresentationProjection读取生成feature。Runtime MUST不读取Library artifact、Analysis Source、Sampling Rig、AssetDatabase或Editor Analyzer，也不得在feature缺失时即时分析AnimationClip。

#### Scenario: Library缓存被删除

- **WHEN** Editor Library artifact在Player构建后被删除
- **THEN** 已发布Player MUST继续只使用Projection运行
- **AND** Runtime行为 MUST不依赖Editor cache存在

### Requirement: Foot Analysis必须生成可显式应用的Locomotion Phase候选

Editor-only Analysis工具 MAY从左右脚稳定Landing/Plant onset生成一条Locomotion Phase候选Curve。候选 MUST携带Artifact identity/content hash、AnimationClip Analysis Input Hash、候选生成时的Registered Curve Hash、完整Clip dependency baseline、Analysis Source、Sampling Rig、Calibration、采样参数、Clip实际秒域coverage与左右脚接触语义，并保持session-local只读。作者显式Apply时 MUST重新校验全部输入，并通过AnimationClip注册Curve正式Mutation替换`presentation.locomotion-phase`完整秒域Curve。Artifact、Profile、Timeline与Projection MUST不保存候选副本。

#### Scenario: 作者应用RunLoop Phase候选

- **WHEN** RunLoop候选未过期且左右脚接触顺序合法
- **THEN** Apply MUST把完整Phase Curve写入精确原生AnimationClip并进入一个Undo事务
- **AND** MUST标记Projection stale而不自动Build或使Analysis Input Hash对应Artifact stale

#### Scenario: 候选显示后Clip发生变化

- **WHEN** AnimationClip dependency baseline、Analysis Input Hash、Registered Curve Hash或Artifact identity在Apply前变化
- **THEN** Apply MUST返回Stale并拒绝写入
- **AND** MUST不按旧frame或旧接触样本继续生成Curve

### Requirement: Foot Analysis必须校验可达Locomotion关系质量

Projection Build MUST只针对PoseState实际可达relation和Gameplay committed clock解析的实际秒域coverage执行质量校验。校验 MUST覆盖Phase整数/半整数与左右脚Landing/Plant onset的时间误差、对侧脚状态、接触顺序、有限source终点双脚Plant差异、脚底位置/高度/速度差异、Transition可见窗口coverage、inverse斜率与跨cycle展开。门槛 MUST属于versioned compiler algorithm，不得成为Transition可调参数。失败 MUST产生稳定typed diagnostic并阻止发布，MUST不搜索其它Phase、禁用同步或使用旧plan。

#### Scenario: MovingTurn只使用前28帧

- **WHEN** MovingTurn Clip总长71帧但正式Gameplay clock只覆盖0至28帧
- **THEN** 质量校验 MUST只把0至28帧作为source实际coverage
- **AND** MUST不使用28帧后的接触样本证明出口相容

### Requirement: Foot Analysis必须发布最小Editor-only Phase Validation Descriptor

Foot Analysis Artifact MUST用`AnimationFootPhaseValidationDescriptor`原子取代旧`AnimationFootSynchronizationDescriptor`。新Descriptor MUST只保存按规范化素材时间排序的左右脚root-local平面位置、calibrated height、local velocity、Plant confidence与Landing/Plant onset事件，以及匹配的Analysis Input Hash；MUST不保存pairwise relation、leader、warp knot、Marker occurrence或Runtime cursor。Descriptor MUST只供Phase候选和Projection Build质量校验读取，MUST不进入Presentation Projection、Player、Snapshot、Replay或Runtime。

#### Scenario: Projection Build校验有限出口

- **WHEN** Compiler校验MovingTurn有限出口与RunLoop候选Phase
- **THEN** Compiler MUST从匹配Analysis Input Hash的Phase Validation Descriptor读取双脚位置、高度、速度与onset
- **AND** MUST不重新运行Analyzer或读取旧pairwise warp descriptor

#### Scenario: Runtime加载Projection

- **WHEN** Player加载已通过质量门槛的Presentation Projection
- **THEN** Projection与Runtime MUST不包含Phase Validation samples
- **AND** Runtime MUST只消费编译后的Phase plan与Artifact validation identity
