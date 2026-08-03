# Tasks

## 1. 统一实施基线与依赖

- [x] 1.1 记录最终Pose Graph节点、端口和阶段枚举的安装版本。
- [x] 1.2 记录AnimationSelectionFrame、source identity和generation的最终合同。
- [x] 1.3 记录MarkerSync输入输出和PlayerSourceUsage的最终合同。
- [x] 1.4 记录SelectedPosePlayer、BlendStack和Inertialization的最终职责边界。
- [x] 1.5 记录CharacterAnimationPresentationProfile的正式producer binding入口。
- [x] 1.6 记录Projection schema、ContractHash和revision发布入口。
- [x] 1.7 记录AnimancerPoseSamplingBackend现有ClipSamplePlan与ManualMixerState链路。
- [x] 1.8 记录Foot Analysis artifact的source identity、store和Projection绑定入口。
- [x] 1.9 记录Character Animation Authoring Workspace最终Navigator、Details、Preview和Live接口。
- [x] 1.11 删除实施分支中任何面向旧PoseSlot Stack或旧Workbench的Blend Space草稿。
- [x] 1.12 确认全部依赖change已经提供目标合同；未安装时停止实现，不创建临时适配器。

## 2. 定义Blend Space核心身份与枚举

- [x] 2.1 新增稳定`CharacterAnimationBlendSpaceId`值对象。
- [x] 2.2 新增稳定`CharacterAnimationBlendSpaceSampleId`值对象。
- [x] 2.3 定义BlendSpace identity格式和非空校验。
- [x] 2.4 定义Sample identity格式和资产内唯一校验。
- [x] 2.5 新增`CharacterAnimationBlendSpaceMode`有限枚举。
- [x] 2.6 只登记`Linear1D`、`FreeformCartesian2D`和`FreeformDirectional2D`。
- [x] 2.7 新增`CharacterAnimationBlendSpacePhasePolicy`有限枚举。
- [x] 2.8 只登记`SharedNormalizedPhase`与`MarkerSynchronizedPhase`。
- [x] 2.9 新增`CharacterAnimationBlendSpaceSampleRole`有限枚举。
- [x] 2.10 只登记`DynamicCycle`与`StationaryPose`。
- [x] 2.11 新增source-local Pose Parameter解析策略枚举。
- [x] 2.12 删除任何Direct、SimpleDirectional、nested或legacy枚举占位。

## 3. 建立Blend Space authoring资产模型

- [x] 3.1 新增`CharacterAnimationBlendSpaceAsset`正式ScriptableObject。
- [x] 3.2 保存稳定BlendSpaceId和content revision。
- [x] 3.3 保存精确Animation Rig identity引用。
- [x] 3.4 保存正式BlendSpace mode。
- [x] 3.5 新增X轴authoring definition。
- [x] 3.6 新增可选Y轴authoring definition。
- [x] 3.7 让轴保存稳定ParameterId、类型、单位、最小值和最大值。
- [x] 3.8 让二维模式必须拥有Y轴并让一维模式禁止Y轴残留。
- [x] 3.9 新增稳定Sample authoring record。
- [x] 3.10 让Sample保存精确AnimationClip引用和clip content identity。
- [x] 3.11 让Sample保存与mode一致的一维或二维位置。
- [x] 3.12 让Sample保存DynamicCycle或StationaryPose角色。
- [x] 3.13 让StationaryPose保存显式固定normalized sample time。
- [x] 3.14 让DynamicCycle保存phase binding。
- [x] 3.15 让Marker模式保存唯一Phase Reference SampleId。
- [x] 3.16 新增每ParameterId的显式source-local解析policy记录。
- [x] 3.17 新增只属于Editor authoring的Preview设置记录。
- [x] 3.18 确保资产不序列化Runtime weight、time、Playable、Animator或Transform状态。

## 4. 建立资产正式修改服务

- [x] 4.1 新增`CharacterAnimationBlendSpaceAuthoringService`唯一mutation入口。
- [x] 4.2 实现资产identity初始化。
- [x] 4.3 实现mode切换并原子整理轴字段。
- [x] 4.4 实现轴ParameterId与range更新。
- [x] 4.5 实现Sample创建并生成新SampleId。
- [x] 4.6 实现Sample复制并生成不同SampleId。
- [x] 4.7 实现Sample删除，并在删除Marker Phase Reference前要求作者先显式更换reference或phase policy。
- [x] 4.8 实现Sample位置更新。
- [x] 4.9 实现Sample clip更新并刷新clip content identity。
- [x] 4.10 实现Sample role与stationary time更新。
- [x] 4.11 实现phase policy与reference sample更新。
- [x] 4.12 实现marker phase binding更新。
- [x] 4.13 实现Pose Parameter policy完整替换。
- [x] 4.14 让全部mutation进入Undo、dirty和content revision递增。
- [x] 4.15 禁止Inspector直接修改serialized list形成第二套mutation逻辑。

## 5. 建立authoring结构Validator

- [x] 5.1 校验BlendSpaceId非空且资产内稳定。
- [x] 5.2 校验Rig引用和Rig revision完整。
- [x] 5.3 校验mode属于正式目录。
- [x] 5.4 校验轴ParameterId、类型和单位完整。
- [x] 5.5 校验轴range有限且最小值小于最大值。
- [x] 5.6 校验轴数量与mode一致。
- [x] 5.7 校验SampleId非空且唯一。
- [x] 5.8 校验clip引用、clip identity和Rig适配完整。
- [x] 5.9 校验sample position有限且处于轴合同允许范围。
- [x] 5.10 校验Linear1D position不重复。
- [x] 5.11 校验Cartesian坐标不重合且求解关系不退化。
- [x] 5.12 校验Directional零向量样本唯一。
- [x] 5.13 校验Directional同方向同半径样本不重复。
- [x] 5.14 校验StationaryPose固定时间位于合法normalized范围。
- [x] 5.15 校验Marker模式Phase Reference存在且为DynamicCycle。
- [x] 5.16 校验全部DynamicCycle拥有统一MarkerId循环拓扑。
- [x] 5.17 校验SharedNormalized模式不残留Marker reference权威字段。
- [x] 5.18 校验全部可发布Pose Parameter拥有显式policy。
- [x] 5.19 输出带AssetId、SampleId和ParameterId的机器可读诊断。

## 6. 实现纯权重求解合同

- [x] 6.1 定义target-neutral编译样本position结构。
- [x] 6.2 定义预分配weight page结构。
- [x] 6.3 定义weight evaluator统一接口。
- [x] 6.4 从Animancer LinearMixer可见语义提炼Linear1D求解规则。
- [x] 6.5 实现Linear1D编译排序和区间索引数据。
- [x] 6.6 实现Linear1D端点夹取。
- [x] 6.7 实现Linear1D相邻样本线性权重。
- [x] 6.8 从Animancer CartesianMixer可见语义提炼Cartesian2D规则。
- [x] 6.9 实现Cartesian2D固定编译数据。
- [x] 6.10 实现Cartesian2D无分配weight求解。
- [x] 6.11 从Animancer DirectionalMixer可见语义提炼Directional2D规则。
- [x] 6.12 实现Directional2D固定编译数据。
- [x] 6.13 实现Directional2D零向量、方向和幅值求解。
- [x] 6.14 实现统一有限值、非负和weight总和校验。
- [x] 6.15 实现唯一normalization pass。
- [x] 6.16 按稳定SampleId输出正权重样本次序。
- [x] 6.17 让Evaluator不引用AnimationClip、Playable、Time、ScriptableObject或Runtime context。
- [x] 6.18 删除任何“取最近样本”数值失败fallback。

## 7. 实现phase编译与映射

- [x] 7.1 定义target-neutral canonical phase数据结构。
- [x] 7.2 定义per-sample effective time输出page。
- [x] 7.3 编译SharedNormalizedPhase的clip length和loop映射。
- [x] 7.4 编译StationaryPose固定sample time。
- [x] 7.5 编译MarkerSynchronizedPhase的reference sample。
- [x] 7.6 编译统一MarkerId dense index。
- [x] 7.7 编译每个DynamicCycle的marker时间表。
- [x] 7.8 把reference sample raw/effective time解析为canonical segment和fraction。
- [x] 7.9 把canonical segment和fraction映射到每个child sample time。
- [x] 7.10 让StationaryPose跳过cycle映射并保持固定sample time。
- [x] 7.11 保留Selection cycle、loop和play rate的正式语义。
- [x] 7.12 定义外部MarkerSync effective phase进入BlendSpacePlayer的合同。
- [x] 7.13 禁止根据每帧最大weight动态更换phase reference。
- [x] 7.14 禁止Marker模式缺失数据时改走SharedNormalizedPhase。

## 8. 扩展表现资源binding与Projection源模型

- [x] 8.1 在`AnimationPoseSourceKind`新增正式BlendSpace值。
- [x] 8.2 扩展producer presentation binding保存BlendSpace asset引用。
- [x] 8.3 让Timeline source禁止残留BlendSpace字段。
- [x] 8.4 让MotionMatching source禁止残留BlendSpace字段。
- [x] 8.5 让BlendSpace source禁止残留Timeline transition字段。
- [x] 8.6 扩展Presentation Authoring Service配置BlendSpace source binding。
- [x] 8.7 从CharacterPipelineDefinition composition roots递归发现稳定producer identity。
- [x] 8.8 禁止按显示名、目录、旧Layer或generated Projection发现producer。
- [x] 8.9 扩展binding validator校验producer、asset、Rig和轴接口。
- [x] 8.10 扩展Projection source binding模型保存BlendSpace identity/revision。
- [x] 8.11 提升Projection schema和Presentation ContractHash。
- [x] 8.12 删除旧Projection reader和任何未知source kind兼容分支。

## 9. 扩展Pose Graph authoring节点

- [x] 9.1 在`CharacterPoseNodeKind`新增正式BlendSpacePlayer值。
- [x] 9.2 定义BlendSpacePlayer的AnimationSelection输入端口。
- [x] 9.3 定义BlendSpacePlayer的typed X Parameter输入端口。
- [x] 9.4 定义二维模式的typed Y Parameter输入端口。
- [x] 9.5 定义BlendSpacePlayer的Pose输出端口。
- [x] 9.6 定义BlendSpacePlayer的Pose Discontinuity输出端口。
- [x] 9.7 定义节点availability policy字段。
- [x] 9.8 禁止节点保存BlendSpace asset引用。
- [x] 9.9 更新节点创建目录和UE接近概念说明。
- [x] 9.10 更新节点序列化合同和content revision。
- [x] 9.11 更新Graph copy/paste对稳定NodeId和端口的处理。
- [x] 9.12 更新Graph删除节点和edge清理逻辑。

## 10. 扩展Pose Graph Validator

- [x] 10.1 校验BlendSpacePlayer精确拥有一个Selection输入。
- [x] 10.2 校验一维模式只连接X参数。
- [x] 10.3 校验二维模式同时连接X和Y参数。
- [x] 10.4 校验参数端口的ParameterId、类型和单位。
- [x] 10.5 解析节点全部可达Selection endpoint。
- [x] 10.6 校验可达endpoint全部绑定BlendSpace source。
- [x] 10.7 校验可达BlendSpace资产轴接口一致。
- [x] 10.8 校验可达BlendSpace资产Rig一致。
- [x] 10.9 校验节点输出只能进入合法Pose/Discontinuity consumer。
- [x] 10.10 校验MarkerSync与BlendSpacePlayer的一对一source usage关系。
- [x] 10.11 禁止Graph compiler自动插入BlendStack或Inertialization。
- [x] 10.12 输出带NodeId、producer identity、AssetId和ParameterId的诊断。

## 11. 编译BlendSpace Projection payload

- [x] 11.1 定义不可变`CharacterAnimationBlendSpacePlan`。
- [x] 11.2 定义dense axis plan。
- [x] 11.3 定义dense sample plan。
- [x] 11.4 定义compiled weight solver plan。
- [x] 11.5 定义compiled phase plan。
- [x] 11.6 定义compiled Pose Parameter policy plan。
- [x] 11.7 定义compiled Foot Analysis binding plan。
- [x] 11.8 为每个可达BlendSpace source编译唯一plan identity。
- [x] 11.9 去重同一Projection内重复引用的BlendSpace plan。
- [x] 11.10 编译最大active sample数。
- [x] 11.11 编译ClipSamplePlan workspace offset。
- [x] 11.12 编译weight、time、parameter和feature page offset。
- [x] 11.13 编译NodeId到BlendSpace plan的source map。
- [x] 11.14 把BlendSpacePlayer降低到SourceAndNativePose阶段。
- [x] 11.15 禁止Runtime plan保留authoring asset或UnityEditor引用。
- [x] 11.16 缺失任何binding或artifact时阻止Projection发布。

## 12. 接入现有Animancer采样后端

- [x] 12.1 扩展ClipSamplePlan表达稳定BlendSpace SampleId。
- [x] 12.2 把正权重样本按稳定次序提交现有ManualMixerState。
- [x] 12.3 复用现有ClipState创建和缓存入口。
- [x] 12.4 为每个sample应用compiled effective time。
- [x] 12.5 为每个sample应用最终normalized weight。
- [x] 12.6 保持现有loop和play rate合同。
- [x] 12.7 保持source pose capture由现有Playable链完成。
- [x] 12.8 在sample退出正权重集合时按正式source资源规则释放或停用child。
- [x] 12.9 禁止Animancer重新计算BlendSpace weight。
- [x] 12.10 禁止Animancer选择phase leader或执行Marker映射。
- [x] 12.11 禁止直接使用MixerState内部Parameter作为Runtime权威。
- [x] 12.12 禁止为Preview创建第二套临时PlayableGraph。

## 13. 实现BlendSpacePlayer Runtime

- [x] 13.1 定义BlendSpacePlayer固定runtime state。
- [x] 13.2 绑定同帧Selection cache。
- [x] 13.3 绑定compiled typed Parameter page。
- [x] 13.4 按source identity和generation识别typed discontinuity。
- [x] 13.5 调用对应mode的纯weight evaluator。
- [x] 13.6 调用compiled phase mapper。
- [x] 13.7 写入预分配ClipSamplePlan page。
- [x] 13.8 调用现有Animancer pose sampling backend。
- [x] 13.9 聚合source-local Pose Parameter。
- [x] 13.10 聚合Foot Analysis feature。
- [x] 13.11 聚合stable source contribution。
- [x] 13.12 发布普通Pose Value和typed availability。
- [x] 13.13 发布typed Pose Discontinuity。
- [x] 13.14 让节点不保存旧source entry或CrossFade clock。
- [x] 13.15 让节点不执行inertial residual。
- [x] 13.16 让节点不执行FootPlacement或final writer。
- [x] 13.17 参数缺失时按节点availability合同输出NoPose或失败。
- [x] 13.18 数值失败时发布诊断并禁止复用上一帧weight/time。

## 14. 接入MarkerSync与连续性节点

- [x] 14.1 扩展MarkerSync source schema识别BlendSpace canonical marker topology。
- [x] 14.2 让MarkerSync只输出BlendSpace source级effective phase。
- [x] 14.3 让BlendSpacePlayer内部映射child sample time。
- [x] 14.4 保持MarkerSync不读取sample weight。
- [x] 14.5 保持BlendSpacePlayer不拥有跨source handoff relation。
- [x] 14.6 让source变化发布discontinuity给下游Inertialization。
- [x] 14.7 保持Inertialization只读取最终单Pose history。
- [x] 14.8 保持BlendStack不读取BlendSpace X/Y参数。
- [x] 14.9 禁止在BlendSpacePlayer内创建Stored Pose或per-bone transition。
- [x] 14.10 更新source usage exact release对BlendSpace sampler资源的处理。

## 15. 扩展Foot Analysis artifact链

- [x] 15.1 定义BlendSpaceAsset/SampleId/Clip正式analysis source identity。
- [x] 15.2 扩展artifact source discovery读取BlendSpace authoring资产。
- [x] 15.3 为每个DynamicCycle sample解析所需Foot Analysis artifact。
- [x] 15.4 为需要foot feature的StationaryPose解析显式artifact或静态feature合同。
- [x] 15.5 把asset/sample/clip/Rig/calibration identity写入artifact binding。
- [x] 15.6 复用现有Artifact Builder和Store，不复制分析算法。
- [x] 15.7 扩展Missing、Stale、Corrupt和identity mismatch诊断路径。
- [x] 15.8 禁止从Timeline同名clip推断BlendSpace sample artifact。
- [x] 15.9 禁止Runtime重新分析AnimationClip。
- [x] 15.10 按effective child sample time读取左右脚feature。
- [x] 15.11 用姿势相同的sample weight聚合foot feature。
- [x] 15.12 让后续LayeredBoneBlend继续乘实际脚部骨骼贡献。
- [x] 15.13 让唯一FootPlacement节点继续只消费最终聚合结果。

## 16. 扩展Pose Parameter聚合

- [x] 16.1 枚举全部BlendSpace sample可发布的source-local ParameterId。
- [x] 16.2 校验每个ParameterId拥有显式asset policy。
- [x] 16.3 编译RequireAllSamplesWeighted策略。
- [x] 16.4 编译WeightedAvailableSamples策略。
- [x] 16.5 编译Unavailable策略。
- [x] 16.6 使用最终sample weight聚合值和availability。
- [x] 16.7 对AvailableSamples策略执行独立权重归一化。
- [x] 16.8 把聚合结果写入普通Pose Value parameter page。
- [x] 16.9 保持下游PoseParameterResolve为跨Pose解析唯一权威。
- [x] 16.10 禁止字符串参数查找和未声明默认值。

## 17. 实现Blend Space资产Workspace

- [x] 17.1 在Character Animation Authoring Workspace登记Blend Space资产模式。
- [x] 17.2 复用正式Navigator/Canvas/Details/Bottom Dock外壳。
- [x] 17.3 在Navigator展示资产、轴、Sample和编译产物层级。
- [x] 17.4 实现Linear1D刻度Canvas。
- [x] 17.5 实现Cartesian2D参数空间Canvas。
- [x] 17.6 实现Directional2D方向/幅值Canvas。
- [x] 17.7 显示SampleId、clip和位置。
- [x] 17.8 显示当前preview落点。
- [x] 17.9 显示正权重样本和贡献连线。
- [x] 17.10 让拖动Sample调用正式Authoring Service。
- [x] 17.11 实现Sample创建、复制和删除命令。
- [x] 17.12 实现多选Sample和批量位置编辑。
- [x] 17.13 接入Undo/Redo并恢复稳定selection。
- [x] 17.14 让authoring变化只标记Stale，不自动Compile。
- [x] 17.15 删除workspace中“尚未安装Blend Space”的禁用占位。
- [x] 17.16 禁止创建独立旧Workbench窗口。

## 18. 实现Details Inspector

- [x] 18.1 在Authoring页编辑mode、Rig和phase policy。
- [x] 18.2 在Authoring页编辑X/Y轴合同。
- [x] 18.3 在Authoring页编辑Sample clip、position和role。
- [x] 18.4 在Authoring页编辑StationaryPose固定time。
- [x] 18.5 在Authoring页编辑Phase Reference。
- [x] 18.6 在Authoring页编辑sample marker binding。
- [x] 18.7 在Authoring页展示Foot Analysis artifact状态。
- [x] 18.8 在Authoring页编辑Pose Parameter policy表。
- [x] 18.9 在Live页显示runtime parameter、weight、phase和effective time。
- [x] 18.10 在Live页显示feature availability和Projection revision。
- [x] 18.11 在References页显示producer binding。
- [x] 18.12 在References页显示Pose Graph NodeId、Rig、clip、artifact和Projection引用。
- [x] 18.13 所有可写字段只调用正式Authoring Service。
- [x] 18.14 所有只读字段明确标记来源和revision。

## 19. 扩展Pose Graph Editor与Details

- [x] 19.1 为BlendSpacePlayer提供节点视觉和typed端口。
- [x] 19.2 在节点标题显示Blend Space Player正式名称。
- [x] 19.3 在节点摘要显示可达asset mode和轴ParameterId。
- [x] 19.4 在Authoring Details显示selection binding和参数连接。
- [x] 19.5 在Live Details显示X/Y、active samples和weights。
- [x] 19.6 在Live Details显示canonical phase和per-sample time。
- [x] 19.7 在References显示可达producer、asset和Projection plan。
- [x] 19.8 扩展Pose Watch捕获BlendSpacePlayer结果。
- [x] 19.9 扩展节点编译诊断定位AssetId和SampleId。
- [x] 19.10 保持Graph编辑只标记Stale且不自动Build。

## 20. 统一Preview与Live Debug

- [x] 20.1 扩展Preview输入生成正式AnimationSelection。
- [x] 20.2 扩展Preview输入生成正式typed Parameter page。
- [x] 20.3 让Preview执行Projection中的同一BlendSpace plan。
- [x] 20.4 让Preview复用正式weight evaluator。
- [x] 20.5 让Preview复用正式phase mapper。
- [x] 20.6 让Preview复用正式Animancer sampling backend。
- [x] 20.7 让Preview复用正式Pose Parameter和foot feature聚合。
- [x] 20.8 支持参数滑块和2D落点拖动驱动Preview。
- [x] 20.9 支持时间播放、暂停和seek进入正式clock合同。
- [x] 20.10 在stale或revision mismatch时拒绝临时编译路径。
- [x] 20.11 扩展Runtime Snapshot保存BlendSpace节点事实。
- [x] 20.12 扩展Live Debug读取Snapshot而不重新求值。
- [x] 20.13 输出NodeId、SampleId、weight、phase、time和feature来源。
- [x] 20.14 保持Timeline Preview、Pose Graph Preview和Runtime使用同一plan。

## 21A. 重基线state-local Blend Space运行ABI

- [x] 21A.1 删除Blend Space resolver的AnimationPlaybackId输入。
- [x] 21A.2 删除Blend Space resolver的AnimationChannelId输入。
- [x] 21A.3 删除Blend Space resolver的ProgramProducerIndex输入。
- [x] 21A.4 让BlendSpacePlayer消费`PoseStateSourceProviderPlan`。
- [x] 21A.5 让BlendSpacePlayer发布`PresentationPoseSourceSample`。
- [x] 21A.6 让BlendSpacePlayer使用Projection-local dense source index与player generation。
- [x] 21A.7 让BlendSpacePlayer发布Pending、Ready与Invalid readiness。
- [x] 21A.8 让PoseState target readiness barrier消费BlendSpace readiness。
- [x] 21A.9 删除Blend Space Gameplay producer binding。
- [x] 21A.10 删除Blend Space通用SelectionInput binding。
- [x] 21A.11 把Blend Space采样workspace归入state-local provider Module。
- [x] 21A.12 删除旧`BlendSpaceAnimationPoseRequestResolver`生命周期归属。
- [x] 21A.13 更新Preview通过PoseGraph Fact adapter驱动BlendSpacePlayer。
- [x] 21A.14 更新diagnostics使用PoseState、provider、player与Presentation source identity。

## 22. 收口Corin主配置并延后独立演示

- [x] 22.1 盘点Corin BaseLocomotion全部可达AnimationChannel和producer identity。
- [x] 22.2 确认现有素材只覆盖离散Idle、起步、循环、停步与转身，不具备正式八向样本集合。
- [x] 22.3 为Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd与MovingTurn分别配置Timeline source。
- [x] 22.4 让Corin BaseLocomotion保持`Selection -> MarkerSync -> SelectedPosePlayer -> Inertialization`。
- [x] 22.5 保持FullBodyAction BlendStack分支不承担BlendSpace参数插值。
- [x] 22.6 删除Corin临时Locomotion BlendSpace资产。
- [x] 22.7 删除Corin主图临时BlendSpacePlayer与速度轴输入节点。
- [x] 22.8 删除Corin主图未再使用的速度参数声明、节点策略与惯性策略过滤。
- [x] 22.9 让正式producer binding工具原子执行纯Timeline拓扑归一化与遗留参数清理。
- [x] 22.10 通过正式Definition Build发布纯Timeline Projection、Float32与Fixed产物。
- [ ] 22.11 在八向素材齐备后盘点独立演示所需AnimationClip、Rig和Foot Analysis状态。
- [ ] 22.12 在动画职责重构完成后创建独立Blend Space演示Definition、Profile与Pose Graph。
- [ ] 22.13 选择与完整样本集合匹配的正式BlendSpace mode。
- [ ] 22.14 配置独立演示的轴ParameterId、单位、范围与Presentation Fact参数投影。
- [ ] 22.15 为独立演示每个样本生成稳定SampleId并配置phase角色。
- [ ] 22.16 通过正式Foot Analysis链生成并绑定独立演示所需artifact。
- [ ] 22.17 配置独立演示完整Pose Parameter policy。
- [ ] 22.18 把独立演示全部可达Pose source一次绑定到合法BlendSpace source。
- [ ] 22.19 在独立演示PoseState inline subgraph连接`Fact Parameter -> BlendSpacePlayer -> 可选Inertialization`。
- [ ] 22.20 通过独立Definition Build发布演示Projection，不修改Corin主图。

## 23. 清理分裂路径

- [x] 23.1 删除任何AnimatorController BlendTree实验资产和加载代码。
- [x] 23.2 删除任何BlendStack内的BlendSpace mode或axis字段。
- [x] 23.3 删除任何BlendSpacePlayer内的CrossFade entry和Stored Pose字段。
- [x] 23.4 删除任何Animancer MixerState Parameter权威读取路径。
- [x] 23.5 删除任何Runtime ScriptableObject或AssetDatabase查找。
- [x] 23.6 删除任何按clip名或producer显示名匹配sample的路径。
- [x] 23.7 删除任何Marker到Normalized的自动fallback。
- [x] 23.8 删除任何数值失败时选择最近样本的fallback。
- [x] 23.9 删除任何旧Workbench Blend Space窗口或菜单。
- [x] 23.10 删除任何Agent Patch/MCP Presentation写入口草稿。
- [x] 23.11 删除Corin临时BlendSpace配置和双写字段，保留唯一离散Timeline主链。
- [x] 23.12 确认Runtime只有Projection计划能够创建BlendSpace采样。

## 24. 同步文档与完成状态

- [x] 24.1 更新节点目录文档，把BlendSpacePlayer加入正式通用节点清单。
- [x] 24.2 更新UE概念映射并说明BlendSpacePlayer与BlendStack区别。
- [x] 24.3 更新AnimationSelection到Final Pose代码链说明。
- [x] 24.4 更新Character Animation Authoring Workspace入口说明。
- [x] 24.5 更新Profile producer binding说明。
- [x] 24.6 更新Foot Analysis source identity说明。
- [x] 24.8 更新Corin纯Timeline主配置与独立Blend Space演示边界。
- [x] 24.9 更新`openspec/project.md`中的最终动画链和明确非目标。
- [x] 24.10 对照实际实现逐项更新本任务状态。
- [x] 24.11 对照current specs检查重复、冲突和过时口径。
- [x] 24.12 严格校验本change的OpenSpec格式与scenario完整性。
