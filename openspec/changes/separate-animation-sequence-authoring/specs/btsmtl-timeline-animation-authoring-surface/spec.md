## MODIFIED Requirements

### Requirement: Timeline Animation作者表面必须只拥有本地时间作者内容

Timeline Editor Core MUST只拥有AnimationTimeCanvas、文档host、frame geometry、Span/Point/Curve lane布局、selection、gesture、mutation transaction、Undo和按需工具区域。Action Timeline文档 MAY投影Track、Sequence Segment、Section、Window与Timeline-local Curve；Sequence文档 MAY投影素材Marker、Notify与Sequence Curve。Core MUST不保存两类文档的业务数据、Profile配置、Projection或Runtime对象。任一文档没有外部Character上下文时 MUST仍可编辑自身全部本地作者内容。

#### Scenario: 独立打开Sequence

- **WHEN** 作者从Project窗口直接打开Animation Sequence
- **THEN** 主Timeline Editor MUST显示Sequence Marker、Notify与registered Curve
- **AND** Core MUST不创建TimelineData、Profile Binding或Action Track

#### Scenario: 独立打开shared Action Timeline

- **WHEN** 作者从Project窗口直接打开shared Timeline
- **THEN** Action Timeline adapter MUST继续完整编辑Segment、Section和本地业务Track
- **AND** Core MUST不反向搜索Profile或Sequence副本

#### Scenario: 领域工具没有配置

- **WHEN** 当前Sequence或Timeline没有任何适用tool provider
- **THEN** 主时间轴 MUST保持完整可用且不增加空白工具行
- **AND** Core MUST不反向搜索Character、Profile或Definition

### Requirement: Timeline Editor必须使用typed打开请求与Session Context

主窗口 MUST通过显式`AnimationTimeDocumentOpenRequest`接收稳定document kind、正式serialized owner、typed document adapter、ownership label、可选Preview binding、Runtime Debug binding与tool catalog，并形成window-local session。Action Timeline adapter MUST显式持有TimelineData与serialized path；Sequence adapter MUST显式持有Sequence owner。Core MUST不保存`object AuthoringContext`、不要求全部文档伪装为TimelineData，也不得通过runtime cast探测领域能力。

#### Scenario: 从Graph打开Action Timeline

- **WHEN** Graph窗口打开TimelineNode引用的Timeline
- **THEN** OpenRequest MUST装配Action Timeline adapter与精确serialized binding
- **AND** Timeline本地编辑 MUST不依赖Graph窗口继续存活

#### Scenario: 从Segment打开Sequence

- **WHEN** 作者双击Sequence Segment
- **THEN** OpenRequest MUST装配Segment精确引用的Sequence adapter
- **AND** MUST不从AnimationClip、显示名或当前Profile猜测Sequence

#### Scenario: Context消费者需要Marker topology

- **WHEN** Action或Pose relation工具需要查询同组Sequence consumer与Once/Loop call site
- **THEN** OpenRequest MUST显式携带typed topology context
- **AND** 本地Sequence/Timeline编辑 MUST不依赖该context存在

### Requirement: Timeline领域工具必须通过显式Provider进入按需工具区域

Timeline Editor MUST提供显式Editor-only tool provider合同和不使用反射的catalog。Provider MUST声明稳定ToolId、适用document kind、selection与所需typed输入。Sequence Foot Analysis工具只适用于Sequence文档；Action Timeline diagnostics工具只适用于Action Timeline文档。Core MUST只托管工具区域、selection通知和生命周期；provider MUST不注入主lane、绕开document mutation或创建第二作者类型。

#### Scenario: Sequence Foot Analysis provider适用

- **WHEN** 作者在Sequence文档选中素材并打开Analysis
- **THEN** Character Editor provider MAY显示精确artifact与候选
- **AND** Apply MUST通过Sequence session mutation提交Marker

#### Scenario: Action Timeline选择Segment

- **WHEN** 作者在Action Timeline选中Sequence Segment
- **THEN** Foot Analysis provider MUST不在Timeline owner写入Marker
- **AND** 工具 MAY提供Open Sequence导航

#### Scenario: Provider程序集不存在

- **WHEN** Timeline Editor运行环境没有安装Character Foot Analysis provider
- **THEN** Sequence与Action Timeline本地作者能力 MUST保持不变
- **AND** Core MUST不通过TypeCache、反射或字符串类名寻找替代provider

### Requirement: Timeline上下文必须区分作者、Topology、领域工具与Runtime Debug

Sequence/Action Timeline本地作者上下文、跨source Topology context、领域工具输入与Runtime Debug binding MUST是独立typed合同。任何合同缺失 MUST只禁用依赖它的功能，不得由其它合同补全、猜测或转型替代。Action Timeline与Sequence context MUST不互相提供可写owner。

#### Scenario: 只有Sequence作者上下文

- **WHEN** Sequence没有Definition topology或Runtime target
- **THEN** 作者 MUST仍可编辑素材Marker、Notify与Curve
- **AND** 依赖Projection relation的预览诊断 MUST显示Unavailable

#### Scenario: 只有Runtime Debug binding

- **WHEN** Action Timeline附着运行实例但没有Sequence/Definition topology
- **THEN** Live Debug MAY显示正式playback状态
- **AND** Marker作者与Analysis MUST不从运行实例推导上下文

#### Scenario: 只有Marker topology context

- **WHEN** Sequence从明确consumer打开但没有Analysis Source或Runtime target
- **THEN** 同步组校验 MAY工作
- **AND** Analysis与Live Debug MUST继续要求自己的显式输入

### Requirement: 生成分析不得成为Timeline主轨lane

只读生成分析 MAY通过Sequence文档按需tool panel或临时overlay显示，但 MUST不创建常驻Track、改变基础lane高度、参与资产selection、Undo、dirty owner或editable Curve Catalog。Action Timeline文档 MUST不因Segment引用Sequence而复制或展示可写Analysis lane。

#### Scenario: 关闭Sequence Analysis工具

- **WHEN** 作者关闭Analysis区域
- **THEN** Editor MUST停止读取artifact和绘制候选overlay
- **AND** Sequence Marker/Notify/Curve lane布局 MUST保持稳定

#### Scenario: 查看多个生成metric

- **WHEN** Analysis provider具有多个生成metric
- **THEN** 工具 MUST通过自己的选择控件按需显示一个metric
- **AND** MUST不为每个metric向主时间轴增加常驻lane
