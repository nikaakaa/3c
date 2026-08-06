## 1. 依赖与唯一所有权收口

- [x] 1.1 对账当前 Presentation Profile、Pose Graph、Projection、Native Pose runtime、Equipment committed state 与 FinalIK generated projection 的真实代码入口
- [x] 1.2 记录 root graph 对 Locomotion、Action Slot、Pose 空间转换、Predictive Foot Placement、FullBodyIK、OutputPose 与 final publication 的唯一所有权
- [x] 1.3 对账 `replace-pose-ik-with-finalik-full-body-solver` 最终 Rig v4 identity、GoalSet ABI、completion、lineage 与唯一 solver 合同
- [x] 1.4 明确当前正式 `CharacterPresentationFactFrame`、FactId、Fact kind 与 schema 生成入口
- [x] 1.5 删除 change 实施范围内任何旧 Layer catalog、LayerId 或并行动态 Graph 路径引用
- [x] 1.6 确认首个 Equipment Linked 实现不依赖尚未闭合的实验 Motion Matching 或 Blend Space 能力
- [x] 1.7 确认 Corin 正式 EquipmentSlotId / EquipmentId 未完成时只交付能力，不建立临时业务接线

## 2. 稳定 identity 与 typed contract

- [x] 2.1 新增 `LinkedPoseInterfaceId` 稳定值类型
- [x] 2.2 新增 `LinkedPoseEntryId` 稳定值类型
- [x] 2.3 新增 `LinkedPoseImplementationId` 稳定值类型
- [x] 2.4 新增 `LinkedPoseGroupId` 稳定值类型
- [x] 2.5 为四种 identity 接入统一序列化、相等、排序与空值拒绝合同
- [x] 2.6 新增 Linked Pose revision 值合同并定义单调比较规则
- [x] 2.7 新增 Interface typed port direction 描述
- [x] 2.8 新增 Interface typed port kind 描述并复用现有 Pose/Value kind
- [x] 2.9 把 Pose 空间与 `component.full-body-ik-goals` ABI 纳入 port descriptor
- [x] 2.10 新增 Interface Entry descriptor 并固定 port 顺序
- [x] 2.11 新增 Presentation Fact contract identity
- [x] 2.12 从正式 Fact schema 确定性生成 Fact contract identity
- [x] 2.13 把 Fact contract 与 Pose runtime execution contract 纳入 Interface signature
- [x] 2.14 实现 Interface signature 的确定性 hash
- [x] 2.15 实现 Implementation authoring content hash，排除 layout view-state
- [x] 2.16 为所有 hash 输入固定排序、编码与空集合语义

## 3. Interface 与 Implementation 作者资产

- [x] 3.1 新增 `CharacterLinkedPoseInterfaceAsset`
- [x] 3.2 为 Interface asset 保存稳定 identity、revision、Entry 与 Fact contract
- [x] 3.3 阻止作者直接写入派生 signature hash
- [x] 3.4 新增 `CharacterLinkedPoseImplementationAsset`
- [x] 3.5 为 Implementation asset 保存稳定 identity、revision 与 Interface 引用
- [x] 3.6 新增 required Entry 到 Pose Graph 的唯一映射
- [x] 3.7 阻止 Implementation 保存 root Profile、Equipment 对象或 runtime handle
- [x] 3.8 阻止 Implementation 重复、遗漏或声明 Interface 外 Entry
- [x] 3.9 接入 Interface 与 Implementation stale 标记
- [x] 3.10 接入 Interface 与 Implementation 的统一资产 owner identity
- [x] 3.11 在 Navigator 中增加 Interface、Implementation、Entry 层级
- [x] 3.12 在 Details 中只显示作者字段与派生只读身份

## 4. Group 与通用 selector 合同

- [x] 4.1 新增只包含 Group identity 与 Interface 引用的 `CharacterLinkedPoseGroupBinding`
- [x] 4.2 阻止 Group binding 保存 EquipmentSlotId、EquipmentId 或 candidates 列表
- [x] 4.3 新增 `CharacterLinkedPoseSelectionFrame`
- [x] 4.4 固定 selection frame 的 GroupId、InterfaceId、ImplementationId 与 SelectionRevision 字段
- [x] 4.5 新增统一 selector provider authoring contract
- [x] 4.6 新增统一 compiled selector descriptor contract
- [x] 4.7 新增统一 runtime selector adapter contract
- [x] 4.8 阻止 selector contract 暴露 authoring asset、资源路径或 runtime Graph 对象
- [x] 4.9 在 Profile 中建立 selector binding 的唯一集合
- [x] 4.10 校验每个 Group 恰好有一个 selector
- [x] 4.11 校验每个 selector 只服务一个已声明 Group
- [x] 4.12 从 selector 精确映射推导 Group candidates
- [x] 4.13 删除或拒绝第二份通用 candidates 配置
- [x] 4.14 校验 selector candidates 全部实现 Group Interface
- [x] 4.15 校验 SelectionRevision 的稳定生成与单调变化
- [x] 4.16 保持 Linked Pose 核心无 `SelectionSourceKind` 与业务类型 switch

## 5. Equipment selector

- [x] 5.1 新增 `CharacterEquipmentLinkedPoseSelectionBinding`
- [x] 5.2 保存目标 Group 与唯一 EquipmentSlotId
- [x] 5.3 保存精确 EquipmentId 到 ImplementationId 映射
- [x] 5.4 保存显式 Empty Equipment 到 Empty Implementation 映射
- [x] 5.5 拒绝重复 EquipmentId 映射
- [x] 5.6 拒绝空 EquipmentId、空 ImplementationId 与跨 Interface 映射
- [x] 5.7 从角色可提交 Equipment 闭包校验 mapping 完整性
- [x] 5.8 新增只读 committed Equipment selection adapter
- [x] 5.9 由 committed slot/id/revision 确定性生成通用 selection frame
- [x] 5.10 保证未变化 Equipment selection 不增加 SelectionRevision
- [x] 5.11 保证变化 Equipment selection 只增加对应 Group 的 SelectionRevision
- [x] 5.12 阻止 Equipment Feature、Equipment Presentation 与 Renderer 保存 Linked Implementation 引用
- [x] 5.13 阻止 Equipment selector 使用 FeatureId、显示名、路径或上一实现猜测
- [x] 5.14 在 Profile Details 显示 Slot、精确映射、Empty 映射与候选闭包

## 6. 首个 Equipment Interface 与空实现合同

- [x] 6.1 定义第一份 Equipment Interface 的稳定 InterfaceId 与 revision
- [x] 6.2 定义 `EquipmentPose` 稳定 EntryId
- [x] 6.3 固定 `EquipmentPose` 的 Local Pose 输入与 Local Pose 输出
- [x] 6.4 定义 `EquipmentHandGoals` 稳定 EntryId
- [x] 6.5 固定 `EquipmentHandGoals` 的 Component Pose 输入
- [x] 6.6 固定 `EquipmentHandGoals` 的 `component.full-body-ik-goals` 输出
- [x] 6.7 把正式 Presentation Fact contract 纳入 Equipment Interface signature
- [x] 6.8 新增正式 Empty Implementation authoring asset
- [x] 6.9 为 Empty Implementation 完整绑定两个 required Entry
- [x] 6.10 让 Empty Pose Entry 精确 passthrough 输入 Local Pose
- [x] 6.11 新增正式 Empty FullBodyIK Goals operation descriptor
- [x] 6.12 让 Empty Goals operation 发布 Ready、GoalCount=0、当前 frame、Rig、completion 与 lineage
- [x] 6.13 修改 GoalSet validation，使合法零 Goals 与 Unavailable/Invalid 明确区分
- [x] 6.14 修改 FullBodyIK plan validation，使零个额外手部 Goals 仍可进入唯一 solver
- [x] 6.15 删除任何通过上一帧 Goals、null 或非法 PoseBoneIKGoals 配置表达空手的路径

## 7. LinkedPoseCall 与共享 Capability

- [x] 7.1 新增 `LinkedPoseCall` authoring node payload
- [x] 7.2 保存 Group、Interface 与 Entry identity
- [x] 7.3 从 Interface Entry 精确投影动态 ports
- [x] 7.4 在 `GraphAuthoringCapabilityCatalog` 注册 Call graph context
- [x] 7.5 注册 Call payload schema、port provider、connection policy 与 Compiler handler
- [x] 7.6 为 Linked Implementation Entry 增加正式 graph context
- [x] 7.7 为 Entry context 建立允许节点 capability 集合
- [x] 7.8 禁止 Entry context 中的 OutputPose
- [x] 7.9 禁止 Entry context 中的 ActionPlaybackInput 与 AnimationSlot
- [x] 7.10 禁止 Entry context 中的 PredictiveFootPlacement 与 FullBodyIK
- [x] 7.11 禁止 Entry context 中的 world query、Gameplay node 与 final writer
- [x] 7.12 禁止 Entry context 中嵌套 LinkedPoseCall
- [x] 7.13 保留 Entry context 中静态 PoseSubgraph 能力
- [x] 7.14 注册 Empty Goals operation 的 payload、ports、context 与 Compiler handler
- [x] 7.15 让人工 Canvas、Details、Document、Mutation、Validator 与 Compiler 共享同一 Capability
- [x] 7.16 校验每个 required Group + Entry 在 root 恰好存在一个 Call
- [x] 7.17 对重复 Call 同时报出两个 node identity
- [x] 7.18 对缺失 required Call 定位 Group、Interface 与 Entry

## 8. Document v3 与 typed Mutation

- [x] 8.1 扩展 Document schema 版本声明与 feature flag
- [x] 8.2 新增只读 Interface context schema
- [x] 8.3 新增 editable Implementation schema
- [x] 8.4 新增 Entry Graph owner 与 layout 分片 schema
- [x] 8.5 新增 Profile Group binding schema
- [x] 8.6 新增通用 selector binding envelope schema
- [x] 8.7 新增 Equipment selector payload schema
- [x] 8.8 为所有新对象声明稳定 object key 与 owner key
- [x] 8.9 将新分片纳入 strict manifest allowlist
- [x] 8.10 将新分片纳入规范 package hash
- [x] 8.11 扩展 Snapshot exporter 导出只读 Interface context
- [x] 8.12 扩展 Snapshot exporter 导出 Implementation 与 Entry Graph
- [x] 8.13 扩展 Snapshot exporter 导出 Group 与 selector binding
- [x] 8.14 保证 exporter 不导出 generated offset、runtime handle 或派生布局为 editable 字段
- [x] 8.15 扩展 strict codec 的未知字段、缺失字段与重复 identity 拒绝
- [x] 8.16 扩展 Reconciler 对 Interface 只读修改的拒绝
- [x] 8.17 新增创建、更新、删除 Implementation 的 typed Mutation
- [x] 8.18 新增创建、更新、删除 Entry Graph 的 typed Mutation
- [x] 8.19 新增创建、更新、删除 Group binding 的 typed Mutation
- [x] 8.20 新增创建、更新、删除 Equipment selector binding 的 typed Mutation
- [x] 8.21 新增 Equipment mapping 增删改 typed Mutation
- [x] 8.22 支持 `local:*` Implementation 与 Entry Graph 计划 identity
- [x] 8.23 在应用成功后反向导出正式 GUID 与 local file id
- [x] 8.24 将 Linked Pose 资产变更纳入现有单一 Unity 资产事务
- [x] 8.25 保证任一失败回滚整个 Document apply
- [x] 8.26 保证 Inspector、选择与文件变化不自动执行 codec、Build 或 Apply

## 9. MCP 固定生命周期桥接

- [x] 9.1 让 checkout_document 返回 Linked Interface context 与 editable 分片
- [x] 9.2 让 rebase_document 对账 Linked owner、revision 与 conflict
- [x] 9.3 让 dry_run_document 编译 Linked typed Mutation plan
- [x] 9.4 让 apply_document 执行同一资产事务并反向导出
- [x] 9.5 让 validate 运行 Linked Capability、authoring 与 Projection validation
- [x] 9.6 为 MCP diagnostics 保留 Group、selector、Implementation、Entry、Graph 与 Node identity
- [x] 9.7 保持 MCP discovery 不新增 Pose 领域 action
- [x] 9.8 拒绝通过 MCP 直接切换活动 runtime Implementation

## 10. Frontend 与 Projection 目录

- [x] 10.1 扩展 Frontend artifact 收集 Interface descriptors
- [x] 10.2 扩展 Frontend artifact 收集 Group descriptors
- [x] 10.3 扩展 Frontend artifact 收集 selector descriptors
- [x] 10.4 扩展 Frontend artifact 收集 Implementation 与 Entry Graph descriptors
- [x] 10.5 校验所有 identity 在各自作用域唯一
- [x] 10.6 校验 Call、Group、Interface 与 Implementation signature 闭合
- [x] 10.7 校验 Implementation Fact contract 与 Interface 一致
- [x] 10.8 校验 Equipment selector mapping 闭合并推导 candidates
- [x] 10.9 编译每个 Entry Graph 的静态 PoseSubgraph
- [x] 10.10 复用现有 typed DAG 与拓扑排序编译 Entry Graph
- [x] 10.11 复用现有 Rig、source、stage、completion 与 lineage validation
- [x] 10.12 为每个 Entry 生成不可变 operation/stage fragment
- [x] 10.13 为 root Call 生成 Linked dispatch operation
- [x] 10.14 生成 Interface descriptor table
- [x] 10.15 生成 Group descriptor table
- [x] 10.16 生成 selector descriptor table
- [x] 10.17 生成 Implementation descriptor table
- [x] 10.18 生成 Entry fragment table
- [x] 10.19 把全部候选 source 合并进唯一 dense source binding table
- [x] 10.20 对同 source identity 去重并保留稳定 dense index
- [x] 10.21 拒绝目录外 source 与运行时路径查找
- [x] 10.22 把 Linked authoring、Fact、Rig、source 与 ABI 纳入 ProjectionRevision
- [x] 10.23 保持 gameplay ContractHash 不包含 Linked Presentation 内容
- [x] 10.24 如保留 catalog hash，只从 Projection 内容确定性派生并限制为诊断身份
- [x] 10.25 把 root Projection、Linked tables、Native Pose fragments 与 generated references 纳入同一原子发布组

## 11. Group 最大布局与 runtime state

- [x] 11.1 为每个 Entry 计算 operation workspace 容量
- [x] 11.2 为每个 Entry 计算 Pose 与 Value page 容量
- [x] 11.3 为每个 Entry 计算现有 typed temporal owner，不新增通用 node state bytes
- [x] 11.4 为每个 Entry 计算 StateMachine、player、Motion Matching、inertialization 与 Root Orientation Warp slots
- [x] 11.5 为每个 Entry 计算 source demand slots
- [x] 11.6 为每个 Entry 计算 completion 与 diagnostics ranges
- [x] 11.7 合计单个 Implementation 的全部 required Entry 需求
- [x] 11.8 对 Group 全部 selector candidates 逐项取最大值作为 admission 与诊断度量
- [x] 11.9 为全部候选 Entry 生成互不重叠的正式 typed owner range
- [x] 11.10 在唯一 Actor runtime 中预分配全部候选的正式 owner range
- [x] 11.11 复用各正式 owner 的 committed / pending frame transaction
- [x] 11.12 保证候选与 Entry 不共享 mutable typed owner state
- [x] 11.13 预分配 active fragment handle table
- [x] 11.14 预分配 source demand 与 diagnostics workspace
- [x] 11.15 禁止运行时切换扩容或创建托管容器
- [x] 11.16 暴露每 Group 候选最大布局与实际活动实现成本

## 12. Session admission 与 selector runtime

- [x] 12.1 在 Session 创建时加载当前 Projection 的 Linked tables
- [x] 12.2 校验每个 Group 恰好一个 compiled selector
- [x] 12.3 校验 selector 推导的全部 candidates 存在
- [x] 12.4 校验全部 candidates 的 Interface signature
- [x] 12.5 校验全部 candidates 的 Rig identity 与 revision
- [x] 12.6 校验全部 candidates 的 Fact contract 与 runtime ABI
- [x] 12.7 准备全部 candidates 的精确 source closure
- [x] 12.8 初始化每个 Group 的 selector adapter state
- [x] 12.9 由 Equipment adapter 读取 committed slot/id/revision
- [x] 12.10 由 Equipment adapter 解析 Empty 与具体 Equipment 精确映射
- [x] 12.11 输出不含 Equipment 字段的通用 selection frame
- [x] 12.12 拒绝未知 Equipment、缺失映射与目录外 Implementation
- [x] 12.13 保证运行中选择不触发 Unity asset、Addressables 或 YooAsset 查询
- [x] 12.14 保证运行中选择不读取 authoring asset

## 13. Generation 切换事务

- [x] 13.1 在每个 Group 首次 Call 前比较 committed selection 与 incoming frame
- [x] 13.2 校验 frame GroupId 与当前 Group 一致
- [x] 13.3 校验 frame InterfaceId 与 Group Interface 一致
- [x] 13.4 校验 frame ImplementationId 属于 selector candidates
- [x] 13.5 校验 SelectionRevision 未回退
- [x] 13.6 对未变化选择复用 committed generation
- [x] 13.7 对变化选择创建新的 generation identity
- [x] 13.8 在 pending frame 按规范默认值重置目标 Implementation 的 typed owner range
- [x] 13.9 初始化 incoming 全部 required Entry state
- [x] 13.10 聚合 incoming 全部 Entry source demand
- [x] 13.11 在 Barrier 前完成 signature、Rig、Fact、ABI 与 readiness validation
- [x] 13.12 让同帧同 Group 全部 Call 读取同一 incoming handle
- [x] 13.13 禁止 Pose Entry 与 Goals Entry 混用不同 generation
- [x] 13.14 Seal 成功后提交各正式 owner 的 pending state 与 Linked handle
- [x] 13.15 Seal 成功后提交 Implementation handle 与 generation
- [x] 13.16 Discard 时保留旧 committed handle 与 state page
- [x] 13.17 Barrier 后失败沿现有合同进入 Faulted
- [x] 13.18 切换时发布 `PoseDiscontinuity`
- [x] 13.19 不迁移 StateMachine、player time、Motion Matching history 或 inertial state
- [x] 13.20 不注入隐藏 crossfade 或默认 Implementation

## 14. Entry 执行与唯一 Pose 事务

- [x] 14.1 把 Linked dispatch operation 接入 root staged Pose Plan
- [x] 14.2 为 Call 输入绑定 root 当前 Pose/Value page
- [x] 14.3 为 Entry GraphInput 建立当前 generation typed view
- [x] 14.4 执行当前 Implementation 的精确 Entry fragment
- [x] 14.5 为 Entry GraphOutput 发布 typed completion
- [x] 14.6 把 Entry 输出绑定回 root Call output page
- [x] 14.7 保留 Pose space、availability、lineage 与 completion
- [x] 14.8 阻止读取上一 generation output page
- [x] 14.9 把 root 与活动 Entry source demand 合并到唯一 backend
- [x] 14.10 保证每 source 每帧最多 capture 一次
- [x] 14.11 保证 PlayableGraph 每帧最多 Evaluate 一次
- [x] 14.12 保证 root 与 Entry 共用一次 Animancer Evaluate Barrier
- [x] 14.13 保证全部 Entry 成功后才允许 Group Seal
- [x] 14.14 保证唯一 FullBodyIK 消费 Linked Hand Goals
- [x] 14.15 保证合法 Empty Goals 不跳过 root solver
- [x] 14.16 保证唯一 final writer 只在全部 completion 合法后发布
- [x] 14.17 接入旧 source retirement permission 与延迟 physical release

## 15. Root graph 作者边界

- [x] 15.1 在正式 root graph 能力中允许 `EquipmentPose` Call 位于持续 Locomotion 与 Action Slot 之间
- [x] 15.2 在正式 root graph 能力中允许 `EquipmentHandGoals` Call 接收 Component Pose
- [x] 15.3 把 Hand Goals 输出接入 FinalIK v4 的正式 GoalSet 合并链
- [x] 15.4 保持 Predictive Foot Placement 位于 root 并继续提供脚部 Goals
- [x] 15.5 保持唯一 FullBodyIK 位于 root
- [x] 15.6 保持 ActionPlaybackInput 与 AnimationSlot 位于 root
- [x] 15.7 保持 OutputPose 与 final publication 位于 root
- [x] 15.8 支持作者在 Pose Call 后显式放置 Inertialization 或 Blend
- [x] 15.9 不为普通 Idle/Run 状态自动创建 Linked selector 或 generation
- [x] 15.10 固定实际迁移规则：一次删除被替代的 root 持续状态分支；本次因正式 Equipment identity 不可用未触发迁移
- [x] 15.11 Corin 正式 Equipment identity 不可用时停止业务接线并保留单一能力链

## 16. Preview、Live Debug 与诊断

- [x] 16.1 扩展 Preview fixture 输入 committed Equipment selection
- [x] 16.2 让 Preview 复用正式 Equipment selector adapter
- [x] 16.3 让 Preview 复用正式 generic selection frame
- [x] 16.4 让 Preview 复用正式 Projection fragment 与 generation transaction
- [x] 16.5 在 Live Debug 显示 Group、Interface 与 signature
- [x] 16.6 显示 selector identity、selection revision 与映射结果
- [x] 16.7 显示 Implementation identity、revision 与 content hash
- [x] 16.8 显示 Entry、Call node、generation 与 state reset
- [x] 16.9 显示 Group 最大布局与活动实现占用
- [x] 16.10 显示 fragment source demand、现有 operation capture contribution 与 physical release，并复用正式 retirement 协议
- [x] 16.11 显示 PoseDiscontinuity 与显式 Inertialization 状态
- [x] 16.12 显示 Empty Goals 的 Ready、GoalCount、Rig、completion 与 lineage
- [x] 16.13 让 Pose Watch 按 Call、Entry、Implementation 与 generation 读取已完成值
- [x] 16.14 阻止 Pose Watch 重新执行 fragment
- [x] 16.15 为缺失 selector、重复 selector、缺失映射、重复 Call 与缺失 Entry 提供稳定诊断码
- [x] 16.16 为 signature、Fact、Rig、ABI、source 与 completion 失败保留完整 identity 链

## 17. 清理与规格对账

- [x] 17.1 删除该 change 中所有 Content 下载、ResourcePackageVersion、MinimumClientBuildVersion 与新 Session 热更新实现任务
- [x] 17.2 删除任何运行时 Graph 解释、未知 opcode 注入或 authoring asset 回读路径
- [x] 17.3 删除任何缺失映射 default、上一实现沿用或临时 Pose fallback
- [x] 17.4 删除任何 Equipment 专用 Linked core 分支
- [x] 17.5 删除任何中央 `SelectionSourceKind` 与业务 switch
- [x] 17.6 删除任何 Group candidates 重复配置
- [x] 17.7 删除任何 Entry 间隐藏 mutable state 共享
- [x] 17.8 删除任何第二 Slot、第二 FullBodyIK、第二 source backend 或第二 final writer
- [x] 17.9 对账 current specs，确认 Linked authoring 只改变 ProjectionRevision
- [x] 17.10 对账 current specs，确认 gameplay ContractHash 语义未扩大
- [x] 17.11 对账 active FinalIK change 的 GoalSet 与 Rig ABI
- [x] 17.12 对账 active Motion Matching 与 Blend Space change，不复制未闭合节点能力
- [x] 17.13 对账 Equipment 规格，确认有限 Action 与持续 Pose 只有一条正式链
- [x] 17.14 更新 `openspec/project.md` 中需要成为长期架构真相的 Linked Pose 边界
- [x] 17.15 运行 `openspec validate add-linked-pose-interface-runtime --strict`
