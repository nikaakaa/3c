## Context

动机与范围见 [proposal.md](proposal.md)。本变更只规划当前主线的通用能力与 Corin 正式接入。

### 当前链路与实际缺口

- `CharacterPoseNativeModifyBoneHandler` 已有节点独立双缓冲和权重读取；位置、旋转、缩放直接读 payload，`ApplyModification` 只改目标 Component Pose，没有重建后代。它不是需要另建的执行系统。
- `CharacterModifyBonePosePayload` 使用 `ModifyBoneOperationMask` 和 `Local/Mesh`，位置的 Local 处理与旋转后乘不是同一套父空间变换语义；新增 Replace 前必须明确合同。
- EventGraph 已提供速度、朝向、水平速度、朝向误差等输入与输出。`HorizontalAcceleration` 是速度差的长度，不包含左右方向；`FacingError` 是朝向误差，不等于转向速度。这两项保持原样。
- Corin 当前链为 `Locomotion PoseStateMachine -> FullBody Action Slot -> Body Control Rig -> Output`；Body Control Rig 已有 Local/Component 转换、Foot Placement、Goal Assembly 和 FullBodyIK。
- `D:/Unity_Project_1/pose-correction` 的 `fdad43a2e` 保存了多样本姿态修正和 Corin 五点样本，执行依赖已经退役的 Worker/Operation，且当时正式运行尚未闭合。本次不合并这个分支。
- 本轮检索当前正式 Configs 和已生成 C# authoring，未发现 `CharacterModifyBonePosePayload` 的实际配置调用；实施前仍需按最新工作区重新枚举，不能把本轮检索当作永久无使用者证明。

### ZZZ 参数证据与使用边界

来源为 `D:/ZZZ_Dump/output/ZZZ_20260831_120908.raw`，该快照 CR3=`0x68c81a000`（十进制 `28127109120`）；构建哈希记录在 `D:/ZZZ_Dump/PIK分析包/快照分析/20260831_120908/manifest.json`，GameAssembly SHA256=`4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4`。字段布局来自同构建正式元数据。快照跨时段采集，以下是保存的配置值，不是逐帧运行采样。

| 已读取对象或字段 | 结果 | 本次用途 |
| --- | --- | --- |
| Corin 配置对象 `0x7005522b000` 的 `UseAnimationBlendTilt/+0x440` | false | 支持采用程序骨骼变换的实现方向 |
| 同对象 `AnimationBlendTiltParam/+0x448` | 空字符串 | 没有启用动画参数侧倾 |
| 同对象 `overrideRunStateTiltBone/+0x438` | 空字符串 | 不能由空值推导具体目标骨骼 |
| 同对象 `overrideRunStates/+0x430` | 字典 count=0 | 未配置角色覆盖；尚未补齐当前活动状态的完整绑定证据 |
| `ConfigEntityRunStates` 对象 `0x700492042a0` 的 KeyDict | 0 指向 `Avatar_Default_RunState_Run`，1 指向 `Avatar_Default_RunState_Walk` | 记录默认配置入口，不把运行状态整数擅自解释为所有业务枚举 |
| 默认跑配置 `0x70046fd3a00` | TiltDuration=0.3 秒，RecoverDuration=0.3 秒，MaxTiltAngle=999 | 0.3 秒作为本项目初始响应时间参考；999 不采用为最终倾角上限 |
| 默认跑 TiltLevDic `0x70046f956e0` | 1→4、2→8、3→12、4→16 | 16 度作为本项目初始上限参考；输入键单位未确认，不能照搬驱动公式 |
| 默认走配置 `0x70046fd3800` 的 TiltLevDic | 30→2、60→4、90→6、120→8 | 仅记录证据，本次不扩大到走路侧倾 |

角色开关原始字节另存于 `D:/ZZZ_Dump/output/lean_inspection/20260927/character-lean-config-evidence.json`。通用输出函数 RVA `0x10F267C0` 已核对到动画参数分支与 `Transform.rotation -> Quaternion.AngleAxis -> Transform.rotation` 分支。倾斜曲线 key、映射表输入语义、Corin 当前活动默认配置与默认目标骨骼未完整闭合，不宣称本方案数值等同 ZZZ。

## Goals / Non-Goals

**Goals:**

- 作者可以给任意已绑定物理骨骼传入动态位置、旋转、缩放，明确控制模式、空间、权重和后代随动。
- Corin 在地面跑动转弯时向弯内侧倾，直行及失去适用条件时回正；效果由正式图驱动并进入现有 IK/输出。
- Runtime 只消费准备好的骨骼索引和规则，使用实例缓冲，正常帧零托管分配。

**Non-Goals:**

- 不新增 Lean 专用 Pose 节点、姿态样本资源、混合样本编辑器、骨骼方向修正系统或通用新 Rig 框架。
- 不恢复旧 Worker、Operation、Document、Program/Projection，不新增 MonoBehaviour 每帧改骨骼或独立 Writer。
- 不改变动作选择、移动碰撞、Root Motion、折返算法或 Foot/FBBIK 求解公式；不新增测试项目或手动验证任务。
- 本次参考空间限于父骨骼局部空间和角色组件空间，不增加世界空间、任意参考对象或多目标批处理接口。

## Decisions

### 1. 扩展原 Modify Bone，业务公式留在图中

节点输入为 Component Pose，输出仍为 Component Pose。保留骨骼稳定 ID，通过同一节点定义/Capability 提供 `position: Vector3`、`rotation: Quaternion`、`scale: Vector3`、`weight: Float32`。

每个变换通道显式选择 `Ignore/Add/Replace` 和 `Constant/Port` 来源。Constant 是作者明确配置的常量，不是缺端口时的补值；Port 必须连接精确类型的同帧生产者，缺失立即按既有图校验失败。Ignore 通道不要求生产者。旋转常量允许以欧拉角度编辑，准备时转换为 Quaternion，运行动态端口只传 Quaternion。权重保持现有显式常量或连接合同。

节点只认识骨骼变换；lean 的公式、阈值、历史、平滑和启停不写进 Handler。缺少的纯数学运算通过现有 EventGraph 原生节点注册体系补齐，不能用一个 `UpdateLean` C# 总节点藏起整段角色业务。

**业务取舍：** 通用节点让腰部侧倾、头部转动和身体偏移共用能力；多样本姿态修正能够提供更精细的多骨骼联动，但需要额外内容制作。当前只要求程序 lean，不承担那套内容系统。

### 2. 统一两种空间与逐通道模式

作者为节点选择一个参考空间：`ParentLocal` 或 `Component`。前者是目标相对父骨骼的局部 TRS，根骨骼使用组件基准；后者是相对角色组件的 TRS。节点进入时从输入 Pose 获取参考变换，不读场景 Transform，不使用已经修改过的自己作为参考。

先在所选空间形成目标值，再按权重与输入值插值，最后转换回 Component Pose：

| 通道 | Add 目标 | Replace 目标 | 权重处理 |
| --- | --- | --- | --- |
| Position | 原位置＋输入偏移 | 输入位置 | 从原位置线性插值 |
| Rotation | `delta * original`，增量轴属于所选参考空间 | 输入旋转 | 最短弧球面插值 |
| Scale | 原缩放逐轴乘输入倍率 | 输入缩放 | 从原缩放线性插值 |

Ignore 保留原值，weight=0 完整透传，weight=1 使用完整目标。旋转和 TRS 合法性沿现有 Rig/姿势数值合同，不自创新的镜像缩放或剪切表示。错误值按现有帧失败规则处理，不补单位变换。

删除旧位掩码和含糊空间配置，以正式 Mutation/C# API 更新实际使用点。旧 Local 旋转后乘与新 ParentLocal 左乘并不天然等价，不能只改枚举名；若实施时发现需要保留既有结果的使用者，必须给出具体图/节点与语义差异请用户决定，不默默翻转乘法顺序。

**业务取舍：** 父局部空间适合按骨架关系摆姿势，组件空间适合绕角色前向做侧倾。任意世界/参考骨骼空间能支持更复杂瞄准，但本次不需要，不扩大接口。

### 3. 后代传播是通用骨骼变换合同

提供一个明确的 `PropagateToChildren` 配置：

- true：保存输入 Pose 中后代各自的局部 TRS，目标变换后按父先子后的拓扑顺序重建后代 Component Pose，后代自然随目标旋转、平移和缩放。
- false：只有目标物理骨骼的 Component Pose 改变，后代 Component Pose 保持输入值；后续转局部空间自然产生相应补偿。

虚拟骨骼不作为直接可写目标；若其源或目标物理骨骼受影响，使用当前 Rig/FinalIK 已有虚拟骨骼派生语义重新形成一致结果。实例绑定阶段准备影响范围和索引，必要共享计算放在现有骨骼姿势数学模块，不新建节点专属第二套虚拟骨骼规则。

**业务取舍：** 传播开启能直接做连续身体倾斜；关闭能保持下游骨骼模型空间位置，适合局部调整。两个模式的结果均明确，作者无需靠节点顺序猜测。

### 4. Corin 在跑动状态子图内施加侧倾

在既有 Run Start 与 Run Loop 状态子图中，Clip 输出后接同一份可复用的通用姿势子图：`LocalToComponent -> Modify Bone -> ComponentToLocal -> 状态输出`。该子图只是现有通用节点的组合，不定义 Lean 专用底层节点。

初始目标选当前正式 Rig 的 `animation-bone/Bip001/Bip001_Pelvis/Bip001_Spine`，旋转模式 Add、空间 Component、后代传播 true，Position/Scale 为 Ignore。绕角色组件前向轴侧倾，骨骼选择通过正式 Rig catalog；身体左右方向按当前 Rig 的组件基准核对，不假定 FBX 的骨骼局部 Z 就是角色前向。

Spine 作为初始目标使上身和持武器的手臂一起侧倾，腿的动画姿势不因直接旋转骨盆而改变。骨骼仍是普通节点配置，作者可以显式改目标；不内置骨盆/脊柱特判或固定多段权重。

```text
Run Start / Run Loop Clip
  -> 通用骨骼变换子图
  -> 原状态混合
  -> 原 FullBody Action Slot
  -> 原 Body Control Rig（Foot Placement / Goal Assembly / FullBodyIK）
  -> 原 Final Publication
```

Idle、Walk、Run End、TurnBack 子图不施加 lean，退出跑动时由原状态过渡混合回未侧倾姿势。有限 Action 仍由原 Slot 权重控制；Action 完全覆盖时不得残留底层 lean，进入/退出动作时按原 Slot 混合。不要在 Slot 之后无条件倾斜整份动作姿势。

**业务取舍：** 跑动分支内修改能利用既有动作覆盖和状态过渡，避免增加动作抑制事件或权重系统；放在最终身体图后处理则可影响所有动作，但需要额外动作授权策略，本次不采用。Foot 输入及最终输出仍只有一条路径，不另建原始脚目标旁路，也不宣称新侧倾保证脚绝对不滑。

### 5. 侧倾由实际运动驱动，保持小而完整

EventGraph 使用本帧正式速度、Grounded、MovementMode、宿主 delta，以及实例保存的上帧水平运动方向。以水平速度方向变化得到带符号转向角速度，而不是直接使用左右按键，也不复用没有方向的 HorizontalAcceleration。业务限定地面跑动；原地转向、直行、低于速度门槛、离地和折返均将目标角设为零。

图中的计算步骤：

1. 在适用状态且速度超过门槛时读取当前 XZ 运动方向；首次或上次方向无效时目标为零，只建立历史。
2. 用上一方向与当前方向的有符号夹角除以本帧 delta，得到度/秒。恰好反向且无法唯一判定左右时设目标为零，交由已有折返动作处理。
3. 将转向速度按 `FullLeanTurnRate` 归一化到 [-1,1]，乘最大倾角和速度权重。速度权重为超过门槛后，在显式 `FullLeanSpeed` 前线性升至 1。
4. 右转驱动向右压身，左转反之；按 Rig 校准的组件前向轴生成旋转四元数，不把 yaw 与 roll 混用。
5. 用标量 MoveTowards 逐帧接近目标。朝同侧更大倾角时使用倾斜时间；回零、减小或换边先使用回正时间，换边先经过零。变化速率为最大倾角除以对应时间，乘宿主 delta；0.3 秒是从零到最大值/从最大值到零的时间，不冒充原版曲线。
6. 发布 `animation.lean.rotation` Quaternion，再在本次公开结果完成后写历史。当前不适用时将方向历史标记无效，倾角继续回零；Reset、Body discontinuity 和图替换按既有 EventGraph 生命周期清零，首帧不由历史跳变产生侧倾。

初始作者参数只有五项，集中放在 Corin EventGraph 的可读调参组中，不另建 Profile 或资源资产：

| 参数 | 初始值及来源 |
| --- | --- |
| 最大侧倾角 | 16°，参考默认跑映射表输出上限；是本项目选值，不是直接采用 `MaxTiltAngle=999` |
| 倾斜时间、回正时间 | 各 0.3 秒，参考默认跑配置 |
| 满侧倾转向速度 | 180°/秒，本项目初始调参 |
| 满侧倾水平速度 | 6 米/秒，本项目动画表现阈值；只决定侧倾何时达到满权重，不控制或复制角色运动速度配置 |

低速门槛先复用原水平移动判定的 0.0001 数值；计算不修改原 MotionPhase 或状态判定。若实际慢速抖动需要另设可调门槛，作为后续显式调参需求，不提前堆开关。

**业务取舍：** 实际运动驱动表现符合角色真正走出的轨迹；输入/期望方向驱动能提前压身但会在碰撞受阻时产生额外倾斜。本次选择前者，避免引入预测和额外事实字段。

### 6. 一条作者链和一条运行链

通用字段、typed 端口、校验、Undo/revision、C# 导出/生成使用当前节点定义与 Mutation；不额外创建作者窗口。修改现有节点详情中的模式、空间、传播与输入来源，端口按有效通道清晰展示。禁止在 OnInspectorGUI 做骨骼遍历重建、资源生成或发布。

Handler 只对同一 completion identity 求值一次，输入页保持只读；输出页与必要 scratch 在实例准备时分配，Commit/Discard/Reset/Dispose 遵循已有框架。EventGraph 持有侧倾历史，Handler 不保存第二份倾角。观察复用正式动画变量和节点已完成 Pose，不新增采样器或重新计算 lean 的诊断模块。

**业务取舍：** 直接在现有作者入口配置能让作者看到完整驱动链；专属 Lean 窗口会隐藏通用节点并引入另一配置 owner，本次不采用。

## Risks / Trade-offs

### EventGraph 发布帧的生命周期补充（2026-09-28）

EventGraph 实例拥有类型化变量读取器和两页值缓冲，初始化绑定，逐帧只读变量、写入备用页、完整发布。变量帧和调用结果采用值类型，Character 包装共享实例合同。当前成功发布帧可读到下一次成功发布前；Reset、Replacement、Dispose、Fault 使其失效。帧带发布版本，过期读取失败，不把复用缓冲伪装为永久快照。

Pose 在同一表现帧内消费变量并完成 Commit/Discard；Pending 保留图历史，但不使下一帧回放更新。采样在同帧复制需要的标量。业务取舍是以明确的借用期消除逐帧快照分配；调用方不能无限期持有变量帧，需要历史时由已有历史或采样模块保存所需数据。错误路径仍允许生成诊断对象，不用静默默认值换取零分配。

- [已有 Modify Bone 使用者的 Local 语义可能不同] → 按最新工作区枚举实际节点；无使用者时直接清理，有正确既有行为冲突时报告具体差异，不静默迁移。
- [旋转轴或符号配置错误导致外倾] → 绑定正式 Rig 骨骼与组件轴；规范明确右转向右、左转向左，不用动画名猜轴。
- [默认参数不等于原版 Corin 的实时参数] → 保留对象地址、字段和未闭合项；仅把 16°/0.3 秒作为有来源的初始选值，敏感度公式属于本项目。
- [完全反向运动或不连续位姿造成角速度尖峰] → 在同一图中对首次、零速、反向歧义和 Reset 明确产零；不添加第二历史系统。
- [父骨骼非均匀缩放与虚拟骨骼影响] → 复用当前姿势 TRS 和虚拟骨骼合同；本次不增加剪切矩阵或镜像 Rig 支持。
- [侧倾可能影响手持武器与肩臂外观] → 初始只使用一根 Spine 的通用变换并传播后代；保留原动作 Slot 和 IK，不提前引入辅助骨样本系统。
- [并行任务正在改主线动画代码] → 以实施时的当前文件为基线，保留未提交改动；发生实际重叠冲突列出给用户决定，不回退其它工作。

## Migration Plan

1. 在当前主线补齐节点领域合同和精确类型端口，同步唯一作者定义与 C# API；删除被替代的位掩码/含糊空间字段及对应消费者。
2. 在当前 Native Handler 内完成变换和传播，提取必要纯数学到现有姿势数学模块；不 cherry-pick 旧 worktree 的执行/作者框架。
3. 在 Corin 正式 EventGraph 增加独立 lean 参数与历史，并通过原作者入口接入 Run Start/Run Loop 子图；保留当前 Clip、状态、Slot、Foot 与折返身份。
4. 使用当前领域准备/发布要求使正式资产与作者生成代码一致，不恢复整角色 Build/Projection。必要配置与内容更新计入同一功能交付。
5. 按通用合同、运行实现、Corin 内容三个可审查部分做中文小步提交；有新变更时仅通过明确逆向提交撤销本功能，不回退其它任务或保留双路径。

## 现行规范对照

| 规范 | 对照结论 |
| --- | --- |
| `native-flowcanvas-pose-runtime` | 原生 Handler、typed 输入、只读输入页、实例缓冲、同帧复用与单一提交全部沿用，无需修改 |
| `character-pose-graph-runtime-architecture` | 不引入 Worker/Operation/IR、图外写骨骼或第二 Final Publication，无需修改 |
| `character-presentation-pose-graph` | 走正式 Mutation 和状态子图，有限 Action 保持原 Slot owner，无需修改 |
| `character-animation-event-graph` | “已有动画数学与阶段语义必须保持”原文把迁移约束写成笼统禁止新增平滑；本次 delta 只允许独立新增 lean，保留旧计算与历史顺序 |
| `character-foot-placement-presentation` | 不修改 Foot/Goal/FBBIK owner、脚部校准数据或求解公式，不创建额外事务 |
| 旧 `add-character-pose-correction` | 位于另一个 worktree 的未完成方案，不是主线现行能力，不作为本变更依赖或交付依据 |

## 实施补充

- 现行 Gameplay 使用字符串形式的 MovementMode 身份，跑步起步与跑步循环姿势都消费 RunLoop 运动状态。EventGraph 正式宿主增加该既有事实的字符串类型输入，并用通用字符串相等节点判定；不新增控制状态枚举或运行分支。
- Pose 变量合同补齐 Vector3 与 Quaternion；实际运行值由 EventGraph 当帧输出提供，不以原标量默认值替代旋转。
- Modify Bone 的 C# 导出从正式节点字段描述读取全部作者字段，再经正式 Payload Codec 重建；导出器不维护新的骨骼字段清单。
- 2026-09-28 用户追加要求准备采样器。沿用既有 Foot IK Generated Sampling 的 Core/Full 程序，在正式帧增加 lean 字段组，包含可用性、适用条件、水平速度、运动方向、有符号转速、目标倾角、平滑倾角和输出 Quaternion。只读同帧事件结果，不重算公式，不执行 replay。
- 原规范中的平滑条款本意是迁移时保留原表现，本次增量澄清其范围，不能将它描述为底层不支持平滑。
- 普通 Subgraph 的复用在现有作者校验中曾被“全部子图恰好一次引用”限制阻止。实现将纯 Subgraph 调用引用与独占所有权分开；仅普通 Subgraph 允许多个纯调用，根图、状态子图、Control Rig 仍保留原约束，运行层沿用每个 Handler 独占的子实例。
- 导出往返检查发现通用 C# 输出规划器在判定延后执行语句之前就归类了外部资源，导致 Definition/Profile 未写入跨步骤结果。正式规划器在确定延后语句后提升这些外部引用，再生成共享结果；不在角色生成代码中保留手工补引用。
