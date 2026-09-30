# 单端接触保护、权重交接与膝盖连续性

> 历史记录：保存当时实现、问题与证据；当前合同见[现行文档索引](../../../../openspec/maintenance-audit.md)，当前进展见[文档入口](../../../README.md)。文中的“当前”“待完成”和旧阻塞均属于该记录时点。

> 当前交付以文末“用户反馈后的收敛修改”为准。前文记录首次实验；其中脚掌包络扩展、骨盆硬约束和世界输出历史已撤回。单端支撑、正式权重交接及 FinalIK 部分权重补偿保留。

## 本轮目标和证据

用户明确要求：脚跟或脚尖一端踩住时，另一端允许悬空；修的是已踩住的一端随后穿地，以及抖动、膝盖突然弯直。本轮沿既有 FootPlacement、Pelvis、FinalIK 链路修改，没有新增运行配置或另一套求解器。

沿用 c28 和 2dad 手动采样。2dad 开始时已存在零权重下的大内部高度修正，恢复权重时出现大跳；正常段有包络/跨边补高和腿长极限。采样没有记录异常高度的最初生成过程，因此不把所有症状归因于某一个提交。

## 一端有效仍提供支撑

CharacterFootCurrentSupportObservation 原来要求双点均命中，任一点 NoHit 就令整只脚支撑无效。本次允许 Heel 或 Toe 一个有效、另一个明确 NoHit 的情况，选择有效端的位置、法线和表面，记录 HeelOnlySupport / ToeOnlySupport；两点有效仍选需要更高脚底的位置。

另一端不伪造命中，缺失端的位移值不参与最大值比较。参数错误、查询容量溢出、世界版本不匹配仍不当作合法单端支撑。正式 CurrentContactVerification 的 Sole 查询同步使用相同接受规则。输出保护及释放目标约束通过同一个 TryResolveHeightConstraint 读取选中端；没有新增物理查询。

这改变的是接触有效性及最低高度，不为了同时贴住两级台阶而倾斜整只脚。当前脚掌旋转策略保留。它不能替代鞋网格逐顶点碰撞验证。

## 权重交接

FootPlacementWeight 为零时明确产生 OutputWeightZero 所有权退出，走现有释放接触、清理插值历史及抑制输出流程。骨盆同步清理历史，避免不可见修正继续存活并在恢复时重入。

权重从非零继续增大时，将上一帧未加权修正乘以前权重/当前权重，换算 PreviousResponseOutputPoint 与响应标量，并让 Swing/Release/Plant 从换算后的可见位置重新捕获各自连续性残差。平稳权重不走这个分支，权重下降仍保留正式淡出语义。已有 correction-response-visible-output-transferred 记录该换算。

这里没有清空正常落地残差，也没有调整动作资源的启用权重。若某段动画正式将 IK 关闭，该段依然由原动画输出，不能承诺它一定贴地；本次修复的是退出和恢复时错误历史的传播。

## 摆腿包络覆盖脚跟和脚尖

原来只按脚底中点取包络高度。本次在同一已有包络上按中点、脚跟、脚尖的水平投影取样，并扣除两个端点相对脚底中点的竖直偏移，得到整只脚所需的最低中点高度。

正式目标为 max(原中点包络高度 + 原动画 FootHeight, 脚掌所需最低高度)，不把 FootHeight 再叠加到脚掌保护高度上。已有抬脚足够时保持原目标；需要保护时才提高。最终仍只修改高度，不改变动画 XZ、不延伸包络、不把未来落点作为整段硬下界。每脚增加两次现有包络数组遍历，没有增加物理探针或托管分配。

当前动画脚姿态用于摆腿包络，最终旋转后的接触仍由输出双点保护检查。这没有解决所有包络拓扑突变，不能仅凭代码断言末端夹取已经为零。

## 骨盆与膝盖

原流程已计算双腿可达高度交集及 MinimumLandingLegCompressionReserve，但主要用于诊断/落地完成判断。本次把有效交集用于平滑目标和最终输出。正式配置仍使用原有 0.02m 压缩余量；没有加另一套角度限幅或配置。

骨盆上一帧输出使用其实际世界位置及世界速度，换算到当前动画姿态和当前权重后再进入既有弹簧。这样身体移动和权重变化不会被错误当作上一帧修正已经随之移动。支持腿姿态偏好不再被第二次截回零到原请求之间、抵消已经求出的可达要求。

平滑结果仍越过有效可达边界时，投影回交集并去掉继续朝越界方向的速度，写回同一历史。ReachConstraintApplied / ReachOutputAdjustment 记录实际应用情况。同平面下降限速仍执行，但不能把最终结果留在腿不可达的位置。可达交集不存在或水平距离本身不可达时，保留原明确失败诊断，不另造选脚优先级或缩短水平步长。

业务代价是身体高度及上下起伏可能变化；接触目标若突然变动，可达边界本身也会变，仍可能需要硬纠正。本次没有直接滤波膝盖角，因为固定脚点与腿长条件下，角度不能独立指定。

## 最终求解与前面检查保持一致

FinalIK 在施加脚目标前会平移骨盆及其后代。原来部分权重混合用的是已随骨盆移动的脚位置，实际脚目标因此比 FootPlacement 检查时多出 (1-w)×骨盆位移。

本次仅对 FootPlacementEffectorTarget 在最后编码为求解器目标时减去该偏移/w；膝盖目标计算与最终求解检查使用同一个转换函数。权重 1 时补偿为零；零权重不做除法。转换结果不写入脚部响应历史，没有额外逐帧分配。绝对手部等其它 effector 目标不使用该转换。

## 检查与交付边界

按用户要求不新增测试，不运行 replay 或 Unity batchmode。修改前确认主 3C_Client 实例，退出 Play 后编辑。进行 Editor 项目编译与 Unity 加载确认；所有 dotnet build 使用 --disable-build-servers /nr:false /p:UseSharedCompilation=false 并在结束后 shutdown。

没有修改后的运行采样，不把编译和静态公式核对称作视觉验收。需要观察的现象是：有效接触端是否保持不穿、悬空端是否仍自由、停走恢复是否跳变、膝盖弯直和身体高度是否自然。

最终 Editor 构建 0 错误、34 警告，日志 tmp/foot-support-reach-delivery-build.log；构建服务器已关闭。Center 记录为“完善脚掌单端支撑与膝盖连续性”，change_id=7d4505a8500a4078a0e539942c848fd8。没有创建回放 Run 或伪造前后比较。


## 用户反馈后的收敛修改

### 依据与撤回

用户反馈骨盆异常，已踩住的脚尖或脚跟仍可能随后穿过踏面。新采样 `20260928-030542-1f4dfd6fcf4f47a885693da5767c07ae` 的骨盆末端约束有 329 帧修正超过 1mm，最大追加约 6.48cm。历史 `3d209c488` 已撤除 Reach 硬执行，`5d8ded540` 已限制骨盆姿态偏好范围，因此本次不继续叠加这条实验。

提交 `65052876f` 恢复原骨盆修正弹簧、姿态偏好范围和中点摆腿包络，删除 ReachConstraintApplied / ReachOutputAdjustment 两个实验字段。保留零权重骨盆历史清理、单端支撑、正式权重交接及 FinalIK 部分权重目标补偿。旧采样和首次实验说明保留追溯。

### 摆腿目标不再依赖脚下即时命中

5700–5705 左脚的 GroundPath 均为 Accepted，InputIdentity 均为 `5633887286461608957`，下一次落脚事件均为 `11993514060683856971`。5701–5704 CurrentSupport 不可用时，正式 IK 权重仍为 1，最终脚目标权重却从 1 降到 0，查询恢复后回到 1。这证明目标取消链路存在问题；它不能单独证明用户看到的全部接触端穿透都来自这里。

原 `TryResolveSupportTarget` 先检查 CurrentSupport，再生成 SwingGround，因此有效摆腿路径也会因脚下当前探针无命中而取消。现在 StateFrame 显式携带已有 GroundPath：SwingGround 的高度仍由原包络和 FootHeight 给出，法线、表面来自同一路径的下一次预测落点，世界版本来自该路径的正式 SurfaceCoverage。路径编号、落脚事件和世界版本由生命周期入口检查一致性。统一 support-target 诊断以 PredictedLanding 标识法线来源。

没有预测路径，或当前接触已取得目标控制权时，仍要求当前有效支撑。没有伪造当前接触，也没有把预测落点设为整段脚部高度硬下界。`AdvanceUnavailable` 的历史清理保持原样，避免重复 `dd8376f5b` 修过的陈旧世界位置问题。

同时补全 OutputWeightZero 的合法枚举位校验：上一轮已产生此退出原因，但入口仍只接受旧的两个位，会错误抛出 Foot lifecycle frame is invalid。既有测试辅助构造仅补传 GroundPath 参数；没有新增测试或执行测试。

### 最终真实 Toe 接入原采样

`CharacterFinalPosePhysicalWriter` 在所有物理骨骼写回完成之后，按现有 Rig 的 ToePhysicalBoneIndex 读取左右 Toe 的世界位置和旋转。`CharacterPhysicalFootPose` 增加 ToeWorldPosition / ToeWorldRotation，沿已有 physical 根进入自动生成采样，不新增采样器、探针或逐帧托管分配。

这些数据表示最终真实脚趾骨骼，能与源动画、校准脚底点和最终踝区分；它们仍不等于整只鞋的网格表面。下次手动采样可以判定穿透发生在目标生成、最终骨骼求解还是接触校准与鞋形状的差异，不能用本次新增字段提前宣布穿透消失。

### 本次交付边界

Editor 项目构建为 0 错误、91 警告，日志 `tmp/foot-contact-convergence-build.log`；已执行 build-server shutdown。同工作目录有其它窗口修改 PoseGraph 求值文件，本次未修改或提交它们，构建不作为固定提交的性能或视觉证据。

不运行 replay、Unity batchmode 或测试。当前修改尚无新的运行采样；接触端穿透、踏地抖动和膝盖弯曲自然度仍需用户手测确认。本次撤回骨盆实验，不宣称已经完成膝盖平滑。

主 Editor `e852139597e42532` 已完成域重载，处于 Edit、不编译、不刷新，Console 错误 0 条。反射确认 GroundPath 输入及 Toe 字段已加载，旧 ReachOutputAdjustment 已移除；正式 FullCaptureProgram 生成的 Schema 已含 `character-foot-ik/main/physical/toe-world-position` 和 `character-foot-ik/main/physical/toe-world-rotation`。这确认代码及采样入口已更新，不替代实际运行验证。
