## 0. 目录与 Ref 复用边界
- [x] 0.1 建立 `Assets/Scripts/Character/Graph/Model`、`Contracts`、`Solver`、`Runtime`、`Diagnostics` 目录。
- [x] 0.2 建立 `Assets/Scripts/Character/Action/Branch/Model`、`Solver`、`Runtime`、`Diagnostics` 目录。
- [x] 0.3 建立 `Assets/Scripts/Character/Action/Timeline/Model`、`Solver`、`Config`、`Diagnostics` 目录。
- [x] 0.4 建立 `Assets/Tests/Editor/Character/Graph`、`Action/Branch`、`Action/Timeline` 测试目录。
- [x] 0.5 确认第一版不直接复制 `Ref/wly970123` runtime runner 到 `Assets/Scripts`。
- [x] 0.6 增加静态边界测试，确认正式 runtime 不引用 Taco `TreeRunner`、`TimelinePlayer`、PlayableGraph、Animator、Transform 或 scene object。
- [x] 0.7 将后续 Taco GraphView / Timeline editor 迁入范围限定为 `Assets/Editor/Character/Graph`、`Assets/Editor/Character/Action/Timeline` 或 `Assets/Editor/Character/RefImport`。

## 1. CharacterGraphDefinition 资产合同
- [x] 1.1 新增 `CharacterGraphDefinition` 或等价顶层纯数据资产合同。
- [x] 1.2 新增 Locomotion、Action、UpperBody、Cue 分支定义字段。
- [x] 1.3 新增分支 id / 稳定引用 / 空分支定义的类型化表达。
- [x] 1.4 明确 `CharacterGraphDefinition` 不持有 Unity scene object、runtime animation object 或 input object。
- [x] 1.5 增加空 `CharacterGraphDefinition` 默认值测试。
- [x] 1.6 增加分支定义序列化/反序列化测试。
- [x] 1.7 增加静态边界测试，确认资产合同不引用 `MonoBehaviour`、`Transform`、`Animator`、Animancer runtime object 或 `InputAction`。

## 2. CharacterExecutionNodeTree 运行时节点树合同
- [x] 2.1 新增 `CharacterExecutionNodeTree` 或等价运行时节点树模型。
- [x] 2.2 新增 `CharacterExecutionRootNode` 或等价 root 合同。
- [x] 2.3 新增 runtime node 单父约束校验。
- [x] 2.4 新增受控 parallel/composite node 合同，支持同帧评估多个子分支。
- [x] 2.5 新增输入向下、输出向上的评估接口或等价合同。
- [x] 2.6 新增节点 state 归属合同，禁止跨分支直接写 state。
- [x] 2.7 新增 `CharacterGraphInput` 或等价只读输入模型。
- [x] 2.8 新增 `CharacterGraphState` 或等价可 restore 状态模型。
- [x] 2.9 新增 `CharacterGraphFrameResult` 或等价候选输出聚合模型。
- [x] 2.10 增加单父节点、并行 composite、空树、空分支和 restore state 的 EditMode 测试。
- [x] 2.11 增加静态边界测试，确认运行时节点树不引用 `TreeRunner`、`TimelinePlayer` 或 MonoBehaviour runner。

## 3. 分支端口合同
- [x] 3.1 新增 `LocomotionBranch` 输入/输出合同。
- [x] 3.2 新增 `ActionBranch` 输入/输出合同。
- [x] 3.3 新增 `UpperBodyBranch` 输入/输出合同。
- [x] 3.4 新增 `CueBranch` 输入/输出合同。
- [x] 3.5 明确 FullBody / UpperBody / LowerBody 是 claim/channel 语义，不是 gameplay owner。
- [x] 3.6 新增未实现分支的明确空候选和诊断结果。
- [x] 3.7 增加四个分支默认输出语义测试。
- [x] 3.8 增加未实现分支不触发 fallback runner、Resources 或场景查找的测试。

## 4. ActionBranch 节点合同
- [x] 4.1 新增 `ActionBranchDefinition` 纯数据模型。
- [x] 4.2 新增 `ActionNodeDefinition` 与节点 kind 合同。
- [x] 4.3 新增 `ActionBranchRuntime` 或等价 Action 分支评估入口。
- [x] 4.4 新增 `ActionBranchOutcome` 或等价本帧输出模型。
- [x] 4.5 新增 `ActionTimelineNodeDefinition`，作为第一种具体 Action 节点。
- [x] 4.6 增加 ActionBranch root、空节点和 TimelineNode 装配测试。
- [x] 4.7 增加 ActionBranch 抽象模型不引用 Dodge 专用类型的测试。

## 5. ActionTimeline 数据模型
- [x] 5.1 新增 `ActionTimelineDefinition` 纯数据模型。
- [x] 5.2 新增 `ActionTimelineTrackDefinition` 纯数据模型。
- [x] 5.3 新增 `ActionTimelineClipDefinition` 纯数据模型。
- [x] 5.4 新增 `ActionTimelineClipKind` 和 `ActionTimelineTrackKind` 枚举。
- [x] 5.5 新增 `ActionTimelineOutcome` 或等价本帧输出模型，并保持 Cue 只作为纯数据请求。
- [x] 5.6 新增 `ActionTimelineValidationResult`。
- [x] 5.7 明确 timeline runtime frame 对齐权威 simulation tick，seconds 只作为工具层显示换算。
- [x] 5.8 增加模型默认值、非法范围、空定义和 frame 边界的 EditMode 测试。

## 6. Evaluator
- [x] 6.1 新增 `ActionTimelineEvaluator`。
- [x] 6.2 支持按 active action state time 和 tick interval 解析权威 current frame。
- [x] 6.3 支持 `AnimationKey` clip 输出 animation intent。
- [x] 6.4 支持 `Motion` clip 输出 motion intent 或 motion spec override。
- [x] 6.5 支持 `HitboxWindow` clip 输出 active window fact。
- [x] 6.6 支持 `CancelWindow` clip 输出 active window fact。
- [x] 6.7 支持 `Cue` clip 输出一次性 cue request 到 outcome/diagnostics，不扩展表现提交路径。
- [x] 6.8 确认 evaluator 不保存跨帧 gameplay state。
- [x] 6.9 增加 clip 起止边界、重叠 track、空 track、frame/seconds 换算和 cue 一次性触发的 EditMode 测试。

## 7. Action Catalog 装配
- [x] 7.1 扩展 Action definition runtime model，使其可以携带或定位 ActionBranch definition。
- [x] 7.2 扩展 Action definition SO 校验，缺失必需 branch/timeline 时报告错误。
- [x] 7.3 确认缺失 branch/timeline 不通过旧 Dodge 字段、Resources 或代码默认值补齐。
- [x] 7.4 增加 Action Catalog 到 runtime definition 的 ActionBranch 装配测试。

## 8. Action Lifecycle 接入
- [x] 8.1 扩展 `ActionLifecycleFrame` 或等价 frame，使其可承载 ActionBranch outcome。
- [x] 8.2 在 `ActionLifecycleRuntime.Tick` 或其调用链中评估 active ActionBranch。
- [x] 8.3 保持 lifecycle restore state 只保存纯数据，不保存 branch evaluator 或 timeline evaluator 实例。
- [x] 8.4 增加 accepted action 启动、持续帧、切换 action、complete 后停止输出的 EditMode 测试。

## 9. Frame Submission 接入
- [x] 9.1 将 ActionBranch animation outcome 合并到 Action animation submission。
- [x] 9.2 将 ActionBranch motion outcome 合并到 Action motion resolve input 或等价 Action candidate。
- [x] 9.3 将 hitbox/cancel window outcome 作为纯数据 facts/candidate 传入 frame submission。
- [x] 9.4 确认 CharacterExecutionNodeTree、ActionBranch 和 timeline outcome 不直接写 `CharacterRuntimeBlackboard`。
- [x] 9.5 增加 `FullBodyActionFrameSubmitter` 或等价 Action submitter 的集成测试。

## 10. Dodge 等价验证
- [x] 10.1 建立 Dodge variant 到 ActionBranch + ActionTimeline runtime model 的 adapter / builder。
- [x] 10.2 建立 Directional Dodge 的等价 ActionBranch 测试定义。
- [x] 10.3 建立 Backstep Dodge 的等价 ActionBranch 测试定义。
- [x] 10.4 验证 timeline 输出的 duration、distance、rotateToDirection 和 animation key 与现有 Dodge definition 一致。
- [x] 10.5 验证 Dodge 完成、Run latch 和 animation-end 等待行为不回退。
- [x] 10.6 增加抽象分离测试，确认 CharacterGraphDefinition、CharacterExecutionNodeTree、ActionBranch、ActionTimeline 模型和 evaluator 不引用 Dodge 专用类型。

## 11. 边界验证
- [x] 11.1 增加静态搜索测试，确认 CharacterExecutionNodeTree / ActionBranch / ActionTimeline runtime 不引用 `MonoBehaviour`、`Transform`、`CharacterController`、`Animator`、Animancer runtime object 或 `InputAction`。
- [x] 11.2 增加静态搜索测试，确认正式 runtime 不引用 `TreeRunner` 或 `TimelinePlayer`。
- [x] 11.3 增加测试确认 CharacterExecutionNodeTree / ActionBranch / ActionTimeline 不调用 motion executor、animation presenter 或 blackboard 写入接口。
- [x] 11.4 运行 `openspec validate add-character-graph-contracts --strict --no-interactive`。

