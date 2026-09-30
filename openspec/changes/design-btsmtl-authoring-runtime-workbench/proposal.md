# BTSMTL Authoring Runtime Workbench

## Why

纯 Timeline 编排、Ability 的实际执行过程和游戏运行诊断需要不同的工作面。旧方案把 Preview 限定为作者 Timeline 播放加 overlay，无法表达决策、Loop、子图调用和历史变量，也让简单内容编辑承担完整调试流程。

## What Changes

- **基础编排**：Authoring / Assembling 负责 Timeline、Clip、曲线、动画、特效和镜头调整，并能直接播放、拖动查看正式效果；不要求先构造 Ability 图。Assembling 是编排职责用语，不新增第四模式或重命名代码 API。
- **Ability Preview**：打开 Ability 查看节点图，将实际节点执行、决策、每次 Loop、子图与 Timeline 调用投影成不断增长的执行时间线。投影片段不是作者资产，不能拖拽改写已发生事实。
- **历史与调参**：执行游标可回看当时节点、变量及角色；正式运行变量命令支持观察后续变化。作者资产修改继续使用 Mutation/Undo 和内容采用合同；从历史修改后分叉保留策略尚未确认。
- **RuntimeDebug**：只读连接实际游戏或明确绑定的预览 Session，复用来源映射和执行事实，不启动第二运行实例。
- **BREAKING**：编辑器效果预览改为 CMC 式隐藏 Scene，在 Edit Mode 自动装配，不启动 Unity Play、不要求用户操作准备流程；执行仍复用正式领域 owner 和现有 Tick。删除 Preview 旧 PlayMode 启动调用，不保留双入口。
- **窗口分工**：原 FlowCanvas 编辑节点图，原 Timeline/Slate 编排作者内容；新增独立可停靠 Preview 窗口，上方为角色视口与运行黑板，下方为执行时间线，分区可拖动。执行时间线复用 Slate，所有窗口共享唯一隐藏 Session 与 Renderer，不按调用创建窗口或恢复旧总控。
- **修改路径**：运行黑板调值复用原作用域和写入规则；Timeline 内容修改走内容刷新，只有编译图配置或结构变化才 Build 图。常用交互自动组织正式采用步骤，不要求用户逐个执行 Export、Prepare、Publish、Adopt。
- **生命周期**：明确宿主、正式 Session、页面绑定和输入归属。暂停保留现场；Stop 与关闭预览如何区分仍待确认，不提前固定销毁行为。
- **动态长度边界**：Ability 执行记录持续增长不改变作者 Clip 合同；仅 TreeDecision TreeClip 的实际退出长度动态，FrameBoundary 仍固定。

## Capabilities

### New Capabilities

- `btsmtl-authoring-runtime-workbench`：三层工作职责、隐藏场景宿主、Ability 执行投影、历史查看与变量命令边界。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`：基础编排效果预览、正式时间定位、执行投影与作者内容分离、TreeDecision 观察。
- `graph-authoring-editor-shell`：Ability 图入口联动执行时间线、隐藏场景生命周期与页面绑定。
- `graph-authoring-domain-framework`：作者字段与预览运行变量的资格、命令及采用状态区分。
- `gameplay-simulation-session-composition`：隐藏场景沿正式 Composition 装配；非 Skill 内容不强制角色 Session。
- `character-animation-pipeline`：正式动画链在隐藏场景执行，保留逻辑与表现域退出规则。
- `character-animation-layer-runtime`：统一正式事实来源和合法参数采用边界。
- `character-motion-matching-presentation-module`：正式角色查询替代独立 Query Fixture 预览。
- `character-pose-inertialization`：正式恢复与只读历史查看保持各自状态边界。
- `unity-simulation-assembly-ownership`：编辑器接入公开运行端口，业务执行留在正式程序集。

## Impact

现有 Timeline 控制器、Slate 投影、FlowCanvas 来源导航、RuntimeDebug 事实、Session 历史与输入端口需要接通新工作方式。运行事实必须能区分调用实例、Loop 迭代、内容版本和逻辑/表现时间，不能在 UI 重算分支或复制 TimelineData 为第二状态源。

本 proposal 定义目标和变更范围，不作为实现完成证据。当前实现与检查结果见 implementation-current.md，未完成事项见 tasks.md；已有 Tick、History 接口或旧 overlay 不代表完整功能。详细生命周期、未决项、现行规范差异与用户查看场景见 design.md；本 change 尚未归档。
