## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Analyzer必须生成Event绑定的Step Time与Step Distance

Analyzer MUST先从完整Toe/Heel/Sole证据生成带进入/退出滞回和最短持续时间的Contact区间。每脚Contact由false进入true MUST生成Landing Event，由true进入false MUST生成LiftOff Event；循环首尾连续Contact段 MUST先合并再生成规范ordinal与cycle关系。

每个Sample MUST解析同脚下一Landing Event：

```text
StepTime = NextLandingAbsoluteTime - SampleAbsoluteTime
```

同一Event内Step Time MUST有限、非负并按素材绝对时间单调趋近0。Landing采样帧 MUST记录0，下一采样帧 MAY切换到下一Event的时间。

有限Clip的首个Landing之前 MUST以Clip开始姿态作为该Landing事务的距离与Path起点，但 MUST不生成虚假Landing Event。最后一个Landing完成后Step Time MUST保持0，Step Distance MUST保持该已完成Landing事务的距离，Foot Path MUST继续相对最后Landing高度解释。若有限Clip某脚整段没有合法Landing Event，该脚 MUST进入显式No Step域：Event页为空、Step Time/Distance为0、Foot Height相对Calibration Ground记录，Contact/Lock仍按证据生成；MUST不伪造Landing或阻止另一脚的数据生成。Loop Clip的Motion Reference Root净位移非零时，左右任一脚没有Landing、Step Time全0或Step Distance全0 MUST整体失败并禁止Apply；Root净位移为零时 MAY形成Stationary No Step。系统 MUST不根据Clip名称预设Grounded或Flight比例。

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

Analyzer MUST在发布Artifact前验证Ground Pose结果有限、Landing帧Step Time与Foot Height归零、Lock Mode与Weight共享有效Anchor、Support总和与绝对Candidate Presence一致、双脚弱Candidate时Support为0。该验证 MUST使用生成过程中的Raw与Candidate事实，不得从已Apply Curve反推。格式合法或Bake Session状态`Same` MUST不代表语义验证通过。

#### Scenario: Support归一化掩盖腾空

- **WHEN** 双脚Candidate都接近0但最终Support总和接近1
- **THEN** Analyzer MUST拒绝Artifact并报告Clip、采样时间、左右Candidate与Support
- **AND** Bake Session MUST不生成可Apply Candidate
