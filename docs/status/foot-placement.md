# Foot Placement 进展与证据入口

整理日期：2026-10-01。脚部质量尚未闭环；阶段报告归档不改变其中记录的失败事实，也不把编译、缓存 Apply 或单段修复当作整体行为验收。

用户新增的“平地上台阶时后脚跳一下”继续归入台阶换面问题。已对照历史与现行源码，将当前接触被下一步包络误抬、实际脚端跨面后末端补高、输入姿态／响应自身上移、重新着地新目标接入四种情况分开。最新录制两次约 20 cm 的接入前旧历史已经清空，不能套用 2dad 的陈旧残差原因。数值及坐标阶段见[后脚换面核对](../diagnostics/foot-placement/README.md#后脚换面核对2026-10-01)，[经验文档](../reference/foot-placement/implementation-lessons.md)已更新当前规则、保留／否决边界及第 80～82 条。

下一笔仍在唯一 Lifecycle 链内处理跨边推进与接触历史交接：目标脚掌查询和回写已有，尚缺整脚平移／旋转／高度在跨面时的连续推进。它与同帧双脚骨盆后的腿长检查共同收口，再覆盖完整动画切换。当前没有合格的联合推进候选，没有新的运行修复或 Replay 结果；本轮未操作 Unity。

本轮按用户要求先完成已有录制的离线筛选。“动画相对响应进入接触时改用当帧动画脚＋已有修正”的单点候选已标记 `rejected-offline-screening`：固定前态比较出现约束前腿长需求增加，原录制还确认净空水平冻结持续积累并由新接触承接。完整证据、方法和未验证边界见[离线结果](../diagnostics/foot-placement/contact-handoff-offline-20261001.json)，可在[原 HTML 的本轮结果](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#offline-comparison)拖动查看释放过程。本候选完整业务实际执行仍为 0 帧，未应用到生产 Assets；真正 Replay 留在离线筛选之后，当前 Play 未被操作。

本轮范围仍是动画混合、Landing/Releasing 弯直突变和台阶跳脚，复用已有采样、正式函数对照入口和各自 HTML；既有配套测试归属保持不变，本轮不新增测试或启动 Replay。

P1 接触期剩余腿长限制是独立候选，基线为 `fbed4b192`，见[候选补丁](../diagnostics/foot-placement/p1-contact-reach-candidate-20261001.patch)与[源码身份及接入状态](../diagnostics/foot-placement/p1-contact-reach-candidate-20261001.json)。候选由 Module 将本次实际加权骨盆位移交给 Completion，仅在正权重 Landing/Locked/Releasing 缩减有效踝修正的水平、向下分量，再按同一旋转与位置权重换回脚底修正、重查输出脚掌、防穿及写回既有历史。Completion 同步更新查询观察的标量与复用探针页，未扩充状态字段。后续独立装配已接入真实双脚骨盆，三份程序集编译通过，但完整业务实际完成 0 帧，结果为 [not-executed](../diagnostics/foot-placement/ik-tests/releasing-p1-result.json)；旧“尚未编译、调用方待迁移”已过期。候选未应用到正式 Assets。原 2023 种子及 2024～2056 的双脚比较仍须单列接触期新增悬脚，不能把几何估算 8→0 写成实测收益。

采样时间边界候选 `441f4140f` 已完成同一 33 帧的直接来源修正版 A/B，结论为 `rejected-quality-regression`。2033 解锁时机改正且右脚峰值降低，但更早两帧的目标超长恶化，超长帧数未减少，最大额外修正单步也增加。两生产文件已恢复到 `fbed4b192`；连续曲线、作者权重、残差、冻结及骨盆均未作为此候选变量。候选源码、实际构建输入和执行程序集已封存，后续报告不得从撤回后的工作区覆盖它们。具体数字与限制见[完整边界候选对照](../diagnostics/foot-placement/ik-tests/releasing-boundary-result.json)和[原因说明](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#release-cause-audit)。这一失败不能被写成起步粘脚、混合、边缘跳脚或伸直已经修复。

最新完整释放窗口已扩至 2024～2056（33 帧），使用 2023 真实种子。实际 ACL、Native Slot、Action Slot、预测、场景查询、双脚生命周期及骨盆连续计算通过；历史 Goal 脚位和旋转误差均为 0，原踝还原最大误差约 0.001 mm，两版 203 根骨局部位置、旋转和缩放一致，各计算段预热后为 0 B。来源修正将右脚目标超长从 10 帧减为 8 帧，相对原动画的最大修正向量单步从 43.4414 cm 减为 30.1888 cm；最大目标伸展比仍为 117.9642%，2031～2037 的连续拉直仍在，不能继续用后半段 12 帧的 5→3 代替完整窗口。左右脚各 33 帧最终脚掌目标查询均有证据，无新增正穿透；这不等于最终鞋网格验证。完整数据与失败校准见[同一业务报告](../diagnostics/foot-placement/ik-tests/releasing-business-summary.json)和[可播放说明](../diagnostics/foot-placement/ik-tests/releasing-action-native.html)。

完整 FBBIK 已实际执行两版，计算段为 0 B，但历史 2033 的六关节点最大误差为 0.240472 mm，超过测试原定 0.2 mm，整体仍为 `failed-ik-calibration`。直接替换为录制的六关节点输入或三个 Goal 都未消除偏差；没有放宽容差或将实际执行等同于通过。来源修正后的 2041／2042 右膝实际输出约 55.2747°／54.3377°，仅保留为校准未通过的求解结果，不作为已验收收益。原录制同两帧均约 0.51°；2040→2041 原动画自身只从 48.288° 到 46.254°，录制 IK 却从 66.771° 到 0.509°。2030→2031 原动画为 60.643°→38.376°，录制 IK 为 48.845°→0.512°，不能把这些额外变化归因于动画本来伸直。

2041 当前正式状态原因是 `NewEventContactAcquired`：新的 Action 接触创建 Landing，承接上一完成世界输出；2042 原动画脚已经超出锚点滑动范围，以 `ContactOutOfSlideRange` 进入 Releasing，2043 原有水平残差继续衰减，仍有额外追赶。当前 Action 没有下一落点，2041 起正式路径为 `NextLandingUnavailable`；这不是把旧 Run 的未来路径硬留给 Action。报告曾在整段计算后读取复用路径页，错误地把全部 33 帧 Accepted 写成 false；改为当帧保存必要标量后重跑，历史右脚 2030～2041 为 Accepted，实际 Goal 数值未变。该错误只属于报告保存，不能用旧 false 列判断运行链路没有消费路径。

连续脚高混合候选已实际比较并撤回。它只按实际左右脚贡献混合 `FootHeight`，Native 历史保存同一混合值，未更改接触事件、模式、锁权重、Support、作者总权重或任何骨骼姿势。第三版 Native／Foot 计算通过且为 0 B，但相对已保留的来源修正，右脚 2038 目标伸展比从 97.7843% 增至 98.9226%，2037、2039～2044 的腿部余量也变差；最大额外修正向量单步从 30.1888 cm 增至 30.2999 cm，2031～2036 则完全不变。完整窗口仍为 8 帧超长，最大 117.9642%，无新增正穿透不能作为接受依据。FBBIK 仍保留前述校准失败，拒绝该候选不依赖未通过校准的膝角数据。失败证据和源码归档在[第三版结果](../diagnostics/foot-placement/ik-tests/releasing-height-summary.json)；三个生产成员逐字节核对后，未提交运行候选已精确撤回，保留原来源及 Native 大参数修复。归档脚本随后重生成时与撤回发生竞态，错误覆盖原 ZIP；已从保留的执行输入重建到诊断目录，三个生产成员仍逐项匹配原 SHA，新的归档 SHA-256 为 `ae1b1ec9a0ada07d3f338b1a2861057607f2c5d47607d7e30e7d5138a5860b6b`。不能将新 ZIP 冒充原文件。撤回后 Animation 定向编译 0 警告、0 错误（7.03 秒），已关闭 build server。

`FootHeight` 是高于作者步间基线的高度；旧 Feature 的 `SoleHeight` 是脚跟、脚尖较低点的绝对高度。两者不能直接替换，也不能由根相对高度差直接断定目标错误。即使曲线标量按权重连续混合，层级骨骼的实际混合脚位与正式事件、路径、接触残差也未因此一致。下一步须处理这些量之间的实际关系，不能重试同一标量平均或用另一平滑参数掩盖此次回归。

原生混合的帧计算已从 Unity 时钟读取中分离：`AnimationSlotBlendJob.ProcessAnimation` 读取 `AnimationStream.deltaTime`，交给同一个 `EvaluateFrame(float)`；完整校验、混合、Stored 捕获、历史提交及输出发布均由该函数执行。固定采样实验可直接提供记录的帧时间，不需要伪造 AnimationStream 或另写混合算法。函数体除移出时钟读取外逐字一致，Animation 定向编译 0 警告、0 错误（6.97 秒）。这是正式计算入口的整理，Native Slot 业务与 2041 释放效果仍待配套窗口实际验证。

该完整入口的首次 Mono 执行在 `CommitPersistentState` 抛出 `InvalidProgramException: Passing an argument of size '10200'`，未生成有效结果帧。实际 Stored 状态从 9816 增至 10200 字节，History 从 9808 增至 10192 字节；新增 FootMotion 后继续用 `NativeArray<T>` 索引器提交整份状态触及大参数边界。修正改为直接引用已有 Native 元素，逐字段提交 Stored/History/Scratch，原地清零并读取；Workspace 重置同样改为原内存清零，Runtime 的 Stored 身份消费者只返回所需的 `ulong`，不再返回完整状态。未增加每帧容器，也未修改混合公式；Animation 定向编译 0 警告、0 错误（6.64 秒）。

配套窗口已用对应工作区源码 SHA 在 Mono 实际重跑 2035～2046 共 12 帧：正式 ACL 解码、虚拟骨、Native Slot 历史提交和完整 Action Slot 通过。原踝位置最大还原误差 0.000994 mm、旋转误差 0，两版 203 根骨的局部位置全相等，预热后的帧计算为 0 B。2041 动作实际权重 0.531232，旧贡献为 0.282207429；当前选中 Action，正式曲线的脚高为 0.0232933685 m、接触为 0.8675726、锁权重为 0，事件为 `5517395441386351926`，不再用 Run 的 0.457684726 m 脚高。生产修复为 `d6d6ab9f9`，配套窗口将[实际结果](../diagnostics/foot-placement/ik-tests/releasing-action-native.json)、[源码绑定](../diagnostics/foot-placement/ik-tests/releasing-action-native-provenance.json)和[可播放业务说明](../diagnostics/foot-placement/ik-tests/releasing-action-native.html)提交于 `7bbc98bec`。本段未执行 Stored 捕获、FootFeatures 混合、脚端预测、Physics、完整 FBBIK 或 Burst 调度，也未执行 Runtime 的 Stored 身份读取；该消费者仅经定向编译。新动作事件对释放目标的影响仍待完整脚端还原，不能据此宣布拉直已修复。

释放段的骨盆初态有明确重建来源：2034 的正式结果为 Releasing，响应已计算且未完成，目标为 0、输出 −0.00117490336、速度 0.008545625、位置权重 1；PrimarySupport 明确为空。按 `ResolvePelvisRelease` 与 `AdvancePelvisResponse` 的提交赋值，支持侧为 default、事件为 0、坡度 Flat，世界 Goal 为该帧 AnimatedPelvis 加沿 up 的加权输出，HasValue 与 HasGoalWorldPosition 均为 true。2035 的 previous-output、previous-velocity、previous-target 和 previous-slope 与此一致。因此可以用 2034 正式输出种下 2035 连续双脚／骨盆实验的状态，不能用默认 Spring，也不必为这个状态一律回溯到 1768 的零权重帧；仍需历史函数重现后才能比较候选。这不补齐 FBBIK 弯曲历史。

为使预测函数直接消费真实采样边界，`CharacterFootPlacementModule.PredictFootPair` 与 `PredictEvent` 的时间线参数收窄为 `hasMotionTimeline` 和 `trajectoryGeneration`；此前它们只读这两项，`currentSegmentRemainingSeconds` 只转传且未被消费，现已删除。唯一正式调用方从同一已提交 timeline 提供原值，完整 timeline 仍交给需要它的身体轨迹生成。固定输入可使用已有 `input/motion-timeline-available` 与 `input/timeline-generation`，不必编造未采样的 owner 字符串。两函数体在精确参数替换后保持逐字一致，Animation 定向编译 0 警告、0 错误（6.39 秒）；完整预测和释放效果仍由配套窗口继续执行验证。

缺少 FBBIK 方向历史不等于每个窗口都无法继续还原。源码 `ApplyLegBendStabilization` 在原动画膝盖到髋踝轴的高度大于腿长 1% 时，会用当帧原姿势覆盖稳定方向，并由当前原轴与目标轴计算应用方向；旧应用方向此时只参与 previous-dot 诊断，不控制方向符号。对已有 full 的 2023～2056 双脚 68 条原骨记录进行几何核对，最低高度比仍为 0.02054072（2033 右脚），全部高于正式阈值。这支持用真实 2023 原姿势与 Goal 预滚后尝试本段完整求解，不需要编造四个方向向量；但尚未实际验证 FBBIK，原历史计数和首帧 previous-dot 也没有因此被还原。仍须核对 Rig、Profile、全部 Goal，并让历史版实际求解的髋、膝、踝重现录制。该结论仅限已检查窗口，不能推广到原动画近乎完全伸直的其它样本。

首个动画混合修正将 FootMotion 从 `CharacterPoseSourceModule` 正式采样写入预分配 Source 页，经 `AnimationPrimitivePoseContribution` 传播，在 `AnimationSlotBlendJob` 的历史与 Stored 捕获中保存；`CharacterPoseWorldContextAdapter` 选择实际参与姿态的 Live/Stored 样本，交给原 FootPlacement 链路。它处理零权重 Live Idle 在 Stored 占据全部姿态时错误地提供锁脚请求的问题。Stored 保留当前接触与锁权重，静态姿态不继续预测未来落地；作者总权重和曲线未改。资源名称与曲线元数据在目录初始化，未增加每帧字符串转换。

同一输入链还确认 `CharacterPoseNativeAnimationSlotHandler` 重复乘动作权重：2041 帧骨骼实际使用 0.531232，贡献记录却为其平方 0.282207429，导致下游仍选 Run。修正让动作贡献只计权一次，基础姿态逐骨骼贡献按实际剩余权重计算，左右脚贡献同理；骨骼混合结果不变。问题追溯到 `ce6cdb09c7`。

动画程序集定向编译通过（0 警告、0 错误）。配套窗口先完成 13 帧无接触 Stored 和 70 帧真实接触 Stored 的正式选择／静态样本比较，随后在 Unity 中实际执行同样 83 帧的 `CharacterFootLifecycle`、Goal 与场景脚掌查询。两段旧版脚位和旋转均精确复现录制；两版预热计算均为 0 B。无接触段相对原动画的最大额外转角从 137.5741° 降至 0.000143°；真实接触段保留原锚点、随后正常释放，额外转角从 1.33253° 降至 0.05192°。有最终查询结果的帧未增加超过浮点误差的正穿透；1174 原本没有可用 Goal 或最终命中，净空仍标记为未知。

上述是 Unity 内独立编译的真实函数实验，不是正式 Test Runner 或完整 FBBIK 验证。它保持录制的混合动画姿态，预测、路径和骨盆可达性裁决仍为固定输入；未执行 Native Slot 历史页捕获及新贡献合成函数。真实接触段固定髋位置的超腿长记录从 4 帧变成 5 帧，两版最大伸展比均为 1.003319；新增的 954 帧约超长 0.54 mm。这项回归已保留，不能宣称伸直改善。结果在 `ik-tests/stored-*-goal.json`，完整边界见[动画来源审计](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#stored-motion-audit)。实验时共享 Editor 仍报告其它模块编译失败，因此未把磁盘候选当成主程序集已加载；本轮没有启动 Replay。

上述生产修正已独立提交 `18e1f2a4f`，配套窗口维护 [Stored 业务函数比较 HTML](../diagnostics/foot-placement/ik-tests/stored-foot-motion.html)及对应固定输入与结果。另确认未被该提交覆盖的 Live→Live 旋转跳变：新 full 右脚 2206→2207，WalkStart 与 Idle 的贡献由 55.556% / 44.444% 交接为 44.444% / 55.556%，原踝只转 3.228°，有效目标转 34.329°。未经权重的目标旋转只变 0.575°，支撑法线均向上，锁权重却从 0 切到约 1；因此只平滑原始目标四元数不能处理这一触发条件。历史 32946→32947 也确为两条 Live 正常跨过主来源交接点，不是 Stored 或动作重复乘权，见[Live 混合审计](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#live-switch-motion-audit)。

第一版旋转候选 `15894ee7b` 已完成三个窗口的真实源码函数实验：Live 23 帧、无接触 Stored 13 帧、从关闭帧开始的真实接触 Stored 85 帧。Live 的整窗最大转角从 37.0679° 降到 25.1218°，相对原动画的修正变化从 31.1801° 降到 9.0765°；最终脚掌查询无新增正穿透，三窗均为 0 B。跨版本比较仍判为失败：Live 固定髋的最大伸展比从 0.983891 升至 0.998176，真实接触 Stored 也存在同帧余量下降。2211 原动画膝角为 25.8279°，按同一髋和实际骨段长度计算的目标所需膝角从 20.8593° 降至 6.9297°。后者是几何需求，不是新 FBBIK 的输出。保留[跨版本对照](../diagnostics/foot-placement/ik-tests/rotation-business-comparison.json)，不能把单版执行成功当作业务通过。

旋转修订候选 `8c0e878f8` 随后完成 2192～2249 的 58 帧双脚实验：正式 Evaluate→PrimarySupport→Intent→PreparePelvis→ResolvePelvis→Complete 连续执行，历史版两脚脚位、旋转及骨盆 Goal 重现录制，三版预热计算均为 0 B。修订版右脚 2207 转角从 34.3287° 降至 13.0994°，相对原动画的最大修正变化从 31.1801° 降至 9.9366°；2211 所需几何膝角从 20.8593° 恢复到 24.5224°，原动画为 25.8278°。但左脚 2229 在原动画仍弯曲 44.4257° 时，目标伸展比从 0.981079 增至 1.007762，距离超过实际腿长约 5.40 mm；两版候选都把左脚超长时间从 1 帧延长到 3 帧（16.67→50 ms）。骨盆 Goal 在该窗未因此改变，最终查询无新增正穿透不能抵消新增拉直。完整 FBBIK 仍未执行，这些是实际 Goal 的几何需求。

因此已从运行代码撤回 `15894ee7b` 与 `8c0e878f8` 的两版旋转响应，6 个相关生产文件精确恢复到 `552f13083`；保留 `18e1f2a4f` 的动画来源和动作贡献修正、固定候选源码与失败结果。不能再次只凭单脚、单帧转角收益接受旋转滤波，也不能用调短半衰期或放宽弯曲余量判据掩盖问题。测试窗口继续维护同一[旋转业务说明](../diagnostics/foot-placement/ik-tests/live-rotation-response.html)，后续先验证正式 Native Slot 捕获、动作贡献合成与 2041 释放，冻结和骨盆算法未改。

撤回同时删除候选专用旋转前态及诊断字段，采样结构恢复 capability 4 / full sampler 3；没有运行候选版本的新采集或 Replay。撤回后的 Animation 与 FootIkDiagnosticSampling 定向编译均 0 错误、0 警告，分别用时 6.99 秒和 5.06 秒，日志在 `tmp/ik-release-sampling-20260930/rotation-withdrawal-*-build-20261001.log`。共享 Editor 最后检查仍报告其它模块编译失败；主 Animation 最后核实的 MVID 为 `cd6cec60-06dd-47f0-9dd6-6ad8029c56d7`。独立函数实验与定向编译均不代表主程序集已加载，也不替代完整骨骼验收。

双脚骨盆还原的输入已核对：`rootPos + rootRotation * (leg/leg-pose/original-hip − pelvis-goal/component-position * pelvis-goal/position-weight)` 与直接记录的 `foot/resolved/reach/landing-hip` 在 3305 条可用记录中最大误差为 0.0056 mm。2192 的真实总权重 0 会正式清空脚端、PelvisSpring 和无候选的 PrimarySupport，支持连续复算的明确起点；不能拿骨盆之后的髋直接作为 FootLifecycle 计算前输入。该核对不补齐完整 FBBIK 骨架及弯曲历史。

58 帧窗口中的 2224 最大世界转角约 178.67°，原动画脚踝本身为 178.6725°，表现根为 176.0301°，不能把该峰当作 IK 追加旋转。双脚报告中的原动画骨段使用同一姿态的髋、膝、踝，不能混合新骨盆后的髋、骨盆前膝和旧骨盆后的踝。

## 正式 owner

输入来自原动画姿势、已提交 Body、动画脚步数据和正式世界查询。唯一 Foot Placement 帧事务提交 typed Goal Contribution，经唯一 Goal Assembly、FullBodyIK 和 final writer 输出。合同见[Foot Placement](../../openspec/specs/character-foot-placement-presentation/spec.md)与[动画管线](../../openspec/specs/character-animation-pipeline/spec.md)。剩余实施由[Foot Path 与 Landing 稳定化](../../openspec/changes/stabilize-character-foot-path-and-landing/proposal.md)拥有，具体清单直接读取该 change，不在本页复制。

## 尚未解决的证据

| 问题 | 可复核记录 |
| --- | --- |
| 零权重内部高度异常、恢复输出大跳及停步穿透 | [2dad 采样](../archive/records/foot-placement/corin-foot-2dad-review-20260928.md) |
| 脚尖跨踏面穿透、下坡晚降与末级响应滞后 | [b173 采样](../archive/records/foot-placement/corin-foot-b173-review-20260928.md) |
| 最终接触几何、膝盖突变和悬脚下降 | [65ec 采样](../archive/records/foot-placement/corin-foot-sample-65ec-20260928.md) |
| 当前踏面与下一步包络共同控制高度 | [c75d 行走/攻击采样](../archive/records/foot-placement/corin-foot-c75d-walk-attack-20260928.md) |

这些样本属于不同代码时点，不能混成一份“当前版本通过”报告。各次修改、撤回、限制和编译结果完整保存在[脚部阶段记录](../archive/records/foot-placement/)。

## 数据与参考

报告附带 JSON、查询图与[台阶连续性解释器](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html)已归入[脚部诊断](../diagnostics/foot-placement/README.md)。正式 CSV、Proof 与分析包仍在项目 `Diagnostics/`；原始数据内容、失败事实和采样身份未改。

2026-10-01 补充 full 的脚端复算输入（capability 4 / sampler 3）：`CharacterFootPlacementModule` 在本帧求值前将左右脚生命周期状态保存到帧 Bank 的预分配页；诊断视图引用该页与 Bank 中的完整身体预测曲线，原生成采样程序在提交后同步写入 packet。输出包括 `foot/pre-state`、`future-body-trajectory` 和响应来源 UTF-8 表，以及事件时间、来源身份、作者总权重与 PosePlanHash。旧包缺失的轨迹及前态不能反推补造，必须重新采样；冻结、权重、骨盆和腿 IK 算法均未修改。这里不是完整骨骼 IK 的输入快照：求解前的整份骨骼姿势及完整 `CharacterFullBodyIkBendHistory` 尚未采集。

补字段的 `abfaf3895` 再次引入了大结构嵌套值拷贝。`23b52330c` 改为上述预分配存储引用后，实际 Unity Mono full Capture 栈帧从 953,392 降至 664,080 字节，减少 289,312 字节；历史修复 `413e931de` 后为 646,528 字节，core 本次保持 205,904 字节。编译与 JIT 检查通过，结果保留在 `tmp/ik-release-sampling-20260930/capture-stack-replay-input-before.json` 与 `capture-stack-replay-input-after.json`。执行规则已写入根目录 `AGENTS.md`；既有业务单元测试仍由配套测试窗口维护。

01:30 已完成原入口的完整运行验证：精确输入 `757f243033414fc7b123c97e2fcb0d70` 的 2716 帧全部回放、停止并保存；新 Foot 包为 `20260930-172333-3cb49fba52604669bfae7fe6eda3aabc`，主表 5432 行、1279 列，左右脚各 2716 行。完整身体轨迹表 6348 行，与每行声明的点数全部一致；响应来源字节表 347520 行，计算前脚态包含实际状态变化。正式 Proof 的 `replay_completed` / `capture_completed` 均为 true，错误列表为空，Foot 与 Presentation 清单均已发布；随后已退出本次 Play。该结果证明这条完整采样流程没有再发生启动闪退，不代表 Landing / Releasing 运动质量已修复。自动比较结果为 `baseline-created:2716`，不是候选 A/B 通过。

本次“编译十分钟”的实际记录是进入 Play 的刷新耗时 604.728 秒，其中 `CompileScripts` 仅 1.760 毫秒，`ProcessInitializeOnLoadAttributes` 占 314.061 秒，资源分类占 140.160 秒；片段保留在 `tmp/ik-release-sampling-20260930/editor-play-reload-20261001.log`。之后连接自行恢复，未重启或重复发起刷新。导出期间观察到整机可用内存约 1.2 GB、提交内存占用 93% 并有大量换页；这是当时的资源压力证据，不能据此断言某个初始化器就是根因。初始化器级别的耗时来源尚未定位，未修改其逻辑。

原理阅读见 [GDC 学习文案](../reference/foot-placement/gdc2016-fitting-the-world.md)；历史否决与重复问题见[实现经验](../reference/foot-placement/implementation-lessons.md)。参数和当前运行路径仍由正式内容与现行规格拥有。

2026-10-01 按用户要求核对原动画伸直和弯曲速率，并逐项审计 HTML 实施状态。当前采样与 9 月 30 日同输入基线的 5432 条脚记录在膝角、脚位、权重和末端补高上均一致；局部接触目标修正不等于六项均已完成。包内原动画对照仍显示 IK 额外拉直和输出脚掌命中高面后的硬补高，详见[原动画、角速度与六项状态报告](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#knee-audit)。本次是已有数据的只读统计，未新增或运行单元测试；输入回放、脚端函数复算和完整 FBBIK 还原的边界在同一报告中明确区分。
