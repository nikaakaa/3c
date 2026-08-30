# 设计：有效Foot目标到共同Pelvis需求

## 已批准调用链

`原动画Pose -> 双脚唯一Resolved目标 -> 共同Pelvis期望 -> 统一Reach边界与一次响应 -> 唯一GoalSet/FBBIK`

只替换三个明确业务决定，不恢复212054卸载，不调整已认可的Foot位置、旋转或查询算法。

## 第1步：位置目标有效性与作者权重分离

State Target、Support Target、有限Sole和现有Sole→Ankle/Rotation解析负责目标合法性。只有解析成功才生成Ready Resolved Foot；Unavailable和Suppress继续发布零权重。

Ready时PositionWeight取frame.FootPlacementWeight。Correction=0表示本帧目标恰好与动画Sole相同，不表示放弃这份位置约束。Swing每帧仍使用新目标，不保存世界锁；正式作者权重0仍关闭可见约束。保留现有极小作者权重的编码数值边界，本步不修改小分母保护或RotationWeight=FormalWeight×LockWeight。

Runtime不新增字段；已有FormalWeight、ResolvedOutcome、SupportTarget、三层PositionWeight和最终Physical事实足够验证。Diagnostics升级facts59/diagnosis28，不复用历史失败facts58，不向旧包补值。

## 第2步：共同高度需求

输入为同一根事务的左右原动画Sole、左右Resolved有效目标Sole和有限单位Component Up。沿Up取高度后计算：

`requestedOffset = min(targetLeftHeight,targetRightHeight) - min(animatedLeftHeight,animatedRightHeight)`。

Resolved有效目标与现有Goal作者权重语义一致：作者0不让不可见修正偷偷影响Pelvis，不读取最终Physical Pose回推。公式允许负值，不保留旧max(0,...)或地形相对高度加项。Stride/Primary Support生产资格本步保持，几何与换代事实可保留，但旧目标字段必须按新含义删除/改名，不伪装成仍消费旧公式。

本步只替换目标生产，保留响应/Reach以便独立比较。下一步才改变硬边界职责。

## 第3步：一次处理可达性与响应

同帧typed Reach Request提供Hip、有效Ankle目标、真实腿长、正式安全余量和lineage。所有实际参与的腿先形成唯一硬区间，进入现有Pelvis模块后一次用于目标和响应合法性。原动画弯曲余量可形成目标偏好，但不再缩小另一份最终输出硬区间。

保留一份根Bank内的Spring状态、原频率与Handoff/Velocity Reset业务。不增加第二响应或新速率参数。最终输出若因几何必须触界，统一阶段记录夹紧并清除继续向外的速度；之后Module只消费结果，不再次改写Pelvis输出。无交集、横向本已不可达等情况继续使用明确typed拒绝及既有Foot Reach保护，不以FBBIK伸直或降低未授权权重掩盖。

此结构不能保证所有几何冲突都无突降；它先减少不必要目标上抬与动画姿态偏好造成的硬压，再用Replay评估剩余真实约束。

## 不变项

- Contact完整世界残差、capture同帧Advance、完成容差和Anchor不变。
- Swing动画XZ、FootHeight、Ground Path、Correction Response及既有旋转政策不变。
- 既有GoalSet/FBBIK、Rig、曲线、世界查询与Gameplay Body不变。
- 不添加未来Pose预测、卸载时钟、Pose后低通、默认地面或第二解释链。

## 验证顺序与失败界限

第1步覆盖L339/L515/R611及原193957的实际零权重/近零修正帧，证明目标持续有效但未锁住Swing；同时核对Goal新增覆盖造成的真实Physical变化。

第2步核对两份最低高度、signed requestedOffset、被替换字段和322/466/675及全包骨盆大步。第3步再核对每腿硬区间、偏好目标、响应前后、唯一夹紧与不相交路径。

每步保持原37项质量规则，只有正式合同随API改变；原始输入/时钟、固定接触帧、Ground Query、脚与骨盆/膝盖输出分别对照。没有动态样本的Action、作者极小权重、重入或退化几何明确标记未覆盖，不以编译或单个总分通过冒充效果完成。
