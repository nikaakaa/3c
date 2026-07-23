## 0. 第一阶段：并行正式模块，不接入

### 0A. 接入门禁与最小合同冻结

- [x] 0.1 记录当前状态只允许第一阶段模块工作
- [x] 0.2 记录第二阶段必须由用户明确解除接入门禁
- [x] 0.3 冻结Physical与Virtual的Pose Bone Kind命名
- [x] 0.4 冻结PhysicalBoneCount、VirtualBoneCount与PoseBoneCount的明确数量语义
- [x] 0.5 冻结Virtual Bone immutable descriptor输入字段
- [x] 0.6 冻结TwoBoneIK immutable descriptor输入字段
- [x] 0.7 冻结Virtual Bone派生typed result与failure字段
- [x] 0.8 冻结TwoBoneIK typed result、ReachClamped、残差与failure字段
- [x] 0.9 确认第一阶段公共合同不引用Unity Asset、Animator、Graph、Preview或Corin
- [x] 0.10 确认第一阶段不修改任何serialized schema

### 0B. 并行模块A：Virtual Bone Pose Derivation

- [x] 0.11 创建最终Virtual Bone Pose Derivation模块
- [x] 0.12 让模块只接受parent-first Physical local pose只读输入
- [x] 0.13 让模块只接受显式Physical parent indices
- [x] 0.14 让模块只接受已校验Virtual Bone descriptors
- [x] 0.15 让模块使用调用方提供的component scratch
- [x] 0.16 让模块使用调用方提供的完整Pose Bone输出span
- [x] 0.17 在模块内建立Physical component position
- [x] 0.18 在模块内建立Physical component rotation
- [x] 0.19 在模块内传播Physical scale对component position的影响
- [x] 0.20 在模块内计算Target相对Source的Virtual local position
- [x] 0.21 在模块内计算Target相对Source的Virtual local rotation
- [x] 0.22 在模块内固定Virtual local scale为1
- [x] 0.23 对数量、索引和非有限输入返回typed failure
- [x] 0.24 保持模块不读取previous pose或velocity
- [x] 0.25 保持模块不注册到source capture、Preview或final writer

### 0C. 并行模块B：Two Bone IK Pose Solver

- [x] 0.26 创建最终Two Bone IK Pose Solver模块
- [x] 0.27 让模块只接受完整输入Pose与已解析descriptor
- [x] 0.28 让模块接受parent-first Pose parent indices并使用调用方提供的component scratch与输出Pose span
- [x] 0.29 让模块先复制完整输入Pose
- [x] 0.30 在component space读取Root、Joint、End、Effector与Joint Target
- [x] 0.31 计算当前两段Physical limb长度
- [x] 0.32 计算合法最小与最大可达距离
- [x] 0.33 将超出范围的目标限制到最近可达距离
- [x] 0.34 使用显式Joint Target建立弯曲平面
- [x] 0.35 求解Root与Joint physical rotation
- [x] 0.36 保持Root、Joint与End local scale
- [x] 0.37 实现PreserveInput End rotation
- [x] 0.38 实现MatchEffector End rotation
- [x] 0.39 按Weight混合输入与solved chain
- [x] 0.40 只修改三个Physical chain local pose
- [x] 0.41 保持其它Physical与Virtual槽位不变
- [x] 0.42 发布ReachClamped与位置残差
- [x] 0.43 对非有限、零长度、弯曲退化与数值失败返回typed failure
- [x] 0.44 保持模块不声明Pose node kind或operation code
- [x] 0.45 保持模块不注册到PoseGraph、native plan、FootPlacement或FinalIK

### 0D. 并行模块C：Pose Constraint Diagnostics Contract

- [x] 0.46 创建最终只读Pose Constraint Diagnostics合同
- [x] 0.47 定义Virtual local与component pose记录
- [x] 0.48 定义Virtual Source与Target identity记录
- [x] 0.49 定义TwoBoneIK chain、Effector与Joint Target identity记录
- [x] 0.50 定义Weight、rotation mode、reach状态与残差记录
- [x] 0.51 定义typed failure记录
- [x] 0.52 使用固定容量诊断页
- [x] 0.53 保持诊断只复制已完成Pose数据
- [x] 0.54 禁止诊断重新派生Virtual Bone
- [x] 0.55 禁止诊断第二次执行TwoBoneIK
- [x] 0.56 保持模块不注册Pose Watch、Live Debug、Runtime snapshot或Preview

### 0E. 第一阶段边界审计

- [x] 0.57 确认生产代码没有引用第一阶段模块
- [x] 0.58 确认Rig、Binding、Mask、Profile与Projection serialized字段没有变化
- [x] 0.59 确认PoseGraph node catalog与operation catalog没有变化
- [x] 0.60 确认Animancer source capture、final writer与FootPlacement没有变化
- [x] 0.61 确认Preview、Pose Watch与Live Debug没有新接线
- [x] 0.62 确认Corin Rig、Mask、Profile、PoseGraph、prefab与generated产物没有变化
- [x] 0.63 确认没有Projection Build、Foot Analysis Build、Motion Matching Build或Player Build触发
- [x] 0.64 确认没有临时adapter、兼容reader、第二套math或并行runtime入口

以下第1至17节全部属于第二阶段。在用户明确解除接入门禁前不得实施、勾选或修改对应代码与资产。

## 1. 第二阶段：依赖与最终合同对齐

- [ ] 1.1 确认`refactor-animation-selection-pose-graph-boundary`已经删除旧source-neutral Pose Request与隐藏Player路径
- [ ] 1.2 确认`add-character-presentation-pose-graph`已经安装唯一compiled Pose Plan、native composition、FootPlacement阶段与final writer
- [ ] 1.3 确认`refactor-animation-playback-to-blend-stack`已经收口Stored Pose、per-bone CrossFade与source release所有权
- [ ] 1.4 确认`refactor-inertial-blending-to-local-pose-node`已经收口单Pose history、residual与rebase所有权
- [ ] 1.5 确认`upgrade-character-animation-authoring-workspace`已经安装唯一Rig导航、PoseGraph Details、Preview与Pose Watch入口
- [ ] 1.6 合并`add-character-presentation-blend-space`后的最终Pose node kind目录，保留`MarkerSync`与`BlendSpacePlayer`
- [ ] 1.7 盘点Corin Blend Space迁移对Rig、Mask、Profile、PoseGraph与Projection资产的最终修改
- [ ] 1.8 固定Virtual Bone迁移在Corin Blend Space资产迁移之后执行
- [ ] 1.9 搜索旧PoseSlot固定Stack、图外Pose Post Process和FinalIK手臂缓存候选路径
- [ ] 1.10 删除或停止任何与本change目标重复的未接线Virtual Bone、hand target或TwoBoneIK实验代码

## 2. Rig authoring数据模型

- [ ] 2.1 将`CharacterAnimationBoneDefinition`破坏性改名为明确的Physical Bone定义类型
- [ ] 2.2 将`CharacterAnimationRigDefinition.Bones`破坏性改名为`PhysicalBones`
- [ ] 2.3 为Physical Bone定义保留稳定BoneId、parent-first physical index与reference local pose
- [ ] 2.4 新增稳定的Virtual Bone定义类型
- [ ] 2.5 为Virtual Bone定义增加稳定VirtualBoneId
- [ ] 2.6 为Virtual Bone定义增加独立DisplayName
- [ ] 2.7 为Virtual Bone定义增加Source Physical BoneId
- [ ] 2.8 为Virtual Bone定义增加Target Physical BoneId
- [ ] 2.9 向`CharacterAnimationRigDefinition`增加有序Virtual Bone集合
- [ ] 2.10 将Rig schema从v1提升为v2
- [ ] 2.11 将Root Bone语义明确限制为Physical Bone
- [ ] 2.12 将Left Foot Bone语义明确限制为Physical Bone
- [ ] 2.13 将Right Foot Bone语义明确限制为Physical Bone
- [ ] 2.14 校验Physical Bone parent-first顺序与唯一root
- [ ] 2.15 校验Physical与Virtual BoneId全集唯一
- [ ] 2.16 校验Virtual Bone Source与Target都存在于Physical catalog
- [ ] 2.17 校验Virtual Bone Source与Target不同
- [ ] 2.18 拒绝Virtual Bone引用Virtual Bone
- [ ] 2.19 新增`RequirePhysicalBoneIndex`并删除含糊的旧`RequireBoneIndex`
- [ ] 2.20 新增统一Pose BoneId到dense index解析合同
- [ ] 2.21 新增dense index到Physical/Virtual Bone Kind解析合同
- [ ] 2.22 删除Rig v1 serialized字段reader与兼容分支

## 3. Compiled Rig与Projection ABI

- [ ] 3.1 将compiled Rig physical payload类型破坏性改名
- [ ] 3.2 新增compiled Virtual Bone payload类型
- [ ] 3.3 在Virtual payload保存VirtualBoneId与DisplayName
- [ ] 3.4 在Virtual payload保存Source physical index
- [ ] 3.5 在Virtual payload保存Target physical index
- [ ] 3.6 在Virtual payload保存append-only dense pose index
- [ ] 3.7 在Rig payload发布`PhysicalBoneCount`
- [ ] 3.8 在Rig payload发布`VirtualBoneCount`
- [ ] 3.9 在Rig payload发布`PoseBoneCount`
- [ ] 3.10 在Rig payload保存统一Pose Bone Kind catalog
- [ ] 3.11 在Rig payload保存统一BoneId到dense pose index catalog
- [ ] 3.12 在Rig payload保存Physical parent indices
- [ ] 3.13 在Rig payload为Virtual Bone写入Source physical parent index
- [ ] 3.14 从Physical reference pose建立Physical component reference pose
- [ ] 3.15 用正式Virtual Bone math建立Virtual reference local position
- [ ] 3.16 用正式Virtual Bone math建立Virtual reference local rotation
- [ ] 3.17 将Virtual reference scale固定为1
- [ ] 3.18 将compiled Rig payload schema提升为v2
- [ ] 3.19 更新Rig payload完整结构校验
- [ ] 3.20 更新`CharacterPresentationProjection.Payload`的Rig校验为Physical/Pose双数量
- [ ] 3.21 把Virtual Bone稳定顺序、identity与Source/Target关系纳入Projection content hash
- [ ] 3.22 把TwoBoneIK compiled descriptor纳入ProjectionRevision
- [ ] 3.23 保持Gameplay SemanticHash与Numeric ProgramHash不包含Virtual Bone
- [ ] 3.24 删除compiled Rig v1 reader、旧schema常量与旧数量推断

## 4. Runtime Rig Binding与采样输入

- [ ] 4.1 将`CharacterAnimationRigBinding.Bones`破坏性改名为`PhysicalBones`
- [ ] 4.2 将Binding serialized Transform数组改为只保存Physical Bone
- [ ] 4.3 更新Binding Configure只接受PhysicalBoneCount个Transform
- [ ] 4.4 更新Binding validation只检查Physical Bone identity、重复与Animator层级
- [ ] 4.5 删除为Virtual Bone预留null Transform的可能路径
- [ ] 4.6 更新`AnimancerPoseSamplingBackend`只分配PhysicalBoneCount个TransformStreamHandle
- [ ] 4.7 更新采样后端只为Physical Bone绑定Animator stream handle
- [ ] 4.8 将Physical reference local pose与完整Pose reference page分离
- [ ] 4.9 为source capture提供compiled Physical parent index
- [ ] 4.10 为source capture提供compiled Virtual Bone descriptor
- [ ] 4.11 为source capture提供预分配component pose scratch
- [ ] 4.12 保持Timeline source使用唯一采样后端
- [ ] 4.13 保持Motion Matching source使用唯一采样后端
- [ ] 4.14 保持BlendSpacePlayer source使用唯一采样后端
- [ ] 4.15 删除任何按PoseBoneCount调用`BindStreamTransform`的代码

## 5. Source Pose派生与连续性

- [ ] 5.1 将`AnimationSourcePoseCaptureJob`的handle循环明确限制为PhysicalBoneCount
- [ ] 5.2 在capture job中先完成全部Physical local pose采样
- [ ] 5.3 保留Root Bone与Scale Policy对Physical Bone的现有语义
- [ ] 5.4 用parent-first Physical local pose建立同帧component position
- [ ] 5.5 用parent-first Physical local pose建立同帧component rotation
- [ ] 5.6 正确传播Physical scale到component position变换
- [ ] 5.7 为每个Virtual Bone读取Source component pose
- [ ] 5.8 为每个Virtual Bone读取Target component pose
- [ ] 5.9 计算Target相对Source的Virtual local position
- [ ] 5.10 计算Target相对Source的Virtual local rotation
- [ ] 5.11 把Virtual local scale写为1
- [ ] 5.12 在Virtual Bone派生完成后统一校验完整current Pose page
- [ ] 5.13 在完整Pose page上计算Physical与Virtual previous pose
- [ ] 5.14 在完整Pose page上计算Physical与Virtual velocity
- [ ] 5.15 source discontinuity时同时重置Physical与Virtual previous状态
- [ ] 5.16 seek、pause与rebase时保持Virtual Bone与source completion identity一致
- [ ] 5.17 source capture失败时发布明确Physical或Virtual阶段失败原因
- [ ] 5.18 删除Virtual Bone派生失败时读取上一帧pose的可能路径
- [ ] 5.19 抽取Preview与Runtime共用的Virtual Bone pose math
- [ ] 5.20 禁止在最终composition或final writer前再次自动派生Virtual Bone

## 6. Pose workspace、Blend Stack与Inertialization

- [ ] 6.1 将`AnimationBlendSourcePoseWorkspace`长度改为PoseBoneCount
- [ ] 6.2 将SelectedPosePlayer输出page长度改为PoseBoneCount
- [ ] 6.3 将BlendSpacePlayer输出page长度改为PoseBoneCount
- [ ] 6.4 将BlendStack entry pose长度改为PoseBoneCount
- [ ] 6.5 将BlendStack entry velocity长度改为PoseBoneCount
- [ ] 6.6 将Stored Pose长度改为PoseBoneCount
- [ ] 6.7 将BlendStack per-bone weight跨度改为PoseBoneCount
- [ ] 6.8 将source release完整性校验改为PoseBoneCount
- [ ] 6.9 将Inertialization history pose长度改为PoseBoneCount
- [ ] 6.10 将Inertialization residual长度改为PoseBoneCount
- [ ] 6.11 将Inertialization velocity长度改为PoseBoneCount
- [ ] 6.12 将Inertialization rebase覆盖全部Virtual Bone
- [ ] 6.13 将`CharacterPresentationPosePlan.BoneCount`破坏性改名为`PoseBoneCount`
- [ ] 6.14 更新`CharacterPoseGraphNativeProgram`按PoseBoneCount分配pose pages
- [ ] 6.15 更新BlendPose遍历完整Pose Bone
- [ ] 6.16 更新LayeredBoneBlend遍历完整Pose Bone
- [ ] 6.17 更新AdditivePose遍历完整Pose Bone
- [ ] 6.18 更新PoseSubgraph输入输出遍历完整Pose Bone
- [ ] 6.19 保持ModifyBone只能解析Physical Bone target
- [ ] 6.20 拒绝ModifyBone直接写Virtual Bone
- [ ] 6.21 更新final source contribution与foot contribution只从Physical脚索引读取
- [ ] 6.22 更新所有workspace span与offset越界校验为明确Physical/Pose数量

## 7. Bone Mask与per-bone Profile

- [ ] 7.1 将`CharacterAnimationBoneMaskAsset.BuildDense`改为构建PoseBoneCount长度
- [ ] 7.2 让Bone Mask解析Physical与Virtual BoneId
- [ ] 7.3 要求Bone Mask显式覆盖全部Physical Bone
- [ ] 7.4 要求Bone Mask显式覆盖全部Virtual Bone
- [ ] 7.5 拒绝Mask重复Physical或Virtual BoneId
- [ ] 7.6 删除Mask对新增Virtual Bone的默认0补全
- [ ] 7.7 删除Mask对新增Virtual Bone的默认1补全
- [ ] 7.8 将`CharacterAnimationBlendProfile.BuildDense`改为PoseBoneCount长度
- [ ] 7.9 让per-bone Blend Profile解析Physical与Virtual BoneId
- [ ] 7.10 要求per-bone Blend Profile显式覆盖全部Pose Bone
- [ ] 7.11 更新compiled Blend Profile catalog的Rig identity与长度校验
- [ ] 7.12 更新BlendStack读取Virtual Bone transition multiplier
- [ ] 7.13 更新PoseGraph编译器检查Mask与Rig v2 revision一致
- [ ] 7.14 更新Mask/Profile Inspector按Physical与Virtual分组显示
- [ ] 7.15 在Virtual Bone条目显示Source/Target只读摘要
- [ ] 7.16 删除旧Mask/Profile v1 serialized数据与兼容reader

## 8. TwoBoneIK authoring合同

- [ ] 8.1 向`CharacterPoseNodeKind`增加稳定`TwoBoneIK`值
- [ ] 8.2 向`CharacterPoseOperationCode`增加稳定`TwoBoneIK`值
- [ ] 8.3 为TwoBoneIK声明一个必需Pose输入
- [ ] 8.4 为TwoBoneIK声明一个必需Pose输出
- [ ] 8.5 为TwoBoneIK接入现有typed Weight输入规则
- [ ] 8.6 在节点authoring保存End Physical BoneId
- [ ] 8.7 在节点authoring保存Effector Pose BoneId
- [ ] 8.8 在节点authoring保存Effector local position offset
- [ ] 8.9 在节点authoring保存Effector local rotation offset
- [ ] 8.10 在节点authoring保存Joint Target reference Pose BoneId
- [ ] 8.11 在节点authoring保存Joint Target offset
- [ ] 8.12 新增`PreserveInput`与`MatchEffector` End Rotation Mode
- [ ] 8.13 校验Weight有限且位于合法范围
- [ ] 8.14 校验End只解析为Physical Bone
- [ ] 8.15 从End向上解析唯一Joint Physical Bone
- [ ] 8.16 从Joint向上解析唯一Root Physical Bone
- [ ] 8.17 校验Effector存在于同一Rig Pose catalog
- [ ] 8.18 校验Effector不属于Root/Joint/End chain
- [ ] 8.19 校验Joint Target reference存在于同一Rig Pose catalog
- [ ] 8.20 校验Joint Target offset有限且非零
- [ ] 8.21 在Rig reference pose中检查两段长度非零
- [ ] 8.22 在Rig reference pose中检查Joint Target弯曲平面不退化
- [ ] 8.23 为非法配置增加稳定validation code与PoseNodeId source map

## 9. TwoBoneIK编译与native求解

- [ ] 9.1 新增immutable compiled TwoBoneIK descriptor
- [ ] 9.2 在descriptor保存Root/Joint/End physical index
- [ ] 9.3 在descriptor保存Effector pose index
- [ ] 9.4 在descriptor保存Joint Target reference pose index
- [ ] 9.5 在descriptor保存两个offset与rotation mode
- [ ] 9.6 在PoseGraph compiler为TwoBoneIK分配descriptor index
- [ ] 9.7 在native program分配固定TwoBoneIK descriptor array
- [ ] 9.8 把TwoBoneIK operation加入native program合法operation目录
- [ ] 9.9 把TwoBoneIK固定在native composition stage
- [ ] 9.10 保持TwoBoneIK位于world-aware FootPlacement之前
- [ ] 9.11 在native job从输入Pose建立chain component pose
- [ ] 9.12 在native job建立Effector component pose与offset
- [ ] 9.13 在native job建立Joint Target component position
- [ ] 9.14 计算当前两段Physical limb长度
- [ ] 9.15 计算两段长度构成的最小与最大可达距离
- [ ] 9.16 将超出可达区间的目标限制到最近可达距离
- [ ] 9.17 为受限目标发布`ReachClamped`与残差
- [ ] 9.18 使用显式Joint Target建立弯曲平面
- [ ] 9.19 求解Root physical rotation
- [ ] 9.20 求解Joint physical rotation
- [ ] 9.21 保持Root/Joint/End local scale不变
- [ ] 9.22 在`PreserveInput`模式保持End输入旋转
- [ ] 9.23 在`MatchEffector`模式按Weight匹配End rotation
- [ ] 9.24 按Weight混合输入与solved chain pose
- [ ] 9.25 重建受影响chain的local pose
- [ ] 9.26 保持输入Pose其余Physical与Virtual Bone不变
- [ ] 9.27 对非有限输入发布typed failure
- [ ] 9.28 对零长度chain发布typed failure
- [ ] 9.29 对弯曲平面退化发布typed failure
- [ ] 9.30 对数值求解失败发布typed failure
- [ ] 9.31 删除上一帧chain、reference pose或世界轴fallback
- [ ] 9.32 更新native program Dispose释放TwoBoneIK descriptor array
- [ ] 9.33 更新operation workspace与offset边界校验

## 10. Final writer、最终帧与FootPlacement隔离

- [ ] 10.1 将final writer handle数量明确为PhysicalBoneCount
- [ ] 10.2 将final writer Pose输入长度明确为PoseBoneCount
- [ ] 10.3 只写`[0, PhysicalBoneCount)`区域到AnimationStream
- [ ] 10.4 禁止final writer为Virtual Bone请求TransformStreamHandle
- [ ] 10.5 在`FinalAnimationPoseFrame`保存Physical/Pose数量
- [ ] 10.6 在最终帧Bone catalog保存Physical/Virtual Kind
- [ ] 10.7 保持最终帧lease覆盖完整Pose page
- [ ] 10.8 保持Foot feature继续引用Physical left/right foot index
- [ ] 10.9 保持FootPlacement读取Physical ankle/toe/sole姿势
- [ ] 10.10 保持`CharacterFootPlacementRig`只绑定Physical Transform
- [ ] 10.11 保持`ICharacterFootPlacementSolver`只消费现有plan
- [ ] 10.12 禁止FootPlacement读取Virtual Bone作为预测落点
- [ ] 10.13 禁止FootPlacement读取Virtual Bone作为Foot Lock或support anchor
- [ ] 10.14 保持FinalIK adapter只属于FootPlacement腿部world-aware阶段
- [ ] 10.15 删除任何手臂TwoBoneIK到FinalIK MonoBehaviour的临时接线

## 11. Rig与PoseGraph作者工作区

- [ ] 11.1 为Rig v2创建唯一Custom Inspector或正式workspace Rig adapter
- [ ] 11.2 在Rig视图分离Physical Bones与Virtual Bones区域
- [ ] 11.3 为Virtual Bone提供显式Add命令
- [ ] 11.4 Add命令生成稳定VirtualBoneId
- [ ] 11.5 Add命令创建独立DisplayName
- [ ] 11.6 Source picker只列出当前Rig Physical Bone
- [ ] 11.7 Target picker只列出当前Rig Physical Bone
- [ ] 11.8 picker排除Source与Target相同选择
- [ ] 11.9 为Virtual Bone提供显式Remove命令
- [ ] 11.10 Remove按稳定VirtualBoneId删除精确条目
- [ ] 11.11 为DisplayName修改接入Undo/Redo与dirty owner
- [ ] 11.12 为Source/Target修改接入Undo/Redo与dirty owner
- [ ] 11.13 为Virtual Bone reorder接入Undo/Redo与Rig revision更新
- [ ] 11.14 Rig修改后将引用的Mask/Profile/PoseGraph显示为Invalid或Stale
- [ ] 11.15 禁止Rig选择、Inspector focus与窗口恢复自动Build
- [ ] 11.16 向PoseGraph node catalog加入TwoBoneIK
- [ ] 11.17 为TwoBoneIK创建合法Pose端口
- [ ] 11.18 在TwoBoneIK Details中只显示Physical End picker
- [ ] 11.19 在TwoBoneIK Details中显示Physical/Virtual Effector picker
- [ ] 11.20 在Effector picker排除当前chain
- [ ] 11.21 在TwoBoneIK Details中显示Joint Target reference与offset
- [ ] 11.22 在TwoBoneIK Details中显示End Rotation Mode与Weight
- [ ] 11.23 在TwoBoneIK Details显示Virtual Bone Source/Target只读摘要
- [ ] 11.24 从TwoBoneIK Details提供精确Rig owner导航
- [ ] 11.25 保持PoseGraph Details不复制Virtual Bone定义写入口

## 12. Preview、Pose Watch与Runtime Diagnostics

- [ ] 12.1 让Authoring Preview加载Rig v2 Projection payload
- [ ] 12.2 让Authoring Preview执行正式source Virtual Bone派生
- [ ] 12.3 让Authoring Preview执行正式TwoBoneIK native operation
- [ ] 12.4 让Preview在Rig/Projection revision不匹配时进入Stale
- [ ] 12.5 禁止Preview临时编译Virtual Bone或TwoBoneIK
- [ ] 12.6 扩展Pose Watch按VirtualBoneId订阅
- [ ] 12.7 Pose Watch发布Virtual local position与rotation
- [ ] 12.8 Pose Watch发布Virtual component position与rotation
- [ ] 12.9 Pose Watch发布Virtual Source/Target Physical BoneId
- [ ] 12.10 Pose Watch发布节点前后Mask贡献
- [ ] 12.11 Runtime snapshot发布Physical/Virtual/Pose Bone count
- [ ] 12.12 Runtime snapshot按PoseNodeId发布TwoBoneIK chain identity
- [ ] 12.13 Runtime snapshot发布Effector与Joint Target identity
- [ ] 12.14 Runtime snapshot发布Weight、rotation mode与reach状态
- [ ] 12.15 Runtime snapshot发布solve前后End pose与残差
- [ ] 12.16 为Rig非法、capture非法、IK配置非法和IK运行退化定义稳定diagnostic code
- [ ] 12.17 保持diagnostics只复制已完成Pose workspace
- [ ] 12.18 禁止diagnostics重新派生Virtual Bone
- [ ] 12.19 禁止diagnostics第二次执行TwoBoneIK
- [ ] 12.20 保持Pose Watch历史容量有界且关闭即释放

## 13. Editor分析与Motion Matching边界

- [ ] 13.1 将Foot Analysis Sampling Rig数量校验改为PhysicalBoneCount
- [ ] 13.2 保持Foot Analysis只采样Physical ankle/toe/sole与Calibration
- [ ] 13.3 将Foot Analysis Rig identity更新为Rig v2 revision
- [ ] 13.4 让旧Foot Analysis artifact按新Rig revision明确Stale
- [ ] 13.5 禁止Foot Analysis artifact保存Virtual Bone pose
- [ ] 13.6 将Motion Matching target hierarchy signature只建立在Physical Bone层级
- [ ] 13.7 保持Motion Matching feature BoneId只解析已声明的Physical feature bone
- [ ] 13.8 禁止Motion Matching默认把Virtual Bone加入search feature vector
- [ ] 13.9 让Motion Matching runtime source采样仍输出完整PoseBoneCount
- [ ] 13.10 让Motion Matching Database按Rig v2 identity明确Stale
- [ ] 13.11 让Blend Space每个sample通过统一capture输出完整Virtual Bone
- [ ] 13.12 保持Blend Space weight同时作用于Physical与Virtual Pose槽位
- [ ] 13.13 保持Blend Space Foot feature聚合只读取Physical脚分析数据
- [ ] 13.14 禁止Editor选择Rig后自动重建Foot Analysis或Motion Matching Database

## 14. 现有Rig与通用资产迁移

- [ ] 14.1 更新`CharacterAnimationPresentationMigrationAuthoringService`创建Rig v2
- [ ] 14.2 让迁移服务把旧Physical Bone稳定顺序原样写入PhysicalBones
- [ ] 14.3 让没有Virtual Bone业务的Rig写入显式空VirtualBones集合
- [ ] 14.4 更新迁移服务配置Physical-only Rig Binding
- [ ] 14.5 更新迁移服务创建PoseBoneCount长度的Mask
- [ ] 14.6 更新迁移服务创建PoseBoneCount长度的per-bone Blend Profile
- [ ] 14.7 迁移所有受版本控制的CharacterAnimationRigDefinition资产到v2
- [ ] 14.8 迁移所有CharacterAnimationRigBinding prefab数据到PhysicalBones字段
- [ ] 14.9 迁移所有Bone Mask资产到新Rig revision与完整Pose Bone表
- [ ] 14.10 迁移所有per-bone Blend Profile资产到新Rig revision与完整Pose Bone表
- [ ] 14.11 删除旧Rig v1 serialized字段
- [ ] 14.12 删除旧Binding `m_Bones` serialized字段
- [ ] 14.13 删除旧Mask/Profile v1 schema与兼容转换器
- [ ] 14.14 删除旧generated Presentation Projection资产
- [ ] 14.15 保持Projection只能通过现有明确Build命令重新发布

## 15. Corin Virtual Bone与双手IK迁移

- [ ] 15.1 在最终Corin Rig v2确认`Bip001_Prop1`武器Physical Bone stable identity
- [ ] 15.2 在最终Corin Rig v2确认Left Hand Physical Bone stable identity
- [ ] 15.3 在最终Corin Rig v2确认Right Hand Physical Bone stable identity
- [ ] 15.4 向Corin Rig添加稳定`VB_Weapon_LeftHand`
- [ ] 15.5 配置Left Virtual Bone Source为`Bip001_Prop1`
- [ ] 15.6 配置Left Virtual Bone Target为Left Hand
- [ ] 15.7 向Corin Rig添加稳定`VB_Weapon_RightHand`
- [ ] 15.8 配置Right Virtual Bone Source为`Bip001_Prop1`
- [ ] 15.9 配置Right Virtual Bone Target为Right Hand
- [ ] 15.10 更新Corin Rig revision
- [ ] 15.11 为Corin FullBody Action Mask显式增加两个Virtual Bone权重
- [ ] 15.12 将Corin FullBody Action Mask的两个Virtual Bone权重设为动作可更新参考的明确值
- [ ] 15.13 为Corin全部其它Mask显式增加两个Virtual Bone权重
- [ ] 15.14 为Corin per-bone Blend Profile显式增加两个Virtual Bone multiplier
- [ ] 15.15 在Corin Rig reference pose配置Left Arm合法Joint Target reference与offset
- [ ] 15.16 在Corin Rig reference pose配置Right Arm合法Joint Target reference与offset
- [ ] 15.17 向最终Corin PoseGraph添加Left Arm TwoBoneIK节点
- [ ] 15.18 配置Left IK End为Left Hand Physical Bone
- [ ] 15.19 配置Left IK Effector为`VB_Weapon_LeftHand`
- [ ] 15.20 配置Left IK Joint Target与rotation mode
- [ ] 15.21 向最终Corin PoseGraph添加Right Arm TwoBoneIK节点
- [ ] 15.22 配置Right IK End为Right Hand Physical Bone
- [ ] 15.23 配置Right IK Effector为`VB_Weapon_RightHand`
- [ ] 15.24 配置Right IK Joint Target与rotation mode
- [ ] 15.25 将左右TwoBoneIK串联在最终FullBody composition与FootPlacement之间
- [ ] 15.26 保持FootPlacement仍连接在双臂TwoBoneIK之后
- [ ] 15.27 保持OutputPose仍只连接唯一FootPlacement完成路径
- [ ] 15.28 更新Corin PoseGraph content revision
- [ ] 15.29 更新Corin Profile引用的Rig、Mask、Profile与PoseGraph identity
- [ ] 15.30 删除Corin prefab中的任何手臂FinalIK、隐藏target或重复IK pass配置
- [ ] 15.31 使Corin旧Projection、Foot Analysis artifact与相关编译产物按Rig v2 revision明确Stale
- [ ] 15.32 保持Corin重新发布只通过正式显式Build入口

## 16. Agent、Gameplay与网络隔离审计

- [ ] 16.1 扫描Character Agent Snapshot确保仍只输出Rig identity/revision与只读Projection诊断
- [ ] 16.2 确认Agent editable Document不输出Virtual Bone或TwoBoneIK可写字段
- [ ] 16.3 确认Agent Patch catalog不增加Rig mutation
- [ ] 16.4 确认Agent lowerer不增加Virtual Bone命令
- [ ] 16.5 确认Agent handler不通过SerializedProperty修改Rig或PoseGraph
- [ ] 16.6 确认Agent validator继续拒绝Presentation写入
- [ ] 16.7 确认MCP bridge不增加Virtual Bone或TwoBoneIK action
- [ ] 16.8 确认Gameplay Semantic IR不保存Virtual Bone
- [ ] 16.9 确认Float32与Fixed Program ABI不因本change提升
- [ ] 16.10 确认CharacterSimulationState与WorldSimulationState不保存IK结果
- [ ] 16.11 确认Snapshot、Hash与Network packet不保存Virtual Bone或TwoBoneIK状态
- [ ] 16.12 确认Network Model与WorldSolver不引用Presentation Rig v2实现

## 17. 旧路径清理与文档收口

- [ ] 17.1 全量搜索`rig.Bones.Count`并逐处改为PhysicalBoneCount或PoseBoneCount
- [ ] 17.2 全量搜索`Rig.Bones.Count`并逐处改为PhysicalBoneCount或PoseBoneCount
- [ ] 17.3 全量搜索旧`CharacterAnimationBoneDefinition`并迁移到Physical命名
- [ ] 17.4 全量搜索旧`CharacterAnimationRigBinding.Bones`并迁移到Physical命名
- [ ] 17.5 全量搜索按Transform数量推断Pose长度的代码并删除
- [ ] 17.6 全量搜索按Pose长度创建Transform handle的代码并删除
- [ ] 17.7 全量搜索图外hand target缓存并删除
- [ ] 17.8 全量搜索隐藏GameObject bend/effector target并确认只剩FootPlacement既有合法边界
- [ ] 17.9 全量搜索Virtual Bone最终重算路径并删除
- [ ] 17.10 全量搜索Runtime默认Virtual Bone Mask权重并删除
- [ ] 17.11 全量搜索旧Rig v1 schema常量、reader与兼容converter并删除
- [ ] 17.12 更新`openspec/project.md`的Animation Presentation目标链与Rig口径
- [ ] 17.13 更新current `character-animation-presentation-authoring`的Rig authoring口径
- [ ] 17.14 更新current `character-animation-pipeline`的Physical/Pose数量与source capture口径
- [ ] 17.15 更新current `character-foot-placement-presentation`明确Virtual Bone不属于world-aware脚部真相
- [ ] 17.16 更新current `character-pipeline-runtime`的diagnostics链显示Physical/Virtual与TwoBoneIK
- [ ] 17.17 合并`character-presentation-pose-graph`最终节点目录，保留BlendSpacePlayer与TwoBoneIK
- [ ] 17.18 更新代码组织说明，保持Animation core、Presentation FootPlacement与FinalIK adapter单向边界
