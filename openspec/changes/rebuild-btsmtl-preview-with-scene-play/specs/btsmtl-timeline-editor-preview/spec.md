## REMOVED Requirements

### Requirement: Timeline 编辑器预览使用管线预览会话

**Reason**：窗口独立推进表现会话已由场景真实运行替代。
**Migration**：播放、暂停、停止与目标连接迁入统一场景预览操作；Timeline 页面保留作者数据、编辑游标和本地运行观察。

### Requirement: Timeline 编辑器预览目标来自正式管线预览目标

**Reason**：预览目标不再是提供独立动画求值的场景组件。
**Migration**：从本次预览场景登记的正式 Session 与 Actor 中精确选择，删除旧预览目标抽象及其窗口字段。

### Requirement: Timeline preview session 必须隔离动画生命周期状态

**Reason**：窗口不再创建自己的 Action、动画工作区和物理输出生命周期。
**Migration**：动画状态由各真实 Actor 独立拥有；多窗口只共享观察数据，受控场景运行统一拥有控制权限。

### Requirement: 预览采样必须复用正式动画Selection与Pose Plan

**Reason**：编辑游标生成 Selection 和独立 seek 重建不再是完整角色预览行为。
**Migration**：通过正式输入运行角色，由实际 Action 和 Pose Plan 产生结果；编辑游标和 Capture 历史不执行角色。

### Requirement: Timeline Editor 必须分离 Authoring Preview 与 Live Debug

**Reason**：表现专用 Authoring Preview 模式被移除。
**Migration**：采用作者编辑、受控场景预览操作和只读真实运行观察；外部 Live 运行不能被预览控制抢占。

### Requirement: Timeline Preview 必须按正式阶段展示 TreeClip

**Reason**：旧合同禁止预览运行 Gameplay，并允许独立纯表现采样，已被场景真实运行替代。
**Migration**：采用“Timeline场景预览必须按正式阶段展示TreeClip”，保留阶段与声明显示，真实执行只来自正式 Session。

### Requirement: Timeline Field内部交互、几何与渲染必须分属明确模块

**Reason**：旧合同要求保留 Authoring Preview 模式及其窗口 session adapter。
**Migration**：采用“Timeline Field必须隔离作者交互、几何绘制与场景运行观察”，完整保留原有交互、几何、绘制和 identity 责任，迁移运行入口。

## MODIFIED Requirements

### Requirement: Timeline 资产不保存编辑器播放状态

Inline TimelineData、shared TimelineAsset 及其持有的 TimelineData MUST只保存作者数据，不得保存预览场景运行、Actor、playback generation、当前运行时间或动画资源状态。编辑游标和窗口选择 MUST保存在本地视图；真实运行状态 MUST由场景正式 Session 与 Actor 拥有。

#### Scenario: 两个页面预览同一个 shared Timeline

- **WHEN** 两个作者页面打开同一个 shared TimelineAsset
- **THEN** 页面 MUST分别保存自己的编辑游标、选择和运行观察绑定
- **AND** 关闭或移动一个页面的游标 MUST不改写资产、另一页面或真实运行

#### Scenario: 预览 inline Timeline

- **WHEN** 正式角色执行 TimelineNode 的 inline Timeline
- **THEN** 运行时间、Track/TreeClip 状态 MUST只存在于正式运行数据
- **AND** MUST不创建作者 Timeline 工作副本作为另一套运行源

### Requirement: 旧 TimelinePlayer 预览路径必须删除

系统 MUST删除旧 TimelinePlayer、自主 Timeline Bind/Evaluate/Unbind 和完整角色的窗口级预览播放器依赖。Timeline 作者入口 MUST统一使用独立场景 Play 运行操作，不得保留旧 target、独立动画时钟或兼容播放分支。

#### Scenario: 打开旧Timeline资产

- **WHEN** 作者打开已有 inline 或 shared Timeline 并请求预览
- **THEN** 播放入口 MUST只进入统一场景预览合同
- **AND** MUST不根据资产年代或窗口类型恢复旧播放器

#### Scenario: 搜索旧播放器入口

- **WHEN** 迁移完成后检查正式 Timeline 播放入口及其调用链
- **THEN** MUST不存在旧 TimelinePlayer、自主 Timeline 绑定或窗口级完整角色预览播放器
- **AND** 所有完整角色播放入口 MUST指向统一场景运行操作

## ADDED Requirements

### Requirement: Timeline必须区分作者编辑与真实运行观察

Timeline 页面 MUST明确区分编辑游标、当前正式运行标记和 Capture 历史位置。场景预览和外部 Live MUST复用正式增量诊断与窗口本地运行绑定；运行观察内容 MUST只读。领域允许的作者调参 MUST位于明确的作者字段，通过共享 Mutation 和真实 Actor 调参入口执行，不能修改观察字段或其它窗口绑定。

#### Scenario: 多个playback使用同一Timeline

- **WHEN** 当前角色存在多个指向同一 Timeline source 的正式 playback
- **THEN** 窗口 MUST明确选择 playback instance 并显示其实际来源
- **AND** MUST不按当前编辑游标推断正在执行哪个实例

#### Scenario: 重载后恢复Timeline页面

- **WHEN** 页面经历 Domain Reload 或预览场景重建
- **THEN** 页面 MUST从稳定作者 owner/path 恢复文档并创建新的本地运行绑定
- **AND** 定位无效时 MUST显示不可用，不猜测另一个 Timeline 或恢复旧播放器

### Requirement: Inline与Shared Timeline必须复用同一场景预览入口

Inline Timeline、shared Timeline 与 TreeClip 下钻页面 MUST复用同一场景运行操作，页面导航 MUST不启动、接管或销毁正式运行。只有能精确关联所选角色和正式调用点的 Timeline 才能提供动作试验输入；缺少关联时 MUST保留编辑并报告不能直接运行的原因。

#### Scenario: 直接打开未被角色引用的shared Timeline

- **WHEN** shared Timeline 在当前预览角色中没有正式调用点
- **THEN** 作者 MUST能够编辑该资产，但动作试验入口 MUST显示缺少角色调用点
- **AND** MUST不临时创建 producer、Action 或图节点

### Requirement: Timeline场景预览必须按正式阶段展示TreeClip

Timeline Editor MUST继续显示 TreeClip 的 Decision/Commit 阶段、inline/shared ownership 和 Blackboard 声明摘要。完整场景预览中 TreeClip、Gameplay Program、Action、Blackboard、GameplayEffect、Motion 与世界求解 MUST只由正式场景 Session 执行；窗口 MUST只观察正式输出，不创建临时图上下文、运行树副本或第二解释器。编辑游标 MUST不执行 TreeClip，也不写作者默认值。

#### Scenario: 场景预览执行Decision TreeClip

- **WHEN** 角色正式运行到一个 Decision TreeClip
- **THEN** Timeline MUST显示同一逻辑 Tick 的正式执行和输出事实
- **AND** MUST不由窗口另行执行一次 Decision

#### Scenario: 没有正式运行目标

- **WHEN** 作者打开含 TreeClip 的 Timeline 但未建立合法场景运行绑定
- **THEN** 页面 MUST继续显示作者内容和缺失上下文原因
- **AND** MUST不创建替代 Session 或假运行结果

#### Scenario: 打开动画素材

- **WHEN** 作者从 Animation Segment 打开原生 AnimationClip
- **THEN** 页面 MUST导航到该素材的正式编辑工具
- **AND** 素材编辑 MUST不被标记为 Timeline Gameplay 已执行

### Requirement: Timeline Field必须隔离作者交互、几何绘制与场景运行观察

Timeline Editor MUST保留现有窗口、Timeline Field、Inspector 和真实运行观察入口；selection/drag/move/resize、time/frame/clip geometry 与 hit-test、track/clip/playhead/overlay rendering、窗口本地运行绑定 MUST由职责独立的模块拥有。selection MUST只读暴露并通过命令修改；interaction MUST依赖窄 host port；rendering MUST显式消费 geometry、viewport、playhead 和 overlay 输入，不反向读取完整 Timeline Field。作者修改 MUST通过唯一 Mutation/Undo；geometry 和 rendering MUST不写资产。场景运行控制 MUST交给共享场景操作，本地 binding MUST不拥有播放器。迁移 MUST保持 Timeline/Track/Clip identity、Source Map、Inspector selection 和多窗口页签行为。

#### Scenario: Resize一个Animation Clip

- **WHEN** 作者在 Edit Mode 拖动 Clip 边缘改变范围
- **THEN** interaction MUST使用 geometry 的 frame 结果生成一个正式 Mutation
- **AND** 同一个 Undo 边界 MUST更新原 Clip identity，rendering 只根据新数据重绘

#### Scenario: 点击右侧Inspector设置

- **WHEN** 作者选择 Clip 后修改允许编辑的作者字段
- **THEN** selection MUST保持同一 Clip authoring identity
- **AND** 重绘 MUST不清空或切换选择

#### Scenario: 作者页面连接场景运行

- **WHEN** 页面开始观察受控预览场景的真实 playback
- **THEN** 本地 adapter MUST建立准确运行绑定，结构交互 MUST进入只读状态
- **AND** geometry 与 rendering MUST复用同一作者 identity 显示真实 overlay，不创建预览 evaluator

#### Scenario: 多个playback overlay

- **WHEN** 同一 Timeline source 存在多个正式 playback
- **THEN** overlay MUST呈现各实例并服从 Follow/Pin 选择
- **AND** rendering MUST不按列表顺序选择赢家或重新执行角色
