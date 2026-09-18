# btsmtl-timeline-animation-authoring-surface Specification

## Purpose
定义Timeline Editor本地作者表面、typed session、可选Topology/Runtime Debug能力和显式领域工具的唯一边界。

## Requirements

### Requirement: Timeline Animation作者表面必须只拥有本地时间作者内容

Timeline Editor Core MUST只拥有Timeline frame geometry、Track/Segment/registered Timeline Curve的布局、selection、gesture、mutation transaction与Undo。生成分析、AnimationClip注册Curve、Character Pipeline配置、Projection状态和Runtime业务对象 MUST不成为Track行、Segment字段或Core程序集依赖。独立Timeline没有外部上下文时 MUST仍可完整编辑全部本地作者内容。

#### Scenario: 独立打开shared Timeline

- **WHEN** 作者从Project窗口直接打开shared Timeline且没有Graph或Character Definition上下文
- **THEN** 作者 MUST仍能编辑Segment、TreeClip与registered Timeline Curve Channel
- **AND** Timeline MUST不显示Sequence、Sync Marker或Clip注册Curve行

#### Scenario: 领域工具没有配置

- **WHEN** 当前Timeline没有任何适用的领域工具provider
- **THEN** 主时间轴 MUST保持完整可用且布局不增加空白工具行
- **AND** Timeline Core MUST不反向搜索Character、Profile或Definition

### Requirement: Timeline Editor必须使用typed打开请求与Session Context

Timeline窗口 MUST通过显式`TimelineEditorOpenRequest`接收TimelineData、serialized owner/path、ownership label、可选Runtime Debug binding与显式tool catalog，并形成typed `TimelineEditorSessionContext`。Open Clip导航所需Character Definition、Profile与Preview Target MUST作为独立typed navigation context传入。Timeline Editor MUST不保存`object AuthoringContext`、Marker topology context或Sequence document context，View MUST不通过runtime cast探测未知领域能力。

#### Scenario: 从Graph打开Timeline

- **WHEN** Graph窗口打开一个TimelineNode引用的Timeline
- **THEN** OpenRequest MUST显式携带本地serialized binding与可选Clip navigation context
- **AND** Timeline本地编辑 MUST不依赖Graph窗口继续存活

#### Scenario: 缺少Clip navigation context

- **WHEN** 作者独立打开Timeline并选择Animation Segment
- **THEN** Segment引用与本地编排 MUST继续可编辑
- **AND** 需要Preview Target的Open Clip MUST显示typed Unavailable而不搜索场景

### Requirement: Timeline领域工具必须通过显式Provider进入按需工具区域

Timeline Editor MUST提供显式Editor-only tool provider合同和不使用反射的catalog。每个provider MUST声明稳定ToolId、显示名、适用selection与所需领域输入。Core MUST只托管工具区域、selection通知和生命周期；provider MUST不改变Track基础高度、注入Runtime Track、修改AnimationClip注册Curve或绕开Timeline mutation transaction。Provider生成的派生数据默认 MUST只读；只有领域已定义Timeline-local作者类型时，作者确认后才 MAY通过Session正式mutation提交。

#### Scenario: Provider程序集不存在

- **WHEN** Timeline Editor运行环境没有安装任何领域provider
- **THEN** Timeline本地作者能力 MUST保持不变
- **AND** Core MUST不通过TypeCache、反射或字符串类名寻找替代provider

#### Scenario: Provider请求修改AnimationClip

- **WHEN** provider尝试通过Timeline Session写入Clip注册Curve
- **THEN** Session MUST拒绝该Mutation
- **AND** provider只能返回Open Animation Clip导航请求

### Requirement: Timeline上下文必须区分作者、领域工具与Runtime Debug

本地作者上下文、领域工具输入与Runtime Debug binding MUST是三个独立合同。任何合同缺失 MUST只禁用依赖它的功能，不得由其它合同补全、猜测或转型替代。Marker topology与Sequence document context MUST不存在。

#### Scenario: 只有Runtime Debug binding

- **WHEN** Timeline附着运行实例但没有Character authoring context
- **THEN** Live Debug MAY显示正式Action playback状态
- **AND** Open Clip Preview Target与领域工具 MUST不从运行实例推导

#### Scenario: 只有本地作者上下文

- **WHEN** shared Timeline没有领域工具输入
- **THEN** Track、Segment与Timeline-local Curve MUST保持完整可编辑
- **AND** Editor MUST不显示空Analysis或Marker区域

### Requirement: Timeline窗口顶栏必须使用模型视图控制器边界

Timeline 顶栏 MUST 由只读 binding model、独立 toolbar view 和窗口 controller 组成。Model 只投影当前 TimelineData、serialized owner/path、ownership、source graph 与 node identity；View 只构造布局、按钮组和控件回调；controller MUST 拥有 binding、mutation、Undo、Slate projection 和观察事件。View MUST NOT 持有 TimelineData 可写状态、runtime fact 或业务 mutation。

#### Scenario: 顶栏操作

- **WHEN** 作者点击返回导航、切换文档或观察 Workspace 模式
- **THEN** View MUST 只把请求交给 controller，由 controller 走正式 binding / mutation / runtime observation 入口
- **AND** View MUST NOT 直接改 TimelineData 或创建第二个 Timeline runtime

### Requirement: Timeline顶栏必须按职责分组

Timeline 顶栏 MUST 将控件分为文档导航组、Workspace 模式组和运行状态组。文档导航组承载返回、文档选择与来源摘要；Workspace 模式组承载显式注册的模式 controls；运行状态组只承载指向正式 Graph Shell / Scene Play ownership 的状态说明。禁止把不同职责控件混入同一无语义堆栈后用绝对位置补救。

#### Scenario: 打开 Timeline 窗口

- **WHEN** 作者打开 bound 或 unbound Timeline 窗口
- **THEN** 顶栏 MUST 仍显示三组职责控件
- **AND** 未注册 Workspace controller 时 MUST 保持文档导航和状态可用，不得创建备用运行按钮

### Requirement: 旧TreeDesigner Timeline UI必须保持删除且FlowCanvas只做图可视化

Timeline 作者面 MUST 只使用唯一嵌入式 Slate surface。旧 TreeDesigner Timeline 自制窗口、列表、Inspector 或第二编辑面 MUST NOT恢复。FlowCanvas / TreeClip graph 窗口只负责 TimelineBody、Marker 触发图和其它正式 graph 的节点可视化与作者入口；MUST NOT重新拥有 Track 布局、Clip 时间几何、selection、Undo transaction 或 Timeline runtime observation。

#### Scenario: 从 TreeClip 打开图

- **WHEN** 作者在 Timeline 中打开 TreeClip 或 Marker 触发图
- **THEN** 编辑器 MUST 打开正式 FlowCanvas graph authoring surface
- **AND** MUST NOT创建第二个 Timeline 窗口或把 graph canvas 当作 Timeline track editor

#### Scenario: 编辑 Timeline 内容

- **WHEN** 作者编辑 Track、Clip、Marker、Section 或 Timeline-local curve
- **THEN** 输入 MUST 进入嵌入式 Slate projection 和正式 Timeline session mutation
- **AND** MUST NOT绕过 Slate 在 TreeDesigner UI 中维护平行内容
