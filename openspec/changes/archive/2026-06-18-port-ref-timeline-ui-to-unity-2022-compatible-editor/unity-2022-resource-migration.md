# Unity 2022 资源迁移规则

## UXML

- 只保留 Unity 2022 可加载的 UXML。
- 不保留 `project://database/Assets/Addon/Taco`、`Taco.Editor.SplitView` 或 Ref 工程绝对资源引用。
- 不能稳定导入的 Ref UXML 改为程序化 UI，并由本项目 USS 继续承接视觉。
- 所有资源放在 `Assets/Editor/Character/Action/Timeline/RefPortedResources/` 下，保持 Editor-only。

## USS

- 类名改为本项目 `committed-action-*` 语义。
- 不引用 Ref 工程路径、Taco 自定义控件或 Unity 2023-only 样式入口。
- 每次新增样式后用静态检查保证没有 Ref `project://database` 残留。

## Meta

- 不复制 Ref `.meta`。
- 新资源由 Unity 2022 在本项目内生成 GUID。
- 已确认不再使用的高风险 UXML 和 `.meta` 直接删除，避免 Unity 启动时加载失败。

## 验证

- 静态测试检查 editor 资源不含 Ref Taco 路径。
- 静态测试检查高风险 TrackHandle / TrackView UXML 不再存在，C# 不再引用对应 path。
- Unity Editor 需能重新编译并打开 `Committed Action Timeline Editor`。

