## Why

2026-09-14 PARALLEL-20260914-DOMAIN-01新增规划接收范围：除原Slate UI线外，本任务登记独立的Timeline直接内容Runtime，详见[timeline-direct-runtime.md](timeline-direct-runtime.md)。本次只更新规划，不启动该Runtime实现；复用现有timeline任务，原UI授权/正确进展不变。历史段落不作为当前实现状态。

用户要求直接使用 Slate 已有的完整 Timeline UI。此前规划把数据适配扩大为纯内存 Editor Model、新 Surface API、交互与 Curve/DopeSheet 输入全面改造，实际成为重做编辑器，用户已明确否决；实现窗口已通过 0aa52f209 按用户要求回退。

本change撤销该方案，复用真实Slate已提供的时间尺、轨道/Clip、选择、拖动、裁剪、缩放、曲线和切线编辑。TimelineData继续是正式业务数据，已有typed配置、Mutation/Undo与预览归属保持；不恢复Timeline内部右侧自制Inspector，正式属性编辑复用原Slate控件接入Unity已有Inspector。

2026-09-13 已对照恢复后的原源码，具体证据、数据接口、Track内UI方法搬迁、曲线/Inspector与事务决策见[Slate原源码解耦决策](slate-source-decoupling.md)。当前ce21aec8f/afcb90056恢复原UI但仍有隐藏组件树；去组件依赖尚未完成。

## What Changes

- 2026-09-13仅规划协调：运行接入对齐[领域运行方案](../replace-character-program-with-domain-runtimes/design.md)，移除Character全量Build、整包Projection、统一ProgramEpoch和Document/v7前置。技能独立构建，Pose同一原生Factory显式重建/历史重置，Camera正式绑定/Reset，控制与网络依Session规则准备。预览原owner报告就绪、配置版本、实际版本和失败，UI不推断采用。
- MotionCurve源、区间、映射由[曲线源迁移](../../specs/character-root-motion-curves/spec.md)拥有，UI只接typed字段与源导航。源XYZ/Yaw不再作为Timeline-local通道，Weight/Ease等局部曲线仍用原Slate编辑。本通知不授权新的实现或修改其它owner文件。

- 硬边界覆盖打开、刷新、新增 Track/Clip、选择、编辑和关闭全链路：不得创建或依赖 Slate 组件树，原生 Cutscene/Actor/Director 约束不得拒绝正式 TimelineData 合法操作。回退后 BuildProjection 隐藏对象仅是待清理残留，不是最终方案或 fallback。

- 删除上一轮纯内存 Surface/Editor Model、Clip包装器/选择系统和曲线工具整体迁移任务，不换名称继续实现。
- 实现窗口负责恢复用户要求的真实 Slate 基线；本规划不执行代码回退，不指定未经核对的回退提交，也不把文档更新当成已恢复。
- 只在现有 Slate 功能上继续数据和操作适配：稳定 ID、正式 Track/Clip/Section、完整曲线、资源和 typed 字段，提交走既有 TimelineData.AddTrack/AddClip/AddSection、Session/owner Undo。
- 原有帧显示、吸附、布局和必要菜单接线保持；复用原Inspector控件、删除proxy字段与Actor前提，不新增Timeline右侧属性区。缺陷针对现成实现局部修改，不重建时间轴、交互或曲线渲染器。
- 直接修改 Slate 原源码的数据绑定：原时间尺/Track/Clip/Curve/DopeSheet 函数继续使用，所读写的 Cutscene/组件字段换为正式 Timeline/薄 adapter 输入和原 Mutation 输出。函数参数可以改，原绘制与交互算法保留。
- 原Track组件内的Editor方法允许搬迁到现有Editor模块并参数化，只保留一份函数主体；不以原入口提前返回到ShowEmbedded列表/时间轴冒充复用。BTSMTL编辑接口不继承运行IDirector/IKeyable，不提供空运行实现。
- 删除临时 Cutscene/GameObject/组件树及专属创建、层级扫描和销毁；Actor/Director/运行采样等无关绑定直接清除。不能保留代用组件绕过接线，也不能另写一套 UI 来达成去依赖。
- Slate 对象无论何种承载均不能成为第二份持久化 Timeline、compiler输入或角色运行 owner；本地编辑不依赖启动 ScenePlay。
- 预览继续归原SkillGraph/Graph Shell协调器、Session与各领域实际采用，Timeline 只编辑和显示已接入的真实观察。
- r2 C# authoring 分工继续：人工编辑不写源码，export_code/generate_assets 各显式调用，编译不生成资产，两工具不自动触发领域准备或Play。公共 typed binding 与代码输出由原任务拥有，不恢复 UI JSON 或旧五工具。

## Capabilities

### New Capabilities

- `btsmtl-timeline-direct-runtime`：直接只读内容与portable表示，独立Prepare/CreatePlayback，Advance候选及Commit/Discard/Stop，循环/Section、窗口/TreeClip调用与分型播放快照。与Slate UI分开，域内唯一清单为tasks第12节。

- 无新增编辑器框架；撤销上一轮声明的纯内存 Slate Surface 架构能力。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`：复用现成 Slate UI，通过现有数据/命令适配完成正式编辑；撤销强制新模型、曲线输入与组件迁移条款。

## Impact

- 新增Runtime规划接收主方案D13的Timeline域内部分；主实现仍拥有共享BtsmtlSkillTimelineCompiler、TreeClip技能执行服务、角色Step/总快照/Host及公共artifact删除。Motion/Warp/Camera、预览协调器、C#生成与资产均保留原owner。
- 对照现行btsmtl-runnable-timeline-node、gameplay-semantic-ir、compiled-simulation-program：Timeline内部operation发射、全角色state slots及ProgramPlan前提被D9与本change的新直接Runtime合同替代，原inline/shared、隔离、结束/取消语义保留。跨领域旧要求的全局delta仍由主方案维护，不覆盖其文档。

- 本次领域协调只修改本change的6份规划文档。现行规范仍要求Motion XYZ/Yaw作为Timeline-local通道，与新源owner规则冲突，源迁移delta归曲线任务；现行总Program/Projection与预览总Epoch条款由领域运行和原预览owner替换。本change明确消费新合同，不覆盖这些owner文档或下发执行。

- 本轮只修改本change文档，增加slate-source-decoupling.md；当前恢复状态按ce21aec8f/afcb90056及源码记录，不修改实现或资产。
- 保留正式TimelineData/identity/资源/Curve/typed Mutation/Undo/Session、真实Slate UI、原Inspector控件、既有Camera Track和已正确的预览；不恢复右侧自制面板。
- 删除文档中“必须新建 SlateTimelineEditorSurface/Editor Model”“必须改造全部 IKeyable/AnimatedParameter”“必须拆 UI 程序集”的指令，避免错误规划继续驱动实现。
- 与 current specs 对比：独立作者能力、合法字段、稳定 identity、正式 Undo 等业务合同继续成立；旧 PreviewSession 条款仍归预览 change 处理。本轮不安装未完成 delta，也不修改其它任务的规范。
- 本次已纠正本change内部右侧Inspector与原生属性接线的冲突；current btsmtl-timeline-editor-preview仍有TimelinePreviewSession/互斥LiveDebug只读条款，和Graph Shell预览决策冲突，由原场景预览change处理。旧timeline-animation-authoring-surface引用路径本次不存在，不再作为已核对依据。
- 不新增测试或验证任务；design 第3节明确原函数保留与接线替换清单。原重做第11节撤销，新第11节仅记录原源码接线任务且保持未勾选，不能把回退或规划更新当成代码完成。
