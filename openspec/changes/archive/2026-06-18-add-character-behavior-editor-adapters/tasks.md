## 0. 范围确认
- [x] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [x] 0.2 确认 runtime behavior submission entry 和 compiled runtime definition 已有稳定定义。
- [x] 0.3 确认本变更只写 Editor-only adapter 和 compiler。

## 1. Authoring Definition
- [x] 1.1 定义 behavior tree authoring asset 或复用批准的 definition。
- [x] 1.2 定义 stable node id / port id / clip id。
- [x] 1.3 定义 schema version。
- [x] 1.4 定义 Locomotion leaf / Committed Action leaf 的 authoring 表达。
- [x] 1.5 明确 authoring asset 不保存 `CharacterFramePipeline` phase 顺序或旧 submitter chain。
- [x] 1.6 增加 authoring asset 纯数据校验测试。

## 2. Compiler
- [x] 2.1 新增 behavior graph compiler。
- [x] 2.2 校验单父、无循环、端口兼容。
- [x] 2.3 编译 root / parallel / leaf。
- [x] 2.4 编译 Action selector / condition / timeline 引用。
- [x] 2.5 编译 Locomotion leaf 到只提交 behavior submission 的 runtime source。
- [x] 2.6 增加 compiler 成功和失败测试。

## 3. GraphView Adapter
- [x] 3.1 盘点 Ref/wly970123 TreeDesigner / GraphView 可移植的窗口、view、manipulator、UXML、USS 和图标资源。
- [x] 3.2 新增 `Assets/Editor/Character/Graph` 下的最小窗口。
- [x] 3.3 显示 root / parallel / leaf 节点。
- [x] 3.4 支持节点稳定 id。
- [x] 3.5 支持边连接保存到本项目 authoring definition。
- [x] 3.6 确认 UI 不暴露 `CharacterFramePipeline` phase 顺序编辑入口。
- [x] 3.7 增加 editor assembly 不进入 runtime 的静态测试。

## 4. Timeline Adapter
- [x] 4.1 盘点 Ref/wly970123 Timeline Editor 可移植的窗口、track view、clip view、frame ruler、UXML、USS、图标和 handle 交互。
- [x] 4.2 新增 `Assets/Editor/Character/Action/Timeline` 下的最小 timeline view。
- [x] 4.3 显示 frame ruler。
- [x] 4.4 显示 track / clip。
- [x] 4.5 支持编辑 AnimationKey / Motion / HitboxWindow / CancelWindow / Cue clip。
- [x] 4.6 增加 timeline authoring 到 runtime definition 的转换测试。

## 5. Ref Import 边界
- [x] 5.1 将移植后的 Ref UI 代码改接本项目 authoring definition、serializer、compiler 和 diagnostics。
- [x] 5.2 新增 Editor-only importer 或 adapter。
- [x] 5.3 禁止正式 runtime 引用 Taco runtime runner。
- [x] 5.4 增加静态测试确认 runtime 不引用 `TreeRunner`、`RunnableTree`、`TimelinePlayer`、PlayableGraph 或 GraphView。

## 6. Dodge 正式配置接入
- [x] 6.1 Timeline editor 默认打开正式 `CorinDodgeActionDefinition.asset`，不再打开废弃 Behavior sample。
- [x] 6.2 正式 Dodge ActionDefinition 包含 Directional / Backstep selector timeline。
- [x] 6.3 正式 Dodge timeline 包含 Animation / Motion / HitboxWindow / CancelWindow / Cue track。
- [x] 6.4 移除 sample compiled runtime definition 路径，并增加正式配置验证测试。

## 7. 通用性声明边界
- [x] 7.1 确认文档、菜单、窗口标题和测试命名不把本阶段称为通用 Skill Editor。
- [x] 7.2 增加静态测试确认 Dodge 示例不作为通用技能编辑器证明。
- [x] 7.3 记录后续通用性验证需要 Block / PerfectBlock、Attack / HitResolve 或等价交互型能力金线。

## 8. 验证
- [x] 8.1 运行相关 Editor / compiler EditMode 测试。
- [x] 8.2 运行静态边界测试。
- [x] 8.3 增加静态测试确认 Locomotion editor leaf 不直接调用 motion executor、animation presenter 或 blackboard writer。
- [x] 8.4 运行 `openspec validate add-character-behavior-editor-adapters --strict --no-interactive`。
