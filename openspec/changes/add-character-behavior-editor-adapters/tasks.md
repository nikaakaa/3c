## 0. 范围确认
- [ ] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [ ] 0.2 确认 runtime behavior submission entry 和 compiled runtime definition 已有稳定定义。
- [ ] 0.3 确认本变更只写 Editor-only adapter 和 compiler。

## 1. Authoring Definition
- [ ] 1.1 定义 behavior tree authoring asset 或复用批准的 definition。
- [ ] 1.2 定义 stable node id / port id / clip id。
- [ ] 1.3 定义 schema version。
- [ ] 1.4 增加 authoring asset 纯数据校验测试。

## 2. Compiler
- [ ] 2.1 新增 behavior graph compiler。
- [ ] 2.2 校验单父、无循环、端口兼容。
- [ ] 2.3 编译 root / parallel / leaf。
- [ ] 2.4 编译 Action selector / condition / timeline 引用。
- [ ] 2.5 增加 compiler 成功和失败测试。

## 3. GraphView Adapter
- [ ] 3.1 盘点 Ref/wly970123 TreeDesigner / GraphView 可移植的窗口、view、manipulator、UXML、USS 和图标资源。
- [ ] 3.2 新增 `Assets/Editor/Character/Graph` 下的最小窗口。
- [ ] 3.3 显示 root / parallel / leaf 节点。
- [ ] 3.4 支持节点稳定 id。
- [ ] 3.5 支持边连接保存到本项目 authoring definition。
- [ ] 3.6 增加 editor assembly 不进入 runtime 的静态测试。

## 4. Timeline Adapter
- [ ] 4.1 盘点 Ref/wly970123 Timeline Editor 可移植的窗口、track view、clip view、frame ruler、UXML、USS、图标和 handle 交互。
- [ ] 4.2 新增 `Assets/Editor/Character/Action/Timeline` 下的最小 timeline view。
- [ ] 4.3 显示 frame ruler。
- [ ] 4.4 显示 track / clip。
- [ ] 4.5 支持编辑 AnimationKey / Motion / HitboxWindow / CancelWindow / Cue clip。
- [ ] 4.6 增加 timeline authoring 到 runtime definition 的转换测试。

## 5. Ref Import 边界
- [ ] 5.1 将移植后的 Ref UI 代码改接本项目 authoring definition、serializer、compiler 和 diagnostics。
- [ ] 5.2 新增 Editor-only importer 或 adapter。
- [ ] 5.3 禁止正式 runtime 引用 Taco runtime runner。
- [ ] 5.4 增加静态测试确认 runtime 不引用 `TreeRunner`、`RunnableTree`、`TimelinePlayer`、PlayableGraph 或 GraphView。

## 6. Dodge 示例资产
- [ ] 6.1 新增最小 Dodge selector + timeline editor sample。
- [ ] 6.2 编译 sample 到 runtime behavior tree。
- [ ] 6.3 验证 sample 不绕过 BehaviorSubmission / CharacterFramePipeline。
- [ ] 6.4 增加 sample compiler 测试。

## 7. 验证
- [ ] 7.1 运行相关 Editor / compiler EditMode 测试。
- [ ] 7.2 运行静态边界测试。
- [ ] 7.3 运行 `openspec validate add-character-behavior-editor-adapters --strict --no-interactive`。
