## ADDED Requirements

### Requirement: Animation Rig必须区分Physical Bone与Virtual Bone

`CharacterAnimationRigDefinition` MUST唯一保存parent-first Physical Bone catalog与有序Virtual Bone catalog。每个Virtual Bone MUST拥有稳定VirtualBoneId、DisplayName、Source Physical BoneId与Target Physical BoneId；Source与Target MUST存在、不同且不得引用Virtual Bone。Physical与Virtual BoneId MUST在同一Rig内全局唯一。

#### Scenario: 创建合法Virtual Bone

- **WHEN** 作者在Rig Inspector中创建Virtual Bone并选择同一Rig内不同的Source与Target Physical Bone
- **THEN** Rig MUST保存稳定identity与Source/Target关系
- **AND** Virtual Bone MUST不要求Animator层级中存在同名Transform

#### Scenario: Virtual Bone引用Virtual Bone

- **WHEN** 作者把另一个Virtual Bone配置为Source或Target
- **THEN** Rig Validator MUST拒绝该配置并定位两个VirtualBoneId
- **AND** MUST不按声明顺序尝试链式求值

### Requirement: Compiled Rig必须发布明确Physical与Pose数量

Projection Rig payload MUST分别发布`PhysicalBoneCount`、`VirtualBoneCount`与`PoseBoneCount`，并保存Physical Bone、Virtual Bone、Bone Kind、统一BoneId到dense pose index和Virtual Source/Target physical index。Physical Bone MUST保持原parent-first顺序，Virtual Bone MUST按authoring稳定顺序追加在Physical Bone之后。

#### Scenario: 编译带Virtual Bone的Rig

- **WHEN** Rig包含N个Physical Bone与M个Virtual Bone
- **THEN** Projection MUST输出`PhysicalBoneCount=N`、`VirtualBoneCount=M`与`PoseBoneCount=N+M`
- **AND** 每个Virtual Bone dense index MUST位于`[N, N+M)`

#### Scenario: 代码请求含糊Bone Count

- **WHEN** runtime或compiler组件需要分配Transform handle、Pose page或Mask page
- **THEN** 合同 MUST要求调用方明确使用PhysicalBoneCount或PoseBoneCount
- **AND** MUST不继续公开同时代表两种含义的旧Rig `Bones.Count` API

### Requirement: 每个Animation source必须在capture阶段派生Virtual Bone

Source capture MUST先采样全部Physical Bone local pose，再用同帧component pose计算每个Virtual Bone的Target相对Source位置与旋转，并把scale固定为1。Virtual Bone MUST在previous pose与velocity计算前写入当前source Pose page。进入下游PoseGraph后Runtime MUST不自动从已经组合的Target重新计算Virtual Bone。

#### Scenario: 基础source生成Virtual Bone

- **WHEN** 当前source中Target相对Source的位置或旋转随动画变化
- **THEN** 该source的Virtual Bone MUST在每帧反映当前Source/Target关系
- **AND** MUST不读取上一帧或另一个source的Target姿势

#### Scenario: 后续Additive改变真实Target

- **WHEN** Additive层通过Mask改变Target Physical Bone但Virtual Bone权重为0
- **THEN** 输出Pose中的Target MUST包含Additive结果
- **AND** Virtual Bone MUST继续保存Additive之前的参考关系

### Requirement: 全部Pose运输节点必须携带Virtual Bone

SelectedPosePlayer、BlendSpacePlayer、BlendStack、Stored Pose、Inertialization、BlendPose、LayeredBoneBlend、AdditivePose、PoseSubgraph与最终Pose workspace MUST使用完整PoseBoneCount运输Virtual Bone local pose与velocity。Virtual Bone MUST使用与所属Pose Value相同的source权重、连续性与lifecycle，不得通过图外side-channel运输。

#### Scenario: 两个source CrossFade

- **WHEN** BlendStack在两个具有不同Virtual Bone参考的source之间CrossFade
- **THEN** Virtual Bone MUST按同一entry weight与per-bone transition profile形成连续结果
- **AND** source release MUST同时释放Physical与Virtual Pose数据

#### Scenario: Inertialization处理不连续

- **WHEN** Player发布包含Virtual Bone跳变的typed discontinuity
- **THEN** 下游Inertialization MUST在同一Pose history中处理该Virtual Bone
- **AND** MUST不为Virtual Bone建立独立history或rebase时钟

### Requirement: Bone Mask与per-bone Profile必须显式覆盖完整Pose Bone

所有Bone Mask与per-bone Blend Profile MUST绑定精确RigId/revision并为每个Physical与Virtual Bone显式保存一项权重或倍率。Compiler MUST拒绝缺失、重复、未知或跨Rig BoneId；系统 MUST不按Source、Target、Bone Kind或旧资产值自动补全Virtual Bone条目。

#### Scenario: Additive排除Virtual Bone

- **WHEN** 作者希望Additive改变真实手臂但保留上游手部参考
- **THEN** 对应Mask MUST显式把手部Virtual Bone权重配置为0
- **AND** 其它Physical与Virtual Bone仍 MUST使用各自明确权重

#### Scenario: 旧Mask没有Virtual Bone条目

- **WHEN** Rig revision增加Virtual Bone而Mask仍只覆盖Physical Bone
- **THEN** Projection Compiler MUST拒绝该Mask并列出缺失VirtualBoneId
- **AND** Runtime MUST不把缺失值解释为0或1

### Requirement: TwoBoneIK必须是显式PoseGraph节点

PoseGraph MUST提供`TwoBoneIK`节点，消费一个Pose并输出一个Pose。节点 MUST显式配置End Physical Bone、Effector Pose Bone、Effector offset、Joint Target reference与offset、End Rotation Mode和Weight。Compiler MUST由End Bone解析唯一Root/Joint/End Physical链，并拒绝缺失parent、跨Rig引用、Effector属于chain、退化Joint Target或Virtual chain bone。

#### Scenario: Virtual Bone驱动手臂IK

- **WHEN** TwoBoneIK的End是Physical Hand且Effector是武器相对手部Virtual Bone
- **THEN** Runtime MUST读取同一输入Pose中的Virtual effector component pose
- **AND** MUST只修改Root、Joint与End三个Physical Bone的local pose

#### Scenario: IK配置非法

- **WHEN** End Bone没有两个Physical parent或Effector属于被控制chain
- **THEN** PoseGraph Compiler MUST拒绝节点并定位PoseNodeId与BoneId
- **AND** MUST不创建默认chain、隐藏target或scene lookup

### Requirement: TwoBoneIK必须使用无拉伸且显式弯曲参考的确定求解

TwoBoneIK MUST在component space使用输入Pose的两段长度和显式Joint Target求解，不允许stretch，不修改local scale。超出两段长度合法可达区间的目标 MUST确定性限制到最近可达距离并发布`ReachClamped`，不得拉长骨骼。`PreserveInput` MUST保留End输入旋转，`MatchEffector` MUST按Weight匹配Effector旋转。运行时非有限、零长度或弯曲平面退化 MUST产生typed failure，不得使用上一帧、Rig reference、世界轴或默认pole继续求解。

#### Scenario: 合法可达目标

- **WHEN** Effector与Joint Target有限、两段长度非零且Weight大于0
- **THEN** Solver MUST保持两段长度并按显式弯曲平面求出Physical chain
- **AND** 输出End位置残差 MUST进入同一节点diagnostics

#### Scenario: Joint Target退化

- **WHEN** 当前输入Pose使Root、Effector与Joint Target无法形成合法弯曲平面
- **THEN** TwoBoneIK MUST发布typed failure
- **AND** 必需OutputPose路径 MUST不发布上一帧IK结果

#### Scenario: Effector超出可达距离

- **WHEN** Effector与Root距离大于两段Physical limb长度之和
- **THEN** TwoBoneIK MUST保持骨骼长度并把求解距离限制到最大可达值
- **AND** diagnostics MUST发布`ReachClamped`与未消除的位置残差

### Requirement: Virtual Bone不得绑定或写入Animator Transform

`CharacterAnimationRigBinding`、Animator handle catalog、Foot Analysis Sampling Rig与final AnimationStream writer MUST只使用PhysicalBoneCount。Virtual Bone MUST不占Transform数组位置、不绑定null占位、不创建GameObject且不写入AnimationStream。最终Pose与diagnostics MAY保留Virtual Bone数据，但所有Transform写入 MUST截止于Physical Bone区域。

#### Scenario: Runtime创建Rig Binding

- **WHEN** Rig包含Virtual Bone且Prefab提供全部Physical Bone Transform
- **THEN** Binding MUST只校验Physical Bone Transform数量、身份与层级
- **AND** MUST不要求Virtual Bone Transform

#### Scenario: Final writer提交姿势

- **WHEN** Pose Plan完成包含Physical与Virtual Bone的最终Pose page
- **THEN** final writer MUST只循环`[0, PhysicalBoneCount)`写入AnimationStream
- **AND** Virtual Bone page MUST保持只读诊断数据

### Requirement: Virtual Bone authoring、Preview与Runtime必须共用唯一Projection

Rig Inspector MUST是Virtual Bone的唯一写入口，并提供稳定identity、Source/Target Physical Bone picker、Undo/Redo、删除与结构化错误。PoseGraph Details MUST只引用Rig内已有BoneId。Preview、Pose Watch、Live Debug与Runtime MUST使用相同Rig payload、Virtual Bone math、TwoBoneIK描述、ProjectionRevision和source map；选择资产、修改Rig或打开Preview MUST不自动Build。

#### Scenario: Rig修改后Projection过期

- **WHEN** 作者新增、删除、重排Virtual Bone或修改Source/Target
- **THEN** Profile、PoseGraph与Preview MUST显示Projection Stale或Invalid
- **AND** 只有明确Build命令 MAY发布新Projection

#### Scenario: 观察Virtual Bone

- **WHEN** 作者对VirtualBoneId启用Pose Watch或选择TwoBoneIK Live Details
- **THEN** diagnostics MUST显示匹配revision的local/component pose、Source/Target、Mask贡献与IK残差
- **AND** MUST不重新采样source或第二次求值PoseGraph

### Requirement: Corin必须通过Virtual Bone完成武器双手稳定

Corin正式Rig MUST为武器Physical Bone到左右手Physical Bone声明两项稳定Virtual Bone。Corin最终PoseGraph MUST在FullBody Action composition之后、FootPlacement之前串联左右臂TwoBoneIK，并让每个Mask与per-bone Profile显式覆盖两项Virtual Bone。该配置 MUST使用同一Corin Profile、Rig、Projection与PosePlan，不得创建prefab私有target或图外FinalIK手臂pass。

#### Scenario: Corin动作过渡

- **WHEN** Corin在Base Locomotion与FullBody Action之间过渡且武器、双手沿不同Physical层级混合
- **THEN** 两项Virtual Bone MUST携带各source动画的武器相对手部参考
- **AND** 左右TwoBoneIK MUST在FootPlacement前修正对应Physical arm chain

#### Scenario: Corin执行FootPlacement

- **WHEN** 同一帧完成双臂TwoBoneIK并进入FootPlacement
- **THEN** FootPlacement MUST继续只消费Physical leg、Foot Analysis、world support与Calibration
- **AND** MUST不读取武器手部Virtual Bone或建立第二套腿部IK真相

### Requirement: Virtual Bone必须保持Presentation边界

Virtual Bone、TwoBoneIK状态与diagnostics MUST只属于Presentation Projection与每帧Pose workspace，不得进入Gameplay Program、CharacterSimulationState、WorldSimulationState、Snapshot、Hash、Network packet或Gameplay决策。Agent authoring MUST继续把Rig/PoseGraph作为只读Presentation context，不得获得Virtual Bone写操作。

#### Scenario: 网络模型切换

- **WHEN** 同一Character Definition运行在Local、Prediction、Authority observed或Rollback表现组合
- **THEN** 各客户端Presentation MAY使用同一Virtual Bone Projection修正可见姿势
- **AND** Gameplay state、World solve、snapshot与packet identity MUST不因TwoBoneIK结果改变

#### Scenario: Agent请求创建Virtual Bone

- **WHEN** Agent Document、Patch或MCP payload尝试修改Rig Virtual Bone或TwoBoneIK配置
- **THEN** 现有只读Presentation边界 MUST拒绝未知写入
- **AND** MUST不通过SerializedProperty、默认配置或第二authoring service执行修改
