## 1. 前置对齐
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 读取 `openspec/specs/committed-action-node-selection/spec.md`，确认 selector / condition / timeline 的现行职责。
- [x] 1.3 读取 `formalize-committed-action-authoring-toolchain` 的 proposal、design、tasks 和相关 spec delta，确认通用 Branch Authoring 的落点。
- [x] 1.4 列出本次会修改的 runtime、config、editor adapter、validator 和 test 文件。
- [x] 1.5 对将修改的核心符号运行 GitNexus impact，记录 direct callers、affected processes 和 risk。
- [x] 1.6 若 impact 为 HIGH 或 CRITICAL，先停下说明风险和拆分方案。

## 2. 现状盘点
- [x] 2.1 查找现有 `CommittedActionBranchDefinition` 或批准等价 branch runtime 类型。
- [x] 2.2 查找现有 condition 节点、selector 节点和 timeline 节点的数据结构。
- [x] 2.3 查找现有 branch evaluator 的 condition 判断入口。
- [x] 2.4 查找 Dodge Directional / Backstep 或等价移动意图选择逻辑。
- [x] 2.5 查找 Action timeline window fact 的 authoring 和 runtime 输出位置。
- [x] 2.6 查找 Branch Editor serialized adapter 的读写入口。
- [x] 2.7 记录当前是否存在动作专用 condition switch、Dodge-only 字段或 branch fallback。

## 3. Condition Kind 模型
- [x] 3.1 定义 `ActionConditionKind` 或批准等价枚举。
- [x] 3.2 加入 `Always`。
- [x] 3.3 加入 `RequestHeld`。
- [x] 3.4 加入 `RequestReleased`。
- [x] 3.5 加入 `RequiredFactActive`。
- [x] 3.6 加入 `TimelineComplete`。
- [x] 3.7 加入 `HasMoveIntent`。
- [x] 3.8 加入 `ActionVariantEquals`。
- [x] 3.9 确认 kind 命名不使用 Block、Attack、GuardCounter、Dodge 专用名称。

## 4. Authoring Payload
- [x] 4.1 定义 condition authoring 数据结构。
- [x] 4.2 为 request 条件提供 request kind 或批准等价 stable id 字段。
- [x] 4.3 为 `RequiredFactActive` 提供 required fact id 字段。
- [x] 4.4 为 `ActionVariantEquals` 提供 action variant stable id 字段。
- [x] 4.5 为 `TimelineComplete` 明确不需要 editor preview time 字段。
- [x] 4.6 为 `HasMoveIntent` 明确只读取已有纯数据 move intent，不保存 camera、Transform 或 input object。
- [x] 4.7 对未使用 payload 字段加入 validator warning 或清理策略。
- [x] 4.8 确认 authoring 数据可被 `CharacterActionDefinitionSO` 的通用 branch authoring 保存。

## 5. Runtime Definition
- [x] 5.1 定义 condition runtime definition。
- [x] 5.2 将 authoring kind 编译到 runtime kind。
- [x] 5.3 将 request kind 编译为 runtime stable id。
- [x] 5.4 将 required fact id 编译为 runtime stable id。
- [x] 5.5 将 action variant 编译为 runtime stable id。
- [x] 5.6 确认 runtime definition 不保存 Unity scene object、GraphView object、Animator、Animancer、InputAction 或 MonoBehaviour。
- [x] 5.7 确认 runtime definition 可用于 deterministic unit test。

## 6. Fact Id 收集与校验
- [x] 6.1 定义 condition 可引用的 fact id 数据结构。
- [x] 6.2 定义共享 `ActionFactCompileContext`、`ActionFactIdResolver` 或批准等价解析入口。
- [x] 6.3 从当前 action/timeline authoring 中收集 window fact id。
- [x] 6.4 从 request fact source 或测试 fixture 中收集 request fact id。
- [x] 6.5 从 runtime fact registry 或批准等价 source 中收集 runtime fact id。
- [x] 6.6 从 locomotion fact source 或批准等价 source 中收集 locomotion fact id。
- [x] 6.7 校验 `RequiredFactActive` 的 fact id 是否存在。
- [x] 6.8 校验空 fact id。
- [x] 6.9 校验非法字符或不符合项目命名规则的 fact id。
- [x] 6.10 校验重复声明的 fact id，并按冲突程度输出 error 或 warning。
- [x] 6.11 校验冲突 fact declaration 不会被静默选择其中一条。
- [x] 6.12 确认缺失 fact id 不会被编译为隐藏 false、true 或默认窗口。
- [x] 6.13 确认 transition policy matrix 能复用同一个 fact resolver 口径。

## 7. Compiler
- [x] 7.1 新增 condition authoring 到 runtime definition 的编译入口。
- [x] 7.2 将编译入口接入 branch authoring compiler。
- [x] 7.3 编译 `Always`。
- [x] 7.4 编译 `RequestHeld`。
- [x] 7.5 编译 `RequestReleased`。
- [x] 7.6 编译 `RequiredFactActive`。
- [x] 7.7 编译 `TimelineComplete`。
- [x] 7.8 编译 `HasMoveIntent`。
- [x] 7.9 编译 `ActionVariantEquals`。
- [x] 7.10 确认 compiler 只做数据转换和 diagnostics，不调用 branch evaluator、lifecycle、motion executor、animation presenter 或 blackboard writer。

## 8. Evaluation Context
- [x] 8.1 定义或扩展 condition evaluation context。
- [x] 8.2 context 提供 accepted action id。
- [x] 8.3 context 提供 active TimelineNode id 或批准等价节点身份。
- [x] 8.4 context 提供 action-local tick。
- [x] 8.5 context 提供 runtime timeline duration ticks。
- [x] 8.6 context 提供 request facts。
- [x] 8.7 context 提供 active window/runtime fact set。
- [x] 8.8 context 提供 locomotion/move intent facts。
- [x] 8.9 context 提供 active action variant。
- [x] 8.10 context 提供 request fact source tick、logic tick 或批准等价新鲜度字段。
- [x] 8.11 context 提供 fact resolver version 或批准等价 diagnostics 信息。
- [x] 8.12 确认 context 不持有 Unity object、InputAction、Animator、AnimancerState 或 GraphView。

## 9. Condition Evaluator
- [x] 9.1 实现 `Always`。
- [x] 9.2 实现 `RequestHeld`。
- [x] 9.3 实现 `RequestReleased`。
- [x] 9.4 实现 `RequiredFactActive`。
- [x] 9.5 实现 `TimelineComplete`。
- [x] 9.6 实现 `HasMoveIntent`。
- [x] 9.7 实现 `ActionVariantEquals`。
- [x] 9.8 实现 release tick 压制同 request kind held 判断的规则。
- [x] 9.9 实现 `TimelineComplete` 的 compiled duration tick 边界。
- [x] 9.10 确认 evaluator 不写黑板。
- [x] 9.11 确认 evaluator 不消费输入。
- [x] 9.12 确认 evaluator 不接受或拒绝 action request。
- [x] 9.13 确认 evaluator 不切换 active action。
- [x] 9.14 确认 evaluator 不执行 motion 或播放 animation。

## 10. Branch Selection 集成
- [x] 10.1 将 condition evaluator 接入 selector child 判断。
- [x] 10.2 保持 selector 按 runtime definition 稳定 child 顺序评估。
- [x] 10.3 确认第一个 condition 通过的 child 获胜。
- [x] 10.4 确认未选中 TimelineNode 不输出 motion、animation、window fact 或 cue。
- [x] 10.5 确认没有 child 通过时返回明确 diagnostics，不使用隐藏 fallback timeline。

## 11. Dodge / 现有动作迁移
- [x] 11.1 找到现有 Directional / Backstep 选择配置。
- [x] 11.2 将移动意图判断迁到 `HasMoveIntent` 或批准等价 condition。
- [x] 11.3 将 variant 判断迁到 `ActionVariantEquals` 或批准等价 condition。
- [x] 11.4 删除或停止正式使用 Dodge 专用 condition 字段。
- [x] 11.5 确认 Action.Dodge runtime 仍通过通用 branch definition 选择 timeline。

## 12. Editor Adapter
- [x] 12.1 Branch Editor adapter 支持读取 condition kind。
- [x] 12.2 Branch Editor adapter 支持写回 condition kind。
- [x] 12.3 Branch Editor adapter 支持读取 request kind payload。
- [x] 12.4 Branch Editor adapter 支持写回 request kind payload。
- [x] 12.5 Branch Editor adapter 支持读取 required fact id。
- [x] 12.6 Branch Editor adapter 支持写回 required fact id。
- [x] 12.7 Branch Editor adapter 支持读取 action variant payload。
- [x] 12.8 Branch Editor adapter 支持写回 action variant payload。
- [x] 12.9 Branch Editor 展示 validator diagnostics。
- [x] 12.10 保存后 `ToDefinition()` 或批准等价编译入口能看到 condition 修改。

## 13. 自动测试：Compiler / Validator
- [x] 13.1 添加 `Always` 编译测试。
- [x] 13.2 添加 `RequestHeld` 编译测试。
- [x] 13.3 添加 `RequestReleased` 编译测试。
- [x] 13.4 添加 `RequiredFactActive` 编译测试。
- [x] 13.5 添加 `TimelineComplete` 编译测试。
- [x] 13.6 添加 `HasMoveIntent` 编译测试。
- [x] 13.7 添加 `ActionVariantEquals` 编译测试。
- [x] 13.8 添加缺失 fact id 报错测试。
- [x] 13.9 添加空 fact id 报错测试。
- [x] 13.10 添加重复 fact id warning/error 测试。
- [x] 13.11 添加非法 payload 报错测试。
- [x] 13.12 添加共享 fact resolver 被 condition 和 policy matrix 复用的测试。
- [x] 13.13 添加冲突 fact declaration 报错测试。
- [x] 13.14 添加 fact resolver 不读取 runtime blackboard 或 scene object 的静态测试。

## 14. 自动测试：Evaluator / Selection
- [x] 14.1 添加 `Always` true 测试。
- [x] 14.2 添加 `RequestHeld` true/false 测试。
- [x] 14.3 添加 `RequestReleased` true/false 测试。
- [x] 14.4 添加 `RequiredFactActive` true/false 测试。
- [x] 14.5 添加 `TimelineComplete` true/false 边界 tick 测试。
- [x] 14.6 添加 `HasMoveIntent` true/false 测试。
- [x] 14.7 添加 `ActionVariantEquals` true/false 测试。
- [x] 14.8 添加 selector child 顺序测试。
- [x] 14.9 添加未选中 timeline 不输出测试。
- [x] 14.10 添加无 child 通过时 diagnostics 且无 fallback 测试。
- [x] 14.11 添加 release tick 上 `RequestHeld` 不抢占 `RequestReleased` 的测试。
- [x] 14.12 添加 released fact 不跨 tick 重复触发的测试。

## 15. 自动测试：Editor / Static Boundary
- [x] 15.1 添加 Branch Editor adapter condition kind 写回测试。
- [x] 15.2 添加 Branch Editor adapter required fact id 写回测试。
- [x] 15.3 添加 Branch Editor adapter request kind 写回测试。
- [x] 15.4 添加 Branch Editor adapter action variant 写回测试。
- [x] 15.5 添加静态边界测试，确认 runtime condition 不引用 UnityEditor。
- [x] 15.6 添加静态边界测试，确认 runtime condition 不引用 GraphView。
- [x] 15.7 添加静态边界测试，确认 runtime condition 不引用 Animator / Animancer。
- [x] 15.8 添加静态边界测试，确认 runtime condition 不引用 InputAction。
- [x] 15.9 添加静态边界测试，确认 runtime condition 不引用 MonoBehaviour / scene object。

## 16. 工具验证
- [x] 16.1 运行 `openspec validate formalize-action-condition-fact-framework --strict --no-interactive`。
- [x] 16.2 通过 Unity MCP 运行新增 condition/fact 定向 EditMode 测试。
- [x] 16.3 通过 Unity MCP 运行相关现有 Committed Action selection 测试。
- [x] 16.4 通过 Unity MCP 运行相关 authoring toolchain 测试。
