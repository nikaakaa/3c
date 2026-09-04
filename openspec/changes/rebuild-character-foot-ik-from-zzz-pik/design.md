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
| 00–18、43–45、77–81 | 构造／绑定／参数 setter／重置／启停／Update 与调用壳 | Profile、Rig、生命周期与宿主适配；原内部算法不遗漏 |
| 19–42、46、51及绘制分支 | 调试 accessor、选脚观察与绘制 | 映射现有只读 Diagnostics；不移植第二运行组件 |

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
