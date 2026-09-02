## Purpose

定义3C作为Generated Diagnostic Sampling独立KK package消费者时的依赖、领域生命周期接入、身份迁移与本地Owner删除合同，保证3C不再维护第二份通用采样框架。

## ADDED Requirements

### Requirement: 3C必须只消费独立KK package

3C Unity项目 MUST通过唯一file dependency解析`com.kk.generated-diagnostic-sampling`，Foot插件 MUST只引用`KK.GeneratedDiagnosticSampling*`正式assembly。3C MUST不跟踪通用框架Runtime、Host、Generator、Analyzer binary、Tools或其镜像。

#### Scenario: Unity解析采样依赖

- **WHEN** 3C Unity项目刷新Package Manager与脚本程序集
- **THEN** 它 MUST只加载独立仓库中的一个KK package与一个Analyzer
- **AND** 旧ThirdPerson package、asmdef或Analyzer MUST不存在于解析图

### Requirement: Foot必须通过生成生命周期Event接入

Foot领域 MUST只定义并发布`CaptureStarted`、`CommittedSample`和`CaptureStopped`三个typed Event，以及具体View／Metadata、Field／Table／Sampler／Program Attribute。CommittedSample MUST只在正式表现帧成功Seal后携带短租约View、lineage和样本维度metadata。Foot MUST不实现Bridge、手写Session控制、Left／Right租包循环、Host Adapter、Column Binding或第二CSV映射。

#### Scenario: 一个Foot帧自动生成左右脚样本

- **WHEN** PoseGraph成功Seal表现帧并发布Foot CommittedSample Event
- **THEN** KK package生成处理器 MUST按声明维度完成packet租用、字段提取、写入与提交
- **AND** Foot领域代码 MUST不直接调用Session、Writer或每字段CSV映射

### Requirement: Analyzer与Publisher必须只读生成产物

Foot Analyzer与Publisher MUST只读取KK package生成的基础CSV、typed artifact和manifest，计算评分、报告和发布结果。它们 MUST不参与采样生命周期，不重新声明Header／Column／CsvBinding，也不得回写Capability manifest或Runtime状态。

#### Scenario: Analyzer处理Completed产物

- **WHEN** Foot Capability基础artifact与manifest已经Completed
- **THEN** Analyzer MAY读取生成产物并发布独立报告身份
- **AND** Analyzer失败 MUST不触发第二次采样或把自身变成Host Adapter

### Requirement: 迁移必须删除3C旧Owner与旧身份

3C切换KK package的同一迁移 MUST删除原package、Tools、精确Repository Policy allowlist、`com.thirdperson.generated-diagnostic-sampling`、`ThirdPerson.GeneratedDiagnosticSampling*`及旧Generated Program identity。系统 MUST不保留wrapper、type forwarder、旧packet Reader、同步脚本或运行时fallback。

#### Scenario: 迁移完成后的源码搜索

- **WHEN** 对3C跟踪文件搜索旧package id、namespace、assembly和Tools路径
- **THEN** 结果 MUST为零
- **AND** 3C只可保留OpenSpec历史证据中明确标记为迁移前身份的文本

### Requirement: Disabled与Capture构建必须保持正式闭包

3C Performance Build Request MUST继续通过`DiagnosticCapabilitySet`选择Foot Disabled或Capture。Disabled MUST排除Foot生命周期Event handler、Definition、Generated Program、page、queue和interest；Capture MUST只包含KK package与匹配Foot Event／Program闭包。迁移不得创建第二Player、Controller、Capture根或Comparer。

#### Scenario: 纯性能基线关闭Foot采样

- **WHEN** Foot Capability Mode为Disabled
- **THEN** Player闭包 MUST不包含Foot生成事件处理器、packet页或空manifest
- **AND** MUST不通过运行时bool或Null Adapter模拟关闭
