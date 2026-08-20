# 实施任务

任务按“独立模块先完成、最终单点接入”排序。只列实际实施工作，不包含人工观察或端到端测试任务。

## 1. 直接拆分 iStep 接触求解

- [x] 1.1 新增 `CharacterFootReactiveContactRequest`、Proposal、typed Reject Reason、Measurement Revision 与 Surface lineage 合同。
- [x] 1.2 直接把 `FootIK.findNewIKPos` 的现有 BoxCast、SphereCast、坡面法线修复、命中点回算和脚底高度补偿移动到唯一 `HoaxGames` Contact Solver，不在 GameScripts 重新实现这些公式。
- [x] 1.3 新增公共 Proposal 外层合同，使 Predictive 与 Reactive 都能表达相对同帧 Original Ankle/Sole 的候选修正、来源和 lineage，但不写 GoalSet。
- [x] 1.4 修改 `FootIK` 让原 Demo 路径也调用同一个 Contact Solver，删除留在 `FootIK` 内的重复 `findNewIKPos` 实现。
- [ ] 1.5 保证项目 Proposal、Arbiter 和纯合同不引用 PhysicsScene、Collider、RaycastHit、Animator、FinalIK、Transform、Gizmo 或 Editor 类型；Unity/iStep 类型只停留在窄 Adapter 边界。

## 2. Calibration、Profile 与正式内容

- [ ] 2.1 为左右脚 Rig Calibration 增加 `SoleHalfWidth`，升级 schema、content revision、geometry validation identity 与显式 authoring 写入。
- [ ] 2.2 在 Foot Placement Profile 增加 Reactive Contact 查询、测量死区和兼容阈值设置，并让全部字段进入 Profile Revision。
- [ ] 2.3 增加不可变 `ReactiveOwnershipCurve` 运行时设置，校验端点、有限值、`[0,1]`范围和单调性。
- [ ] 2.4 把 Corin 与 TrainingEnemy 写入同一正式 Calibration/Profile 配置，不增加 Prefab override、默认值补全或 iStep 参数副本。

## 3. 修改 iStep Solver 与项目 Adapter

- [x] 3.1 把原 `FootIK` 的 Animator、Transform和组件字段输入改为显式 Contact Request，并让Solver接受当前PhysicsScene、Layer与Trigger policy。
- [x] 3.2 保留原 iStep Footprint BoxCast 的起点、half extents、前向偏移、查询距离和命中点回算，只增加项目正式自碰撞、非法几何与坡度过滤。
- [x] 3.3 保留原 iStep SphereCast、`alpha/beta`比较与叉乘法线修复，只增加同Surface和有限修复邻域约束。
- [x] 3.4 让修改后的Solver一次只发布一个Surface、点、法线和iStep IK接触结果；BoxCast失败时不允许SphereCast生成替代接触。
- [x] 3.5 新增 `CharacterIstepReactiveContactAdapter`，只负责Request/Result映射与lineage，不复制Physics查询或接触几何。

## 4. 独立响应式模块

- [x] 4.1 新增每脚唯一 Measurement Pending/Committed 页，并接入 BeginPending、Seal、Discard、Reset、Retarget 与 Dispose。
- [x] 4.2 实现同 Surface 点位/法线死区复用、超死区新 revision、Surface 变化立即换代和查询失败当前 Proposal Rejected。
- [ ] 4.3 从同帧原生 Component Pose、Rig、Calibration、Profile 与修改后的 iStep Solver生成左右脚 Reactive Proposal，不读取预测 Landing、Ground Path、Support Lock 或 Pelvis 状态。
- [x] 4.4 增加只读响应查询与 Measurement diagnostics，保证未接入最终 Goal 前不修改角色、Prefab、Animator 或 Physical Bone。
- [x] 4.5 正式Runtime只引用修改后抽出的 `HoaxGames` Contact Solver；删除对 `FootIK.OnAnimatorIK`、iStep Grounded、Body Placement、骨骼writer和Demo类型的调用，不删除或重新实现Solver中直接复用的接触代码。

## 5. 所有权曲线与 Proposal Arbiter

- [ ] 5.1 用每脚 Event/Release/LiftOff/Approach/Landing phase 建立唯一生物力学接触权重，不新增独立计时器。
- [ ] 5.2 分别用左右脚接触权重采样同一 `ReactiveOwnershipCurve`，保持现有 `animation.foot-placement-weight` 为最终 Foot Placement 总强度。
- [ ] 5.3 新增每脚唯一 Arbiter Pending/Committed Owner 状态、typed compatibility 与 handoff reason。
- [ ] 5.4 实现同 Frame/Completion/Rig/Event 的 Surface 和几何兼容判断；兼容时只混合相对 Original Pose 的修正。
- [ ] 5.5 Surface 或几何不兼容时延续仍合法的 Committed Owner，并在响应曲线终点与正式接触条件成立时执行 typed handoff；禁止绝对世界点跨踏面 Lerp。
- [ ] 5.6 两个 Proposal 都不具备正式所有权时发布 typed rejection，不沿用旧目标、默认地面或隐藏 fallback。

## 6. 预测式基线与最终统一接入

- [ ] 6.1 在最终接入前对账 `complete-character-predictive-foot-ik` 的实际完成状态；先归档其单一基线，或把剩余最终 Goal 所有权整体移交到本 change，禁止双 change 同时组装 Goal。
- [ ] 6.2 把现有 Swing、Support 与 Landing 计算结果降为 Predictive Proposal，保持 Future Landing、Ground Path、Envelope 与当前预测诊断语义不变。
- [ ] 6.3 在 `CharacterFootPlacementRuntime` 同一 Pending Frame 内计算 Predictive/Reactive Proposal，并为左右脚各执行一次唯一 Arbiter。
- [ ] 6.4 落地完成时把本事件最后 Resolved Contact 原值晋级为 `LastLanding`；下一 Ground Path 从该提交点出发，`NextSwingLanding` 继续只表达未来预测。
- [ ] 6.5 让现有 Support Lock 锁住 Resolved Contact Anchor；锁定后响应查询不得按动画脚位置搬动 Anchor，失效时只走正式 Unlock/Reacquire。
- [ ] 6.6 每脚只在 Arbiter 与 Support Lock 后执行一次现有 GoalTransition，不在响应模块增加 Lerp、SmoothDamp、外推或 Reset Blend。
- [ ] 6.7 只用裁决、锁定和 GoalTransition 后的最终左右 Sole 计算唯一 Pelvis，删除任何 Predictive/Reactive 双 Pelvis 输入。
- [ ] 6.8 保证最终仍只写 Pelvis、LeftFoot、RightFoot 三个 Goal，并执行一次 FullBodyIK 和一次 final writer。

## 7. 诊断与旧路径清理

- [ ] 7.1 扩展 Foot Placement committed diagnostics，记录响应请求、footprint、法线修复、Measurement revision、相位曲线、双 Proposal、兼容结果、Owner、Resolved Contact 与 handoff。
- [ ] 7.2 在现有Foot Placement调试面板增加session-local `Predictive Only`、`Reactive Only`、`Hybrid`对比模式，不创建新面板或第二运行入口。
- [ ] 7.3 让三种模式只覆盖同一Arbiter的Effective Ownership；模式切换发布typed handoff并继续使用唯一GoalTransition，Reactive Only失效时明确Rejected而不回退预测。
- [ ] 7.4 面板关闭、Runtime重建或diagnostics interest释放时恢复Hybrid；禁止把调试模式写入Profile、Projection、Prefab、Gameplay、Snapshot、网络或正式Player UI。
- [ ] 7.5 扩展 Gizmo 与 CSV，只读取成功 Seal 的同一 Frame/Completion/Rig 事实，并继续对账 Goal、FBBIK solved result 和 Physical Bone writer。
- [ ] 7.6 删除为响应模块产生的临时 MonoBehaviour、Animator IK Pass、Prefab 开关、独立 Goal writer、独立 Pelvis、独立 Grounded 或演示接线。
- [ ] 7.7 对账 `character-foot-placement-presentation`、`openspec/project.md` 与其它 active changes，把“禁止响应式结果”收敛为“只允许同一 Foot Placement 事务内的响应候选，禁止第二 IK 链”。
- [ ] 7.8 运行 `openspec validate add-reactive-foot-contact-arbitration --strict --no-interactive` 并修复全部错误。
