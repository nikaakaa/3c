## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`。
- [x] 1.2 读取本变更 `design.md`。
- [x] 1.3 读取本变更 `tasks.md`。
- [x] 1.4 确认本变更显式依赖 `formalize-character-frame-arbitration-contract`，且本次不跨 change 完成全部前置实现。
- [x] 1.5 读取 `character-frame-pipeline` 当前 spec。
- [x] 1.6 读取 `character-runtime-ports` 当前 spec。
- [x] 1.7 读取 `fullbody-action-framework` 当前 spec。
- [x] 1.8 搜索 `FullBodySubmissionBuilder`、`ICharacterFrameRuntimePort`、`CharacterFrameSubmissionSource.FullBody`、`FramePipelineHost` 的现有调用点。
- [x] 1.9 对 `ICharacterFrameRuntimePort`、`CharacterFramePipeline`、`CharacterFramePipelineHost`、`FullBodySubmissionBuilder`、`PlayerFullBodyActionController`、`CharacterFrameSubmission`、`FullBodyRuntimePortAdapter`、`CharacterFrameOutput`、`CharacterFrameResult` 和 `CharacterFrameOutputComposer` 执行 GitNexus impact analysis。
- [x] 1.10 确认本次 impact analysis 未返回 HIGH 或 CRITICAL 风险。

## 2. 测试先行
- [x] 2.1 增加静态测试：`ICharacterFrameRuntimePort` 不得继承 FullBody runtime ports。
- [x] 2.2 增加静态测试：`PlayerFullBodyActionController` 不得作为正式路径直接创建 `CharacterFramePipelineHost`。
- [x] 2.3 增加静态测试：FullBody integrated submitter 只能作为 legacy adapter 实现 request/output submitter interfaces。
- [x] 2.4 增加静态测试：`CharacterFrameSubmissionSource.FullBody` 不得作为正式 output authority 判断。
- [x] 2.5 增加 EditMode 覆盖：`CharacterFramePlan` path 能表达 Locomotion-only 输出。
- [x] 2.6 增加 EditMode 覆盖：`CharacterFramePlan` path 能表达 FullBody claim 压制 Locomotion 输出。
- [x] 2.7 保留 rollback/pipeline characterization tests，证明 legacy integrated adapter 迁移后 Corin 当前 Dodge/Locomotion 场景行为等价。

## 3. Runtime Interface 降级
- [x] 3.1 新增 `ICharacterFrameSubmissionRuntimePort`，承载角色帧 request/output 构建所需事实读取面。
- [x] 3.2 新增 `ICharacterFrameOutputRuntimePort`，承载角色帧 output apply 所需执行面。
- [x] 3.3 将 `ICharacterFrameRuntimePort` 改为继承角色级 submission/output runtime ports，而不是继承 FullBody ports。
- [x] 3.4 将 `IFullBodySubmissionRuntimePort` 保留为 FullBody adapter 侧领域 port，并让它继承角色级 submission runtime port。
- [x] 3.5 将 `IFullBodyOutputRuntimePort` 保留为 FullBody adapter 侧领域 port，并让它继承角色级 output runtime port。
- [x] 3.6 迁移 `CharacterFramePipeline` 和 output applier 的调用点到角色级 output runtime Interface。

## 4. Integrated Submitter 降级
- [x] 4.1 新增 `FullBodyIntegratedFrameAdapter`，作为迁移期 integrated adapter 实现 `ICharacterFrameRequestSubmitter` 和 `ICharacterFrameOutputSubmitter`。
- [x] 4.2 移除 `FullBodySubmissionBuilder` 直接实现角色级 submitter interfaces 的正式身份。
- [x] 4.3 将 `FullBodySubmissionBuilder` 收窄为 integrated adapter 内部使用的构建 Implementation。
- [x] 4.4 将 `FullBodySubmissionBuilder` 的 runtime 输入改为角色级 `ICharacterFrameSubmissionRuntimePort`。
- [x] 4.5 保留后续 Locomotion submitter / FullBody Action submitter 拆分为独立 change，不在本 change 偷做未审批的新 runtime path。

## 5. Host Ownership 降级
- [x] 5.1 引入 `CharacterFrameRuntimeHost` 作为 Character-level runtime host。
- [x] 5.2 让 `PlayerFullBodyActionController.Tick` 通过 `CharacterFrameRuntimeHost` 推进。
- [x] 5.3 移除 `PlayerFullBodyActionController` 直接创建正式 `CharacterFramePipelineHost` 的职责。
- [x] 5.4 保留 `PlayerFullBodyActionController` 的 Unity 引用装配、配置解析和诊断 view 职责。
- [x] 5.5 确认没有新增第二 runner、第二 pipeline、第二 motion executor 或第二 Presenter。

## 6. Submission / Plan 收敛
- [x] 6.1 将 output composer 的正式入口收敛为先创建 `CharacterFramePlan`，再用 plan 生成 output。
- [x] 6.2 将 `CharacterFrameSubmissionSource.FullBody` 重命名为 `LegacyFullBodyIntegrated`。
- [x] 6.3 将单 submission composer overload 标记为 legacy adapter，并让正式 pipeline path 不再调用该 overload。
- [x] 6.4 确认 output applier 只消费最终 output/plan 结果，不重新做身体仲裁。
- [x] 6.5 确认 frame model 不持有 Unity scene object、Animancer runtime object 或 fallback 配置。

## 7. 规格和验证
- [x] 7.1 确认 `character-frame-pipeline` spec delta 覆盖单一 FullBody source 退役。
- [x] 7.2 确认 `character-runtime-ports` spec delta 覆盖 Character runtime port 去 FullBody 化。
- [x] 7.3 确认 `fullbody-action-framework` spec delta 覆盖 FullBody 主调度入口和 Locomotion 子职责退役。
- [x] 7.4 运行相关 Unity EditMode 定向测试。
- [x] 7.5 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore`。
- [x] 7.6 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 7.7 运行 `openspec validate retire-fullbody-integrated-frame-paths --strict --no-interactive`。
- [x] 7.8 运行 GitNexus `detect_changes()`。
- [x] 7.9 不运行 Unity batchmode。
