## 1. 现有 Dodge 链路确认

- [x] 1.1 确认 `Left Shift` 继续只绑定现有 Dodge action。
- [x] 1.2 确认 DodgeForward/DodgeBack 继续由现有 Dodge request 进入。
- [x] 1.3 确认 ActivateActionInstanceNode 继续作为唯一 request 消费点。
- [x] 1.4 确认两个 Dodge state 继续使用现有 ActionProfile、ActionContext、Timeline、motion curve 和 IFrame。
- [x] 1.5 确认 Dodge OnEnter/OnExit 继续写入同一 pipeline blackboard `IsDodging`。
- [x] 1.6 确认本变更不新增输入、Dodge 动作状态或 Dodge 资产。

## 2. Tick 起点角色朝向事实

- [x] 2.1 定义只包含稳定平面姿态数据的 actor pose snapshot。
- [x] 2.2 将 actor Transform 作为显式构造依赖传入 CharacterGraphContext。
- [x] 2.3 在每个 logic tick 的 BTSMTL 执行前捕获 actor pose snapshot。
- [x] 2.4 通过 CharacterGraphContext 暴露只读 actor pose snapshot 查询。
- [x] 2.5 在 deactivate/dispose 边界清理 snapshot 有效状态。

## 3. 朝向误差条件节点

- [x] 3.1 抽取 camera-relative locomotion 世界方向共用解析器。
- [x] 3.2 让 LocomotionInputMotionNode 复用共用方向解析器。
- [x] 3.3 新增接收 Vector2 PropertyPort 的 `CharacterMoveFacingAngleInfoNode`。
- [x] 3.4 让 facing-angle 节点只读取 camera basis 与 actor pose snapshot。
- [x] 3.5 明确零输入或无效 snapshot 时的零值和错误语义。
- [x] 3.6 将 facing-angle 节点注册到 Agent node emitter。
- [x] 3.7 扩展 Agent patch compiler 生成 MoveAxis -> facing angle -> compare -> result 条件图。
- [x] 3.8 扩展 Agent validator 识别 facing-angle 条件节点依赖。

## 4. Agent authoring 所有权状态能力

- [x] 4.1 扩展 Agent patch 条件术语以读取 pipeline blackboard bool。
- [x] 4.2 复用现有 PipelineBlackboardBoolInfoNode、NotNode、AndNode 和 CompareNode 生成条件图。
- [x] 4.3 扩展 Agent patch 模型以表达无 Timeline、无 motion 的 ActionOverride inline state。
- [x] 4.4 扩展 Agent graph validator 校验 ActionOverride 不引用 Dodge 资产或提交 motion。
- [x] 4.5 保持 patch 第二次应用不新增重复 state、edge 或条件节点。

## 5. Corin Locomotion StateMachine

- [x] 5.1 在 Locomotion StateMachine 中新增唯一 ActionOverride StateNode。
- [x] 5.2 保持 ActionOverride inline body 无动画、无 Timeline、无 motion。
- [x] 5.3 从 Idle 增加高优先级 `IsDodging -> ActionOverride` 边。
- [x] 5.4 从 WalkStart 增加高优先级 `IsDodging -> ActionOverride` 边。
- [x] 5.5 从 WalkLoop 增加高优先级 `IsDodging -> ActionOverride` 边。
- [x] 5.6 从 WalkEnd 增加高优先级 `IsDodging -> ActionOverride` 边。
- [x] 5.7 从 RunStart 增加高优先级 `IsDodging -> ActionOverride` 边。
- [x] 5.8 从 RunLoop 增加高优先级 `IsDodging -> ActionOverride` 边。
- [x] 5.9 从 RunEnd 增加高优先级 `IsDodging -> ActionOverride` 边。
- [x] 5.10 从 MovingTurn 增加高优先级 `IsDodging -> ActionOverride` 边。
- [x] 5.11 配置 `ActionOverride -> RunLoop` 的 `NOT IsDodging AND HasMove` 边。
- [x] 5.12 配置 `ActionOverride -> RunEnd` 的 `NOT IsDodging AND NoMove` 边。
- [x] 5.13 将 `RunLoop -> MovingTurn` 条件迁移为 Run 区间与 facing-angle threshold 组合。
- [x] 5.14 删除 Corin 正式 ConditionRuleGraph 中旧 input-angle-delta 条件节点和 property edge。
- [x] 5.15 调整 MovingTurnAngleThreshold 正式初值。
- [x] 5.16 保持 ActionOverride 为 StateNode inline graph，不创建 SubTree asset。
- [x] 5.17 保持同 source Transition priority 稳定且无重复边。

## 6. 编译、资产校验与规格收口

- [x] 6.1 运行 Agent patch compile/apply 并确认第二次应用保持幂等。
- [x] 6.2 运行 Agent graph validator，修复 ActionOverride、blackboard 条件和 facing-angle 条件错误。
- [x] 6.3 触发 Unity 正常脚本与资产导入并清理新增 console error。
- [x] 6.4 使用禁用 build server 的命令编译相关解决方案并关闭 .NET build server。
- [x] 6.5 对照 proposal、design 和 spec delta 确认每项任务真实完成。
- [x] 6.6 将本文件已完成任务更新为 `[x]`。
- [x] 6.7 运行 `openspec validate add-dodge-and-facing-turn-authoring --strict --no-interactive`。
