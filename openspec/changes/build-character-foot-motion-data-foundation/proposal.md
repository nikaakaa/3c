# Change: 建立AnimationClip脚步运动数据基础

## Why

当前Foot行为问题无法稳定收敛，根因是动画脚步数据与Runtime消费政策同时变化：Step事件、未来落地时间、动画步长、Foot Path高度、接触、锁定与承重没有形成可在Unity Animation Window直接检查的作者真相，运行时只能继续解释隐藏Artifact字段或旧Phase结果。

GDC参考要求每脚自动生成预测所需的Step delay/time与distance、Foot Height Above Path、Toe Position/Velocity Filter、IK Pose Error Filter、Foot Locking Scenario和Support Leg数据。项目当前Artifact已有部分近似字段，但定义、坐标空间和事件身份不统一，且正式结果不可直接在`.anim`中审查。此前自动写入的Contact/Support实验曲线又直接进入了失败Runtime行为，不能作为数据已经正确的证据。

本change只建立数据作者链：每个Runtime Target Clip显式绑定一个Editor-only Motion Reference Clip；Analyzer从Motion Reference的真实Root Motion与骨骼生成完整候选，作者显式Apply后，Target原生`.anim`成为全部GDC脚步数据的唯一正式owner。新曲线在本change内不进入Runtime Foot Placement、Goal、Pelvis或FBBIK；Foot行为继续保持`bd5780a`架构下的`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`Oracle。

## What Changes

- 固定短名`Clip Curves`接收器和简短Animation Window property名称，保留`Gait Phase`与`Foot IK`，为左右脚各新增11条正式注册曲线。
- 每脚新增`Step Time`、`Step Distance`、`Height Above Path`、`Toe Height`、`Toe Speed`、`Ground Pose Position Error`、`Ground Pose Rotation Error`、`Contact`、`Lock Mode`、`Lock Weight`与`Support`。
- Foot Forward继续由原动画Ankle/Heel/Toe/Sole骨骼轨迹表达，不复制为第二条位置曲线。
- Analysis Source为每个Target显式登记Motion Reference；两份Clip必须除声明Root Motion通道外逐值一致，不按名字猜配对。Loop移动与Stationary只由实际Root净位移和事件事实区分，不预设Walk、Run、Grounded或Flight语义。
- Analyzer按固定采样率完整采样Motion Reference的Root、Hip、Knee、Ankle、Heel、Toe和Sole，使用统一Rig Calibration地面与双空间坐标生成规范事件和全部候选。
- Landing Event成为Step Time、Step Distance、Height与Lock数据的共同主键；循环首尾、有限Clip边界和左右脚事件分别校验。
- Analyzer生成一个携带完整lineage的session-local候选；显式Apply必须在一个Undo事务中原子替换左右脚全部22条曲线，不能按单曲线部分写回。
- 增加唯一精确单Clip Bake Session：以Analysis Source与Target Clip为输入，只读解析正式Motion Reference，先生成Current与Candidate的22曲线Diff；已有曲线不同时默认拒绝Apply，只有作者显式Replace或工具提交精确plan hash与replace确认后才允许覆盖。
- `.anim`中的22条曲线成为正式作者真相并进入Registered Curve Hash；Library Artifact只保存可重建原始证据和候选lineage，不拥有Apply后的第二份正式曲线。
- Agent Document与Animation Window统一通过唯一channel catalog识别这些曲线；不按propertyName猜测或提供key级写入。
- 删除当前失败实验生成的Contact/Support曲线并重新生成：Contact/Lock使用Motion Reference地面相对速度，Support只使用动画承重姿态且不由Contact/Lock门控，不从旧Phase、Constraint、PlantConfidence或旧曲线复制。
- Ground Pose Filter必须从逐采样Sole平地目标与Hip/Knee/Ankle双骨段几何生成真实关节修正和不可达残差；Support必须同时保存绝对Presence与左右Share，禁止把任意弱单侧Candidate归一为1，并以跨曲线语义Validator阻止格式合法但业务矛盾的数据Apply。

## Dependency And Ordering

- `refactor-character-pose-constraint-transaction`继续提供唯一Foot Module、Context、Goal、FBBIK和Writer基线。
- 本change必须先于任何新的Foot行为change完成并归档。
- 后续Foot Placement行为change必须只消费本change归档后的`.anim`曲线，不再自行生成Contact Plan或解释旧Phase。
- `TrainingEnemy`不在范围内。

## Impact

- Affected specs: `character-animation-clip-authoring`、`character-animation-foot-analysis-artifact`、`agent-character-controller-synthesis`、`character-animation-pipeline`
- Affected editor: Clip curve catalog、Foot Analyzer、唯一Bake Session、Artifact/候选/Diff/Apply、Animation Window导航、精确Unity自定义工具、Agent Document、诊断与质量校验
- Affected content: Corin稳定可达Direct Clip、Blend Space Sample和有限Action AnimationClip
- Affected runtime: 本change不新增Runtime消费字段，不修改Foot行为

## Non-Goals

- 不修改Landing Prediction、Ground Path、Swing、Landing、Locked、Sliding、Release、Support选择、Pelvis、Goal、FBBIK或Writer。
- 不把新曲线降低进Runtime Projection，也不创建未消费payload。
- 不自动Apply、不在import或build时修改AnimationClip。
- 不复制Foot Forward骨骼轨迹，不把世界地形、预测点、Anchor或IK结果写入AnimationClip。
- 不新增fallback、旧新双读、兼容reader或临时Runtime开关。
- 不处理TrainingEnemy、Heel/Toe双点IK、脚掌旋转、移动平台或Reactive。
- 不新增自动测试。

## Current Spec Conflicts

- current `character-animation-clip-authoring`只登记`Locomotion Phase`与`Foot Placement Weight`，必须扩展为唯一24-channel catalog。
- current `character-animation-foot-analysis-artifact`把Plant confidence和隐藏Step feature视为Projection输入；本change要求正式GDC派生数据Apply后只由AnimationClip拥有。
- 任何从旧LandingPhase、ReleasePhase和隐藏Constraint生成Contact Plan的行为提案都与本change冲突；后续change必须是新曲线的纯消费者。
- current `character-foot-placement-presentation`继续描述8fc基线Ground Path与Swing行为；本change不得修改该spec，直到后续行为change建立。

## Success Criteria

```text
Clip Curves可见名称简短且唯一
左右脚22条曲线完整覆盖Clip秒域
同一候选Apply原子写入全部22条曲线
单Clip Analyze先返回精确Motion Reference、plan hash与逐channel Diff
已有22条与Candidate不同时默认拒绝，只有显式Replace可以覆盖
Step Time在同一Landing Event内单调趋近0
Step Distance只来自显式Motion Reference的Motion-space相邻Landing
Height Above Path非负且Landing端点回到0容差
Toe Height/Speed与Ground Pose Error可直接审查Contact结果
Lock Mode只取Unlocked/Sliding/Locked且Lock Weight连续
Support独立于Contact/Lock门控，Sliding或暂时不可锁的承重脚仍可非零
Ground Pose Pos/Rot Error来自正式逐采样腿链几何而非Sole高度与Up夹角别名
Support左右和表达绝对Presence，腾空时可为0，单侧弱Candidate不得提升成1
循环Locomotion左右脚都必须生成Landing、非零Step Time/Dist、Contact与Support，否则禁止Apply
应用曲线不改变AnimationClipAnalysisInputHash，只改变Registered Curve Hash
Projection与Runtime不包含新消费字段
Foot运行行为保持bd5780a/8fc基线
TrainingEnemy没有文件变化
```
