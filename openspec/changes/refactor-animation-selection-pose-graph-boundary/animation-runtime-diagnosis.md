# 双端帧同步动画异常诊断账本

## 文档用途

本文档持续记录双端帧同步场景中的动画异常，目的是把用户现象、运行日志、资产事实、代码事实和推断分开，逐项排除原因。

状态定义：

- `已证实`：日志、Agent Snapshot 或代码能够直接证明。
- `已排除`：其预测与已捕获现象冲突，不能作为当前主因。
- `并存缺陷`：确实有问题，但不能解释当前全部现象。
- `主因候选`：已经形成代码到表现的完整解释，但仍需修复后用原场景反证。
- `待验证`：缺少能够作出结论的证据。

只有满足以下两个条件，原因才能从`主因候选`升级为`已确认根因`：

1. 修复只改变该原因对应的变量。
2. 原双端场景中的同一组现象不再复现。

## 当前结论

Run 与 MovingTurn/TurnBack 不可见的根因已经确认：

> PoseGraph 迁移后，Animation Selection 的普通终止生命周期没有形成“当前选择退出”的正式状态变化。正常播放收到`CompleteProducer`或`ReleaseProducer`时只记录 terminal，Lifecycle 对`Complete`和`Release`也只校验 PlaybackId；只有回滚事件的`Retire(SelectProducer)`路径会把`m_Selections`改为 Empty。因此已经结束的 FullBodyAction 可以继续保持 Selected，有限 BaseLocomotion producer 也可能继续被当成当前 Sample source。

这个候选同时解释：

- MovingTurn 的 MotionCurve 已经驱动逻辑位移和转向，但骨骼表现被残留的 FullBodyAction 全身覆盖。
- Run、WalkStart 等有限 BaseLocomotion source 结束后仍被继续请求，随后成为`SourceIncomplete`。
- 新 producer 接管时 Pose 暂时恢复，表现为 WASD、攻击和冲刺衔接时闪一下。
- 动画链失败后逻辑位置仍正常，因为 MotionCurve/Fixed 模拟和 Presentation Pose 采样不是同一条输出链。

代码端已经按 Selection availability 修复该缺口：

- `AllowEmpty`通道在当前 playback 收到更新的 terminal 后，对 PoseGraph 提交正式 Empty。
- `RequireSelection`通道不因 terminal 被伪造为 Empty，必须等待逻辑层提交下一位正式 winner。
- source usage、marker sample 和 Player 选中判断统一读取同一份 effective Selection。
- raw Selection 和 sampling history 继续保留，用于 rollback 撤销 terminal 后恢复正式选择。
- 同一 playback 的 Complete 与 Release 按 EventId 分别保留，回滚撤销较新的 Release 时不会丢失仍然有效的 Complete。

2026-07-23 用户已在重新构建后的原双端帧同步场景中确认 Run 和转身动画恢复。修复只改变 terminal 后 Selection 的表现有效性，没有修改 Run/MovingTurn 状态边、MotionCurve 或动画资产，因此 H12 已满足“单变量修复 + 原场景反证”，升级为`已确认根因`。

当前继续追踪两个未闭合问题：

1. `P1 攻击衔接闪帧`：Attack 能播放，但攻击 Timeline 内两个 Animation Clip 的衔接附近会闪一下。优先区分 Clip 覆盖空洞、权重接缝、同一 producer 内 playback identity 切换，以及 FullBodyAction 退出混合四类边界。
2. `P2 动作后摄像机偶发异常`：攻击或冲刺等动作结束后，摄像机有时出现跟随异常。需要区分摄像机 Follow/LookAt 引用变化、逻辑根与 VisualRoot 瞬时偏移、动作 RootMotion 发布时序，以及相机朝向状态被动作状态污染。

这两个问题不再使用 RunStart 入边、BlendSpace 或“动画整体没有播放”解释；它们必须分别在 Timeline/Pose 播放边界和 Camera target 消费边界取证。

## 用户现象基线

以下内容只记录用户实际观察，不把解释混入现象：

1. 两个客户端都会出现。
2. 逻辑位置持续更新，模型曾长期停在出生点；后续位置链修复后位移能够正常。
3. WASD 按下时动画会闪一下。
4. Run 没有正常循环动画，动画会卡住。
5. Run 状态内能够触发 MovingTurn/TurnBack 的 RootMotion，但看不到对应动画。
6. Attack 或 Shift 能触发动作，但动作衔接闪烁，动作可能停在原地或覆盖其它动画。
7. 动画报错或失效后，模型曾瞬间追上逻辑位置；之后逻辑位置同步仍然正常。
8. 当前问题集中在动画表现，不是角色是否产生逻辑位移。

后续每次复现必须与这组现象对照，不能用附近的其它报错代替当前问题。

## 可重复反馈入口

当前反馈入口是正式双端帧同步运行产物及其自动日志：

- 场景：Deterministic Rollback Network Test，Peer A 与 Peer B。
- 当前证据日志目录：`3cDemo/Client/3C_Client/Build/Network/RunLogs/DeterministicRollback/20260723-162504`
- 当前干净构建身份：`20260723-081743`
- 构建选项：Development、StrictMode、CleanBuildCache。

当前限制：

- 复现需要用户操作客户端，尚无可自动输入的最小复现脚本。
- 日志已经能观测 Selection、Player、Pose availability、source time、weight、Pose hash 和 Inertialization。
- 日志不能直接观测普通`CompleteProducer/ReleaseProducer`是否到达以及到达后是否改变`m_Selections`。

在补充 terminal 边界日志前，不能把“terminal 一定到达”视为已证实。

## 已证实的完整事实

### MovingTurn 逻辑与动画 producer 都已进入

Agent Snapshot 已确认 MovingTurn 状态使用`CorinMovingTurnTimeline`：

- producer：`04758f73-9b79-4142-ab05-5c4159ea6ba8`
- AnimationTrack：`e1e9df79-5033-4856-857e-0060a28517f2`
- channel：`BaseLocomotion`
- animation asset：`Corin_Pipeline_MovingTurn_Inplace.anim`
- AnimationTrack 与 MotionCurveTrack 均覆盖帧`0..71`
- playback mode：Once

日志第 251、259、275、283 行显示该 source：

- 处于`BaseLocomotion:Selected/Pose`
- `Sample=True`
- 时间从`0.0166666657`推进到`0.2`
- PoseWatch hash 持续变化

结论：MovingTurn 的状态、Timeline producer、动画 source 和 Player 采样都实际发生了。不能再用“状态没有进入”“动画没有绑定”“source 没采到”解释这一次 MovingTurn 无动画。

### MovingTurn 同期存在权重为 1 的 FullBodyAction

同一批日志第 251、259、275、283 行同时显示：

- FullBodyAction producer：`86e3cd9c-eab8-41b0-b2f5-e9a73c3cfa27`
- Player：`31359528bfe141d2a51e1d5843935ca6`
- phase：`Selected`
- availability：`Pose`
- weight：`1`
- source time 从`1.070732`继续推进到`2.4956944`

第 291、299、503、519、535 行仍可见同一 FullBodyAction generation 长时间保持 Selected，source time 最终推进到`6.11648226`。

结论：MovingTurn source 本身在产生骨骼 Pose，但最终合成时存在持续的全身 Action Pose。当前“有 MovingTurn RootMotion、无 MovingTurn 可见动画”与 FullBodyAction 残留覆盖直接一致。

### BaseLocomotion 会进入 SourceIncomplete

日志第 203 行显示 BaseLocomotion 仍为`Selected/Pose`，source time 为`1.18333328`。

日志第 211、219 行随后显示：

- Final：`Invalid/SourceIncomplete`
- InvalidOperation：`SelectedPosePlayer`
- BaseLocomotion：`PendingFirstSample/Invalid`
- source：`Invalid`
- Sample：`False`
- FullBodyAction 同时仍为`Selected/Pose`

第 447 到 463 行再次出现同样转换，证明不是单次偶发。

结论：PoseGraph 没有整体冻结；特定 BaseLocomotion source 从可用 Pose 转成了 SourceIncomplete。

### PoseGraph 时钟和骨骼采样没有整体停摆

在有效帧中：

- source time 正常推进。
- PoseWatch hash 持续变化。
- Inertialization 经历`Capture`、`Rebase`、`Continue`和`Complete`。
- 最终 Pose 在部分帧正常发布。

结论：不能把问题归因于整个 PoseGraph 不 Evaluate、Animancer 全局停播或渲染帧时钟完全冻结。

### 修复前正常 terminal 不改变当前 Selection

代码事实：

1. `CharacterAnimationPlaybackRuntime.PublishTerminal`只向`m_Terminals`追加记录。
2. `Present`把 terminal 转成 Lifecycle command，然后清空`m_Terminals`。
3. `AnimationPlaybackLifecycle.ApplyCommand`处理`Complete`和`Release`时只验证 PlaybackId，未修改 ChannelState。
4. `CharacterAnimationPlaybackRuntime.BuildPlayerSourceUsages`遍历`m_Selections`，只要 Selection 和 Sampling 仍存在，就继续声明`PlayerSourceUsageKind.Sample`。
5. 只有`CharacterAnimationPlaybackRuntime.Retire`处理被回滚撤销的`SelectProducer`时，会按 EventId 把对应 channel 的`m_Selections`改为 Empty。

结论：修复前普通播放结束和回滚撤销使用了不同的 Selection 退出语义，没有一条与普通 terminal 对应的“当前 Selection 变 Empty 或切换到新 winner”的明确路径。

修复后不改写 raw`m_Selections`，而是在唯一表现边界投影 effective Selection：

- FullBodyAction 是`AllowEmpty`，terminal 后投影 Empty。
- BaseLocomotion 是`RequireSelection`，terminal 后仍保持逻辑 winner，直到逻辑层提交下一位 producer。
- rollback Retire 按 EventId 删除对应 terminal，effective Selection 随同一 raw Selection 恢复。

### terminal pose 保持条件无法覆盖仍被标记为 Sample 的当前 source

`TimelineAnimationPoseRequestResolver`只在`clipCount == 0 && holdTerminalPose`时钳制到有限 Timeline 的末端 Pose。

调用方只对“Retained 且不是 Sample”的 usage 开启`holdTerminalPose`。仍保留在`m_Selections`中的 source 会被`BuildPlayerSourceUsages`标记为`Sample`，因此有限片段越界后不会进入末端保持路径。

结论：有限 BaseLocomotion source 在结束后仍作为当前 Sample 被请求时，出现`SourceIncomplete`符合当前代码行为。

## 原因排除表

| 编号 | 假设 | 状态 | 证据与结论 |
|---|---|---|---|
| H1 | VisualRoot 没识别，导致当前全部动画异常 | 已排除为当前主因 | MovingTurn source 已在 Player 中产生 Pose，Pose hash 变化；逻辑位置与 RootMotion 也能工作。VisualRoot 曾影响位置表现，但不能解释同帧 FullBodyAction 长期 Selected 和 Base SourceIncomplete。 |
| H2 | 摄像机跟随了错误逻辑对象 | 已排除为当前动画主因 | 摄像机/模型分离是早期位置投影问题的表现；当前日志中的故障发生在 Animation Selection、Player 和最终 Pose availability 内。它不能制造 FullBodyAction 残留 Selection。 |
| H3 | 双端帧同步或 Fixed 模拟没有产生移动 | 已排除 | 用户确认逻辑位置持续更新，MovingTurn RootMotion 能触发；两端都出现相同表现问题。当前断点位于 Presentation Pose，不是 Fixed 移动结果。 |
| H4 | MovingTurn 状态没有进入 | 已排除 | MovingTurn producer `04758f73...`已经成为 BaseLocomotion Selected source，并持续采样。 |
| H5 | MovingTurn 动画资产缺失或没有绑定 | 已排除 | Agent Snapshot 中 AnimationTrack 正式绑定`Corin_Pipeline_MovingTurn_Inplace.anim`；运行日志中 source 有合法 Pose 和变化中的 hash。 |
| H6 | MovingTurn 只有 MotionCurve，没有 AnimationTrack | 已排除 | 同一 Timeline 的 AnimationTrack 与 MotionCurveTrack 都覆盖`0..71`帧。 |
| H7 | PoseGraph/Animancer 整体没有推进 | 已排除 | source time、Pose hash、Inertialization 状态均持续变化。 |
| H8 | 增量编译或旧 Player 缓存造成当前问题 | 已排除 | 使用 CleanBuildCache 的完整 Player 构建后，双端运行仍复现同一现象。 |
| H9 | RunStart 缺少入边导致所有现象 | 并存资产缺口，不是当前主因 | Agent Snapshot 显示 RunStart 没有正式入边。但当前 Shift 链不是 Walk→RunStart：DodgeForward 写入`HasDirectionalDodgeRunIntent`，ActionOverride 释放后按现行合同直接进入 RunLoop，再由 RunLoop 进入 MovingTurn。这个事实与用户观测到的 RootMotion 一致。RunStart 可达性需要按独立业务入口处理，不能解释已经进入 RunLoop/MovingTurn 后动画不可见。 |
| H10 | Run/TurnBack 动画文件本身全部坏掉 | 已排除为统一主因 | MovingTurn source 能生成变化中的 Pose。Run 资产仍需单独核对 loop mode、长度和绑定，但资产损坏不能统一解释 Action 残留与 Base SourceIncomplete。 |
| H11 | LayeredBoneBlend 配置主动要求 Action 永久全身覆盖 | 待验证 | 当前证据能证明 Action 权重为 1，但还需核对 ActionWeight 的正式输入和空 Selection 后的节点输出。即使 Mask 正确，结束后仍不应长期保持旧 Action Selected。 |
| H12 | 普通 terminal 没有终止当前 channel Selection，导致残留覆盖和有限 source 越界 | 已确认根因 | 代码链、修复前日志和修复点一致；用户已在重新构建后的原双端场景确认 Run 与转身恢复。修复没有改状态边、MotionCurve 或动画资产，完成单变量反证。 |

## 当前问题 P1：攻击 Timeline 衔接闪帧

### 已知现象

1. Attack 动画能够触发和播放，不再是 FullBodyAction 永久残留造成的“所有动画不可见”。
2. 闪烁集中在攻击动画衔接处，用户判断可能与 Timeline 内两个 Animation Clip 的连接有关。
3. 当前双端日志已经捕获到 Attack Pose 有效、但 BaseLocomotion 先失效并使 Final Pose 整体 Invalid 的完整链路。
4. 五条 Attack Timeline 的两个 Animation Clip 都是首尾精确相接，没有 overlap，也没有 self ease；这是确定存在的硬切配置，但不能解释日志中的持续 SourceIncomplete。

### 排查分支

| 编号 | 假设 | 状态 | 可证伪预测 |
|---|---|---|---|
| A0 | FullBodyAction 有 Pose，但已结束的有限 BaseLocomotion 仍作为当前 Sample 越界，导致 Final Pose 整体 Invalid | 已证实，当前第一修复目标 | Frame 17652 起 Attack1 保持`Selected/Pose, Weight=1`，Base MovingTurn `04758f73...@5`变为`PendingFirstSample/Invalid`，Final 明确为`Invalid/SourceIncomplete`；同一 Invalid 跨 Attack2、Attack3 延续。 |
| A1 | 两个 Animation Clip 在 Timeline 帧区间之间存在空洞 | 已排除 | 五条 Attack Timeline 的两个 Animation Clip 都是首尾同帧相接：49、48、81、89、125，没有帧区间空洞。 |
| A2 | 两个 Clip 时间相接但没有重叠/混合，边界权重发生硬切 | 已确认视觉根因，修复中 | 用户在A0修复后的新Build中确认，闪烁精确发生在攻击主体Animation Clip与收武器Animation Clip接缝；五条Attack均为0 overlap、0 self ease。 |
| A3 | 同一 Attack producer 的 Clip 接缝被错误发布为新 playback/source | 待验证 | 状态和 producer 不变，但接缝处 PlaybackId、generation 或 Player source identity 改变。 |
| A4 | 闪烁实际发生在 FullBodyAction 退出到 BaseLocomotion，而非 Timeline 内部 | 待验证 | 闪帧 tick 紧邻 Complete/Release，FullBodyAction 从 live source 进入 Stored Pose/Empty；Timeline 内部 Clip 接缝没有异常。 |
| A5 | 第二个 Clip 的 Avatar/Root Transform 或导入设置与第一个不一致 | 待验证 | Timeline 采样始终有效，但第二段开始时骨骼根、姿态或 Root Transform 突跳；资产元数据存在不一致。 |

### Attack Clip authoring 边界

当前 Agent v17 的正式 Timeline Clip 写能力只有：

- `move_timeline_clip`：起止帧一起平移。
- `configure_timeline_clip_ease`：只配置片段自身 Ease。

它不能在保持第二段结束帧不变的前提下单独提前开始帧。因此：

1. 用`move_timeline_clip`把第二段提前会同步提前结束，在 Action Timeline 尾部制造新的无 Animation Clip 区间。
2. 两段仍然首尾相接时配置 SelfEase，会让接缝两侧分别衰减到零，不能形成两段 Pose 的交叉混合。
3. 直接修改 Unity YAML 会绕过 Agent transaction、stable identity、validator 和 revision，不采用。
4. 已批准的`refactor-agent-authoring-to-synced-json-document`明确冻结 v16/v17 Patch，不再增加 operation；不能在本修复中私自扩展 v18 宽 Patch。

新Build现场已经确认同一接缝帧仍闪烁。进一步核对五条Timeline的Tree生命周期后，现有`move_timeline_clip`可以无副作用完成修复，不需要新增range mutation：

| Attack | 收武器Animation原范围 | Tree结束帧 | 平移后范围 | Overlap |
|---|---:|---:|---:|---:|
| Attack1 | 49..168 | 162 | 43..162 | 6帧 |
| Attack2 | 48..173 | 167 | 42..167 | 6帧 |
| Attack3 | 81..206 | 200 | 75..200 | 6帧 |
| Attack4 | 89..282 | 276 | 83..276 | 6帧 |
| Attack5 | 125..212 | 206 | 119..206 | 6帧 |

只平移第二段Animation Clip，不平移MotionCurve：

1. 第二段Animation与第一段形成6帧交叉混合。
2. 第二段新的结束帧恰好等于对应Tree生命周期结束帧，没有尾部空洞。
3. MotionCurve继续使用原时间范围，保持已经正确的RootMotion位移采样。
4. 修改走Agent stable identity、dry-run、apply和validator，不增加新operation或第二条资产路径。

### 已捕获的 A0 证据

Peer A 的 actor A 在 Frame 17600：

- BaseLocomotion 是 MovingTurn producer `04758f73-9b79-4142-ab05-5c4159ea6ba8`，source time `0.8666667`，Pose 有效。
- FullBodyAction 是 Attack1 producer `10f4cb90-8b9a-4944-b77c-14efc9a3124d`，刚开始进入 BlendStack。

Frame 17631：

- MovingTurn source time 已推进到 `1.04999924`。
- Attack1 已经是`Selected/Pose, Weight=1`。

Frame 17652：

- BaseLocomotion 变为`PendingFirstSample/Invalid`，InvalidOperation 明确指向 Base 的`SelectedPosePlayer`。
- Attack1 仍是`Selected/Pose, Weight=1`，source time `0.35`。
- Final 变为`Invalid/SourceIncomplete`。

Frame 17725 至 17872：

- FullBodyAction 已按连击依次进入 Attack2 和 Attack3，二者一直有合法 Pose 和满权重。
- Base 仍引用同一个已结束的 MovingTurn playback，并持续 Invalid。
- Final 因 Base 输入无效而持续 Invalid。

结论：当前至少一类“攻击闪一下/动画丢失”发生在有限 Base producer 结束后。`RequireSelection`只保留 selection identity 还不够；terminal 后继续把有限 source 当作 Sample，仍会越界。正式修复应让 terminal 后仍被 Required channel 选中的旧 source转为 Retained，并由既有 terminal pose 规则保持末帧，直到逻辑层提交下一位 Base winner。不得把 FullBodyAction Pose 直接绕过 Final 图，也不得新增默认 Idle fallback。

代码已按这条边界修改：

- Required channel 的当前 selection 在 terminal 之后仍保留正式 playback identity。
- 该 playback 的 Player usage 从`Sample`切换为`Retained`，不再继续越界采样。
- Timeline resolver 复用既有 terminal pose 保持语义，输出有限片段的末帧 Pose。
- 新的 Base winner 到来时仍按正式 Selection 接管；没有新增 Idle、Animator 或资产 fallback。

### 需要捕获的最小证据

1. 闪帧前后至少各 3 个表现帧的 Tick、producer、PlaybackId、source time、clip identity、clip weight、availability 和 Pose hash。
2. 对应 Attack Timeline 的两个 Animation Clip 的精确起止帧、ease/overlap、clip-in、speed 和绑定动画资产。
3. 闪帧发生在 Timeline 内部还是 FullBodyAction terminal/退出混合边界。

## 当前问题 P2：动作后摄像机偶发异常

### 已知现象

1. 问题发生在动作之后，并非每次稳定触发。
2. 早期“摄像机跟随虚空、模型停在出生点”属于已经修过的位置表现问题，不能直接等同于当前摄像机异常。
3. Corin 当前 Projection 没有 Camera producer，Attack/Dodge Timeline 也没有 Camera Track；动作不会通过 Program 切换 Camera target、state 或 response。
4. Rollback prefab 的 CameraFollowAnchor 是角色逻辑根，CameraAimAnchor 是逻辑根下固定的`(0, 1.25, 0)`；两者都不在动画骨骼或 VisualRoot 下。
5. Body branch replacement 会重置 Animation 和 FootPlacement，但 Camera 没有消费同一个 ResetSequence；Cinemachine 的 PreviousState 会跨修正前后继续保留。

### 排查分支

| 编号 | 假设 | 状态 | 可证伪预测 |
|---|---|---|---|
| C1 | 动作结束后 Camera Follow/LookAt 引用被替换或短暂失效 | 已排除为当前主因 | 当前 Projection/Timeline 没有 Camera producer；CameraRig 每帧把 FreeLook 重新绑定到显式的持久 Follow/Aim target。 |
| C2 | 相机目标引用稳定，但逻辑根与 VisualRoot 在动作退出帧出现瞬时偏移 | 已排除为已捕获时段主因 | 当前空间日志中 Visible、VisualRoot before/after 与 AnimationRoot 一致，且没有 Animation mutated VisualRoot；仍保留异常现场复核。 |
| C3 | 动作 RootMotion 的位置发布正确，但朝向/相机 yaw 状态没有在退出时恢复 | 待验证 | 位置连续，角色或相机 heading 在动作 terminal 附近跳变或冻结。 |
| C4 | Body branch replacement 后 Camera 没有清除修正前的 Cinemachine 跟随历史 | 主因候选 | 同一 ResetSequence 已被 Animation 和 FootPlacement消费，Camera 没有对应入口；`SnapTargets`也没有使`PreviousStateIsValid=false`。RootMotion/转身动作放大修正前后空间差，因此问题呈现为动作后偶发。 |
| C5 | Cinemachine 状态或 recenter/lock-on 参数被动作逻辑写入后未清理 | 已排除为当前主因 | 当前没有动作 Camera producer；Camera runtime 始终解析为 FreeLook base，不存在动作留下的 state/target/response 请求。 |

### 当前摄像机修复边界

正式修复只处理 Body stream discontinuity：

1. Camera runtime 记录上一帧 Body ResetSequence。
2. ResetSequence 改变时，先把本帧正式 follow/aim point 写入目标。
3. 保留玩家当前 FreeLook X/Y orbit 值，只把 Cinemachine `PreviousStateIsValid`置为 false。
4. 在同一表现帧 ManualUpdate，使相机从修正后的目标重新建立状态。
5. 普通动作帧、普通移动帧继续使用现有 damping，不每帧强制 snap。

这条修复不引入动作专属摄像机逻辑，也不把相机绑定回 VisualRoot。

代码已按该边界修改：

- Camera runtime 以 Body ResetSequence 识别初始化、committed branch replacement 和 selected stream reset。
- discontinuity 帧仍先解析正式 FreeLook/Target plan，再要求 CameraRig 重建 tracking state。
- CameraRig 保留当前 orbit 轴和本帧 look input，只清除 Cinemachine 的`PreviousStateIsValid`并在同帧 ManualUpdate。
- 非 discontinuity 帧继续沿原有 Apply 路径更新，阻尼行为不变。

### 需要捕获的最小证据

1. 异常动作前、动作中、动作后 Camera Follow/LookAt 的实例身份和有效性。
2. 同 tick 的逻辑 Actor 根、Presentation 根、VisualRoot、Camera target 和相机自身位置/朝向。
3. 动作 Complete/Release、RootMotion 发布、Presentation Commit 和相机更新的先后顺序。

## 当前因果链

### Action 覆盖链

1. Attack/Dodge producer 成为 FullBodyAction 当前 Selection。
2. Action Timeline 进入完成或释放阶段。
3. 普通 terminal 只进入`m_Terminals`和 Lifecycle command，不改变`m_Selections`。
4. 下一表现帧继续从`m_Selections`提交同一 FullBodyAction Selection。
5. `BuildPlayerSourceUsages`继续把旧 Action source 声明为 Sample。
6. FullBodyAction BlendStack 继续输出权重为 1 的全身 Pose。
7. BaseLocomotion 的 MovingTurn Pose 虽然已采样，却在最终组合中不可见。
8. MotionCurve 属于模拟/逻辑位移链，因此 TurnBack RootMotion 仍然正确。

### Base SourceIncomplete 链

1. WalkStart、RunStart、MovingTurn 等有限 producer 成为 BaseLocomotion 当前 Selection。
2. 有限 AnimationTrack 到达末端。
3. Selection 没有在普通 terminal 后退出，旧 source 仍被声明为 Sample。
4. `holdTerminalPose`只服务于 Retained 非 Sample source，当前旧 source 不满足条件。
5. Timeline sample 越界后返回零 clip。
6. SelectedPosePlayer 发布`Invalid/SourceIncomplete`。
7. Required BaseLocomotion 使最终 Pose 也变为 Invalid。
8. 下一个 producer 或 generation 接管时 Pose 恢复，形成肉眼可见的闪烁。

## 仍需补齐的证据

现有 Playback lifecycle trace 已能在下一次运行中显示 effective Empty、Player source usage、BlendStack entry 和 Pose availability。先使用这条正式 diagnostics 链收集以下证据，不再新增第二套日志：

1. 普通`CompleteProducer/ReleaseProducer`之后，FullBodyAction 是否在下一表现帧成为 Empty。
2. FullBodyAction BlendStack 是否从旧 live source 进入 Stored Pose 退出混合并 exact release。
3. BaseLocomotion 是否持续拥有有效 Selection 和 Pose。
4. MovingTurn 的 source Pose hash 是否反映到 Final Pose。
5. 是否仍出现单帧`SourceIncomplete`或旧 Action 回闪。

## 修复后的反证条件

修复必须在原双端场景中同时满足：

1. Attack/Dodge 完成后，旧 FullBodyAction playback 不再长期处于 Selected。
2. AllowEmpty FullBodyAction 明确进入 Empty，并让 BaseLocomotion 正常通过。
3. 已结束的有限 Base producer 不再以当前 Sample 身份越界到`SourceIncomplete`。
4. MovingTurn 被触发时，其 Base source Pose hash 变化能够反映到最终可见 Pose。
5. RunLoop 连续推进，不因 RunStart 或其它有限 producer 结束而卡住。
6. WASD、Shift、Attack 的切换不再产生单帧 Invalid 或旧动作回闪。
7. 两个客户端的逻辑位置、模型位置和动画状态同时保持一致。

如果修复 H12 后第 1 至 3 项通过，而 Run 仍不能进入，则单独处理 H9 的状态机入边；不能把两个问题重新混为一个问题。

## 当前发布与运行现场

- Player BuildId：`20260723-090701`
- 双端 RunId：`20260723-171423`
- 运行日志目录：`3cDemo/Client/3C_Client/Build/Network/RunLogs/DeterministicRollback/20260723-171423`
- 进程拓扑：两个`3C_Client.exe`和一个`ThirdPerson.DeterministicRollback.Server.exe`
- Relay持续报告`invalid=0`、`dropped=0`
- 当前两端日志中没有`Final=Invalid`、非`None`的`InvalidOperation`、`SourceIncomplete`、stale lease或运行时异常
- 已记录到Idle、WalkStart、WalkLoop、RunLoop、RunEnd、MovingTurn、Attack1和Dodge的合法 Pose
- 当前空间日志中Visible、RootBefore、RootAfter和AnimationRoot保持一致

当前日志证明新构建不再复现旧的统一Pose失效，但还不能把A2判定为已修复：现有diagnostics是事件/间隔采样，没有精确覆盖Attack两段Animation Clip的接缝帧。动作后的相机观感也仍需要本次运行现场的用户侧观察，不能仅凭Camera target连续就宣告完成。

## 维护记录

### 2026-07-23

- 建立首版诊断账本。
- 固化用户现象基线。
- 记录 clean build 后仍复现，排除增量编译缓存作为当前主因。
- 记录 MovingTurn producer、AnimationTrack、MotionCurveTrack 和运行采样证据。
- 记录 FullBodyAction 长期 Selected、BaseLocomotion SourceIncomplete 证据。
- 将“普通 terminal 未形成 Selection 退出”列为最高优先级主因候选。
- 将 RunStart 缺少入边降为并存缺陷，不再用它解释 Run 状态内 MovingTurn 无动画。
- 实现 terminal 的 effective Selection 投影：AllowEmpty 进入 Empty，RequireSelection 保持逻辑 winner。
- 让 Player source usage、marker sample 与选中判断统一读取 effective Selection。
- 保留 raw Selection 与 sampling history，使 rollback 撤销 terminal 后能够恢复。
- 按 EventId 保留同一 playback 的多条活跃 terminal，避免撤销 Release 时遗失仍有效的 Complete。
- 确认 Shift 的正式链路为 DodgeForward→ActionOverride→RunLoop→MovingTurn，没有为当前表现故障擅自新增 RunStart 边或 BlendSpace。
- 使用重新构建后的原双端帧同步场景完成用户侧反证：Run 与转身动画恢复。
- 将 H12 从`主因候选`升级为`已确认根因`。
- 新增 P1“攻击 Timeline 衔接闪帧”账目，拆分五条可证伪原因。
- 新增 P2“动作后摄像机偶发异常”账目，拆分五条可证伪原因。
- 从当前双端日志确认 A0：Attack Pose 有效时，已结束 MovingTurn 仍作为 Base Sample 越界，使 Final Pose 持续`Invalid/SourceIncomplete`。
- 从 Agent v17 Snapshot 确认五条 Attack Timeline 的两个 Animation Clip 都无空洞，但全部为 0 overlap、0 self ease 的硬切配置。
- 修改 Required channel 的 terminal source usage：保留 selection，但由`Sample`转为`Retained`并保持末帧，等待下一位正式 Base winner。
- 确认当前 Agent v17 不能无副作用地把 Attack 第二段起点提前并保持结束帧；拒绝用整体平移、SelfEase 或 YAML 修改制造分裂路径。
- 对照已批准的 Agent Document v1 change，确认 v16/v17 Patch 已冻结；Attack overlap 资产修改等待接缝帧反证后进入正式 Document mutation，不擅自增加 v18 宽 Patch。
- 确认当前 Corin 没有 Camera producer/Camera Track，排除动作相机状态未清理。
- 确认 Rollback Camera Follow/Aim 都绑定逻辑根系，排除动画骨骼替换 Follow 引用。
- 将“Body branch replacement 未同步清除 Cinemachine 跟随历史”列为 P2 当前主因候选。
- 实现 Camera 对 Body ResetSequence 的正式消费：仅在 discontinuity 帧重建 Cinemachine tracking history。
- 首次重建 Player 前确认 loaded Definition/Agent Snapshot 是 Source revision `798bcd5d...`，而 generated Presentation Projection 仍引用`70c91d07...`；Projection 的 Semantic与Contract hash也同时不一致。
- 使用 Presentation正式`build_producer_bindings`入口，以当前 Snapshot 中全部十四个 producer 的既有 Timeline source绑定原样重建 Projection、Float32和Fixed target，没有改写authoring配置。
- 重建任务成功，新的 Projection revision 为`508bab229f92ffcf22b5ae52492d6040bbcc9ce991d7ea2e09b06f9828dbb028`。
- 正式重建与Asset refresh后，Definition、Projection、Float32和Fixed统一为canonical Source revision `70c91d07...`与Semantic hash `2f9dc3bb...`；先前`798bcd5d...`是重建前loaded authoring revision，不能继续作为发布产物预期值。
- 首轮 Player Build 在新产物提交前中断，旧 BuildId `20260723-081743`未被覆盖；后续只从上述target重建成功状态重新发起一次Build。
- 完整构建成功并提交BuildId `20260723-090701`，Unity Console为0 error。
- 启动双端RunId `20260723-171423`，确认两个客户端和一个relay进程存活。
- 精确过滤两端当前日志：没有`Final=Invalid`、`SourceIncomplete`、stale lease或运行时异常；Relay持续`invalid=0`、`dropped=0`。
- 保持A2和相机观感为待现场反证，不用间隔采样日志冒充接缝帧或画面验证。
- 用户在BuildId `20260723-090701`现场确认闪烁精确发生在攻击主体Animation与收武器Animation接缝，将A2升级为已确认视觉根因。
- 核对五条Attack的Tree结束帧都比第二段Animation结束帧早6帧，确认现有`move_timeline_clip(-6)`可以同时形成6帧overlap并让Animation终点对齐Tree终点。
- 确定只移动第二段Animation Clip，不移动MotionCurve，避免改变已经正常的RootMotion业务行为。
