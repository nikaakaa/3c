## MODIFIED Requirements

### Requirement: Definition Build必须精确消费Artifact并发布Projection

Definition Build MUST收集全部可达直接Clip Binding、Blend Space Dynamic Sample与有限Action AnimationClip引用，按精确`AnimationClip + Analysis Source + Geometry Validation` identity读取并校验已经显式生成的Artifact，再把普通Foot Feature按stable source binding嵌入CharacterPresentationProjection。相同AnimationClip MAY复用一次artifact读取，但每个source usage MUST保持独立identity。Locomotion Sync Group成员还 MUST使用Artifact校验Phase接触侧、实际source coverage和每条可达PoseState relation的脚部相容性。任一Artifact缺失、损坏、Calibration revision不匹配、Geometry Validation过期或Phase关系质量失败 MUST阻止Projection发布。Definition Build MUST不现场运行Analyzer、不生成Artifact，也不得发布Sampling Rig、Preview Clip对象或pairwise warp payload。

#### Scenario: Artifact Ready但Phase接触侧相反

- **WHEN** Clip的Phase整数声明右脚接触但Artifact显示右脚未Plant且左脚Plant
- **THEN** Definition Build MUST报告精确Clip、Phase和接触侧冲突
- **AND** MUST不发布Phase plan或修正Curve

#### Scenario: Artifact完整匹配

- **WHEN** Artifact payload、Calibration revision、Geometry Validation identity与Phase关系质量全部匹配
- **THEN** Definition Build MAY复用该payload而不重新采样AnimationClip
- **AND** Projection MUST发布精确validation identity供Runtime create核对

## ADDED Requirements

### Requirement: Foot Analysis必须生成可显式应用的Locomotion Phase候选

Editor-only Analysis工具 MAY从左右脚稳定Plant onset生成一条Locomotion Phase候选Curve。候选 MUST携带Artifact identity/content hash、AnimationClip dependency、Analysis Source、Sampling Rig、Calibration、采样参数、Clip实际coverage与左右脚接触语义，并保持session-local只读。作者显式Apply时 MUST重新校验全部输入，并通过AnimationClip注册Curve正式Mutation替换`presentation.locomotion-phase`完整Curve。Artifact、Profile、Timeline与Projection MUST不保存候选副本。

#### Scenario: 作者应用RunLoop Phase候选

- **WHEN** RunLoop候选未过期且左右脚接触顺序合法
- **THEN** Apply MUST把完整Phase Curve写入精确原生AnimationClip并进入一个Undo事务
- **AND** MUST标记Projection stale而不自动Build

#### Scenario: 候选显示后Clip发生变化

- **WHEN** AnimationClip dependency或Artifact identity在Apply前变化
- **THEN** Apply MUST返回Stale并拒绝写入
- **AND** MUST不按旧frame或旧接触样本继续生成Curve

### Requirement: Foot Analysis必须校验可达Locomotion关系质量

Projection Build MUST只针对PoseState实际可达relation和Gameplay committed clock解析的实际coverage执行质量校验。校验 MUST覆盖Phase整数/半整数接触侧、有限source终点双脚Plant差异、脚底位置/高度/速度差异、Transition可见窗口coverage、inverse斜率与跨cycle展开。门槛 MUST属于versioned compiler algorithm，不得成为Transition可调参数。失败 MUST产生稳定typed diagnostic并阻止发布，MUST不搜索其它Phase、禁用同步或使用旧plan。

#### Scenario: MovingTurn只使用前28帧

- **WHEN** MovingTurn Clip总长71帧但正式Gameplay clock只覆盖0至28帧
- **THEN** 质量校验 MUST只把0至28帧作为source实际coverage
- **AND** MUST不使用28帧后的接触样本证明出口相容

## REMOVED Requirements

### Requirement: Foot Analysis必须生成可校验的接触Marker候选

该Requirement被Locomotion Phase候选与关系质量校验取代；Foot Analysis不再生成或写入Point Marker。

#### Scenario: 旧Marker候选被请求

- **WHEN** Editor或Document请求生成LeftFootContact或RightFootContact Marker occurrence
- **THEN** capability校验 MUST拒绝该请求
- **AND** MUST不创建Timeline、Profile或Sequence Marker
