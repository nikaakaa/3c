# btsmtl-timeline-animation-authoring-surface Specification

## Purpose
定义Timeline Editor本地作者表面、typed session、可选Topology/Runtime Debug能力和显式领域工具的唯一边界。

## Requirements

### Requirement: Timeline Animation作者表面必须只拥有本地时间作者内容

Timeline Editor Core MUST只拥有Timeline frame geometry、Track/Clip/Point Marker/registered editable Curve的布局、selection、gesture、mutation transaction与Undo。生成分析、Character Pipeline配置、Projection状态和Runtime业务对象 MUST不成为Track行、Clip字段或Core程序集依赖。独立Timeline没有外部上下文时 MUST仍可完整编辑全部本地作者内容。

#### Scenario: 独立打开shared Timeline

- **WHEN** 作者从Project窗口直接打开shared Timeline且没有Graph或Character Definition上下文
- **THEN** 作者 MUST仍能编辑Clip、Sync Marker、TreeClip与registered Curve Channel
- **AND** Timeline MUST不显示要求Definition context的空Track或阻止本地编辑

#### Scenario: 领域工具没有配置

- **WHEN** 当前Timeline没有任何适用的领域工具provider
- **THEN** 主时间轴 MUST保持完整可用且布局不增加空白工具行
- **AND** Timeline Core MUST不反向搜索Character、Profile或Definition

### Requirement: Timeline Editor必须使用typed打开请求与Session Context

Timeline窗口 MUST通过显式`TimelineEditorOpenRequest`接收TimelineData、serialized owner/path、ownership label、可选Marker topology context、可选Runtime Debug binding与显式tool catalog，并形成typed `TimelineEditorSessionContext`。Timeline Editor MUST不保存`object AuthoringContext`，View MUST不通过runtime cast探测未知领域能力。

#### Scenario: 从Graph打开Timeline

- **WHEN** Graph窗口打开一个TimelineNode引用的Timeline
- **THEN** OpenRequest MUST显式携带本地serialized binding与可选Marker topology context
- **AND** Timeline本地编辑 MUST不依赖Graph窗口继续存活

#### Scenario: Context消费者需要Marker topology

- **WHEN** Sync Marker工具需要查询同组producer和Once/Loop call site
- **THEN** 它 MUST只消费typed Marker topology context
- **AND** MUST不把该context用于Foot Analysis、Clip编辑或Runtime Debug

### Requirement: Timeline领域工具必须通过显式Provider进入按需工具区域

Timeline Editor MUST提供显式Editor-only tool provider合同和不使用反射的catalog。每个provider MUST声明稳定ToolId、显示名、适用selection与所需领域输入。Core MUST只托管工具区域、selection通知和生命周期；provider MUST不改变Track基础高度、注入Runtime Track或绕开mutation transaction。Provider生成的派生数据默认 MUST只读；当领域显式定义“候选转换为已有作者类型”时，必须由作者确认并通过Session正式mutation提交，不得静默写入或创建第二种序列化类型。

#### Scenario: Foot Analysis provider适用于Animation Clip

- **WHEN** 作者选中Animation Clip并打开Analysis工具区域
- **THEN** Character Editor注册的provider MAY创建Foot Analysis面板
- **AND** Timeline Core MUST不引用Foot Placement、Analysis Source或Projection类型

#### Scenario: Provider程序集不存在

- **WHEN** Timeline Editor运行环境没有安装Character Foot Analysis provider
- **THEN** Timeline本地作者能力 MUST保持不变
- **AND** Core MUST不通过TypeCache、反射或字符串类名寻找替代provider

#### Scenario: 作者应用脚接触候选

- **WHEN** Foot Analysis provider显示未过期的Left/Right contact候选且作者确认目标AnimationTrack
- **THEN** Provider MUST通过`TimelineEditorSessionContext.Apply`转换为正式AnimationSyncMarker
- **AND** MUST只替换LeftFootContact与RightFootContact集合、保留其它业务Marker并触发现有validator/compiler链

### Requirement: Timeline上下文必须区分作者、Topology、领域工具与Runtime Debug

本地作者上下文、跨producer Topology context、领域工具输入与Runtime Debug binding MUST是四个独立合同。任何合同缺失 MUST只禁用依赖它的功能，不得由其它合同补全、猜测或转型替代。

#### Scenario: 只有Runtime Debug binding

- **WHEN** Timeline附着运行实例但没有Character authoring topology
- **THEN** Live Debug MAY显示正式playback状态
- **AND** Marker group作者校验与Foot Analysis MUST不从运行实例推导作者上下文

#### Scenario: 只有Marker topology context

- **WHEN** Timeline从Graph打开但没有选择Analysis Source
- **THEN** Marker group校验 MAY工作
- **AND** Foot Analysis MUST继续要求自己的显式领域输入

### Requirement: 生成分析不得成为Timeline主轨lane

只读生成分析 MAY通过按需tool panel显示，但 MUST不创建常驻Track child lane、改变Track高度、参与Timeline hit test、selection、Undo、dirty owner或editable Curve Channel Catalog。

#### Scenario: 关闭Analysis工具

- **WHEN** 作者关闭Timeline Analysis工具区域
- **THEN** Editor MUST不读取分析artifact或创建分析curve renderer
- **AND** Track高度与滚动范围 MUST与未安装分析provider时一致

#### Scenario: 查看多个生成metric

- **WHEN** 领域工具具有多个生成metric
- **THEN** 工具 MUST通过自己的选择控件按需显示
- **AND** MUST不为每个metric向主时间轴增加一行
