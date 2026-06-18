## 1. 前置对齐
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 读取 `openspec/specs/action-interrupt-policy-data/spec.md`。
- [x] 1.3 读取 `formalize-action-condition-fact-framework` 的 fact id 校验设计。
- [x] 1.4 读取 `add-config-only-action-golden-path` 的 TestCounter 目标链路。
- [x] 1.5 列出本次会修改的 policy data、compiler、validator、editor adapter、arbiter test 和 static test 文件。
- [x] 1.6 对将修改的核心符号运行 GitNexus impact，记录 direct callers、affected processes 和 risk。
- [x] 1.7 若 impact 为 HIGH 或 CRITICAL，先停下说明风险和拆分方案。

## 2. 现状盘点
- [x] 2.1 查找现有 Action interrupt policy authoring 数据源。
- [x] 2.2 查找现有 `ActionInterruptPolicy` runtime 数据结构。
- [x] 2.3 查找现有 Action interrupt policy compiler。
- [x] 2.4 查找现有 Action interrupt policy validator。
- [x] 2.5 查找 `ActionInterruptArbiter` 或批准等价仲裁入口。
- [x] 2.6 查找现有 policy Inspector / Editor 入口。
- [x] 2.7 查找现有 elapsed time timing rule 使用点。
- [x] 2.8 记录现有 policy 是否已经支持 required fact id、priority、force、resistance。

## 3. Matrix Row Authoring 数据
- [x] 3.1 定义 matrix row authoring 数据结构。
- [x] 3.2 row 支持 from action id。
- [x] 3.3 row 支持 to action id。
- [x] 3.4 row 支持 request kind。
- [x] 3.5 row 支持 required fact id。
- [x] 3.6 row 支持 min priority。
- [x] 3.7 row 支持 force。
- [x] 3.8 row 支持 resistance rule 或与现有 resistance 输入的映射说明。
- [x] 3.9 row 支持 diagnostics label 或批准等价调试字段，且该字段不参与 runtime 判断。
- [x] 3.10 确认 row 不包含 window start/end timing 字段。
- [x] 3.11 确认 row 不引用 Branch TimelineNode 作为 to action。
- [x] 3.12 确认 row authoring scope 只接受 `Action.*` 或批准等价 action id。
- [x] 3.13 确认 row authoring 不接受 Locomotion state、TurnBack state、GraphView node 或 editor lane 作为 from/to。

## 4. Runtime Policy 映射
- [x] 4.1 确认 matrix row 编译目标是现有 `ActionInterruptPolicy`、状态请求策略 runtime policy 或批准等价类型。
- [x] 4.2 映射 from action id。
- [x] 4.3 映射 to action id。
- [x] 4.4 映射 request kind。
- [x] 4.5 映射 required fact id。
- [x] 4.6 映射 min priority。
- [x] 4.7 映射 force。
- [x] 4.8 映射 resistance rule。
- [x] 4.9 保持 row 顺序稳定。
- [x] 4.10 确认 runtime policy 不保存 GraphView、EditorWindow、AnimationClip、Animator、Animancer、Transform、CharacterController 或 MonoBehaviour。
- [x] 4.11 若底层 runtime policy 字段名仍为 state id，确认 compiler 映射不扩大 Matrix 作者视图 scope。
- [x] 4.12 确认 resistance rule 只映射现有 resistance 语义，不新增第二套 resistance 权威。

## 5. Validator
- [x] 5.1 校验 from action id 非空。
- [x] 5.2 校验 to action id 非空。
- [x] 5.3 校验 request kind 非空。
- [x] 5.4 校验 from/to 都是 Action ID 或批准等价 action id。
- [x] 5.5 校验 Locomotion / TurnBack state 不能作为本 Matrix row 的 from/to。
- [x] 5.6 校验 Branch TimelineNode 不能作为跨 Action target。
- [x] 5.7 校验 min priority 非负。
- [x] 5.8 校验 required fact id 通过共享 fact resolver 存在。
- [x] 5.9 校验 required fact id 不通过前缀猜测解析。
- [x] 5.10 校验重复 row 并输出 warning 或 error。
- [x] 5.11 校验 row 不定义 window start/end timing。
- [x] 5.12 校验 row 不把 Branch Graph edge 作为正式 runtime 数据。

## 6. Compiler
- [x] 6.1 新增或扩展 matrix row 到 runtime policy 的 compiler。
- [x] 6.2 compiler 收集 validator diagnostics。
- [x] 6.3 compiler 在 error 存在时不输出可被正式 runtime 消费的半成品 policy。
- [x] 6.4 compiler 保持多 row 配置顺序。
- [x] 6.5 compiler 不调用 `ActionInterruptArbiter`。
- [x] 6.6 compiler 不调用 Action lifecycle。
- [x] 6.7 compiler 不调用 motion executor。
- [x] 6.8 compiler 不调用 animation presenter。
- [x] 6.9 compiler 不写 runtime blackboard。

## 7. Arbiter 消费
- [x] 7.1 确认 `ActionInterruptArbiter` 可读取 matrix 编译后的 runtime policy。
- [x] 7.2 active required fact 存在且 priority 满足时返回 accepted。
- [x] 7.3 required fact 缺失时返回 rejected 或明确 diagnostics。
- [x] 7.4 priority 不足时返回 rejected。
- [x] 7.5 resistance 阻挡时返回 rejected。
- [x] 7.6 force 允许时按现有 force 语义覆盖 resistance。
- [x] 7.7 仲裁结果只交给 Action lifecycle，不直接切换 Branch。

## 8. Editor Adapter
- [x] 8.1 新增或扩展 Editor-only matrix adapter。
- [x] 8.2 adapter 支持读取 row 列表。
- [x] 8.3 adapter 支持新增 row。
- [x] 8.4 adapter 支持删除 row。
- [x] 8.5 adapter 支持重排 row。
- [x] 8.6 adapter 支持编辑 from action id。
- [x] 8.7 adapter 支持编辑 to action id。
- [x] 8.8 adapter 支持编辑 request kind。
- [x] 8.9 adapter 支持编辑 required fact id。
- [x] 8.10 adapter 支持编辑 min priority。
- [x] 8.11 adapter 支持编辑 force。
- [x] 8.12 adapter 支持编辑 resistance rule。
- [x] 8.13 adapter 展示 validator diagnostics。
- [x] 8.14 保存后能立即编译为 runtime policy。

## 9. Branch 边界确认
- [x] 9.1 搜索 Branch runtime definition，确认不存在跨 Action target edge 字段。
- [x] 9.2 搜索 Branch authoring definition，确认不存在 to action id 字段。
- [x] 9.3 确认 Branch condition 命中 required fact 时只影响当前 action 内部 TimelineNode selection。
- [x] 9.4 确认 Branch evaluator 不创建新的 Action lifecycle state。
- [x] 9.5 若发现旧跨 Action branch 边或等价残留，删除或迁移到 policy matrix，不保留兼容路径。

## 10. 自动测试：Compiler / Validator
- [x] 10.1 添加单 row 编译测试。
- [x] 10.2 添加多 row 顺序保持测试。
- [x] 10.3 添加 from action id 为空报错测试。
- [x] 10.4 添加 to action id 为空报错测试。
- [x] 10.5 添加 request kind 为空报错测试。
- [x] 10.6 添加 min priority 负数报错测试。
- [x] 10.7 添加 required fact id 缺失报错测试。
- [x] 10.8 添加 required fact id 前缀猜测不通过测试。
- [x] 10.9 添加重复 row warning/error 测试。
- [x] 10.10 添加禁止 window start/end timing 测试。
- [x] 10.11 添加 Locomotion target 被 Matrix validator 拒绝的测试。
- [x] 10.12 添加 Branch TimelineNode target 被 Matrix validator 拒绝的测试。
- [x] 10.13 添加共享 fact resolver 与 condition validator 一致的测试。
- [x] 10.14 添加 bottom runtime policy state-id 字段不扩大 authoring scope 的测试。

## 11. 自动测试：Arbiter / Runtime Boundary
- [x] 11.1 添加 required fact active 时 accepted 测试。
- [x] 11.2 添加 required fact missing 时 rejected 测试。
- [x] 11.3 添加 priority 不足 rejected 测试。
- [x] 11.4 添加 resistance 阻挡 rejected 测试。
- [x] 11.5 添加 force 语义测试。
- [x] 11.6 添加仲裁结果交给 lifecycle 的集成测试或批准等价边界测试。
- [x] 11.7 添加 Branch 不直接跨 Action 跳转的结构测试。

## 12. 自动测试：Editor / Static Boundary
- [x] 12.1 添加 matrix adapter 新增 row 写回测试。
- [x] 12.2 添加 matrix adapter 删除 row 写回测试。
- [x] 12.3 添加 matrix adapter 重排 row 写回测试。
- [x] 12.4 添加 matrix adapter 编辑字段写回测试。
- [x] 12.5 添加静态边界测试，确认 matrix compiler/editor 不调用 Action lifecycle。
- [x] 12.6 添加静态边界测试，确认 matrix compiler/editor 不调用 motion executor。
- [x] 12.7 添加静态边界测试，确认 matrix compiler/editor 不调用 animation presenter。
- [x] 12.8 添加静态边界测试，确认 matrix compiler/editor 不写 runtime blackboard。
- [x] 12.9 添加静态边界测试，确认 matrix compiler/editor 不创建角色帧入口或 runner。

## 13. 工具验证
- [x] 13.1 运行 `openspec validate formalize-action-transition-policy-matrix --strict --no-interactive`。
- [x] 13.2 通过 Unity MCP 运行新增 Action transition policy matrix 定向 EditMode 测试。
- [x] 13.3 通过 Unity MCP 运行相关现有 action interrupt policy 测试。
- [x] 13.4 通过 Unity MCP 运行相关 action interrupt arbiter 测试。
