# PoseGraph r3 实现质量审查

日期：2026-09-14。范围：当前原生运行实现的源码审查。用户要求记录以下问题并通知既有 PoseGraph 实现窗口处理。

规划窗口：`01a09594-1751-7512-b8c0-08b04185055b`。实现窗口：`01a081f3-46f4-7c91-8930-73923ff7950b`。

本文是现有 r3 设计的实现缺陷记录，不增加第二套设计或任务清单。对应原任务 3.5、3.8、3.9、3.13、3.17；不重开已经完成的只读输入任务，不新增测试或验证任务。实现窗口在 `execution.md` 记录实际修正和提交。以下结论来自静态调用链与数据引用检查，未运行复现，不代表完整审查或当前编译通过。源码行号为记录时的位置。

## 总体判断

当前实现已保留节点实例隔离、既有 Source/Job 复用和唯一 Final Publication 等边界，但准备阶段、分支参与和已提交数据保护存在具体缺陷。下列问题保留首次发现时的触发依据；最新状态见文末“D19 对账与修正跟进”。增加节点数量不能替代修正这些公共执行语义。

## R1：准备阶段提前读取子图 Pose 输入

状态：实现记录已报告修正；当前复核范围见文末。优先级：P1。

代码位置以 `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/` 为根：

- `CharacterPoseNativeSubgraphHandler.cs:122`：`PrepareFrame` 调用 `BindInputs`；其第 311 行通过父 runtime 的 `ReadInputValue` 立即读取连接值。
- `CharacterPoseNativeClipPlayerHandler.cs:223`：`EvaluateOutput` 在 `m_Output` 尚未产生时抛出异常；第 287 行才在 Barrier 后的 `EvaluateFrame` 建立本帧 Pose 输出。

触发路径：Clip Pose 输出连接到子图 Pose 输入。父图进入 Prepare，子图立即读取该 Pose，但 Clip 尚未完成本帧采样，因此准备阶段失败。调整节点序列不能解决采样尚未发生的问题。

业务影响：正常的播放器到 Body/Control Rig 子图链路不能完成一帧。

修正要求：区分准备阶段可读取的控制输入和采样完成后才能读取的姿态输入。Prepare 沿节点依赖准备实际 source demand；Pose 输入在同次采样完成后的求值阶段交接，并保持父子调用的 completion identity 和独立实例状态。不使用上一帧 Pose、默认 Pose 或第二次 Animancer Barrier 绕过阶段问题。

取舍：需要明确输入所属阶段，子图调用实现会增加必要的阶段处理；换来采样顺序正确，并避免把普通子图连接变成隐式提前求值。

## R2：整图准备无条件推进未参与输出的播放器

状态：实现记录已报告修正；当前复核范围见文末。优先级：P1。

代码位置：

- `CharacterPoseNativeGraphEvaluator.cs:167`：遍历全部图节点并调用每个 handler 的 `PrepareFrame`，未根据本帧输出依赖或节点选择限制参与集合。
- `CharacterPoseNativeClipPlayerHandler.cs:195`：无条件 `SetRelevant(true)`，随后推进播放器时间并生成采样请求。

触发路径：图中存在未接入当前输出的播放器，或后续选择节点未选中的分支。当前 evaluator 仍准备该节点，播放器仍被标为 relevant 并推进。

业务影响：未参与姿态的动画提前播放、消耗采样资源；其资源未就绪还可能使本来有效的输出帧失败。节点缓存只能防止重复读取，不能代替分支参与判断。

修正要求：由根输出及当前节点选择确定本帧参与分支，保留节点业务定义的 relevance、进入/退出和时间语义。只为实际参与采样的节点建立需求并驱动对应阶段；共享分支在同一调用实例和阶段只推进一次。不重建旧 Image/Operation 调度表，不用全图 `SetRelevant(true)` 代替激活逻辑。

取舍：节点需要表达真实依赖与参与关系；可以减少无效采样，并保持作者对状态切换和动画起始时间的预期。已有节点明确配置的非活跃更新时间规则仍应保留，不能简单把所有未选中节点永久停住。

## R3：按 BeginFrame 翻页会覆盖最后成功提交的姿态

状态：实现记录已报告修正；当前复核范围见文末。优先级：P1。

代码位置：

- `CharacterPoseNativeClipPlayerHandler.cs:177` 与 `CharacterPoseNativeBlendPoseHandler.cs:76`：每次 `BeginFrame` 无条件翻转 `m_PageIndex`。
- Clip 的 `DiscardFrame` 只丢弃 Player 状态并清理帧字段，不恢复页选择；Blend 同样只清理帧字段。
- `CharacterPoseNativeGraphRuntime.cs:729`：保存 `evaluation.Output` 作为最后提交结果；其中 Native slice 仍引用节点缓冲，未复制为独立快照。

触发顺序：A 页提交成功；下一帧写 B 页后丢弃；再下一帧翻回 A 页并开始写入。此时最后成功提交的输出和观察仍引用 A 页，却已被未提交帧覆盖。即使第三帧随后也丢弃，旧结果的数据仍已改变。

业务影响：最后成功姿态和 Pose Watch 的数据与提交身份不一致，破坏失败帧不覆盖合法结果的承诺。

修正要求：明确区分已提交页与工作页；工作页始终避开仍被当前提交结果持有的页，只在 Commit 成功后交换身份。Discard 保持已提交页不动。沿同一模式检查 Clip、BlendSpace、Selected、Blend 和空间转换等节点及观察引用的持有期；需要延长持有期时使用正式租约释放，不复制整份姿态或恢复全图 Workspace。

取舍：页所有权必须随提交状态管理，不能只按帧数轮换；仍可保留按实例复用缓冲的内存策略，同时保证失败帧和观察读取不会改写已提交数据。

## 实施边界

由既有 PoseGraph 实现窗口在自身原生 runtime/handler 范围修正，保留已有正确的输入合同、Source 服务、IK 算法及最终发布所有权。若发现与其它窗口同一字段或文件的实际冲突，记录冲突交用户决定，不覆盖并行改动。本文不把 Host 尚未安装、旧引用尚未清完等实施进度缺口冒充新增缺陷，也不以这些问题修正完成代表整个 r3 已完成。

## R4：旋转混合双方互相对齐，反而保留了相反符号

状态：实现记录已报告修正；当前复核范围见文末。优先级：P1。

代码位置：`CharacterPoseNativeBlendPoseHandler.cs:300`、`CharacterPoseNativeLayeredBoneBlendHandler.cs:314`；数学入口为同级上层 `BlendStack/AnimationBlendPoseTypes.cs:46` 的 `AlignAndScale`。

两种 Blend 都计算 `AlignAndScale(base, overlay, baseWeight) + AlignAndScale(overlay, base, overlayWeight)`。当点积为负时两项一起取反，仍处于相反半球。取表示同一旋转的合法四元数 `q` 与 `-q`，权重各半，结果为 `-0.5q + 0.5q = 0`；随后 `BlendWeighted` 会抛出 degenerate rotation。这是代数推导，未运行 Unity。

业务影响：输入是相同骨骼朝向，仅四元数符号不同，混合仍会失败；一般点积为负的输入也会走错误的插值方向。

修正要求：所有参与项相对同一个旋转参考对齐后加权；复用现有数学入口正确的调用方式，保留既有权重及归一化合同。普通 Blend 与 Layered Bone Blend 一并修正，不通过零旋转替代或吞掉异常掩盖计算错误。

取舍：这是调用方式修正，无需更换混合算法或增加第二数学实现；保持动画采样对四元数等价符号的正常支持。

## R5：聚合子图采样需求时丢失调用实例身份

状态：实现记录已报告修正；当前复核范围见文末。优先级：P1。

代码位置：`CharacterPoseNativeSubgraphHandler.cs:130` 直接返回 child demand 的 Requests；`CharacterPoseNativeGraphEvaluator.cs` 把所有节点需求展平；`Contracts/Pose/CharacterPoseNativeRuntimeContracts.cs:619` 的 request 只保存 NodeId、SourceSlot、Required，第 646 行按 `HashSet<PoseNodeId>` 拒绝重复。

触发路径：父图的两个 Call 同时调用同一子图资产。两个独立子实例具有相同的作者 Player NodeId；父图汇总需求时把它们判成同一请求并抛出异常。虽然子 runtime 有 InstanceId，request 展平后没有保留这个区别。

业务影响：可复用子图不能在同一帧同时调用两次，独立节点时间与资源绑定也无法从汇总后的请求身份区分。

修正要求：采样请求保留实际图调用实例身份，去重和路由使用调用实例与节点组成的身份。同一实例内重复请求应拒绝，不同实例的同一作者节点必须可区分；沿现有 Source 合同消费该身份，不改作者 NodeId，不重建全图 Operation index。

取舍：请求需携带必要的实例标识，才能保持可复用子图的独立执行；只用作者 NodeId 虽短，却不满足实例隔离的业务含义。

## R6：节点提交前校验收到的是整图最终输出

状态：实现记录已报告修正；当前复核范围见文末。优先级：P1。

代码位置：`CharacterPoseNativeGraphEvaluator.cs:293` 的 `ValidatePending` 把同一个根 output 传给每个 handler；`CharacterPoseNativeSpaceConversionHandler.cs:251` 按当前转换节点的目标空间检查这个 output；Clip handler 的同名方法则要求 Local Pose。

触发路径：`Clip(Local) → LocalToComponent → ComponentToLocal → Output(Local)`。最终 Local Pose 被传给 LocalToComponent handler，该 handler 要求 Component Pose，因而拒绝本来合法的整条链。在 Component 输出子图中，Clip handler 也会拿到 Component 根输出并错误拒绝。

业务影响：正常空间转换链不能通过提交前校验。同类型链也存在校验了根结果却没有检查本节点候选输出的问题。

修正要求：节点校验自己的本帧候选输出、完成身份和私有状态；整图输出校验留在图边界或 Final Publication。校验只读取已完成候选，不重新求值，不为通过校验而放宽 Local/Component 类型检查。接口参数应明确代表节点结果还是图结果。

取舍：需要分清两种校验对象，避免一个通用 output 参数混淆职责；保留严格空间规则，同时允许合法的空间转换组合。

## R7：Blend 在候选求值时直接改写已提交的连续性历史

状态：实现记录已报告修正；当前复核范围见文末。优先级：P2。

代码位置：`CharacterPoseNativeBlendPoseHandler.cs:112` 在 EvaluateOutput 调用 ResolveContinuity；第 236 行起直接写 m_LastBaseContinuity、m_LastOverlayContinuity、m_LastWeight、m_ContinuityIdentity。Commit 与 Discard 都只调用 ClearFrame，第 487 行的 ClearFrame 未回滚这些历史字段。

触发路径：上一成功帧输入身份/权重为 A；候选 B 在 Blend 求值时覆盖历史，随后下游失败并 Discard；下一成功帧恢复 A 时，Blend 仍按未提交 B 作为上次状态比较，并生成不同的连续性身份。

业务影响：节点历史包含从未发布的帧，后续连续性判断不再以最后成功状态为依据。此问题独立于 R3 的缓冲页覆盖，即使页选择修好仍存在。

修正要求：将本帧候选连续性状态与已提交历史分开，在 Commit 成功时接纳，Discard 保留此前合法历史；单调编号可继续递增，但不能把未提交输入记录成上一成功输入。同步检查新 Blend 系列 handler 是否复制了该模式。

取舍：只需少量节点状态字段表达候选与已提交历史，无需复制姿态或增加全局状态容器。本条不把“权重变化是否意味着不连续”自行判定为新设计，应继续遵循正式节点的连续性语义。
## D19 对账与修正跟进

依据：主方案更新提交 `399581706`，本目录 `design.md` 11.7/11.9、`tasks.md` 第 3 组，以及主方案 D19、spec-audit Q14/Q15。已读取当前 `execution.md` 的 R1—R7 修正记录并检索工作区对应实现。以下只更新本审查的当前事实；不重发旧问题，不改实现记录，不增加任务。

### R1—R7 当前状态

| 问题 | 本次源码可见变化 | 当前结论与边界 |
|---|---|---|
| R1 | Subgraph 的 BindInputs 分为 Prepare 控制输入与 Evaluate 姿态输入 | 原来的无差别提前取值已调整；跨 handler 求值顺序及实际子图链尚未运行复核 |
| R2 | evaluator 从图输出反向建立可达节点集合，只驱动集合内节点 | 孤立节点推进已针对性修正；静态可达不等于本帧动态参与，状态/Linked 等具体服务与动态分支仍待接通核对，不把本项全部结案 |
| R3 | Blend 等节点按 committed page 选择工作页，并在 Commit 接纳页身份 | 已看到提交页与工作页分离；完整节点集合、观察租约和失败路径仍需继续源码对账，未作运行通过结论 |
| R4 | Blend 的旋转和改为 Base 加权项加上相对 Base 对齐的 Overlay；实现记录说明 Layered 同步修正 | 原错误已针对性处理，不重复要求重做数学模块；尚无本轮运行证据 |
| R5 | Source request 携带 ScopeInstanceId，去重使用实例与 NodeId | 请求身份已针对性修正；正式 Source 装配及下游路由仍属未完成接线 |
| R6 | 节点 handler 的 ValidatePending 不再接收根 output，节点使用自身候选值 | 已修正接口对象混淆；最终图/发布边界由各自入口继续负责 |
| R7 | Blend 增加 committed 连续性字段，BeginFrame/Discard 恢复历史，Commit 接纳候选 | 已看到针对性状态修正；实现记录还报告 Layered/Additive 同模式收口，不能据记录推定所有节点及运行已通过 |

这些条目是修正跟进，不把旧缺陷原文当作当前仍完全未处理的事实，也不把源码变化当作完整角色验收。

### 具体服务、装配与下游的剩余边界

| 范围 | 已有代码事实 | 仍需完成的职责 | 原任务/所有者 |
|---|---|---|---|
| Clip / BlendSpace / Selected | 原生 handler、Source binding 与现有采样 Job 调用 | 正式实例装配注入，并让同次采样结果进入实际图输出 | Pose 3.9/3.11；共享 Host 核心 4.7 |
| Blend / Layered / Additive / 空间转换 / 惯性化 | 已有具体值运算或 policy 处理，不是仅接口 | 按正式图建立实例、保留状态和输入语义，接到实际输出消费者 | Pose 3.9/3.13 |
| StateMachine | 有 handler；全 Main C# 检索 ICharacterPoseNativeStateMachineSource 仅见声明、字段、构造参数 | 实现实际状态转换服务并注入，不能以 handler 存在视为状态行为完成 | Pose 3.9/3.10 |
| Slot / BlendStack | 已有 handler；Slot 有具体 Source 实现，BlendStack 有 Source module binding | 核对实际构造注入、动作命令及下游采样消费；不能统一称为尚无实现 | Pose 3.9/3.11 |
| Linked / MotionMatching | 当前对应 Source 接口检索仅见声明与注入引用 | 接入各自正式选择/绑定服务及调用实例，保留原业务状态所有权 | Pose 3.10 |
| Foot / Goal / FBBIK | 已有 Constraint 节点适配 | 与当前 IK 服务真实注入、次序及候选结果交接收口，不重写算法 | Pose 3.12；算法仍归 IK owner |
| Final Publication | CharacterFinalPoseNativePublication 及物理/属性写入入口已存在；原生 runtime 有接收 publication 的 Commit 重载 | 完成领域结果/Writer 的实例装配与最终消费者接入；服务存在不代表角色调用已发生 | Pose 3.14；共享 Host 核心 4.7 |
| Handler factory / Host / Barrier | factory 已有接口及入参；本次精确检索未见角色对原生 Create/Replace 的调用 | Pose 提供可安装的真实领域能力，核心拥有共享 Host 与唯一 Barrier 接线 | Pose 原 3.1—3.17；核心 4.7 |

删除顺序以 D17 和已更新 3.18 为准：先删已经取消的 Image 专属职责，允许中间错误；不以 Host 未接完为由保留已取消职责。已有编译 Pass 删除不重复执行；混合文件保留有效算法和作者定义，同文件并行冲突按原规则处理。原 20 项完成记录不变。