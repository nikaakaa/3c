# Design: Character Definition Inspector 产物状态分层

## Context

Definition Inspector 同时面对两类需求：作者需要随时选中资产而不卡顿；产物发布又需要准确判断 authoring source 是否已经变化。完整 stale 检查会沿 Definition 计算 SourceRevision、解码 Program 并重算 Projection/Target expectation，它适合显式检查和发布边界，不适合 Unity IMGUI 每帧执行。

## Status Model

Inspector 使用以下状态：

- `Missing`：Program 或 Projection 引用缺失。
- `Invalid`：轻量发布 Header 缺少正式 compiler/version/revision/hash 元数据。
- `Unchecked`：轻量 Header 完整，但本次 Inspector 生命周期尚未比较当前 authoring source。
- `Needs Compile`：作者通过当前 Inspector 修改了 Definition 字段。
- `Ready`：显式 Refresh 得到非 stale，或 Compile 已成功完成。
- `Stale`：显式 Refresh 得到 stale。

状态只保存在当前 Inspector 实例，不写回 Definition，不形成第二份 authoring 数据。重新选中或 Domain Reload 后重新执行轻量 Header 检查，已发布产物回到 `Unchecked`。

## Decisions

### Decision: 轻量检查和精确检查必须是两个 API

轻量 Header 检查只读取 Definition 的 Program/Projection 引用和已经序列化的 compiler、version、revision、hash 字段。它不得计算 ProgramId、SourceRevision、ProjectionRevision，不得调用 `ProgramAsset.Load`，也不得访问 authoring dependency graph。

精确检查继续由 `IsStale` 完成。Inspector 只能从 `Refresh Status` 显式命令调用它；Build、发布和产品运行准备仍按各自正式边界调用它。

### Decision: 默认展示 Unchecked，而不是自动计算 Ready/Stale

选择 `Unchecked` 的业务收益是作者切换角色配置、Timeline 与动作资产时保持即时响应，同时界面不会把“未检查”伪装成“Ready”。代价是作者想确认产物是否最新时需要多点一次按钮。

不选择“选中后自动延迟计算”，因为计算仍在 Unity 主线程执行，只是把卡顿推迟到选中后。也不选择“每个依赖变化自动维护精确缓存”，因为这需要完整反向依赖索引和持久化失效协议，复杂度远高于当前求职 Demo 的作者体验收益。

### Decision: 缓存属于 Inspector 会话，不持久化

会话缓存实现简单，并保证 Domain Reload、代码变更或重新选中后不会继续展示旧的精确结论。代价是 `Ready/Stale` 不跨选择保存；界面通过 `Unchecked` 明确表达这一点。

不把缓存写进 Definition 或 Generated Artifact，因为 UI 检查状态不是业务资产，不应改变 SourceRevision、Undo 或版本控制内容。

## State Transitions

```text
OnEnable -> Missing | Invalid | Unchecked
Edit     -> Needs Compile
Refresh  -> Missing | Invalid | Ready | Stale
Compile success -> Ready
Compile failure -> Needs Compile 或当前轻量状态
```

`OnInspectorGUI` 只绘制当前状态。任何 Layout、Repaint、foldout 或普通选中事件都不得触发 `Refresh` 状态迁移。

## Risks

- 外部工具在 Inspector 保持打开时修改依赖，当前 `Ready` 不会自动变成 `Stale`。状态代表“上次显式检查结果”，作者可通过 `Refresh Status` 获取最新结论；正式 Build/运行准备仍执行自己的严格检查。
- 轻量 Header 只能发现发布元数据结构问题，不能证明源码一致。这正是 `Unchecked` 与 `Ready` 分离的原因。
