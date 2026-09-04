## Context

本次按用户 2026-09-04 的明确要求，目标为完整复刻 AMLegIK 行为。旧 proposal 中“只迁移状态／历史、几何与骨盆继续用现有 3C 算法”已经取消。动机见 [proposal](proposal.md)，外部行为见 [spec delta](specs/character-foot-placement-presentation/spec.md)。

这里的完整复刻覆盖正式输入生产、实例运动、逐脚查询／预测／锁定、目标计算、骨盆和控制参数语义。Unity 组件绑定、物理 API、内存布局、调度和发布接入项目已有模块；这些适配只能转换表达，不能替换原计算。最终骨骼仍进入项目唯一 Solver operation；原生目标正确和实际骨骼正确分别验收，不能互相代替。

### 证据基准与本次核验

| 编号 | 正式入口 | 本次用途与证据边界 |
|---|---|---|
| E1 | [AMLegIK 实现链](D:/ZZZ_Dump/PIK分析包/源码重建/AMLegIK实现链.md)、[82 方法索引](D:/ZZZ_Dump/PIK分析包/源码重建/重建索引与证据.md) | 使用真名、签名和普通原生调用顺序；旧 ManualWait、匿名 B/D 和旧 C++ 草图不再作为设计依据 |
| E2 | [真实字段映射](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/字段映射.json)、[元数据核验](D:/ZZZ_Dump/PIK分析包/元数据/verification.json) | 137 个 AMLegIK 字段、22 个 PredictState 字段、7 个 FootLockInfo 字段；值类型 payload 偏移与托管对象偏移分开 |
| E3 | [方法核验](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/核验.json)、[采样核验](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/采样核验.json) | 82 方法、11978 条指令、46 文件、171379 行；已有审计的 2629700 次字段／原字节比较无差异 |
| E4 | [脚区段分析](D:/ZZZ_Dump/PIK分析包/快照分析/20260831_120908/foot_regions/分析.md)、[区段记录](D:/ZZZ_Dump/PIK分析包/快照分析/20260831_120908/foot_regions/segments.csv) | 区域输入、端点、下一边界、remainTime、GroundPositionL 与 kneeState 的实际生产者 |
| E5 | [实例参数](D:/ZZZ_Dump/PIK分析包/快照分析/20260831_120908/实例参数.md)、[完整参数字节](D:/ZZZ_Dump/PIK分析包/快照分析/20260831_120908/configs_snapshot.csv) | A 实例通过 Rig 779516 绑定为可琳；参数是该实例快照，不冒称所有动作的资产默认值 |
| E6 | [动画系统调研](D:/ZZZ_Dump/PIK分析包/快照分析/20260831_120908/animation_system/调研.md)、[实际图与资源](D:/ZZZ_Dump/PIK分析包/快照分析/20260831_120908/animation_system/active_assets_recovery.json) | FootL／FootR／Pelvis → 原生 NodeTaskFBIK；区分控制参数、InScale、最终骨骼与缺页 |
| E7 | [逐项资料缺口](D:/ZZZ_Dump/资料管理/可还原级数据缺口与采集清单.md) | 作为查找清单；某项已有更晚直接证据时更新其采用结论，不把旧“未命名／未闭合”永久当作事实 |

2026-09-04 本次已重新计算全部 46 份 CSV 的 SHA256，46/46 与 E3 一致。磁盘 GameAssembly 的 SHA256 为 `4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4`，与 E2 及方法阅读版一致。已直接从同一映像 RVA `0x02816B84` 读取字节 `00 00 40 40`，确认下文进入区域阈值倍率为 3。

采样行不是函数调用或表现帧。轮询可能跨调用观察多个字段；46 份中既有标准记录，也有实例、节点和未定型原始块。可琳标准楼梯／攻击、19 个快速重入窗口和两份扩展录制分别使用原 manifest 与字段映射，不能把它们拼成同一时钟的完整输入录像。

### 已纠正的旧结论

| 旧说法 | 采用的直接证据 |
|---|---|
| ManualWaitForFixedUpdate 是主求解入口 | RVA 171DB640 的真名为 OnAnimatorIK |
| current／target 是两份待混合动画 Pose | OnAnimatorIK 混合 OrdinaryIkHitGround 与 PredictIkHitGround 的位置和法线 |
| A/B/C/D 是匿名位置和参考 | 分别是 currentFootprint、nextFootprint、currentFootnormal、nextFootnormal |
| 空中达到距离阈值就重写，抬脚时放大阈值 | 171D6A2E 的 isMoving 非零跳过此段；!isMoving 进入判定，enterGroundedZone 时阈值乘 3 |
| remainTime 是 clip 剩余时间，OnFootPlant 每次更新它 | 区段生产者先调用 OnFootPlant，再按当前／下一区段和停止帧写 remainTime |
| 速率按目标修正增减选择 | DoCalculateTarget 按实例 isRaising 选择当前 footUpVelocityLimit／footDownVelocityLimit |
| 骨盆仍保留 3C 的 3Hz Spring 就是完整移植 | 两种骨盆候选模式与外层响应都有原函数，必须迁移 |
| SetControlParam 的附加标量就是最终 IK 权重 | 原接口字段为 InScale，必须追到 NodeTaskFBIK 的实际消费者 |

## Goals / Non-Goals

**Goals:**

- 同一合法输入、上一状态、查询记录和参数进入时，脚目标、骨盆目标、历史更新和控制参数遵循同构建原函数。
- 复刻区域事件到 PredictState 的实际链路，包含状态过渡、循环、零长度区段、脚点与时间生产；不以最终脚轨迹反推作者配置。
- 每个原函数都有“输入、输出、状态写者、调用顺序、证据、项目 Owner、验证状态”的覆盖记录。大函数尚未翻译属于待完成工作，不作为保留旧算法的理由。
- 保持项目单一编译计划、根 Bank 事务、Goal Assembler、骨骼求解操作和最终 Writer。源码名迁入项目命名，来源名称／RVA 只作为设计与证据映射。
- 删除被替换的 3C Foot 算法、数据、配置、诊断列和消费者；已有正确的事务、采样框架、Gameplay KCC 与网络边界不改。

**Non-Goals:**

- 不复制内存地址、托管对象布局、ILFix 分派系统、Animage 通用图引擎或调试绘制组件；需要的算法输入和输出通过正式接口表达。
- 不修改 TrainingEnemy 的资产、曲线、Projection 或验收内容。共享 Foot 内核统一迁移；不承诺未迁移敌人仍可运行，也不为其保留旧版本。
- 不把零字节、缺页或未采到当成原版默认值；不要求用户先重复整批采样。先检索已有 raw、元数据、资源导出和磁盘指令，再登记最小缺项。
- 不把已存在的 3C 测试／诊断系统扩成第二套执行器，不新增测试。

## Decisions

### 1. 按原算法完整替换 Foot，保留宿主边界

| 可行方案 | 业务收益 | 业务代价 | 本次选择 |
|---|---|---|---|
| 继续保留 3C 查询／预测／骨盆，只移植状态和历史 | 现有楼梯场景接入成本较小，改动范围较窄 | 输出仍受 3C 策略控制，无法用 ZZZ 原输入逐阶段解释 | 不满足用户本次完整复刻目标 |
| 复刻 AMLegIK 与所需输入生产，装入现有 Foot／Constraint／Goal 接口 | 每次落脚和骨盆响应都能追到原算法；只有一条正式实现 | 需要迁移配置、区域输入、几何与诊断，旧效果基线允许变化 | 采用 |

抽象分为不可变输入、显式状态、查询请求／结果、脚控制结果、骨盆结果。内核不访问 Animator、场景 Transform、PhysicsScene 或可变 Diagnostics。宿主在既有 Source／World-Aware 准备阶段冻结所需输入；Query adapter 按请求执行真实物理查询；结果经当前 Constraint Bank 整帧提交。

原算法同时计算普通和预测支撑是同一次求值的两个操作数，不是两套 Runtime 或失败切换。原算法明确的无命中／关闭分支必须保留；读取失败后改走旧算法则禁止。

### 2. 输入生产与时间统一进入正式编译链

| 原输入 | 项目生产者与输入类型 | 禁止的替代 |
|---|---|---|
| 当前／下一状态、PIK 标签、过渡量 | 既有 PoseStateMachine／Transition Routing 发布状态身份、当前／目标 PIK 标记和同一过渡量 | 不从 dominant source、Foot 权重或 Contact 曲线推定 |
| FootPrint 区域、区段、回调顺序 | 正式 Foot Motion 作者数据扩展区域／区段语义，Compiler 降为固定表，Source 阶段按原区域调度规则产生有序单脚命令 | 不把 bool 插值，不按占比最高的动画自动选区段 |
| remainTime／GroundPositionL／kneeState | 同一脚区域求值器，按 E4 的语句顺序写入根事务输入 | 不用旧 TimeToLanding 或动画脚底拟合 |
| AMLegIK Owner 运动 | Rig／Host 显式绑定的 Owner 位置、旋转、矩阵、上一运动样本和 deltaTime | 不默认等同 Animator 根、PoseRoot 或 KCC 期望速度 |
| Foot／Toe／Pelvis 动画输入 | 已完成上游 Pose 的 Physical 骨骼与经校验的绑定变换 | 不读取上一最终 Transform 冒充动画输入 |
| Rigidbody 与 Transform 差量、平台及阻塞 | 正式世界输入中的明确位置／身份／状态；由原 GetDeltaFromRigidbodyToTransform 消费 | 不借旧 Surface、默认 Up 或任意身体对象补齐 |
| 全局／状态参数和一次性命令 | Profile 默认值、现有 actor-local Tuning／正式 Presentation 参数输入 | 不为同一参数建立第二覆盖表，不按动作名硬编码 |

原事件进入时间与目标求值时间分别保存；项目由同一编译计划执行一次求值，不能因有多层动画重复执行 OnAnimatorIK 语义。deltaTime 来自明确输入，原字段是否按 dt 缩放由原操作数决定。区域命令在当前 Foot 求值前依序消费，Finalize 只消费本次已处理的事件；失败帧整体丢弃。

完整区域选择包含 E6 的 BeforePhysics 队列、当前／目标状态、白黑名单、IsSeam、循环与进入／退出规则。移植的是这些 Foot 输入语义，不移植整套游戏 ECS。PIK→PIK 的 pIkWeight 恒为 1，不能据此跳过两状态脚区域交接的实现。

### 3. 区段、状态与历史采用真名和精确写入

区段作者数据为 `StartFrame / EndFrame / IsSliding / GroundPositionL`；每状态还包含 `NextAnimStateNameHash / NextAnimTransitionOffset / IsPredictedLoop / NonUniformMotionPlayerStopFrame / EnableKneeSmooth`。帧数 F、状态时长 L、调用输入 t 与资源身份一起编译，不能把模型内整数键当作角色 ID。

当前区段按列表顺序命中 `StartFrame/F <= t <= EndFrame/F`，保留闭区间与单帧区段，不私自对 t 取模。未来最近边界同时比较每段起点和终点：`wrap01(x)=fmod(x,1); x<0 时加1`；距离相同保留先枚举者。

| 情形 | 原计算 |
|---|---|
| 当前状态候选 | remainCandidate = nearestNormalizedBoundaryDistance × L |
| 非循环预测且满足原下一状态分支、下一表可用 | remainCandidate = (1-t)×L + nextDistance×nextStateFrameCount/60；nextDistance 使用原 NextAnimTransitionOffset |
| 下一表不存在 | 执行 OnUpdate 原分支选择当前表末段；不借用 3C 下一状态猜测 |
| 停止帧大于 0 | remainTime = min(remainCandidate, max(stopFrame/60-t×L,0)) |
| 写入顺序 | 调用 OnFootPlant → 计算／写 remainTime 与选中区段 GroundPositionL → 原 kneeState 计算 |

每脚 OnFootPlant 先算 `lockNow=isInZone&&!isSliding`。下表未列字段保留，清理由 Finalize 执行：

| 旧 isMoving | 旧 isLocking | 当前 isInZone | 事件写入及 time |
|---:|---:|---:|---|
| true | false | true | enterGroundedZone=true；enterLockZone=lockNow；旧 remainTime>旧 time 时才置 isBreakToGround=true；time=0 |
| true | false | false | enterLockZone=false；time 保留 |
| false | true | true | leaveLockZone=!lockNow；time 保留 |
| false | true | false | leaveLockZone=true；leaveGroundedZone=true；time=0 |
| false | false | true | enterLockZone=lockNow；time 保留 |
| false | false | false | enterLockZone=false；leaveGroundedZone=true；time=0 |

最后统一写 `isMoving=!isInZone; isLocking=lockNow`。两旧位同时为真不作为合法正常态扩展。OnFootPlant 不写 enablePIK；这里的 isLocking 是区域语义，不能额外增加 3C LandingCompleted 的写入门。

预测记录完整保留 E2 的 22 项；current／next footprint 为世界落点，current／next footnormal 为世界方向，CompSpaceFootGroundPt／AirPt 为对应组件空间点。两份高度历史保存世界 Y；原 Owner-local 标量另存在 lastFrameIKCastDist。接触“纪元”仅用于解释／诊断一次记录重写，不能再添加一个原版不存在的状态选择器。

已直接核对 PredictIkHitGround 的记录更新：

- `isMoving=true` 跳过 171D6A3A–171D6C0A 的接触重写判定，进入后续原预测分支。
- `!isMoving` 时，比较普通位置与 nextFootprint 的 XZ 距离；阈值为 PredictPointXZDistanceThreshold，enterGroundedZone 为真时乘 3。
- 距离严格超阈值、needRaycast、isLeavingMovingPlatformAndFootInAir 或 isBreakToGround 进入重写：current／next footprint 同取 ordinaryHitPos，current／next normal 同取 ordinaryHitNormal，两份高度历史同取 ordinaryHitPos.y，并按原分支清平台离开标志。
- 未重写且 enterGroundedZone 为真时，先 currentFootprint=旧 nextFootprint、currentFootnormal=旧 nextFootnormal；再统一 nextFootprint=currentFootprint、nextFootnormal=currentFootnormal。无该事件时只有后两项赋值。
- 后续 EnablePIKWarp、enablePIK、leaveGroundedZone、lastFrameState 与高度历史分支继续按 66 号函数执行；不得把上述局部规则扩成“每次接触就清所有历史”。

旧五态、PlantWorldResidual、ContactWorldResidual／AnimationRelativeScalar 域切换和 CompletedLockWeightEventIdentity 不继续裁决新目标。原操作数所需历史按真实名称迁入唯一根 Bank。

### 4. 普通／预测混合和最终权重分开

IsInPIKState 的普通非平台分支中，a／b 为当前／下一状态的 PIK 标记，t 为原过渡量经 clamp01：

| 状态变化 | pIkWeight |
|---|---|
| 非 PIK → PIK | t |
| PIK → 非 PIK | 1-t |
| PIK → PIK | 1 |
| 非 PIK → 非 PIK | 0 |

平台分支的权重置零、当前／下一状态用于 bool 返回的判据照原函数保留。PreprocessPredictionIK 先查询状态，再依 EnablePIKWarp 条件调用 PredictFoot；状态不在 PIK 时清逐脚 enablePIK；needPIK 仍要计入剩余正 pIkWeight，不能直接等同任一开关。

每脚按下式组合本次两个支撑结果：

```text
w = clamp01(pIkWeight)
finalHitPos = ordinaryHitPos + (predictHitPos - ordinaryHitPos) * w
finalHitNormal = ordinaryHitNormal + (predictHitNormal - ordinaryHitNormal) * w
```

位置和法线在这里线性组合；法线归一化的位置由 CalculateFootTarget 原代码决定。它们共享同一动画 Pose 输入，不需要两份 source／target 动画骨骼，更不能增加第二次 Pose CrossFade。

CalculateFootTarget 中另有 `k=clamp01((1-pelvisIkWeight)×IKWeight)`：候选局部位置与 currentFootAnimPosLocal 混合，旋转按原 quaternion 接口处理；lastFrameIKCastDist 与 lastFrameNormal 同时按原尾段回归。k、pIkWeight、逐脚 enablePIK 和 SetFootControlParam 的 InScale 分别记录。进入 Goal 时不得再应用一次同义的 k。

### 5. 普通查询、预测查询和目标数学完整翻译

| 原函数组 | 必须迁移的业务 | 项目责任 |
|---|---|---|
| 47–50 ApplyPlayerMotion／Prepare／CalculatePredictFootTarget／PredictFoot | Owner 运动、动画输入、预测点、步幅和记录更新 | Foot 输入准备、运动状态与预测模块 |
| 52–56 IsInPIKState 至 PreprocessAnimPos | 模式权重、CrossCheck、每脚 LockFoot 与预处理位置 | 模式计算与单脚输入模块 |
| 57–59 HipHeightLiftingDelta 两种函数、IsFootMoving | 原脚高度／腿距候选与是否重新查询 | 脚几何与查询门 |
| 60–66 GetRaycastHit 至 PredictIkHitGround | 真实查询形状、过滤、Rigidbody 差量、完整多点组合、缓存和高度历史 | Query 合同／Unity adapter 与纯目标几何 |
| 67–70 DoCalculateTarget 至 FinalizeFootIk | 方向、标量、位置／旋转、控制量和收尾 | 唯一脚响应、目标编码与状态提交准备 |
| 71–75 Min／MaxHipsDelta 与三种骨盆入口 | 两脚候选、模式、响应、查询、权重和骨盆输出 | 唯一骨盆模块 |
| 76、区域 OnUpdate／FindNextSegmentIndex | 单脚状态、事件、时间与作者脚点 | 正式区域输入与事件应用 |
| 00–16、43–45、77–81 | 构造／绑定／参数 setter／重置／启停／Update 与调用壳 | Profile、Rig、生命周期与宿主适配；原内部算法不遗漏 |
| 17–42、46、51及绘制分支 | 调试 accessor、选脚观察与绘制 | 映射现有只读 Diagnostics；不移植第二运行组件 |

82 方法逐项 disposition 必须展开在同一 design 的实施对账记录中，不能仅靠组表把未翻译函数标为完成。

HitGroundSimpleImpl 与 HitGroundImpl 是原版明确的质量配置分支，分别完整实现；普通结果始终参与主算法。GetRaycastHit 的名称不决定查询重载，必须追到 E6 的原生绑定与调用实参。脚跟／脚尖延伸、横向宽度、多点候选、位置与法线组合、退化和无命中出口均照原操作数，不用当前双 SphereCast 纯 Up 位移、平均法线或最近 Surface 替代。平台差量与 world down／Owner up／normal 不能混用。

PredictIkHitGround 的两份高度历史分别按 `history += clamp(target-history,-PIKSmoothSpeed×dt,+PIKSmoothSpeed×dt)` 更新，并保留事件初始化及预测关闭分支。最终预测点仍须完整翻译该函数剩余坐标、distFraction 与几何运算；不能把这条局部公式当作整个预测实现。

DoCalculateTarget 完整保留前段 FootNormalForwardAngleThreshold 和后段 FootRotVelocityLimit；后者在已核对的段内按每次调用限角，不能私自再乘 dt。位置标量按 AMLegIK 实例 isRaising 选择生效 footUpVelocityLimit／footDownVelocityLimit，然后乘原 deltaTime；PredictState.enablePIK 或 disableDamping 的原分支会旁路这一段。原高度区间、带符号处理、Owner 变换和 quaternion 合成也在范围内，不把局部 Y 修正替换成沿地面法线平移。

### 6. 骨盆与脚输出保持原顺序，统一事务最后发布

完整顺序为：

```text
正式状态／区域命令与参数输入
→ ApplyPlayerMotion / Update 运动输入的对应阶段
→ Prepare
→ PreprocessPredictionIK
→ CrossCheck
→ 对每脚：PreprocessAnimPos → OrdinaryIkHitGround → PredictIkHitGround
          → 普通／预测支撑混合 → CalculateFootTarget → Pending 控制量编码
          → 原模式要求的 HipHeightLiftingDeltaByMinDist → FinalizeFootIk
→ CalculatePelvisTarget：原模式候选 → 原响应／查询／权重 → Pending 骨盆控制量
→ 完整 Foot Result → 唯一 Goal Assembler → 唯一 Solver operation
→ Final Publication 与根 Bank 一次 Seal
```

Finalize 在脚目标形成后、骨盆计算前进行内部 Pending 状态更新：保存 ordinary／final hit 与 normal；time+=deltaTime；清 enterGroundedZone、leaveGroundedZone、isBreakToGround、enterLockZone、leaveLockZone、needRaycast；写 lastFrameState=needPIK；更新原动画位置和经对应变换的 target 位置／旋转。它不清 isMoving、isLocking、enablePIK，也不提交整帧。

CalculatePelvisTarget 的原模式选择、Min／Max 候选、PelvisFootMinDistance、上下速率、PD、lastPelvisHeightDelta／Diff、旋转、pelvisCastDist 与权重全部迁移。disableDamping 先供两脚读取，再由骨盆普通路径消费并清零。3C 原 Primary Support 选择、3Hz Spring、同层下沉限速和 LandingReach 准入不继续叠在原版目标后；需要保留的 Reach 仅是求解／诊断观察，不改变原输入目标。

原两个骨盆模式在项目中使用表达行为的正式模式名，并记录原 PelvisAdjustmentAdvance 的映射；原函数名中的 Legacy 不是保留 3C 旧算法的理由。不能仅因函数较大就继续使用 3C Spring。

E6 已证实当前可琳原生图为 Input → FullBodyIK → Output，三个控制口为 FootL／FootR／Pelvis；SetControlParamPosQuatWithScale 的额外输入名为 InScale。必须逐项确定 Rig 绑定、坐标、旋转、标量消费与当前 Goal ABI 的映射。若既有 Goal 不能表达原输入，就扩展同一正式合同并迁移其唯一消费者，不能新建旁路目标或偷偷修改骨骼长度。

当前项目 FinalIK backend 与 ZZZ NodeTaskFBIK 的内部等价尚未被证明。完整验收必须记录原目标、正式编码目标、Solver 输出与物理骨骼；若目标已一致而骨骼仍有差异，继续核对原生配置和消费算法。任何确需替换现有 Solver 实现的发现必须先在现有 Solver 模块边界明确 tradeoff 和 spec 影响，不能绕过已正确的唯一求解路径，也不能宣称整个原生引擎已复刻。

### 7. 配置完整性与 Corin 正式基准

原始值以 E5 中 A 实例的原字节为准，以下为可读近似值：

| 参数组 | 已确认可琳快照值 |
|---|---|
| 脚尺寸／限幅 | FootHeights=0.08，FootDisableIkHeight=0.18，FootOnGroundHeight=0.2，FootOffGroundHeight=0.4，EffectorCastMaxDist=0.5 |
| 脚响应／方向 | FootUpVelocityLimit=1.8，FootDownVelocityLimit=1.5，FootRotVelocityLimit=10，FootNormalForwardAngleThreshold=15 |
| 查询几何 | RayCastHeight 与 NewScene=0.5，PredictRayCastHeight 与 NewScene=1，Heel／Toe 延伸各0.01，FootWidthRadius=0.005 |
| 预测／步幅 | PIKSmoothSpeed=1.2，PredictPointXZDistanceThreshold=0.1，PredictiveMovingHeight=0.75，MinStrideScale=1 |
| 脚锁 | Height=0.1，Speed=0.1，Range=0.2，Damping=0.6，Stiffness=0.2，CrossCheck=false |
| 骨盆 | Up=0.5，Down=0.8，FootMinDistance=0.2，Threshold=0.02，MaxStiffness=0.4，MaxDamping=0.4，EnablePelvisPD=true，PelvisAdjustmentAdvance=false |
| 开关／可见权重 | EnablePIKWarp=false，EnablePredictiveMoving=false，EnableLockFoot=false，EnableLowQuality=false，enableStrideWrapping=false，IKWeight=1，pelvisIkWeight／Target=0 |

完整配置还需覆盖 E2 全部被算法消费的字段、静态平台查询高度及状态覆盖输入；此表不能替代完整配置清单。IKLayerMask 的原值32768只证明原工程层位，项目应绑定实际 Foot 查询层与碰撞几何，不照抄层整数。只读场景／绑定适配必须记录这种物理输入差异。

原配置、当前有效参数、运行历史和采样观察四类分别存储。当前／默认七项状态参数通过原 Set／Get／Reset 语义进入现有 Tuning；不靠缺省字段维持旧资产。开关关闭时仍按原 OnAnimatorIK 普通链求值；完整实现未启用分支和默认打开这些分支是不同任务。

### 8. 已有资料足够推进的部分与具体缺项

| 条目 | 当前可用 | 完整完成还需什么 |
|---|---|---|
| 原核心算法 | 同构建 DLL、82 方法、元数据、真实字段、46 CSV 全部已核验 | 完成大函数与相关外部 primitive 的离线翻译、分支／常量／操作数对账；不先要求新采样 |
| 区域生产链 | E4/E6 已确认 OnUpdate→OnFootPlant→remainTime／GroundPt 与区域调度 | 按当前资产恢复 Corin 全部实际使用状态；本 raw 的 Walk_Loop／Run_Loop 数组缺页、199条记录没有完整角色归属，不能自动导入为可琳完整表 |
| 参数与动作覆盖 | 已绑定 Corin 的实例快照及真实 setter | 搜索已有资源／raw 与调用者，恢复生效参数的动作覆盖和恢复顺序；不假定全部动作永远等于一个快照 |
| LockFoot | 完整字段／方法指令、配置 | 当前快照 FootLockInfo 数组缺页；需从其它已有快照／初始化和原指令恢复；缺少开启运行片段时如实标覆盖为空 |
| 最终骨骼消费 | Graph332430、Rig779516、控制口和 NodeTaskFBIK 已绑定 | FBIKSetting379248 的547字节数据缓冲在该raw缺页；先检索其他raw／已导出资源，核对 InScale 与求解配置 |
| 帧级等价 | 大量轮询样本、静态调用顺序、现有3C固定Trace | 区分可直接复算片段与不同步／缺局部量片段；不可把171379行称为171379次完整调用 |

这些是源资料恢复和实现完成条件，方向已确定为完整复刻。没有证据时记录精确资源／状态／字段／地址、已搜索来源和影响；不采用代用品并标为完成，也不因一处缺页停止其它已具备证据的算法迁移。

### 9. 与 current specs 和旧 change 的逐项处理

| 现行要求／任务 | 本 change 处理 | 理由 |
|---|---|---|
| 唯一 Constraint／Goal／FBBIK／Final Publication，Gameplay／Network隔离 | 保留 | 宿主装配与发布边界已正确 |
| Foot 首个 requirement 的 ad3527 保行为承诺 | 修改 | 本次明确改变目标算法，只保留系统边界和未修改模块 |
| Landing Prediction canonical Key／KCC Future Translation／最近Surface | 移除其旧业务要求，替换为原预测／查询／缓存合同 | 原版查询门与目标生成不由这些3C策略决定 |
| Ground Path两端、Capsule接触、Convex Hull、Future Body Workspace | 移除Foot消费者与专属配置；其他模块真正使用的公共服务保留 | 不保留原版不存在的第二目标来源 |
| 原五态／三维PlantWorldResidual／原Landing完成准入 | 替换为原区域、PredictState、查询和目标语义 | 不再把3C响应完成当作原版所有权 |
| Resolved Foot 保持233436目标／权重／连续性 | 移除旧requirement，新增紧凑原控制结果合同 | 最终结果来自原脚控制量；不能同时要求旧数值不变 |
| Primary Support／233436 Pelvis Spring／Handoff | 替换为原骨盆完整计算 | 保留观察，不叠加旧响应 |
| 旧change 6.18、6.24、6.31–6.35 的位置basis／残差／当前目标混合方案 | 撤销冲突任务，以本design第3–6节替换 | 正确保留坐标分型，算法按原输入重建 |
| 旧change 7.5–7.7 的3C骨盆硬Reach候选 | 撤销 | 原版参数与响应单独迁移，不恢复已否决实验 |
| 旧change 10.9 的 CaptureViewLease 链 | 采用current spec的Committed事实→DiagnosticEvent→Generated Capability | 不恢复已删除采样入口 |
| project.md 的旧Foot目标链与“无双Support不得补f” | 实施时替换为普通／预测支撑混合；禁止第二Pose混合继续有效 | 原误判混淆了两种混合对象 |
| Foot Analysis／Animation Pipeline／Presentation Authoring的原22条Curve与来源合同 | 区域表、状态元数据与曲线的唯一归属须在同一作者闭包明确，并为受影响能力补齐delta后实施；已有Phase等实际消费者保留 | 不把结构化原区段塞成第二Contact／Lock曲线或绕开现有作者入口 |
| TrainingEnemy禁区 | 保留资产／验收禁区，明确共享内核无旧路径 | 不为未迁移敌人制造兼容模式 |

本次 update skill 只允许修改状态返回的四份已存在 artifact，因此没有把新算法提前写成 current truth，也没有改写其他 change。上述冲突已定位，但仍存在于 project／旧 change 的原文；实施开始先按此表清理其计划口径，正式安装本 delta 后才更新 current truth。不能同时执行旧change的相反任务。

## Risks / Trade-offs

- [控制目标已复刻但物理脚仍不同] → 分别保存动画输入、控制参数、Solver与物理输出；逐项核对Rig、原生配置和InScale，不靠调权重掩盖。
- [完整原算法在3C场景里表现不同] → 原算法等价与项目环境效果分开评估；先检查碰撞几何、Owner运动、区域和参数输入。复刻正确的段不因旧评分降低就改回3C公式。
- [完整替换与小步可撤销冲突] → 按独立模块翻译，在唯一编译链切换相依闭包；未接入段不宣称已生效，不创建old/new运行开关。
- [原代码存在多种质量／骨盆／平台分支] → 将其作为有来源的正式配置和同一算法条件分支，按实际Corin配置验收；不将原有分支删除成简化版，也不制造无来源分支。
- [轮询覆盖误当调用覆盖] → 按E3字段与原字节核对，只对输入完整片段作公式复算；没有局部量的片段保留观察结果和覆盖缺项。
- [新增宿主输入破坏其它角色] → Corin成套迁移；共享算法保持单一。TrainingEnemy未迁移的资源明确无效，不修复其内容、不提供默认兼容。
- [完整复刻范围蔓延为整套游戏重建] → 覆盖表逐项限定Foot直接数据依赖与消费者；原生引擎通用运行壳使用项目现有实现，行为差异明确记录。

## Migration Plan

1. 使用E1–E6建立精确版本、字段、方法和资源覆盖表。保留旧提交记录：`9b6b6aaec`恢复旧Foot基线，`3d75913b3`只增加部分配置，`9cfe69505`只增加状态槽位；它们都不是完整复刻完成证明。
2. 先恢复区域／参数／绑定数据，完成Owner运动、事件输入、remainTime和状态顺序；每个字段只有一个写入阶段。
3. 逐函数迁移普通地面几何、预测、LockFoot、两份高度历史和普通／预测混合，再迁移完整脚目标与最终权重。
4. 完成原骨盆两模式、PD／速度／权重和Finalize顺序；将已编码脚结果、骨盆结果及历史一起提交到唯一根Bank。
5. 成套替换Corin Profile、Calibration、区域数据和生成物，删除旧Foot专属算法／字段／消费者，保留无关模块及原始研究／失败档案。
6. 在既有诊断框架增加真名映射的正式阶段事实，分别完成原证据对账与Corin同输入回放，核对目标与实际骨骼。
7. 每个实施切片形成中文提交；失败证据先保留，只有确认由本切片引入的错误才撤销本切片，不回退用户其它修改。

现有固定输入为 [43357ff3cd384e5cba75d2c31175b116](../../../3cDemo/Client/3C_Client/Diagnostics/CharacterInputTraces/20260827-183705-081-43357ff3cd384e5cba75d2c31175b116.json)，本次SHA256为 `24d97232f35246c0b85a003b5980ac8f199d6ff63e9f74a0001b082f57eb89a6`。[历史baseline proof](../../../3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260901-near-ground-rotation-adoption/baseline-proof.json)只作历史定位；开始实施必须另行锁定当时代码、Profile、Projection、Program、World、初始状态与Presentation Schedule的同一有效baseline，不能把已回退f2dbbf8a9视为当前有效结果。

原语义对账要求分支、事件、索引、配置来源完全一致；有限Float32标量和位置按 `abs(error)<=1e-5+1e-5×abs(reference)` 核对，单位向量／四元数另外记录角误差，允许误差上限0.001度。它们是本项目离线对账阈值，不声称是ZZZ的内部阈值；临界分支仍必须按原操作次序解释。缺输入不能计入通过数。

Corin效果对账沿用现有Replay和诊断程序，保存逐脚穿透、接触距离、位置／旋转跳变、腿长比、膝盖弯曲及骨盆速度的数值和代表帧，不调整评分或生成第二报告器。只有原语义与最终场景两类证据都闭合，才可称完整完成。手动视觉验收不写入tasks。

## Open Questions

没有待选的替代算法方向：用户已选择完整复刻。第8节的具体资料恢复、复杂函数翻译和最终消费核对必须完成；取得与本文不同的原始证据时，先修正文档与对应任务，不凭猜测维持旧结论。

## 实施对账：82个原方法

2026-09-04 展开任务1.4。下表只表示每个方法的迁移归属与已知缺项，不表示其算法已经实现。签名、RVA和指令数来自E1的命名阅读版与方法JSON；入参中的引用属性继续以原签名为准。错误／热替换／运行壳按design既定边界处理，表内“待迁移”包含对应普通计算分支与必要外部primitive；不可从函数存在推断动态分支已覆盖。

原列表17／18也是Pelvis调试accessor，故调试范围为17–42；00–16是参数／初始化接口。分类纠正不改变运行代码。

| 序号 | 原方法签名／证据 | 指令数 | 输入 → 输出 | 计划Owner与状态写入 | 当前缺项 |
|---:|---|---:|---|---|---|
| 0 | [void .ctor()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/00__ctor_171DBD10.asm) | 48 | 公开初值与构造常量 → 初始化后的实例参数 | Profile／Foot初始化；写实例初值 | 原初值、数组与初始化分支待迁移 |
| 1 | [void .cctor()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/01__cctor_171DBE60.asm) | 15 | 静态常量／缓存定义 → 类静态初始状态 | 固定参数与有界共享资源初始化 | 原静态写入需逐项归属，不复制全局可变Foot状态 |
| 2 | [float get_RealRayCastHeight()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/02_get_RealRayCastHeight_171CD970.asm) | 60 | 查询高度状态／平台状态 → 普通查询高度 | 查询参数；只读有效配置 | 场景／平台分支待迁移 |
| 3 | [float get_RealPredictRayCastHeight()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/03_get_RealPredictRayCastHeight_171CDA70.asm) | 60 | 查询高度状态／平台状态 → 预测查询高度 | 查询参数；只读有效配置 | 场景／平台分支待迁移 |
| 4 | [float get_IKWeight()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/04_get_IKWeight_171CDB70.asm) | 2 | 实例IKWeight → float | 正式表现参数；只读 | 值来源与绑定待接入 |
| 5 | [void set_IKWeight(float value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/05_set_IKWeight_171CDB80.asm) | 2 | value → IKWeight | 正式表现参数；写IKWeight | 原setter调用来源待接入 |
| 6 | [Vector3 get_HipDelta()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/06_get_HipDelta_171CDB90.asm) | 6 | 实例HipDelta → Vector3 | Foot公共结果／状态；只读 | 原消费者待对账 |
| 7 | [void set_HipDelta(Vector3 value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/07_set_HipDelta_171CDBB0.asm) | 5 | value → HipDelta | Foot公共状态；写HipDelta | 原调用者与消费顺序待对账 |
| 8 | [void set_DisableDamping(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/08_set_DisableDamping_171CDBD0.asm) | 2 | value → disableDamping | 原一次性命令；写disableDamping | 原触发者及脚／骨盆消费顺序待接入 |
| 9 | [void set_IsOnMovingPlatform(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/09_set_IsOnMovingPlatform_171CDBE0.asm) | 87 | value／原平台状态 → 平台及逐脚离开记录 | 平台输入应用；写平台与查询状态 | 保留20条补回指令对应的离开分支 |
| 10 | [bool get_EnableLockFootReal()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/10_get_EnableLockFootReal_171CDD30.asm) | 27 | EnableLockFoot／isOnMovingPlatform → bool | 脚锁门控；只读 | 按原getter接入，不等同区域isLocking |
| 11 | [void set_EnablePelvisIkRot(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/11_set_EnablePelvisIkRot_171CDD90.asm) | 2 | value → enablePelvisIkRotTarget | 骨盆参数；写旋转目标开关 | 目标到当前状态推进待接入 |
| 12 | [void set_PelvisIkWeight(float value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/12_set_PelvisIkWeight_171CDDA0.asm) | 2 | value → pelvisIkWeightTarget | 骨盆参数；写权重目标 | Prepare内当前权重响应待接入 |
| 13 | [void set_PelvisCastDist(float value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/13_set_PelvisCastDist_171CDDB0.asm) | 2 | value → pelvisCastDist | 骨盆参数；写查询距离 | 原调用来源与查询消费待接入 |
| 14 | [void SetStateDependParams(bool enableStrideWrapping, float footOnGroundHeight, float footOffGroundHeight, float footUpVelocityLimit, float footDownVelocityLimit, float pelvisUpVelLimit, float pelvisDownVelLimit)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/14_SetStateDependParams_171CDDC0.asm) | 53 | 七个显式参数 → 当前状态参数 | Tuning应用；写当前七项 | 独立于默认参数；原调用来源待接入 |
| 15 | [ValueTuple<bool, float, float, float, float, float, float> GetStateDependParams()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/15_GetStateDependParams_171CDEF0.asm) | 44 | 当前七项 → 原七元素tuple | Tuning读取；只读 | 原tuple字段顺序与调用者待对账 |
| 16 | [void ResetStateDependParams()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/16_ResetStateDependParams_171CDFB0.asm) | 25 | 公开默认七项 → 当前状态七项 | 参数重置；写当前七项 | 仅原字段恢复，不清理其它Foot历史 |
| 17 | [bool get_EnablePelvisDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/17_get_EnablePelvisDebug_171CE020.asm) | 2 | EnablePelvisDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 18 | [void set_EnablePelvisDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/18_set_EnablePelvisDebug_171CE030.asm) | 2 | value → EnablePelvisDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 19 | [bool get_EnableLockFootDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/19_get_EnableLockFootDebug_171CE040.asm) | 2 | EnableLockFootDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 20 | [void set_EnableLockFootDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/20_set_EnableLockFootDebug_171CE050.asm) | 2 | value → EnableLockFootDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 21 | [bool get_EnableFootMovingDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/21_get_EnableFootMovingDebug_171CE060.asm) | 2 | EnableFootMovingDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 22 | [void set_EnableFootMovingDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/22_set_EnableFootMovingDebug_171CE070.asm) | 2 | value → EnableFootMovingDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 23 | [bool get_EnableOrdinaryHitGroundDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/23_get_EnableOrdinaryHitGroundDebug_171CE080.asm) | 2 | EnableOrdinaryHitGroundDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 24 | [void set_EnableOrdinaryHitGroundDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/24_set_EnableOrdinaryHitGroundDebug_171CE090.asm) | 2 | value → EnableOrdinaryHitGroundDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 25 | [bool get_EnablePikHitGroundDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/25_get_EnablePikHitGroundDebug_171CE0A0.asm) | 2 | EnablePikHitGroundDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 26 | [void set_EnablePikHitGroundDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/26_set_EnablePikHitGroundDebug_171CE0B0.asm) | 2 | value → EnablePikHitGroundDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 27 | [bool get_EnableFinalHitGroundDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/27_get_EnableFinalHitGroundDebug_171CE0C0.asm) | 2 | EnableFinalHitGroundDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 28 | [void set_EnableFinalHitGroundDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/28_set_EnableFinalHitGroundDebug_171CE0D0.asm) | 2 | value → EnableFinalHitGroundDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 29 | [bool get_EnableTargetDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/29_get_EnableTargetDebug_171CE0E0.asm) | 2 | EnableTargetDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 30 | [void set_EnableTargetDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/30_set_EnableTargetDebug_171CE0F0.asm) | 2 | value → EnableTargetDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 31 | [bool get_EnableRaycastDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/31_get_EnableRaycastDebug_171CE100.asm) | 2 | EnableRaycastDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 32 | [void set_EnableRaycastDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/32_set_EnableRaycastDebug_171CE110.asm) | 2 | value → EnableRaycastDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 33 | [bool get_EnableAnimDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/33_get_EnableAnimDebug_171CE120.asm) | 2 | EnableAnimDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 34 | [void set_EnableAnimDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/34_set_EnableAnimDebug_171CE130.asm) | 2 | value → EnableAnimDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 35 | [bool get_EnablePIKDrawDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/35_get_EnablePIKDrawDebug_171CE140.asm) | 2 | EnablePIKDrawDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 36 | [void set_EnablePIKDrawDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/36_set_EnablePIKDrawDebug_171CE150.asm) | 2 | value → EnablePIKDrawDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 37 | [bool get_EnablePredictiveMovingDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/37_get_EnablePredictiveMovingDebug_171CE160.asm) | 2 | EnablePredictiveMovingDebug → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 38 | [void set_EnablePredictiveMovingDebug(bool value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/38_set_EnablePredictiveMovingDebug_171CE170.asm) | 2 | value → EnablePredictiveMovingDebug | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 39 | [float get_GizmosLastTime()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/39_get_GizmosLastTime_171CE180.asm) | 2 | GizmosLastTime → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 40 | [void set_GizmosLastTime(float value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/40_set_GizmosLastTime_171CE190.asm) | 2 | value → GizmosLastTime | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 41 | [int get_FootIndexToWatch()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/41_get_FootIndexToWatch_171CE1A0.asm) | 2 | FootIndexToWatch → 原返回类型 | 现有只读诊断／视图设置；不写Foot业务历史 | 只读观察映射未接入 |
| 42 | [void set_FootIndexToWatch(int value)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/42_set_FootIndexToWatch_171CE1B0.asm) | 2 | value → FootIndexToWatch | 现有只读诊断／视图设置；不写Foot业务历史 | 调试设置映射未接入；不新增运行参数 |
| 43 | [void Initialize()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/43_Initialize_171CE1C0.asm) | 944 | 骨骼／Animage／配置 → 绑定、缓存和初始化结果 | Rig绑定与Foot初始化 | 944条指令的绑定／退化／外部接口待迁移 |
| 44 | [void InitializeFootLockState()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/44_InitializeFootLockState_171CF3E0.asm) | 193 | 初始骨骼／实例记录 → FootLockInfo及相应初值 | Foot初始化；写原脚锁记录 | 默认记录与初始化位置待核对 |
| 45 | [void InitSceneDependParams()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/45_InitSceneDependParams_171CF760.asm) | 107 | 场景条件／默认高度 → 生效查询参数 | 场景参数应用 | 48条补回指令及原场景分支待迁移 |
| 46 | [bool DebugFocusedFoot(int footIndex)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/46_DebugFocusedFoot_171CF960.asm) | 30 | 调试选择 → 是否关注当前实例／脚 | 现有只读诊断选择 | 不进入脚目标算法；原显示能力另行映射 |
| 47 | [void ApplyPlayerMotion()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/47_ApplyPlayerMotion_171CF9C0.asm) | 86 | Owner世界高度／lastHeight → isMoving、isRaising、lastHeight | Foot实例运动；写运动历史 | 对象绑定、死区、保留isRaising分支待迁移 |
| 48 | [void Prepare()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/48_Prepare_171CFB80.asm) | 581 | 动画／运动／当前参数 → 本轮脚与骨盆输入、当前权重 | Foot输入准备；写原准备状态 | 581条指令、原骨骼读取与权重响应待迁移 |
| 49 | [Vector3 CalculatePredictFootTarget(int footIndex)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/49_CalculatePredictFootTarget_171D0650.asm) | 442 | 脚点／时间／运动／几何 → 预测脚目标 | Foot预测计算 | 442条指令及原坐标／预测移动分支待迁移 |
| 50 | [void PredictFoot(int currentFootIdx)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/50_PredictFoot_171D0EE0.asm) | 564 | 单脚PredictState／运动／查询 → 原预测记录 | Foot预测更新；写PredictState | 564条指令、记录／平台／enablePIK分支待迁移 |
| 51 | [void PredictDrawDebug()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/51_PredictDrawDebug_171D1B70.asm) | 37 | 预测记录／显示开关 → 调试绘制 | 现有只读诊断视图 | 不建立运行组件；16条补回指令仅作为显示依据 |
| 52 | [bool IsInPIKState(float& stateIKWeight)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/52_IsInPIKState_171D1BF0.asm) | 158 | 原状态／PIK标签／过渡／平台 → bool及stateIKWeight | 模式计算；写返回权重 | 四类过渡及原bool返回判据待接入 |
| 53 | [bool PreprocessPredictionIK(float& pIkWeight)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/53_PreprocessPredictionIK_171D1EB0.asm) | 79 | 状态／pIkWeight／EnablePIKWarp → needPIK及逐脚enablePIK | 模式准备；条件调用PredictFoot | 原调用、清理及权重淡出分支待迁移 |
| 54 | [bool CrossCheck()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/54_CrossCheck_171D1FB0.asm) | 96 | 原双脚几何／配置 → CrossCheck结果 | 交叉判断；只读本轮输入 | 96条指令的原判断待迁移 |
| 55 | [void LockFoot(int footIndex, Vector3 footPosLocal, bool isCross, Vector3& footLockXZLocalOffset)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/55_LockFoot_171D2160.asm) | 444 | 单脚几何／PIK／阻塞／锁定参数 → 锁脚位置及FootLockInfo | 原脚锁更新；写单脚锁记录 | 444条指令、进入／保持／退出及坐标待迁移 |
| 56 | [void PreprocessAnimPos(int footIndex, bool needPIK, bool isCross, float pIkWeight)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/56_PreprocessAnimPos_171D29C0.asm) | 212 | footIndex／needPIK／isCross／pIkWeight → 锁脚后Foot／Toe输入 | 单脚预处理；写本轮缓存 | 原LockFoot调用门和212条指令待迁移 |
| 57 | [float HipHeightLiftingDelta(float straightestLegLength, Vector3 rootJointPos, Vector3 endJointPos)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/57_HipHeightLiftingDelta_171D2DB0.asm) | 100 | 原脚几何／当前参数 → 抬升高度结果 | 脚几何候选 | 100条指令含24条补回指令待迁移 |
| 58 | [ValueTuple<float, bool> HipHeightLiftingDeltaByMinDist(int footIndex, float shortestAllowableDist, Vector3 rootJointPos, Vector3 endJointPos)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/58_HipHeightLiftingDeltaByMinDist_171D2F40.asm) | 149 | 脚编号／最小距／骨盆及目标 → 腿距抬升候选 | 脚几何候选；供骨盆消费 | 原有效性／距离分支待迁移 |
| 59 | [bool IsFootMoving(int footIndex, Vector3 footPos, Vector3& posDelta)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/59_IsFootMoving_171D31A0.asm) | 273 | 本轮／上次脚输入及门控 → 是否移动与原输出参数 | 普通查询门 | 273条指令、缓存／阈值／平台分支待迁移 |
| 60 | [RaycastHit GetRaycastHit(Vector3 startPoint, float castDistance)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/60_GetRaycastHit_171D3680.asm) | 88 | startPoint／castDistance／层 → 原RaycastHit | 唯一世界查询adapter | 原生重载、过滤及无命中语义待核对 |
| 61 | [Vector3 GetDeltaFromRigidbodyToTransform(Transform transform, Rigidbody rigidbody)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/61_GetDeltaFromRigidbodyToTransform_171D3830.asm) | 150 | 原Rigidbody与Transform及命中 → 对应位置差量 | 世界输入／几何primitive | 150条指令、对象与坐标绑定待迁移 |
| 62 | [void HitGround(int footIndex, Vector3 footGlobalPos, Quaternion footGlobalRot, Vector3 toeGlobalPos, float castHeight, Vector3& hitPos, Vector3& hitNormal)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/62_HitGround_171D19A0.asm) | 101 | Foot／Toe／高度／质量配置 → hitPos、hitNormal | 查询分派；只选择原明确模式 | 完整／低质量两分支待接入 |
| 63 | [void HitGroundSimpleImpl(int footIndex, Vector3 footGlobalPos, Quaternion footGlobalRot, Vector3 toeGlobalPos, float castHeight, Vector3& hitPos, Vector3& hitNormal)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/63_HitGroundSimpleImpl_171D3B40.asm) | 803 | 原多点实参与查询结果 → 简化模式支撑位置／方向 | 普通地面几何 | 803条指令的组合／退化／无命中出口待翻译 |
| 64 | [void HitGroundImpl(int footIndex, Vector3 footGlobalPos, Quaternion footGlobalRot, Vector3 toeGlobalPos, float castHeight, Vector3& hitPos, Vector3& hitNormal)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/64_HitGroundImpl_171D4BA0.asm) | 1237 | 原多点实参与查询结果 → 完整模式支撑位置／方向 | 普通地面几何 | 1237条指令的组合／退化／无命中出口待翻译 |
| 65 | [ValueTuple<Vector3, Vector3> OrdinaryIkHitGround(int footIndex)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/65_OrdinaryIkHitGround_171D65E0.asm) | 173 | 单脚输入／是否移动／历史 → 普通支撑位置／方向 | 普通支撑计算与缓存 | 重新查询、缓存使用与出口待迁移 |
| 66 | [ValueTuple<Vector3, Vector3> PredictIkHitGround(int footIndex, bool needPIK, Vector3 ordinaryHitPos, Vector3 ordinaryHitNormal)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/66_PredictIkHitGround_171D6920.asm) | 819 | 普通支撑／needPIK／PredictState → 预测支撑与记录历史 | 预测支撑与高度历史 | 接触极性／3倍阈值已核对；819条指令整体待迁移 |
| 67 | [void DoCalculateTarget(int footIndex, Vector3 hitPos, Vector3 hitNormal, Vector3& targetPos, Quaternion& targetRot)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/67_DoCalculateTarget_171D7910.asm) | 879 | 支撑输入／原方向和标量历史／当前参数 → 脚位置旋转及新历史 | 唯一脚目标响应 | 879条指令的坐标、两类角限制、高度区间与旁路待迁移 |
| 68 | [ValueTuple<Vector3, Quaternion> CalculateFootTarget(int footIndex, Vector3 finalHitPos, Vector3 finalHitNormal)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/68_CalculateFootTarget_171D8A00.asm) | 286 | 目标／动画基准／IK及骨盆权重 → 混合目标与回归历史 | 原脚基准混合 | 位置、quaternion与缓存回归待迁移 |
| 69 | [void SetFootControlParam(int footIndex, Vector3 targetPos, Quaternion targetRot)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/69_SetFootControlParam_171D8F30.asm) | 132 | 脚目标／脚高／句柄 → 控制参数位置、旋转及InScale | 唯一Goal编码边界 | 原标量消费者未核对完成；不能直接当权重 |
| 70 | [void FinalizeFootIk(int footIndex, bool needPIK, Vector3 ordinaryHitPos, Vector3 targetPos, Quaternion targetRot, Vector3 finalHitPos, Vector3 finalHitNormal)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/70_FinalizeFootIk_171D9160.asm) | 258 | 普通／最终支撑与目标 → 原缓存、time和事件清理 | 单脚收尾；写该脚Pending历史 | 258条指令、变换与先脚后骨盆顺序待迁移 |
| 71 | [float MinHipsDelta()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/71_MinHipsDelta_171D95F0.asm) | 59 | 原双脚hipsDelta → 最小候选 | 骨盆候选归并 | 有效性与最小选择待迁移 |
| 72 | [float MaxHipsDelta()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/72_MaxHipsDelta_171D96D0.asm) | 59 | 原双脚hipsDelta → 最大候选 | 骨盆候选归并 | 有效性与最大选择待迁移 |
| 73 | [float CalculatePelvisTargetLegacy()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/73_CalculatePelvisTargetLegacy_171D97B0.asm) | 198 | 原脚候选／骨盆输入 → 普通模式标量候选 | 唯一骨盆候选 | 198条指令待迁移；不保留3C旧Spring |
| 74 | [float CalculatePelvisTargetAdcance()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/74_CalculatePelvisTargetAdcance_171D9AF0.asm) | 229 | 原脚候选／骨盆输入 → Advance模式标量候选 | 唯一骨盆候选 | 229条指令待迁移；与外层响应分开 |
| 75 | [ValueTuple<Vector3, Quaternion> CalculatePelvisTarget()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/75_CalculatePelvisTarget_171D9EE0.asm) | 618 | 候选／历史／原参数／查询 → 骨盆位置旋转与新历史 | 唯一骨盆响应 | 618条指令、PD／速率／权重／disableDamping消费待迁移 |
| 76 | [void OnFootPlant(FootType footType, bool isInZone, bool isSliding)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/76_OnFootPlant_171DAB60.asm) | 159 | footType／isInZone／isSliding／旧time和remainTime → 单脚状态与事件 | 单脚区域应用；写PredictState原事件 | 现有槽位仅部分存在；保留位／正式输入／Finalize尚未闭合 |
| 77 | [void Start()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/77_Start_171DADE0.asm) | 89 | 初始配置／绑定 → 原启用初始状态 | 既有角色初始化接入 | 原Initialize／脚锁／参数复制顺序待接入 |
| 78 | [void OnEnable()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/78_OnEnable_171DAFB0.asm) | 89 | 重新启用配置／绑定 → 原启用初始状态 | 既有角色启用接入 | 不新建MonoBehaviour；原重置范围待接入 |
| 79 | [void Update()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/79_Update_171DB180.asm) | 231 | Owner位置／时间／历史 → 实例运动量与lastPos | 既有表现准备阶段 | 231条指令、原时钟与运动计算待迁移 |
| 80 | [void OnAnimatorIK(int layerIndex)](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/80_OnAnimatorIK_171DB640.asm) | 306 | 全套正式本轮输入 → 两脚／骨盆控制量与历史 | 唯一Foot Operation内部顺序 | 306条指令的算法依赖分批接入；不复制引擎回调 |
| 81 | [void LateUpdate()](D:/ZZZ_Dump/PIK分析包/源码重建/真名阅读版/methods/81_LateUpdate_171DBCB0.asm) | 26 | OnLateUpdate委托 → 原条件分派 | 既有生命周期／明确回调适配 | 当前绑定为空不推定永远为空；禁止新增第二骨骼写入 |

覆盖核对：序号0–81各一次，合计82方法、11978条指令。区域OnUpdate、FindNextSegmentIndex和原生控制参数消费者属于额外直接依赖，继续按任务4／10登记，不伪装为上述82方法已经覆盖的内容。
