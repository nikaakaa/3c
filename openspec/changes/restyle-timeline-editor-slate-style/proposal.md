## Why

用户要求真实 Slate Timeline UI，且打开窗口不得创建临时 Slate GameObject/Cutscene/Group/Track/ActionClip 组件树。当前 BuildProjection 仍创建隐藏宿主和组件，Surface 及 Curve/DopeSheet 直接依赖 Cutscene.Validate、Transform、IKeyable/runtime root。隐藏对象不是解耦；这是先前适配实现的选择错误，不是 Timeline 的业务依赖。

本次将现有 Slate Editor 绘制/交互代码的输入改为纯内存 Editor Model/adapter。TimelineData 继续是唯一持久化 Timeline 业务模型，正式创建、字段、曲线、Mutation/Undo 不变。已有右侧 Inspector、布局、帧单位、选择、曲线交互和 Camera Track 保留，不重新仿制 UI。

## What Changes

- 将 CutsceneEditorSurface 改为普通可释放的 SlateTimelineEditorSurface，Bind 接收纯内存读视图、编辑命令 port 和 UI host，不接受 Cutscene、Transform 或 ScenePlay。
- 在现有 Timeline adapter 中生成轻量行视图、稳定 ID、曲线 descriptor 和当前手势草稿；不继承 MonoBehaviour/ScriptableObject，不保存或导出 Editor Model，不复制 Clip 业务配置。
- 迁移 Surface 的选择/拖动/裁剪/排序/复制/Section/快捷键/Undo/生命周期，以及 CurveEditor/DopeSheet/参数列表的组件输入；保留 Slate 的实际绘制、命中、关键帧和切线算法。
- 正式新增仍调用 TimelineData.AddTrack/AddClip/AddSection 和共同强类型配置；手势只修改内存草稿，提交仍走既有 TimelineEditorSessionContext/Mutation/正式 owner Undo。
- 删除 __BTSMTL_SlateTimelineProjection__、BtsmtlSlateGroup/Track/ActionClip 组件类、groupsRoot/Transform/组件扫描/Cutscene.Validate、proxy Undo 与销毁路径，重命名 adapter 并迁移调用者；不保留隐藏场景、代用组件或 fallback。
- 将全局组件 selection/current、runtime root.currentTime 和曲线永久回调迁到窗口局部 state/selection、帧映射、可释放缓存与实例命令。
- Timeline 无 ScenePlay、Actor、Director 或插件运行内核时完整可编辑；现有预览联动只作为外部可选观察/导航，保留正式 Session/adoption。
- 仍使用 Slate 自身 Cutscene 窗口的真实消费者如存在，只在插件边界读取真实 Cutscene，复用同一个纯内存 Surface；BTSMTL 不创建替代 Cutscene，不维护第二套 UI。
- r2 作者合同保留：只有公共 export_code 显式从正式资产完整写 C#，generate_assets 显式执行已编译入口、保存明确范围和根挂接。人工编辑不写源码，编译不生成资产，两工具不自动 Build/Play。
- 本任务维护 projection/Editor adapter；公共 TimelineAuthoringClipBinding.cs 与代码输出/生成仍由 C# authoring 任务负责，保留已经完成的强类型接线，不恢复 JSON/五工具或第二事务。
- tasks 第11节单独记录本次尚未实施的纯内存迁移，原组件投影完成记录只作为历史，不把文档改写列为代码完成。

## Capabilities

### New Capabilities

- Slate Editor 的纯内存 Timeline 内容/曲线输入与正式 BTSMTL adapter。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`：实际 Slate UI 改为无组件输入，编辑 Session/Undo 保留，ScenePlay 为可选外部观察。
- `btsmtl-timeline-animation-authoring-surface`：本地作者能力不依赖场景的现有合同由纯内存实现满足；本轮不直接安装未完成实现到 current specs。

## Impact

- 主要修改位置：Slate Editor Surface、Clip交互、CurveEditor、DopeSheetEditor、参数绘制/缓存，以及 BTSMTL TimelineEditorWindow、Editor adapter 与其调用者。
- 删除对象仅为错误 UI 宿主、组件代理和相关无消费者 Editor 路径，不删除真实 Timeline/资源/生成内容，不修改技能、相机、Pose 或运行内核。
- Shared UI 的输入和程序集单向依赖须迁移，不能只改 BuildProjection；大于原“最小 transaction hook”的改动范围，是本次明确允许的 Editor 解耦。
- 单窗口、现有右侧 Inspector、帧几何和实际作者交互保持。preview-integration-plan.md 更新输入边界，原运行控制任务不重做。
- 与旧 design/spec 的“HideAndDontSave 兼容对象”“Cutscene.Validate 发现 Clip”“必须 AnimatedParameter 组件参数”冲突，已在本 change 替换。current 独立作者合同保留；current 旧 PreviewSession 条款由原场景预览 change 的 delta 退役，不以旧规范恢复临时组件树。
- 本轮只修改本任务规划文档，不修改业务代码/资产或其它任务文档，不新增测试和验证任务。迁移顺序、API 职责和用户行为标准见 design.md。
