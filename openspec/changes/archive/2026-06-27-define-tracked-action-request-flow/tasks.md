# Tasks

## 1. 收口术语和旧歧义

- [x] 1.1 将 `ActionInstance` 口径写入正式 runtime 文档或代码注释边界，明确它是动作事务实例。
- [x] 1.2 删除或重命名 `TrackedActionNodeContract`，避免暗示特殊 Action node。
- [x] 1.3 将 `ActionStartRequest` / `ActionEndRequest` 重命名或替换为 `TrackedActionStartRequest` / `TrackedActionEndRequest`。
- [x] 1.4 确认正式 runtime 不新增 `ActionModule`、`ActionTree`、`AbilityTree`、节点 action identity 或 node membership table。

## 2. 定义 TrackedActionRequest 数据

- [x] 2.1 `TrackedActionStartRequest` 表达 action id 或 profile id。
- [x] 2.2 `TrackedActionStartRequest` 表达 source input request id。
- [x] 2.3 `TrackedActionStartRequest` 表达 input sequence。
- [x] 2.4 `TrackedActionStartRequest` 表达 simulation tick。
- [x] 2.5 `TrackedActionStartRequest` 表达 target key。
- [x] 2.6 `TrackedActionStartRequest` 表达 target snapshot。
- [x] 2.7 `TrackedActionStartRequest` 表达 source graph/node identity 作为 debug 来源。
- [x] 2.8 `TrackedActionEndRequest` 表达 action instance id 和 reason。

## 3. 接入 CharacterPipeline

- [x] 3.1 `CharacterPipelineDefinition` 增加正式 ActionProfile 列表。
- [x] 3.2 `CharacterPipelineDefinition` 校验 ActionProfile 缺失、空 action id 和重复 action id。
- [x] 3.3 `CharacterPipeline` 创建并持有 `ActionRuntime`。
- [x] 3.4 `CharacterPipeline` 初始化时注册 definition 中的 ActionProfile。
- [x] 3.5 `CharacterPipeline` dispose 时释放或清理 ActionRuntime 状态。

## 4. 接入 CharacterGraphContext

- [x] 4.1 `CharacterGraphContext` 暴露 tracked action request service。
- [x] 4.2 Graph 可提交 `TrackedActionStartRequest` 并拿到 `ActionStartResult`。
- [x] 4.3 Graph 可读取当前 `ActionContext`。
- [x] 4.4 Graph 可提交 `TrackedActionEndRequest`。
- [x] 4.5 GraphContext 不直接播放 Timeline、不裁决命中，只转交 request 和上下文。

## 5. Graph authoring UI 闭环

- [x] 5.1 定义普通 request submit authoring 元素，名称避免 `Ability`、`ActionModule` 和静态 node identity。
- [x] 5.2 Graph authoring UI 可选择 ActionProfile 或 action id。
- [x] 5.3 Graph authoring UI 可选择 source input request id。
- [x] 5.4 Graph authoring UI 可配置 target key。
- [x] 5.5 Graph authoring UI 可配置是否消费 source input request。
- [x] 5.6 Graph authoring UI 可配置 instance id 输出 fact key。
- [x] 5.7 Graph authoring UI 不暴露 window/motion/cue 完整网络策略。

## 6. Timeline 和非 Timeline 事实归属

- [x] 6.1 Timeline window fact 可携带当前 ActionInstanceId。
- [x] 6.2 Timeline motion fact 可携带当前 ActionInstanceId、input sequence 和 simulation tick。
- [x] 6.3 Timeline cue fact 可携带当前 ActionInstanceId 和 cue policy key。
- [x] 6.4 没有 ActionContext 时 Timeline 仍按普通 Timeline 播放，不自动创建 ActionInstance。
- [x] 6.5 Graph 可在非 Timeline 动作中提交 window、combat 或 cue fact 并挂当前 ActionInstanceId。

## 7. Editor Inspector 和 Debug

- [x] 7.1 `ActionProfile` Inspector 按 Identity、Network、Tags、Windows、Motion、Cues、Debug 分区。
- [x] 7.2 `CharacterPipelineDefinition` Inspector 展示 ActionProfile 列表和配置错误。
- [x] 7.3 Timeline window Inspector 只编辑 WindowType、WindowId、时间和参数。
- [x] 7.4 Runtime Debug 展示 input request 到 ActionInstance 的链路。
- [x] 7.5 Runtime Debug 展示 ActionInstance 关联的 window、motion、combat、cue 和网络状态。

## 8. 验证

- [x] 8.1 使用 `rg` 确认正式 runtime 不存在 `ActionModule`、`ActionTree`、`AbilityTree`、`AbilityBodyGraph`。
- [x] 8.2 使用 `rg` 确认不存在 `TrackedActionNodeContract` 或等价特殊 node contract 命名。
- [x] 8.3 使用 `rg` 确认 `ActionProfile` 不引用 Graph、Timeline 或 Motion runtime。
- [x] 8.4 运行 `openspec validate define-tracked-action-request-flow --strict --no-interactive`。

