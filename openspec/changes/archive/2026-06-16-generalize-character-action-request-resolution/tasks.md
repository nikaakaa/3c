# Tasks

## 0. 方案前置
- [x] 0.1 使用 UTF-8 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 0.2 使用 UTF-8 读取 `openspec/project.md`、`openspec/AGENTS.md` 和相关现有规格。
- [x] 0.3 运行 `openspec list` 与 `openspec list --specs`，确认 active change 顺序。
- [x] 0.4 确认本 change 在 `add-light-attack-combo-action` 之前 apply。

## 1. 影响面确认
- [x] 1.1 对 `CharacterActionRequestSubmissionArbiter` 做 GitNexus impact analysis，并记录 blast radius。
- [x] 1.2 对 `FullBodyActionRequestSubmissionProviderCollection` 做 GitNexus impact analysis，并记录 blast radius。
- [x] 1.3 对 `FullBodyActionInputRequestBuilder` 做 GitNexus impact analysis，并记录 blast radius。
- [x] 1.4 对 `DodgeActionRequest` 与 `DodgeActionPlanner` 做 GitNexus impact analysis，并记录 blast radius。
- [x] 1.5 如果出现 HIGH 或 CRITICAL impact，明确记录风险后继续按 proposal 实施。

## 2. 请求数据契约
- [x] 2.1 新增纯数据 `CharacterActionRequest`，只表达请求来源、类型、时序、优先级提示和可选值类型 payload。
- [x] 2.2 确认 `CharacterActionRequest` 不包含 target state、动画 key、motion spec、Unity object、Animancer 引用或 controller 引用。
- [x] 2.3 新增 `CharacterActionResolveContext`，集中承载当前状态、timeline facts、locomotion facts、step 和必要只读 facts。
- [x] 2.4 新增 `CharacterResolvedAction`，表达 target state、request fact、interrupt request、motion/animation seed 和 source request 信息。
- [x] 2.5 为无效 request、未解析 request 和已解析 action 提供纯数据判定 API。

## 3. Provider / Resolver 接口
- [x] 3.1 新增 `ICharacterActionRequestProvider` 或等价接口，使 provider 只输出 `CharacterActionRequest`。
- [x] 3.2 新增 `ICharacterActionRequestResolver` 或等价接口，使 resolver 消费 request、context 和正式配置。
- [x] 3.3 新增 resolver collection 或 registry，允许 Attack、Dodge、Jump 通过新增 resolver 扩展。
- [x] 3.4 确认 arbiter 主流程不直接新增 Attack、Dodge、Jump、HitReact 的目标状态分支。
- [x] 3.5 确认 provider 不能直接消费或修改 Unity scene object、Animator、AnimancerComponent、CharacterController。

## 4. Dodge 行为保持迁移
- [x] 4.1 将 Dodge 输入读取迁移为 Dodge request provider。
- [x] 4.2 将 Dodge variant、direction、priority、state、animation 和 motion seed 解析迁移为 Dodge resolver。
- [x] 4.3 保持现有 directional dodge 与 backstep 行为。
- [x] 4.4 保持 rejected Dodge request 不被错误消费。
- [x] 4.5 删除或降级旧 Dodge-only builder 在正式主线中的直接入口含义。

## 5. Attack / Jump 扩展约束
- [x] 5.1 增加 Attack request provider 的接口测试替身，验证 provider 只提交 request，不输出 `Attack01/02/03`。
- [x] 5.2 增加 Attack resolver 的接口测试替身，验证 target state 只能由 resolver 输出。
- [x] 5.3 增加 Jump request provider/resolver 的接口测试替身，验证 Jump 不需要修改 arbiter 主流程。
- [x] 5.4 更新 `add-light-attack-combo-action` 的实施依赖说明，使其基于本 change 的 resolved action contract。

## 6. 测试
- [x] 6.1 增加 `CharacterActionRequestProviderTests`，覆盖 provider 不输出 target state。
- [x] 6.2 增加 `CharacterActionRequestResolverTests`，覆盖 resolver 输出纯数据 resolved action。
- [x] 6.3 增加 Dodge provider/resolver 行为保持测试。
- [x] 6.4 增加 arbiter 多 provider priority/tie-break 测试。
- [x] 6.5 增加 input buffer 测试，确认 `InputRequestKind` 只作为缓冲键。
- [x] 6.6 增加 rollback replay 定向测试，确认 Attack pressed request 仍可保留给后续 combo。
- [x] 6.7 跑相关 Unity EditMode 定向测试。

## 7. 构建与校验
- [x] 7.1 跑 C# build 检查。
- [x] 7.2 跑 `openspec validate generalize-character-action-request-resolution --strict --no-interactive`。
- [x] 7.3 跑 GitNexus `detect_changes()`。
- [x] 7.4 更新本 `tasks.md` checklist。
