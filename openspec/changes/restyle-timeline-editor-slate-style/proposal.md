## Why

用户要求直接使用 Slate 已有的完整 Timeline UI。此前规划把数据适配扩大为纯内存 Editor Model、新 Surface API、交互与 Curve/DopeSheet 输入全面改造，实际成为重做编辑器，用户已明确否决；实现窗口已通过 0aa52f209 按用户要求回退。

本 change 撤销该方案，复用真实 Slate 已提供的时间尺、轨道/Clip、选择、拖动、裁剪、缩放、曲线和切线编辑。TimelineData 继续是正式业务数据，现有 typed 配置、Mutation/Undo、右侧 Inspector 与预览边界保留。

## What Changes

- 硬边界覆盖打开、刷新、新增 Track/Clip、选择、编辑和关闭全链路：不得创建或依赖 Slate 组件树，原生 Cutscene/Actor/Director 约束不得拒绝正式 TimelineData 合法操作。回退后 BuildProjection 隐藏对象仅是待清理残留，不是最终方案或 fallback。

- 删除上一轮纯内存 Surface/Editor Model、Clip包装器/选择系统和曲线工具整体迁移任务，不换名称继续实现。
- 实现窗口负责恢复用户要求的真实 Slate 基线；本规划不执行代码回退，不指定未经核对的回退提交，也不把文档更新当成已恢复。
- 只在现有 Slate 功能上继续数据和操作适配：稳定 ID、正式 Track/Clip/Section、完整曲线、资源和 typed 字段，提交走既有 TimelineData.AddTrack/AddClip/AddSection、Session/owner Undo。
- 原有帧显示、吸附、右侧 Inspector、布局和必要菜单接线保持；缺陷针对现成实现局部修改，不重建时间轴、交互或曲线渲染器。
- 直接修改 Slate 原源码的数据绑定：原时间尺/Track/Clip/Curve/DopeSheet 函数继续使用，所读写的 Cutscene/组件字段换为正式 Timeline/薄 adapter 输入和原 Mutation 输出。函数参数可以改，原绘制与交互算法保留。
- 删除临时 Cutscene/GameObject/组件树及专属创建、层级扫描和销毁；Actor/Director/运行采样等无关绑定直接清除。不能保留代用组件绕过接线，也不能另写一套 UI 来达成去依赖。
- Slate 对象无论何种承载均不能成为第二份持久化 Timeline、compiler输入或角色运行 owner；本地编辑不依赖启动 ScenePlay。
- 预览继续归正式 SkillGraph/Graph Shell、Session/adoption，Timeline 只编辑和显示已接入的真实观察。
- r2 C# authoring 分工继续：人工编辑不写源码，export_code/generate_assets 各显式调用，编译不生成资产，两工具不自动 Character Build/Play。公共 typed binding 与代码输出由原任务拥有，不恢复 UI JSON 或旧五工具。

## Capabilities

### New Capabilities

- 无新增编辑器框架；撤销上一轮声明的纯内存 Slate Surface 架构能力。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`：复用现成 Slate UI，通过现有数据/命令适配完成正式编辑；撤销强制新模型、曲线输入与组件迁移条款。

## Impact

- 本轮只修改本 change 的 proposal/design/tasks/specs 与 preview-integration-plan.md。实现窗口已提交 0aa52f209 回退，代码恢复状态仍由其实际结果记录。
- 保留正式 TimelineData/identity/资源/Curve/typed Mutation/Undo/Session、真实 Slate UI、右侧 Inspector、既有 Camera Track 和已正确的预览。
- 删除文档中“必须新建 SlateTimelineEditorSurface/Editor Model”“必须改造全部 IKeyable/AnimatedParameter”“必须拆 UI 程序集”的指令，避免错误规划继续驱动实现。
- 与 current specs 对比：独立作者能力、合法字段、稳定 identity、正式 Undo 等业务合同继续成立；旧 PreviewSession 条款仍归预览 change 处理。本轮不安装未完成 delta，也不修改其它任务的规范。
- 不新增测试或验证任务；design 第3节明确原函数保留与接线替换清单。原重做第11节撤销，新第11节仅记录原源码接线任务且保持未勾选，不能把回退或规划更新当成代码完成。
