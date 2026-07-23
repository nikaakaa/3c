# Change: 新增角色动画虚拟骨骼与姿势内双骨骼IK

## Why

当前`CharacterAnimationRigDefinition`把“Rig中的骨骼”与“场景中必须绑定的Transform”视为同一集合。每个source pose只采样真实Transform，Blend Stack、Inertialization、Bone Mask和Pose Graph也只运输这组真实骨骼。系统因此无法在基础动画采样时保存“目标骨骼相对于另一骨骼的参考关系”，并在后续CrossFade、Layered Blend或Additive修改真实肢体后，用同一份随动画变化的参考关系做IK修正。

继续在FinalIK adapter、MonoBehaviour或PoseGraph节点外缓存手部位置会形成第二条姿势数据链：它不知道当前Selection分支、Blend Stack entry、Stored Pose、Inertialization、Bone Mask和Projection revision，也无法与Preview、Pose Watch和Runtime使用同一事实。把美术FBX辅助骨骼当作必需条件则会要求修改模型与全部动画来源，并且无法表达“由现有Source/Target骨骼自动派生”的关系。

本change把Virtual Bone定义为Rig拥有、Projection编译、source pose生成并由整个Pose Plan运输的只读数据骨骼，同时新增显式`TwoBoneIK` PoseGraph节点作为首个正式消费者。Corin使用现有武器骨骼分支与左右手建立武器相对手部参考，在动作CrossFade和后续分层表现后修正双手漂移。预测落脚、地面查询、Foot Lock和pelvis求解继续只由现有`FootPlacement` world-aware阶段拥有。

## What Changes

- `CharacterAnimationRigDefinition`从单一`Bones`集合升级为显式`PhysicalBones`与`VirtualBones`：Physical Bone对应Animator层级中的真实Transform；Virtual Bone保存稳定`VirtualBoneId`、显示名、Source Physical Bone和Target Physical Bone，不绑定Transform、不驱动蒙皮。
- Rig schema、Projection Rig payload与全部dense pose合同升级。Physical Bone保持原有稳定顺序，Virtual Bone按authoring稳定顺序追加为dense pose槽位；payload显式发布`PhysicalBoneCount`、`VirtualBoneCount`与`PoseBoneCount`，不再用一个含糊的`Bones.Count`同时表达三种数量。
- 每个Animation source完成Physical Bone local pose采样后，在同一个capture job中先建立component pose，再计算`VirtualLocal = inverse(SourceComponent) * TargetComponent`的位置和旋转；Virtual Bone scale固定为1。派生完成后才计算previous pose与velocity。
- Virtual Bone只在source capture时派生。进入SelectedPosePlayer、BlendSpacePlayer、BlendStack、Stored Pose、Inertialization、Blend、Layered Blend与Additive后，它与普通Pose槽位一起被混合，但不得被下游自动重算，否则无法保留上游动画参考。
- Bone Mask与per-bone Blend Profile必须显式覆盖全部Pose Bone。作者可以让动作层更新Virtual Bone，也可以把Virtual Bone权重设为0以保留被排除层之前的参考；不存在按骨骼类型自动补权重。
- 新增显式`TwoBoneIK` PoseGraph节点。节点读取输入Pose中的Effector Bone与明确配置的Joint Target reference，修改一个由三个Physical Bone组成的肢体链；Virtual Bone可以作为Effector或Joint Target reference，但不能作为被写入的chain bone。
- `TwoBoneIK`固定在native pose composition阶段执行，位于作者连接的上游Blend/Additive之后、`FootPlacement`之前。节点不创建GameObject、Transform target、FinalIK组件或图外更新回调。
- Runtime Rig Binding、Animator stream handle和final writer只覆盖Physical Bone。Virtual Bone没有Transform绑定，不能写入AnimationStream；Final pose diagnostics按Bone Kind显示Physical/Virtual与Source/Target关系。
- Animation Presentation Rig Inspector增加唯一Virtual Bone authoring入口，采用Source/Target物理骨骼选择、稳定identity、Undo/Redo和结构化校验。PoseGraph Details只引用已经存在于Rig的BoneId，不复制Virtual Bone定义。
- Projection Compiler把Virtual Bone描述、dense索引、reference pose、TwoBoneIK描述和source map编译进唯一Projection，并把Rig变化纳入ProjectionRevision。缺失、重复、跨Rig、Source/Target相同、引用Virtual Bone作为Source/Target、IK链非法或Mask/Profile未覆盖全部Pose Bone时直接失败。
- Preview、Pose Watch、Live Debug与Runtime执行相同Virtual Bone派生、混合和TwoBoneIK计划；打开窗口、选择资产或修改Rig不得自动Build。
- Corin Rig新增武器分支到左右手的Virtual Bone，Pose Graph新增双臂`TwoBoneIK`节点，并迁移所有Bone Mask与Blend Profile显式覆盖新增Pose Bone。旧Rig schema、旧payload、旧mask/profile serialized数据和旧generated Projection直接删除或重建，不保留兼容reader与runtime fallback。

## Capabilities

### Added

- `character-animation-virtual-bones`：定义Physical/Virtual Pose Bone模型、authoring、Projection编译、source派生、Pose运输、Mask/Profile语义、TwoBoneIK消费、输出隔离、诊断与Corin武器双手防漂移闭环。

### Modified

- `character-presentation-pose-graph`：把`TwoBoneIK`加入有限正式节点目录，并规定它只读取Pose Bone reference、只写Physical Bone肢体链且位于native composition阶段。

## Dependencies And Sequencing

1. `refactor-animation-selection-pose-graph-boundary`先固定Selection、Player、source capture与最终Pose Plan边界。
2. `add-character-presentation-pose-graph`先安装唯一PoseGraph compiler、native plan、world-aware FootPlacement与final writer。
3. `refactor-animation-playback-to-blend-stack`与`refactor-inertial-blending-to-local-pose-node`先收口Stored Pose、per-bone transition和单Pose history所有权。
4. `upgrade-character-animation-authoring-workspace`先提供正式Rig导航、PoseGraph Details、Preview、Pose Watch与显式Build边界。
5. `add-character-presentation-blend-space`与本change共享source capture。两者的运行时代码可以保持正交，但Corin资产迁移必须先完成Blend Space的locomotion重写，再一次加入Virtual Bone和TwoBoneIK，不能并行覆盖同一Rig、Mask、Profile或PoseGraph资产。
6. 本change不得先向旧PoseSlot固定Stack、图外Pose Post Process或FinalIK MonoBehaviour安装临时Virtual Bone缓存。

## Implementation Staging

本change分为两个阶段。当前只允许实施第一阶段；第二阶段必须等待用户明确确认当前动画闭环结束并解除接入门禁。

### 第一阶段：并行模块，不接入

先串行冻结最小公共合同：Pose Bone identity、Physical/Virtual Bone Kind、显式Physical/Pose数量、Virtual Bone descriptor、TwoBoneIK descriptor与typed failure/result。合同冻结后并行完成三个最终模块：

1. `Virtual Bone Pose Derivation`：输入Physical local pose、parent-first physical hierarchy与Virtual Bone descriptor，输出完整Pose Bone page或typed failure。
2. `Two Bone IK Pose Solver`：输入完整Pose page与已解析chain descriptor，输出修改后的完整Pose page、reach状态、残差或typed failure。
3. `Pose Constraint Diagnostics Contract`：只定义从已完成Pose page复制的有界Virtual Bone与TwoBoneIK诊断，不注册Live Debug、Pose Watch或Runtime发布入口。

第一阶段产物必须使用第二阶段会直接复用的正式命名、数据结构与算法，不允许建立临时adapter、兼容reader、第二套math或未来需要保留的并行runtime。第一阶段明确禁止：

- 修改`CharacterAnimationRigDefinition`、`CharacterAnimationRigBinding`、Mask/Profile或现有资产的serialized schema。
- 注册Projection Compiler、PoseGraph node kind、operation code、native plan operation、source capture、final writer、Preview、Pose Watch或Live Debug接线。
- 修改Corin Rig、Mask、Blend Profile、PoseGraph、prefab、Foot Analysis、Motion Matching Database或generated Projection。
- 触发Projection Build、Foot Analysis、Motion Matching Database Build、Unity编译或Player构建。

### 第二阶段：统一接入与破坏性迁移

门禁解除后，才把第一阶段模块一次接入唯一正式链路，并在同一个阶段完成Rig v2破坏性改名、Projection ABI、source capture、全部Pose运输、Mask/Profile、TwoBoneIK节点、final writer、作者工作区、Preview、diagnostics、通用资产与Corin迁移。第二阶段不得让Rig v1/v2、旧/新Pose count、旧/新Mask或图内/图外IK同时可运行。

## Deliberate Scope

- 不在Maya/Blender/FBX中自动创建真实辅助骨骼，也不把已有`ik_*`或武器辅助骨骼复制成同义Virtual Bone。
- 不支持Virtual Bone引用另一个Virtual Bone；Source与Target必须是两个不同Physical Bone。需要链式数据骨骼时由后续独立change定义DAG与求值顺序。
- Virtual Bone只派生位置与旋转，scale固定为1；不提供Virtual Bone scale动画、蒙皮或Transform写回。
- 首个消费节点只提供不拉伸的三关节Two Bone IK，不新增FABRIK、CCD、多链约束、Control Rig、Unity Animation Rigging或通用Constraint Graph。
- 不把Virtual Bone作为世界锁点、预测落点、地面支撑、鞋底校准、Foot Analysis语义或网络状态。
- 不增加Gameplay Program、Simulation State、World State、Snapshot、Hash或Network协议字段。
- Agent Document/Snapshot继续只读看到Rig identity与Projection诊断；本change不增加Rig/PoseGraph Patch、lowerer、handler、validator或MCP写入口。
- 不增加自动Build、自动Foot Analysis、自动Motion Matching Database构建或Unity batchmode流程。

## Current Spec Comparison

- current `character-animation-presentation-authoring`与`character-pipeline-definition-authoring`只规定Profile引用Rig Definition，没有区分Transform绑定骨骼与Pose数据骨骼。本change把该区分收口在唯一Rig资产内，不新增第二份Virtual Bone配置资产。
- current `character-animation-pipeline`和工作区代码把Rig `Bones.Count`同时用于Transform handle、source workspace、Mask、Blend与final writer；本change将其拆成Physical/Pose数量，避免Virtual Bone被错误绑定或写回。
- active `character-presentation-pose-graph`把正式节点目录限定在现有节点集合，尚不包含`TwoBoneIK`；本change明确修改该有限目录，不通过隐藏job或图外solver绕过节点合同。
- active `upgrade-character-animation-authoring-workspace`把“新增runtime节点”列为该change的Non-Goal。本change在其完成后正式安装TwoBoneIK authoring与Live显示，不反向扩大workspace change的范围。
- active `add-character-presentation-blend-space`新增`BlendSpacePlayer`并复用source采样。本change在合并后的节点目录中同时保留`MarkerSync`、`BlendSpacePlayer`与`TwoBoneIK`，并要求BlendSpace采出的Pose同样生成Virtual Bone。
- current `character-foot-placement-presentation`规定FootPlacement只消费最终动画脚特征、显式rig/calibration、世界查询与`ICharacterFootPlacementSolver`。本change不修改该真相；Virtual Bone不得代替ankle/toe/sole、预测落点或Foot Lock。
- current与active Agent specs都禁止Agent修改Rig与PoseGraph。本change保持该只读边界，因此不增加Agent authoring schema；若未来允许Agent配置Virtual Bone，必须独立升级完整Agent链和`btsmtl-agent-authoring`技能。

## Breaking Changes

- `CharacterAnimationRigDefinition`、compiled Rig payload、Pose Plan、Bone Mask与Blend Profile schema提升；v1 Rig与旧generated Projection不再可加载。
- `CharacterAnimationRigBinding`只接受Physical Bone Transform数组；旧代码若以Rig总Pose Bone数校验Transform数量将直接失败并必须迁移。
- 所有Mask与per-bone Profile必须显式增加每个Virtual Bone条目；不按Target Bone复制权重，也不以0或1补默认值。
- Corin Rig revision、ProjectionRevision、Mask/Profile identity与generated Projection更新；旧Projection直接删除后由明确Build命令重建。
- PoseGraph node kind与native operation schema增加`TwoBoneIK`；旧Projection没有该operation时不得由Runtime临时补建。

## Success Criteria

- 作者能在唯一Rig Inspector中创建Virtual Bone，明确选择Source/Target Physical Bone，并在PoseGraph Mask/Profile与TwoBoneIK Details中引用同一稳定BoneId。
- 任意Timeline、Motion Matching或Blend Space source都在同一source capture后产生完整Pose Bone page；Virtual Bone在CrossFade、Stored Pose、Inertialization、Layered Blend与Additive中使用与其Pose相同的权重和连续性。
- TwoBoneIK能读取混合后的Virtual Bone参考并只修改合法Physical Bone链；final writer只写Physical Bone，场景中没有Virtual Bone GameObject或隐藏target。
- Corin武器与左右手在基础动画和FullBody Action过渡中使用武器相对手部Virtual Bone与双臂TwoBoneIK，且FootPlacement继续独立完成腿部world-aware修正。
- Preview、Pose Watch、Live Debug和Runtime对同一Rig/Projection显示相同Virtual Bone local/component pose、Mask贡献、IK输入输出与失败原因。
- 代码与资产中不存在旧Rig v1 reader、按总Pose Bone数量绑定Transform的路径、图外Virtual Bone缓存或第二个IK更新循环。
