## 1. 基线与变更边界

- [x] 1.1 对照 `character-foot-placement-presentation` current spec、`stabilize-character-foot-path-and-landing` 未完成任务和本 change delta，建立文件/字段/Owner 对账表；验证没有新增第二 Foot 主线或重复消费者
- [x] 1.2 固定有效基线 Trace、采样目录和当前 Corin Float32/Fixed Projection identity；验证基线回放证明与输入 Trace identity 已保存
- [x] 1.3 对当前 `f2dbbf8a9` 探针执行一次真实 Edit 状态加载证明；验证未加载时不把回放结果计入候选，必要时恢复到可复现基线并保存证据
- [x] 1.4 在 Foot Profile 中登记本 change 需要的显式阈值、速率、角度和 LockFoot 参数及 Revision；验证缺失、非法和 Revision 不匹配均产生 typed 不可用

## 2. ZZZ 每脚状态与边沿

- [ ] 2.1 建立每脚持久 `isMoving/isLocking`、上一请求、Event identity、边沿事件和 `remainTime` 的 typed 状态块；验证每个字段只有一个写入 Owner
- [ ] 2.2 按 `lockNow = isInZone && !isSliding` 实现单脚 `OnFootPlant` 状态转移；验证另一脚状态不会被当前脚调用修改
- [ ] 2.3 分离持久状态、一次性边沿和 `enablePIK`/预测启用状态；验证 OnFootPlant 不会直接写入或清空 `enablePIK`
- [ ] 2.4 让边沿状态随根 Bank Prepare/Seal/Discard 更新，调整 Finalize 只清理已消费的一次性事件；验证失败帧不会推进边沿历史或清除上一提交状态
- [ ] 2.5 接入状态与边沿诊断并创建独立中文提交；使用固定 Trace 验证 Landing、Release、PlantPositive、左右脚 Event identity 与基线对账，无输出回归

## 3. 接触纪元与历史槽位

- [ ] 3.1 为每脚加入 A/B 位置、C/D 参考、双高度历史、纪元 identity 和一次性记录门；验证状态布局属于现有 Interpolation/Contact 根 Bank
- [ ] 3.2 实现空中/接触换代时的距离阈值、抬脚边沿缩放、重入和一次性刷新判定；验证每次判定都发布唯一 rewrite 或 carry 原因
- [ ] 3.3 实现 rewrite 的 A/B、C/D、height history 一次性覆盖；验证覆盖不会读取上一物理 Transform、旧 Surface 或另一脚状态
- [ ] 3.4 实现 carry 的 A/B、C/D 槽位承接；验证同纪元不会产生新的 Event、查询或额外世界位置 lerp
- [ ] 3.5 将纪元结果接入 State Target/Interpolation 的唯一输入并创建独立中文提交；固定 Trace 验证重点 Contact/Reentry 窗口的纪元 identity、rewrite/carry 比例和根事务 Discard 结果

## 4. 分层连续响应

- [ ] 4.1 将 Target Height History 与 Position Epoch History 分离，并让 Corin 使用显式 Target Height Adoption/Force Refresh 配置；验证稳定同 Event 帧不会因 Phase 推进重复换代
- [ ] 4.2 将 Requested/Applied Direction History 接入现有唯一 Interpolation；验证方向按显式角度上限推进且不重投影 Correction scalar
- [ ] 4.3 将 Correction Response scalar、增减方向、两档速率和初始化/重置原因接入现有唯一 Interpolation；验证正常目标变化不会隐式清空其它历史
- [ ] 4.4 按 ZZZ 顺序实现当前态/目标态 `clamp01(f)` 线性混合与目标计算；验证 Ground/Reach/Post Constraint 不回写连续历史
- [ ] 4.5 用接触纪元与分层历史替换跨接触完整三维 `PlantWorldResidual` 承接；验证新纪元不会继续使用旧 Contact 输出拖尾
- [ ] 4.6 将响应阶段、Ground 安全测量、Goal Weight 和 Final Publication 的责任重新对账；验证不存在第二低通、第二 Goal 或 Ground 对 Interpolation 的反向写入
- [ ] 4.7 为本阶段每个行为切片建立中文提交并回放固定 Trace；验证最大穿透、PlantDistance、左右脚跳变、膝盖弯曲和伸展范围按重点帧逐项保存，失败切片可独立回退

## 5. LockFoot 能力

- [ ] 5.1 建立每脚 `LockGoal/LocalPos/LocalDiff/WorldPos/LastFrameOrigin/LockState/IsSetLockGoal` typed 状态；验证状态与 Contact Event、纪元和根 Bank 使用同一 lineage
- [ ] 5.2 按显式 PIK、脚高、脚速、锁距、阻塞和接触门控实现 LockFoot；验证 LockFoot 只产生局部 XZ 修正，不写垂直 Target、Pelvis、Goal 或 Final Pose
- [ ] 5.3 接入 LockFoot 的进入、维持、退出和 Reset/Retarget/Discard 清理；验证关闭开关时不执行锁脚状态推进且不出现第二运行路径
- [ ] 5.4 以 Corin 当前关闭开关配置回放固定 Trace并提交；验证关闭 LockFoot 时主链诊断和输出与无该能力基线一致
- [ ] 5.5 单独建立开启 LockFoot 的 A/B 回放产物；验证只在通过 Contact、Reach、穿透容差和根事务完成资格时保留候选，否则保存失败证据并回退开启实验

## 6. 调用顺序、收尾与下游边界

- [ ] 6.1 将 Foot 执行顺序对齐为 Prepare、Prediction/普通查询、逐脚 LockFoot、目标历史、当前/目标混合、目标计算、写回、Finalize、Pelvis；验证每个阶段只有一个调用 Owner
- [ ] 6.2 对齐 Finalize 对 last cache、事件、时间和持久状态的清理；验证 Release、Same-Event Reentry、Reset、Retarget 和 Source/Profile/World lineage 失效按显式原因处理
- [ ] 6.3 让 Support/Pelvis 只读取 typed Foot 请求和 Reach 观察，Goal Assembler/FBBIK/Final Publication 继续使用唯一 Result；验证 Foot 内部状态不会泄露为下游第二输入
- [ ] 6.4 重新生成 Corin Float32/Fixed Presentation Projection 与 Program；验证 Profile/Projection/Program identity、Revision 和容量闭包一致

## 7. 诊断与旧路径清理

- [ ] 7.1 将状态边沿、纪元动作、历史 Owner、LockFoot 门控和最终输出事实接入现有唯一诊断链；验证 Diagnostics 只读取成功 Seal 的 Committed typed Result
- [ ] 7.2 删除失去消费者的旧 `PlantWorldResidual` 跨接触字段、旧所有权解释和重复诊断列；验证源码与 Projection 不再引用这些名称
- [ ] 7.3 删除旧状态机兼容入口、fallback 配置、重复 Interpolation/Goal/Writer 逻辑；验证 `rg` 与严格架构检查只剩一条 Foot 运行链
- [ ] 7.4 对全部行为提交、回退提交和恢复回放产物建立变更索引；验证每个失败实验都有输入、样本、对比、原因和精确回退记录

## 8. 最终封口

- [ ] 8.1 用固定 Trace 完整回放当前唯一链路；验证 Landing/Release/PlantPositive、最大穿透、PlantDistance、膝盖弯曲、伸展范围、左右脚跳变及重点帧均有可追溯结果
- [ ] 8.2 对账 ZZZ 已证实的 `OnFootPlant` 边沿、接触纪元 rewrite/carry、分层历史、LockFoot 门控、调用顺序和 Finalize 清理；验证未还原 native helper 已明确标为适配边界
- [ ] 8.3 执行 `openspec validate rebuild-character-foot-ik-from-zzz-pik --type change --strict --no-interactive` 并检查本 change 与 current specs 的矛盾；验证严格校验、任务闭合和无失效路径引用
- [ ] 8.4 生成最终实现/回放/失败经验索引并停止在代码实施边界；验证手动视觉验收仍由用户在 OpenSpec 之外完成
