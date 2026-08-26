# character-animation-foot-analysis-artifact Specification

## Purpose
定义单AnimationClip脚分析的Editor-only规范产物、精确缓存身份、Definition消费与Runtime Projection边界。
## Requirements
### Requirement: Animation Foot Analysis必须拥有Editor-only规范产物

Animation Foot Analysis MUST为`Target AnimationClip imported content + 显式Motion Reference AnimationClip imported content + Rig Definition + Sampling Rig prefab + Rig Calibration + Geometry Validation Result + Analysis Settings + Analyzer Version`生成不可变Editor-only规范Artifact。Artifact MUST保存Target与Motion Reference的精确对象身份和Analysis Input Hash，并原子保存完整Raw Motion Samples、Toe/Heel/Sole Contact Evidence、Landing/LiftOff Event topology、Event绑定的Step Time/Distance、Foot Path decomposition、Ground Pose Error、Contact、Lock Mode/Weight与Support候选证据。

Analysis Source MUST为每个Target Clip显式绑定唯一Motion Reference Clip。两份Clip MUST具有相同Duration、Loop、Sample Rate，并且Motion Root、Pelvis与左右Hip/Knee/Ankle/Toe分析闭包除声明Root Translation/Yaw通道外逐时刻一致；Prop、毛发和上肢不得进入Foot Analysis输入身份。不得按名称或目录猜配对。Motion Reference Root Curve MUST从0开始并覆盖素材秒域；最后一个Source Sample区间 MAY由Unity原生尾值Clamp，第二个区间只在尾段不超过两个Source Sample且末端Tangent估算的平移分量变化不超过2 mm、Quaternion分量变化不超过0.01时允许，更长或仍明显运动的尾段 MUST拒绝。Raw Sample MUST从Motion Reference覆盖Root Motion、Hip、Knee、Ankle、Heel、Toe与Sole的Root-local和Clip-motion姿态。Root-local表示同一时刻相对Root的姿态；Clip-motion以Motion Reference起点为共同原点并保留Root Motion。速度 MUST在完整位置页完成后用规范中心差分生成。Rig Calibration MUST唯一提供Up、Ground Reference、Sole Frame、Heel与Toe；Analyzer不得按Clip最低点、Transform名称或当前Scene补数据。

Geometry Validation对Calibration Preview Clip的依赖 MUST使用排除注册表现Curve的`AnimationClipAnalysisInputHash`。修改Gait Phase、Foot IK或Foot Motion Data Curve MUST不使同一标定姿势的Geometry Validation过期；修改骨骼、Root、Loop或Source Duration MUST使其过期。

Artifact MUST写入固定`Library`存储根，并只作为原始证据、候选重建和lineage owner。作者Apply后的正式22条Curve MUST只由原生AnimationClip拥有；Artifact不得保存第二份可被Build或Runtime优先读取的正式Curve。Reader MUST拒绝缺少Raw/Event/Path/Filter页的旧format，不保留兼容reader或从旧feature反推补全。

#### Scenario: 相同合法输入重复分析

- **WHEN** 相同Target、Motion Reference、用途、Rig、Calibration、Geometry和Analyzer设置重复生成候选
- **THEN** Raw与全部派生证据 MUST产生相同canonical payload和hash
- **AND** 22条候选Curve MUST逐值相同

#### Scenario: Apply后重新读取Artifact

- **WHEN** 作者Apply候选且骨骼、Root和Loop输入未变化
- **THEN** AnimationClipAnalysisInputHash对应Artifact MUST继续Ready
- **AND** 正式Curve MUST从AnimationClip Catalog读取而不是从Artifact读取

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

正式Analyzer MUST只接受精确AnimationClip、Rig Definition v4、Sampling Rig、Rig Calibration、Analysis Settings与Analyzer Version。它 MUST通过Rig v4 Physical BoneId绑定Sampling Rig Transform并执行独立PlayableGraph sampling，不得读取Tree、StateMachine、Timeline call site、CharacterPipelineDefinition、Profile runtime、PresentationProjection、CharacterPipelineHost、当前Scene或Transform名称。Analyzer MUST生成左右脚有限feature curve set，先写完全部采样帧的heel、toe、sole位置与高度，再从完整循环位置序列计算中心差分速度，不得在未来采样帧尚未写入时读取它。Sampling Rig、Rig与Calibration identity/revision/hash不一致 MUST明确失败。

#### Scenario: 从独立Timeline分析Clip

- **WHEN** 作者选择AnimationClip与合法Analysis Source并执行Rebuild Selected Clip
- **THEN** Analyzer MUST只使用Analysis Source提供的Rig v4、Sampling Rig与Calibration生成或更新对应artifact
- **AND** MUST不执行Authoring Discovery、Semantic compile、Numeric lowering或完整Projection Build

#### Scenario: Sampling Rig Calibration不匹配

- **WHEN** Calibration声明的rig identity与Analysis Source的Rig v4或Sampling Rig不一致
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

### Requirement: Analyzer必须生成Event绑定的Step Time与Step Distance

Analyzer MUST先从完整Toe/Heel/Sole证据生成带进入/退出滞回和最短持续时间的Contact区间。每脚Contact由false进入true MUST生成Landing Event，由true进入false MUST生成LiftOff Event；循环首尾连续Contact段 MUST先合并再生成规范ordinal与cycle关系。

每个Sample MUST解析同脚下一Landing Event：

```text
StepTime = NextLandingAbsoluteTime - SampleAbsoluteTime
```

同一Event内Step Time MUST有限、非负并按素材绝对时间单调趋近0。Landing采样帧 MUST记录0，下一采样帧 MAY切换到下一Event的时间。

有限Clip的首个Landing之前 MUST以Clip开始姿态作为该Landing事务的距离与Path起点，但 MUST不生成虚假Landing Event。最后一个Landing完成后Step Time MUST保持0，Step Distance MUST保持该已完成Landing事务的距离，Foot Path MUST继续相对最后Landing高度解释。有限Clip位置 MUST逐索引直接读取同帧Raw Sample，MUST不因最后一帧索引等于区间数而使用Loop展开或下一周期首帧；其每帧Foot Path Animation Height MUST等于同帧Raw Sole Motion Y。若有限Clip某脚整段没有合法Landing Event，该脚 MUST进入显式No Step域：Event页为空、Step Time/Distance为0、Foot Height相对Calibration Ground记录，Contact/Lock仍按证据生成；MUST不伪造Landing或阻止另一脚的数据生成。Loop Clip的Motion Reference Root净位移非零时，左右任一脚没有Landing、Step Time全0或Step Distance全0 MUST整体失败并禁止Apply；Root净位移为零时 MAY形成Stationary No Step。系统 MUST不根据Clip名称预设Grounded或Flight比例。

每个Landing MUST记录RootLocalLanding、MotionSpaceLanding和LandingSoleRotation。相邻同脚Landing MUST生成：

```text
StepVector = NextMotionSpaceLanding - PreviousMotionSpaceLanding
StepDistance = length(ProjectOnPlane(StepVector, CalibrationUp))
```

Step Distance MUST只描述AnimationClip素材步长，不得包含Runtime速度、输入方向、Future Body Translation、世界地形或预测查询结果。

#### Scenario: 循环Run跨越Clip结尾

- **WHEN** 当前Sample的下一同脚Landing位于下一cycle
- **THEN** Step Time MUST使用展开后的绝对素材时间继续单调趋近0
- **AND** Step Distance MUST使用规范相邻Motion-space Landing生成

#### Scenario: In-place Target使用Root Motion Reference

- **WHEN** Target是Root X/Z已归零的In-place Clip
- **THEN** Analyzer MUST从显式Motion Reference采样真实Root-local/Clip-motion轨迹
- **AND** MUST把22条结果写回Target而不修改或发布Motion Reference
- **AND** MUST不通过Target的恒等Root或跨时刻Root-local位置直接相减生成Step Distance

### Requirement: Analyzer必须生成Foot Height Above Path

Foot Forward MUST直接来自动画Sole的Clip-motion平面轨迹。相邻Landing之间 MUST按该轨迹的累计平面距离生成Path Progress，并按Progress在前后Landing平面高度间生成Foot Path Baseline：

```text
HeightAbovePath = max(0, AnimationSoleHeight - BaselineHeight)
```

候选`Foot Height`曲线 MUST保存HeightAbovePath，有限、非负并在前后Landing端点回到正式容差。Analyzer MUST保存Path Progress、Baseline Height与Animation Sole Height用于解释候选，但不得把世界Ground Path、未来世界Landing高度、Current Trace、Anchor或IK结果写入AnimationClip。

#### Scenario: 动画脚在Swing中抬高

- **WHEN** Sole在两次Landing之间高于动画Foot Path 12厘米
- **THEN** Foot Height候选 MUST在对应Foot Forward位置记录约0.12米
- **AND** MUST不改变原动画脚XZ或复制Foot Forward位置曲线

#### Scenario: 前后Landing素材高度不同

- **WHEN** 相邻Landing在Clip-motion中具有不同高度
- **THEN** Baseline MUST按累计Foot Forward距离连续连接两端高度
- **AND** Foot Height MUST只保存动画Sole高于Baseline的剩余量

### Requirement: Analyzer必须生成Toe与Ground Pose Filter数据

Toe Height MUST是Motion Reference Toe相对Calibration Ground沿Calibration Up的距离；Toe Speed MUST是Toe在Motion Reference clip-motion空间的线速度模长。Ground Pose Filter MUST在Editor中把当前Sole沿Up投影到Calibration Ground并保留当前平面朝向构造Ground-aligned Sole目标；MUST从当前Sole-Ankle局部关系得到目标Ankle，并使用当前Hip-Knee、Knee-Ankle长度、作者膝弯平面和余弦定理求唯一目标Knee。不可达目标 MUST夹紧到双骨段可达区间并保留Residual。Pos Error MUST为当前Ankle/Knee到解算Ankle/Knee的RMS位移加Residual；Rot Error MUST为当前Sole到Ground-aligned Sole的Quaternion角度；Reach MUST只从同一Residual生成。

Contact候选 MUST同时消费Toe Height/Speed、Artifact中的Heel/Sole证据与Pos/Rot Error，并输出`[0,1]`连续曲线。Contact不得直接复制旧PlantConfidence、Constraint或旧Contact曲线。

#### Scenario: Toe低且速度低

- **WHEN** Toe接近Calibration Ground、速度低且Ground Pose Error处于稳定范围
- **THEN** Contact候选 MUST进入高可信区间
- **AND** 对应Toe Height、Toe Speed、Pos Error与Rot Error MUST能在Animation Window直接对照

#### Scenario: 脚高于地面快速摆动

- **WHEN** Toe Height或Toe Speed超过正式退出范围
- **THEN** Contact候选 MUST退出接触区间
- **AND** MUST不因旧Lock或Support曲线为高值而继续Contact

### Requirement: Analyzer必须生成独立Lock Scenario与Support

Lock Mode MUST只取`Unlocked=0 / Sliding=1 / Locked=2`。Analyzer MUST从Contact、Toe/Sole速度、Ground Pose Pos/Rot Error与腿可达生成Mode，使用versioned进入/退出滞回和最短持续时间。低速Sole证据 MUST只负责建立动画Lock Anchor；Anchor建立后的Locked退出 MUST使用Sole相对该Anchor的累计最大平面漂移、Pos/Rot退出误差与腿可达性，MUST不因单个中心差分速度尖峰直接释放。平面漂移进入、退出预算 MUST由对应速度阈值乘以最短持续时间得到。Lock Weight MUST从完成最短持续时间过滤后的同一Anchor累计最大漂移、Contact、Pos/Rot Error和Reach连续生成，不能直接等于Contact或由Mode除以2得到；同一次Lock内已经降低的漂移权重 MUST不因脚返回Anchor附近重新升高，已过滤为Sliding或Unlocked的短Locked片段 MUST不保留独立满权重尖峰。

Support MUST表达动画承重意图，并且 MUST不以Contact、Lock Mode或Lock Weight作为有效性门槛。每脚 MUST从Heel/Toe Ground Proximity、Minimum Landing Segment中心窗口内的Sole Vertical Stability、相对该脚整段最大Root-local Hip-Sole向下距离的Downward Extension，以及Hip-Ankle/Rig Leg Length在0.55到0.8映射的Leg Extension生成连续绝对Candidate。Candidate MUST等于`Ground Proximity × Vertical Stability × Lerp(0.5, 1, Sqrt(Downward Extension × Leg Extension))`；垂直稳定度的进入、退出位移预算 MUST分别由对应Contact Speed乘Minimum Landing Segment得到。Pelvis投影 MUST只计算左右Share；最终`Left Support + Right Support` MUST等于`max(Left Candidate, Right Candidate)`，单侧存在时该侧Support MUST等于自身Candidate而不是固定1。明确空中区间左右 MUST为0。Support不得复制Contact、Lock Weight或旧PlantConfidence，Sliding或暂时不可锁的承重脚仍可保持非零Support。

#### Scenario: 接触脚存在少量动画滑动

- **WHEN** Contact仍有效但Toe/Sole平面速度或Pose Error位于中等范围
- **THEN** Lock Mode MAY进入Sliding且Lock Weight保持非零连续值
- **AND** MUST不直接切成Unlocked或继续宣称完全Locked

#### Scenario: 循环脚存在单帧速度尖峰

- **WHEN** Contact区间已经由低速证据建立Lock Anchor且单帧中心差分速度超过退出阈值，但Sole累计平面漂移和Pos/Rot Error仍在退出预算内
- **THEN** Lock Mode MUST保持Locked并由同一Anchor连续生成Lock Weight
- **AND** MUST不产生一帧Locked、一帧Sliding或`Sliding + Lock Weight 1`的交替结果

#### Scenario: 双支撑重心转移

- **WHEN** 左右脚都有效且Pelvis投影从右脚侧移动到左脚侧
- **THEN** Support候选 MUST连续把权重从右脚转移到左脚
- **AND** Contact与Lock Mode MAY保持不变

#### Scenario: 承重脚暂时不可锁

- **WHEN** 动画姿态表明左腿承重但左脚Lock Mode为Sliding或Unlocked
- **THEN** Left Support MUST仍由承重姿态保持非零
- **AND** MUST不因世界锁脚资格丢失而把左右Support同时清零

#### Scenario: 双脚只有弱Support Candidate

- **WHEN** 双脚都未形成有效承重姿态且Candidate Presence接近0
- **THEN** Left与Right Support之和 MUST保持接近0
- **AND** MUST不把相对更低的一只脚提升为Support 1

### Requirement: Foot Motion派生数据必须通过跨曲线语义验证

Analyzer MUST在发布Artifact前验证Ground Pose结果有限、Landing帧Step Time与Foot Height归零、有限Clip的派生Animation Height与同帧Raw Sole Height一致、Height Above Path与Animation Height/Baseline关系一致、Lock Mode与Weight共享有效Anchor、Support总和与绝对Candidate Presence一致、双脚弱Candidate时Support为0。该验证 MUST使用生成过程中的Raw与Candidate事实，不得从已Apply Curve反推。格式合法或Bake Session状态`Same` MUST不代表语义验证通过。

#### Scenario: Support归一化掩盖腾空

- **WHEN** 双脚Candidate都接近0但最终Support总和接近1
- **THEN** Analyzer MUST拒绝Artifact并报告Clip、采样时间、左右Candidate与Support
- **AND** Bake Session MUST不生成可Apply Candidate
