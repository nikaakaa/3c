## 1. 基线与依赖确认

- [x] 1.1 读取并确认 `refactor-pipeline-blackboard-owned-scopes` 的最终实现与 delta
- [x] 1.2 读取并确认 `refactor-animation-transition-lifecycle` 的最终实现与 delta
- [x] 1.3 读取并确认 `refactor-timeline-window-authoring-to-treeclips` 的最终实现与 delta
- [x] 1.4 读取并确认 `refactor-timeline-node-inline-shared-authoring` 的最终实现与 delta
- [x] 1.5 确认不恢复 current spec 中已过期的 ActionWindowTrack 描述
- [x] 1.6 导出 Corin 当前 Agent Snapshot
- [x] 1.7 记录外层 Action StateMachine 的 node、edge、rule graph 与 transition definition identity
- [x] 1.8 记录 Attack1 State body 的 Action activation、Timeline、TreeClip、declaration reference 与 lifecycle
- [x] 1.9 记录 Attack2 State body 的 Action activation、Timeline、TreeClip、declaration reference 与 lifecycle
- [x] 1.10 确认当前 inline managed-reference owner/path 可原子迁移到嵌套 StateMachineGraph
- [x] 1.11 若 owner/path 无法安全迁移，停止实施并记录序列化缺口

## 2. 嵌套 execution path 合同

- [x] 2.1 定义 outer-to-inner 的 StateMachine execution path 值类型
- [x] 2.2 定义 execution path frame 的 runtime identity
- [x] 2.3 定义 execution path frame 的 state identity
- [x] 2.4 定义 execution path frame 的 activation generation
- [x] 2.5 定义 execution path frame 对应的 State body Graph owner/runtime identity
- [x] 2.6 在 StateMachineGraphRuntime 进入状态时创建完整 frame
- [x] 2.7 在嵌套 StateMachineNode 初始化 runtime graph 时继承父 path
- [x] 2.8 在状态 body tick 前 push frame
- [x] 2.9 在状态 body tick 后严格 pop 同一 frame
- [x] 2.10 在 State.OnExit tick 期间保持同一 execution path
- [x] 2.11 在 ForceStop 期间保持可解析的退出 path
- [x] 2.12 对 path push/pop 不匹配直接报告错误
- [x] 2.13 删除只把栈顶 scope 当作完整嵌套上下文的调用

## 3. Blackboard 层级 owner 解析

- [x] 3.1 让 PipelineBlackboardAccessScope 携带完整 execution path
- [x] 3.2 让 State declaration reference 保持 declaration owner identity
- [x] 3.3 按 declaration owner 解析对应 execution path frame
- [x] 3.4 让外层 Attack body 的 State variable 绑定外层 Attack activation
- [x] 3.5 让内层 Attack1 body 的 State variable 绑定 Attack1 activation
- [x] 3.6 让内层 Attack2 body 的 State variable 绑定 Attack2 activation
- [x] 3.7 保持 Character declaration 不依赖 State path
- [x] 3.8 保持 Graph declaration按 Graph runtime owner 寻址
- [x] 3.9 保持 ActionInstance declaration 按 ActionInstanceId 寻址
- [x] 3.10 找不到唯一 State owner frame 时返回失败
- [x] 3.11 删除嵌套解析失败时降级到栈顶 scope 的路径
- [x] 3.12 让 State exit 只清理匹配 activation frame 的 bucket

## 4. Animation transition domain

- [x] 4.1 定义顶层 StateMachineNode 的 animation transition domain identity
- [x] 4.2 让并行 Locomotion 与 Action StateMachineNode 创建不同 domain
- [x] 4.3 让 State body 内嵌套 StateMachineNode 继承父 domain
- [x] 4.4 将 domain identity 接入 StateMachineGraphRuntime
- [x] 4.5 将 domain identity 接入 AnimationTransitionRequest
- [x] 4.6 将 TransitionRuntime active transition key 从单个 nested runtime scope 收敛到 domain
- [x] 4.7 保持同一 domain 最多一个 active transition
- [x] 4.8 保持不同 domain 可并行推进 transition
- [x] 4.9 保持 transition instance identity 与 authoring definition identity 可调试

## 5. Presentation leaf owner 解析

- [x] 5.1 定义逻辑 State activation owner 到 presentation leaf owner 的显式绑定
- [x] 5.2 Timeline request 继续归属当前 active leaf owner
- [x] 5.3 内层 Attack1 首次 tick 时注册 Attack -> Attack1 leaf 关系
- [x] 5.4 内层 Attack2 首次 tick 时更新 Attack -> Attack2 leaf 关系
- [x] 5.5 父 transition request 提交时解析 source leaf owner
- [x] 5.6 父 target 尚未执行时保持 WaitingTarget
- [x] 5.7 target 内层 leaf 首次执行时解析 target leaf owner
- [x] 5.8 TargetReady 与 leaf binding 在同一表现批次可见
- [x] 5.9 target leaf 没有 contribution 时保持正式空输出
- [x] 5.10 不按节点名称或 Graph path 推断 leaf owner
- [x] 5.11 parent owner release 前保留最后合法 source leaf 映射
- [x] 5.12 parent owner release 后确定性清理 leaf 映射

## 6. 父子 transition 收敛

- [x] 6.1 让内层 Attack1 -> Attack2 使用 Action domain 发布 leaf handoff
- [x] 6.2 让内层 Attack2 -> Attack1 使用同一 Action domain 发布 leaf handoff
- [x] 6.3 让内层 Exit release 与外层 replacement transition 使用同一 domain
- [x] 6.4 保持同 tick lifecycle command 的确定顺序
- [x] 6.5 让后提交的父 transition supersede 同 tick 内层 terminal transition
- [x] 6.6 从当前最终视觉结果 capture superseded transition
- [x] 6.7 禁止父子 transition 在同一 Action domain 并行叠加权重
- [x] 6.8 保持 Locomotion domain transition 不受 Action supersede 影响
- [x] 6.9 保持 Immediate、ContributionCrossFade 与 Inertialization 三种 strategy
- [x] 6.10 保持 source logic 停止后表现 transition 独立推进

## 7. 嵌套停止生命周期

- [x] 7.1 将父 State transition 的 NodeStopContext 传入嵌套 StateMachineNode
- [x] 7.2 保持 OriginCause 不被子层覆盖
- [x] 7.3 保持 replacement edge/node identity 不被子层覆盖
- [x] 7.4 让内层 active Attack state 先停止 Timeline gameplay 采样
- [x] 7.5 让内层 active Attack state 执行 State.OnExit
- [x] 7.6 让内层 leaf Action lifecycle 只提交一次 terminal transition
- [x] 7.7 让外层 Attack OnExit 不重复提交 Action lifecycle
- [x] 7.8 让外层 State 等待嵌套 StateMachineNode StopCompleted
- [x] 7.9 让 Self/LowerPriority/Parent graceful stop 复用同一链路
- [x] 7.10 让 ForceStop 立即释放所有嵌套 runtime owner
- [x] 7.11 ForceStop 不伪造 gameplay Cancel、Interrupt 或 Abort
- [x] 7.12 清理内层 Timeline playback、Action Context、Blackboard bucket 与 animation membership

## 8. Agent Snapshot 与编译链路

- [x] 8.1 让 compact Snapshot 递归输出 State body 内 StateMachineNode 摘要
- [x] 8.2 输出 nested graph id、graph path 与 ownership
- [x] 8.3 输出 nested states 与 transitions
- [x] 8.4 输出 nested leaf 的 Action activation、Timeline 与 lifecycle 摘要
- [x] 8.5 更新 two_hit_combo intent 展开目标为外层 Attack + 内层 combo SM
- [x] 8.6 使用普通 StateMachineNode/StateNode Patch IR 表达嵌套
- [x] 8.7 不新增 Attack 专用 Patch opcode
- [x] 8.8 让 Compiler 使用正式 inline graph authoring API 创建嵌套图
- [x] 8.9 让 Validator 拒绝 Attack1/Attack2 残留在外层 Action SM
- [x] 8.10 让 Validator 检查 Attack Root 的 nested StateMachineNode
- [x] 8.11 让 Validator 检查内层 Action Context、Timeline ownership 与 lifecycle
- [x] 8.12 让 Validator 检查 execution path 与 transition domain 可解析

## 9. Corin 外层 Action StateMachine 迁移

- [x] 9.1 在外层 Action StateMachine 创建 Attack StateNode
- [x] 9.2 为 Attack 创建 inline StateBehaviorSubTree
- [x] 9.3 保持 Attack OnEnter 不激活 ActionProfile
- [x] 9.4 保持 Attack OnExit 不提交 Action lifecycle
- [x] 9.5 在 Attack Root 创建普通 StateMachineNode
- [x] 9.6 为该节点创建 inline Attack Combo StateMachineGraph
- [x] 9.7 将外层 None -> Attack1 改为 None -> Attack
- [x] 9.8 保持 None -> Attack rule 只查询 Attack request
- [x] 9.9 配置外层 Attack -> None 使用 StateRootCompleted
- [x] 9.10 保持外层 DodgeBack 与 DodgeForward 状态不迁入 Attack SM
- [x] 9.11 保持外层 Dodge request 条件和优先级

## 10. Corin 内层 Attack StateMachine 迁移

- [x] 10.1 在内层 graph 建立 Enter、AnyState 与 Exit 控制节点
- [x] 10.2 将 Attack1 StateNode 移入内层 graph
- [x] 10.3 将 Attack2 StateNode 移入内层 graph
- [x] 10.4 重绑 Attack1 inline StateBehaviorSubTree owner/path
- [x] 10.5 重绑 Attack2 inline StateBehaviorSubTree owner/path
- [x] 10.6 保持 Attack1 activation 消费 Attack request
- [x] 10.7 保持 Attack2 activation 消费 Attack request
- [x] 10.8 保持每个 leaf 创建新的 Action Context
- [x] 10.9 保持 Attack1 inline TimelineData 与所有 track/clip
- [x] 10.10 保持 Attack2 inline TimelineData 与所有 track/clip
- [x] 10.11 保持 Attack1 Hit/Cancel Decision TreeClip
- [x] 10.12 保持 Attack2 Hit/Cancel Decision TreeClip
- [x] 10.13 保持 Attack1/Attack2 motion curve 与 animation reference
- [x] 10.14 将 Attack1 -> Attack2 edge 与 rule graph 移入内层
- [x] 10.15 将 Attack2 -> Attack1 edge 与 rule graph 移入内层
- [x] 10.16 保持 combo rule 为 Cancel window AND Attack request
- [x] 10.17 保持 combo condition query 不消费 request
- [x] 10.18 将 Attack1 正常完成 edge 改连内层 Exit
- [x] 10.19 将 Attack2 正常完成 edge 改连内层 Exit
- [x] 10.20 保持正常完成 lifecycle 只提交一次 Complete
- [x] 10.21 保持 combo 离开 lifecycle 提交 Cancel(ComboWindow)
- [x] 10.22 保持 Tree abort lifecycle 使用原始 StateExitContext

## 11. 旧结构清理

- [x] 11.1 删除外层旧 Attack1 StateNode
- [x] 11.2 删除外层旧 Attack2 StateNode
- [x] 11.3 删除外层旧 Attack1 -> Attack2 edge
- [x] 11.4 删除外层旧 Attack2 -> Attack1 edge
- [x] 11.5 删除外层旧 Attack1/Attack2 -> None edge
- [x] 11.6 删除迁移后 orphan ConditionRuleGraph
- [x] 11.7 删除迁移后 orphan owner/path metadata
- [x] 11.8 搜索确认外层 Action SM 不再平铺具体攻击段
- [x] 11.9 搜索确认没有 shared 临时 StateMachineGraph/SubTree/Timeline asset
- [x] 11.10 搜索确认没有 Attack 专用 runtime 或兼容读取

## 12. 自动验证与文档收口

- [x] 12.1 编译 BTSMTL runtime 程序集
- [x] 12.2 编译 BTSMTL editor 程序集
- [x] 12.3 编译 Character Pipeline runtime 程序集
- [x] 12.4 编译 Character Pipeline editor 程序集
- [x] 12.5 编译 Assembly-CSharp
- [x] 12.6 重导出 Corin compact Agent Snapshot
- [x] 12.7 运行正式 Agent graph validator
- [x] 12.8 确认 validator 报告外层 4 个 action category state
- [x] 12.9 确认 validator 报告内层 Attack1/Attack2 state
- [x] 12.10 确认 validator 报告 2 个 inline Attack Timeline 且无一次性 Timeline asset
- [x] 12.11 确认 validator 报告 4 个 Attack Hit/Cancel TreeClip
- [x] 12.12 确认 validator 无 orphan owner/path、scope 或 transition domain 错误
- [x] 12.13 更新 `openspec/project.md` 的 Corin Attack 层级口径
- [x] 12.14 更新本 change tasks 为真实完成状态
- [x] 12.15 运行 `openspec validate refactor-corin-attack-to-nested-state-machine --strict --no-interactive`

## 13. 连段条件真实连线修复

- [x] 13.1 复查 Attack1 -> Attack2 ConditionRuleGraph 的节点与 PropertyEdge
- [x] 13.2 复查 Attack2 -> Attack1 ConditionRuleGraph 的节点与 PropertyEdge
- [x] 13.3 确认两条规则均因 Cancel 条件未接入 And 而恒为 false
- [x] 13.4 通过正式 Agent Patch 编译链重建 Attack1Cancel AND Attack request 条件
- [x] 13.5 通过正式 Agent Patch 编译链重建 Attack2Cancel AND Attack request 条件
- [x] 13.6 保持原 combo transition edge 与 animation transition identity
- [x] 13.7 让 Corin Validator 校验 Cancel 与 Attack request 到 And 的真实 PropertyEdge
- [x] 13.8 让 Corin Validator 校验 And 到 Result 的真实 PropertyEdge
- [x] 13.9 重导出 Corin compact Agent Snapshot
- [x] 13.10 运行正式 Agent graph validator
- [x] 13.11 编译 Character Pipeline editor 程序集
- [x] 13.12 更新本节 tasks 为真实完成状态
- [x] 13.13 运行严格 OpenSpec validate
