## 1. 前置对齐
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 确认 `formalize-committed-action-authoring-toolchain` 的通用 branch authoring 已可用。
- [x] 1.3 确认 `formalize-action-condition-fact-framework` 的 condition/fact 编译、validator 和 evaluator 已可用。
- [x] 1.4 确认 `formalize-action-transition-policy-matrix` 的跨 Action policy 编译、validator 和 arbiter 消费已可用。
- [x] 1.5 读取 `openspec/specs/action-domain-runtime/spec.md`，确认 Action domain 的 request / lifecycle / claim / output 边界。
- [x] 1.6 列出本次会修改或新增的 test fixture、test helper、action definition fixture、policy fixture 和 static boundary test 文件。
- [x] 1.7 对将修改的核心符号运行 GitNexus impact，记录 direct callers、affected processes 和 risk。
- [x] 1.8 若 impact 为 HIGH 或 CRITICAL，先停下说明风险和拆分方案。

## 2. 现状盘点
- [x] 2.1 查找 Action Catalog 测试装配方式。
- [x] 2.2 查找 Action request provider 或测试 request fixture 的现有入口。
- [x] 2.3 查找 Action lifecycle 推进测试。
- [x] 2.4 查找 Branch selection 测试。
- [x] 2.5 查找 Timeline outcome / fact 输出测试。
- [x] 2.6 查找 Action interrupt policy / arbiter 测试。
- [x] 2.7 查找 CharacterFramePipeline integration test 或可复用 harness。
- [x] 2.8 查找 fake motion executor、fake animation presenter、fake output port 或批准等价测试替身。
- [x] 2.9 记录是否已有 sample asset / Resources fallback；若有，确认本 change 不复用为正式路径。

## 3. Test Action ID 与 Fixture 命名
- [x] 3.1 定义 `Action.TestHold` 测试 action id。
- [x] 3.2 定义 `Action.TestCounter` 测试 action id。
- [x] 3.3 定义 TestHold request kind 或使用批准等价现有测试 request。
- [x] 3.4 定义 TestCounter request kind 或使用批准等价现有测试 request。
- [x] 3.5 定义 `window.test.counter.open` fact id。
- [x] 3.6 定义 `Action.TestHold.Start` animation key。
- [x] 3.7 定义 `Action.TestHold.Loop` animation key。
- [x] 3.8 定义 `Action.TestHold.End` animation key。
- [x] 3.9 定义 `Action.TestCounter.Main` animation key。
- [x] 3.10 确认所有 TestAction fixture 不进入正式 Corin gameplay 配置。

## 4. TestHold Action Definition Fixture
- [x] 4.1 构造 `Action.TestHold` action definition。
- [x] 4.2 配置 TestHold request binding 或批准等价测试 provider。
- [x] 4.3 配置 TestHold FullBody claim 或批准等价测试 claim。
- [x] 4.4 配置 Start TimelineNode。
- [x] 4.5 Start TimelineNode 输出 `Action.TestHold.Start` animation key。
- [x] 4.6 配置 Loop TimelineNode。
- [x] 4.7 Loop TimelineNode 输出 `Action.TestHold.Loop` animation key。
- [x] 4.8 Loop TimelineNode 在指定 tick 输出 `window.test.counter.open`。
- [x] 4.9 配置 End TimelineNode。
- [x] 4.10 End TimelineNode 输出 `Action.TestHold.End` animation key。
- [x] 4.11 配置 Start -> Loop 的 `TimelineComplete` condition。
- [x] 4.12 配置 Loop -> Loop 的 `RequestHeld` condition。
- [x] 4.13 配置 Loop -> End 的 `RequestReleased` condition。
- [x] 4.14 配置 End timeline complete 后由正式 Action lifecycle completion 退出，不新增 Branch Exit 节点。
- [x] 4.15 编译 TestHold definition 并确认无 validator error。
- [x] 4.16 确认 TestHold runtime branch / timeline / condition 均来自正式 compiler 输出。
- [x] 4.17 确认 TestHold fixture 不直接 new test-only runtime definition 绕过 compiler。

## 5. TestCounter Action Definition Fixture
- [x] 5.1 构造 `Action.TestCounter` action definition。
- [x] 5.2 配置 TestCounter request binding 或批准等价测试 provider。
- [x] 5.3 配置 TestCounter FullBody claim 或批准等价测试 claim。
- [x] 5.4 配置 Counter TimelineNode。
- [x] 5.5 Counter TimelineNode 输出 `Action.TestCounter.Main` animation key。
- [x] 5.6 配置 Counter timeline complete 后由正式 Action lifecycle completion 退出或批准等价退出规则。
- [x] 5.7 编译 TestCounter definition 并确认无 validator error。
- [x] 5.8 确认 TestCounter runtime branch / timeline / condition 均来自正式 compiler 输出。
- [x] 5.9 确认 TestCounter fixture 不直接 new test-only runtime definition 绕过 compiler。

## 6. Transition Policy Fixture
- [x] 6.1 构造 matrix row：from `Action.TestHold`。
- [x] 6.2 配置 matrix row：to `Action.TestCounter`。
- [x] 6.3 配置 matrix row：request kind 为 TestCounter request 或批准等价请求。
- [x] 6.4 配置 matrix row：required fact id 为 `window.test.counter.open`。
- [x] 6.5 配置 matrix row：min priority 为测试用非负值。
- [x] 6.6 配置 matrix row：force 为 false。
- [x] 6.7 配置 matrix row：resistance 使用默认或批准等价测试规则。
- [x] 6.8 编译 policy fixture 并确认无 validator error。
- [x] 6.9 确认 TestHold Branch 不持有 TestCounter target。
- [x] 6.10 确认 policy fixture 使用正式 matrix row authoring 或批准等价 policy authoring。
- [x] 6.11 确认 policy runtime 来自正式 compiler 输出，不直接手搓 runtime policy。
- [x] 6.12 确认 policy required fact 通过共享 fact resolver 解析。

## 7. Catalog / Runtime Harness
- [x] 7.1 将 TestHold definition 注入测试用 Action Catalog。
- [x] 7.2 将 TestCounter definition 注入测试用 Action Catalog。
- [x] 7.3 将 transition policy fixture 注入测试用 Action runtime 配置。
- [x] 7.4 配置测试 request provider。
- [x] 7.5 配置 fake motion output port。
- [x] 7.6 配置 fake animation output port。
- [x] 7.7 配置可推进固定 tick 的 CharacterFramePipeline harness 或批准等价集成 harness。
- [x] 7.8 确认 harness 不使用 Unity batchmode。
- [x] 7.9 确认 harness 使用正式 Action Catalog / ActionDefinition 查找链路。
- [x] 7.10 确认 harness 不通过 Resources、sample asset fallback 或正式 Corin prefab 注入 TestAction。

## 8. 自动测试：TestHold Branch / Timeline
- [x] 8.1 添加 TestHold request accepted 后进入 Start 的测试。
- [x] 8.2 添加 Start timeline complete 后进入 Loop 的测试。
- [x] 8.3 添加 request held 时保持 Loop 的测试。
- [x] 8.4 添加 request released 时进入 End 的测试。
- [x] 8.5 添加 End timeline complete 后通过正式 Action lifecycle completion 退出 Action 的测试。
- [x] 8.6 添加 Loop 输出 `window.test.counter.open` 的测试。
- [x] 8.7 添加 window 未激活 tick 不输出 `window.test.counter.open` 的测试。
- [x] 8.8 添加 TestHold 输出 FullBody 或批准等价 claim 的测试。
- [x] 8.9 添加 TestHold 输出对应 animation key 的测试。
- [x] 8.10 添加 TestHold compiler error 时 golden path 失败的测试。
- [x] 8.11 添加 TestHold fixture 不手搓 runtime definition 的静态或结构测试。

## 9. 自动测试：TestCounter / Policy
- [x] 9.1 添加 TestCounter definition 编译测试。
- [x] 9.2 添加 TestHold active 且 `window.test.counter.open` active 时 TestCounter request accepted 的测试。
- [x] 9.3 添加 `window.test.counter.open` missing 时 TestCounter request rejected 的测试。
- [x] 9.4 添加 request priority 不足时 rejected 的测试。
- [x] 9.5 添加 resistance 阻挡时 rejected 的测试。
- [x] 9.6 添加 TestCounter accepted 后 lifecycle active action 变为 `Action.TestCounter` 的测试。
- [x] 9.7 添加 TestCounter 输出 `Action.TestCounter.Main` animation key 的测试。
- [x] 9.8 添加 TestHold Branch 不直接跳 TestCounter 的结构测试。
- [x] 9.9 添加 TestCounter policy runtime 来自 compiler 输出的测试。
- [x] 9.10 添加 TestCounter required fact 使用共享 fact resolver 的测试。

## 10. 自动测试：CharacterFramePipeline / Output
- [x] 10.1 添加 CharacterFramePipeline 推进 TestHold Start 的测试。
- [x] 10.2 添加 CharacterFramePipeline 推进 TestHold Loop 的测试。
- [x] 10.3 添加 CharacterFramePipeline 推进 TestHold End 的测试。
- [x] 10.4 添加 CharacterFramePipeline 从 TestHold 进入 TestCounter 的测试。
- [x] 10.5 添加 fake motion output port 收到计划输出的测试。
- [x] 10.6 添加 fake animation output port 收到 animation key 的测试。
- [x] 10.7 添加输出只经 OutputApplier 或批准等价角色级出口的测试。
- [x] 10.8 添加 Action runtime 不直接写 `CharacterRuntimeBlackboard` 的边界测试或批准等价静态测试。
- [x] 10.9 添加 FullBody claim 被采纳后 `BaseSlot` owner 为 Action-side owner 或批准等价 owner 的测试。
- [x] 10.10 添加 FullBody 不作为 slot owner 输出的测试。
- [x] 10.11 添加 `UpperBodySlotSuppressed` 或批准等价压制结果的测试。
- [x] 10.12 添加 golden path 测试不读取 `BaseLayerOwner` 或旧 layer 口径的静态测试。

## 11. 自动测试：Static Boundary
- [x] 11.1 添加静态测试，确认不存在 `PlayerTestActionController`。
- [x] 11.2 添加静态测试，确认不存在 TestAction 专用 MonoBehaviour gameplay 入口。
- [x] 11.3 添加静态测试，确认 `CharacterFramePipeline` 没有 TestHold/TestCounter 专用分支。
- [x] 11.4 添加静态测试，确认 Action motion resolver 没有 TestHold/TestCounter action id switch。
- [x] 11.5 添加静态测试，确认 animation presenter 没有 TestHold/TestCounter action id switch。
- [x] 11.6 添加静态测试，确认没有 TestAction 专用 motion executor。
- [x] 11.7 添加静态测试，确认没有 TestAction 专用 animation presenter。
- [x] 11.8 添加静态测试，确认 TestAction 不通过 Resources、sample asset fallback 或正式 Corin prefab 注入 runtime。
- [x] 11.9 添加静态测试，确认 TestAction 不通过 test-only runtime definition 绕过正式 compiler。
- [x] 11.10 添加静态测试，确认 TestAction 不进入正式 Corin Action Catalog 或正式 scene/prefab。

## 12. 故障处理边界
- [x] 12.1 如果 TestHold 需要新增 condition kind，停止并回到 `formalize-action-condition-fact-framework` 补 proposal 或任务。
- [x] 12.2 如果 TestCounter 需要 Branch 跨 Action 边，停止并回到 `formalize-action-transition-policy-matrix`。
- [x] 12.3 如果 pipeline 必须新增 TestAction 分支，停止并回到 Action domain runtime / authoring toolchain 设计。
- [x] 12.4 如果需要新增正式动画资源，拆出具体动作或动画 profile change，不混入 golden path。

## 13. 工具验证
- [x] 13.1 运行 `openspec validate add-config-only-action-golden-path --strict --no-interactive`。
- [x] 13.2 通过 Unity MCP 运行新增 TestHold / TestCounter 定向 EditMode 测试。
- [x] 13.3 通过 Unity MCP 运行相关 Action authoring 测试。
- [x] 13.4 通过 Unity MCP 运行相关 condition/fact 测试。
- [x] 13.5 通过 Unity MCP 运行相关 transition policy 测试。
- [x] 13.6 通过 Unity MCP 运行相关 CharacterFramePipeline 测试。
