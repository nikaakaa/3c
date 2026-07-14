# Tasks

## 1. 清理 Ability 执行体语义

- [x] 1.1 删除 `IAbilityBody` 源文件和 `.meta`。
- [x] 1.2 从 `AbilityAsset` 移除 `BodyGraph` 字段和 `TreeDesigner.RunnableTree` 依赖。
- [x] 1.3 将 `AbilityAsset` 重命名或替换为 `ActionProfile`。
- [x] 1.4 将 `AbilityRuntime` 重命名或替换为 `ActionRuntime`。
- [x] 1.5 将 `AbilitySpec`、`AbilityRequest`、`AbilityActivation`、`AbilityContext` 重命名或替换为 Action 语义。
- [x] 1.6 删除正式 runtime 中 `Ability` 作为执行单元的命名和目录，不保留兼容 alias。

## 2. 建立 ActionProfile 策略模型

- [x] 2.1 新增 `ActionProfile`，表达 action id、显示名、调试分类和 tags。
- [x] 2.2 新增 action 级 prediction、authority、replication、correction policy。
- [x] 2.3 新增 action 级 block tags、cancel tags 和 target policy。
- [x] 2.4 新增 window policy 数据结构，按 window type 配置 authority/history/replication/digest 策略。
- [x] 2.5 新增 motion policy 数据结构，按 motion source type 配置 prediction/correction 策略。
- [x] 2.6 新增 cue policy 数据结构，按 cue type 配置 local/predicted/confirmed 策略。
- [x] 2.7 确认 `ActionProfile` 不引用 Graph、Timeline 或 Motion runtime 对象。

## 3. 建立 ActionInstance 运行时模型

- [x] 3.1 新增 `ActionInstance`，表达 instance id、action id、prediction key、input sequence、start tick、target snapshot。
- [x] 3.2 新增 action phase 枚举，表达 startup、active、recovery、cancel、ended。
- [x] 3.3 新增 action state 枚举，表达 requested、predicted、confirmed、rejected、cancelled、ended、corrected。
- [x] 3.4 新增 `ActionStartRequest`，表达 Graph 提交的动作启动请求。
- [x] 3.5 新增 `ActionEndRequest` 或等价结束请求。
- [x] 3.6 新增 `ActionContext`，暴露当前 active instance 只读信息。
- [x] 3.7 实现 `ActionRuntime` 的 begin、confirm、reject、cancel、end 状态流转。

## 4. 规划产出事实归属

- [x] 4.1 为后续 Graph 节点定义 `BeginTrackedAction` / `EndTrackedAction` service 边界。
- [x] 4.2 为后续 Timeline window fact 定义 `ActionInstanceId`、`WindowId`、`WindowType`、tick range 和 digest 字段。
- [x] 4.3 为后续 Motion fact 定义 `ActionInstanceId`、`InputSequence`、simulation tick 和 correction id 字段。
- [x] 4.4 为后续 Presentation cue 定义 action 归属和 cue policy key 字段。
- [x] 4.5 为后续 Combat event 定义 action instance、window id、target id 和 combat tick 字段。
- [x] 4.6 确认不新增 Tree/SubTree/SMNode 网络类型标记。

## 5. Inspector 和调试规划

- [x] 5.1 规划 `ActionProfile` Inspector 分区：Identity、Network、Windows、Motion、Cues、Tags、Debug。
- [x] 5.2 规划 `BeginTrackedActionNode` Inspector 只引用 ActionProfile/ActionId 和 TargetKey。
- [x] 5.3 规划 Timeline window Inspector 只配置 WindowType、WindowId 和窗口参数。
- [x] 5.4 规划 Runtime Debug Inspector 展示 ActionInstance、Window、Network confirm/reject/correction 链路。

## 6. 验证

- [x] 6.1 使用 `rg` 确认正式 runtime 中不存在 `AbilityAsset`、`AbilityRuntime`、`AbilityRequest`、`AbilityActivation`、`AbilityContext`、`IAbilityBody`。
- [x] 6.2 使用 `rg` 确认正式 runtime 中不存在 `BodyGraph` ability 执行体引用。
- [x] 6.3 使用 `rg` 确认 `ActionProfile` 不引用 Graph/Timeline/Motion runtime。
- [x] 6.4 运行 `openspec validate refactor-ability-to-action-instance-network-policy --strict --no-interactive`。
