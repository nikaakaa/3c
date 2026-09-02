## Purpose

定义Generated Diagnostic Sampling作为独立KK Unity Package的唯一源码、正式公开身份、发布闭包和跨项目消费合同，防止3C、pik或未来项目复制出各自的Generator、Runtime、Host与协议实现。

## ADDED Requirements

### Requirement: 独立仓库必须是采样框架唯一源码Owner

系统 MUST由独立`generated-diagnostic-sampling`仓库唯一拥有通用Contracts、Runtime、Host、Source Generator、Analyzer binary、portable build工具、Probe和框架OpenSpec。3C、`pik`及未来消费者 MUST只通过版本化UPM package依赖使用该能力，不得跟踪第二份源码、Analyzer、Session、Writer、Reader或Host Finalizer。

#### Scenario: 3C与pik同时使用采样框架

- **WHEN** 3C Foot诊断和`pik`插件同时启用Generated Diagnostic Sampling
- **THEN** 两个Unity项目 MUST解析到同一独立仓库中的同一package identity与源码
- **AND** 任一消费者仓库 MUST不存在框架源码镜像或同步脚本

### Requirement: 正式公开身份必须一次迁移为KK命名

独立package MUST使用`com.kk.generated-diagnostic-sampling`作为UPM identity，并使用`KK.GeneratedDiagnosticSampling`、`KK.GeneratedDiagnosticSampling.Host`和`KK.GeneratedDiagnosticSampling.Generator`作为正式namespace／assembly identity。旧`com.thirdperson.generated-diagnostic-sampling`与`ThirdPerson.GeneratedDiagnosticSampling*` MUST被删除，不得保留wrapper、type forwarder、双asmdef或兼容Analyzer。

#### Scenario: 消费者完成破坏性升级

- **WHEN** 3C或`pik`切换到独立package
- **THEN** 其manifest、asmdef、using、生成程序和构建闭包 MUST只引用KK正式身份
- **AND** 对旧ThirdPerson身份的任何编译引用 MUST直接失败而不是被兼容层转发

### Requirement: Package发布物必须闭合Runtime与Generator身份

每个package版本 MUST同时发布Runtime／Host源码、与当前Generator源码完全一致的Analyzer DLL、package manifest和许可证。Generator binary identity、程序集binding、Schema identity、Program identity与Capability Set identity MUST形成同一版本闭包；源码与Analyzer hash不一致的package MUST不得被3C、`pik`或发布流程接受。

#### Scenario: Analyzer落后于Generator源码

- **WHEN** package内Analyzer DLL的hash或MVID不匹配当前Generator Release产物
- **THEN** 发布预检 MUST失败并阻止Unity消费者刷新为可用版本
- **AND** 系统 MUST不使用旧Analyzer继续生成表面可编译的Schema

### Requirement: 消费者必须通过正式依赖而不是复制接入

本地工作区中的3C与`pik` MUST通过各自Unity project manifest的唯一file dependency解析独立package；`com.kk.pik`公开package MUST声明对`com.kk.generated-diagnostic-sampling`正式版本的依赖。消费者 MAY定义领域Capability、具体View、具体Capture Metadata、Extractor、Bridge与Host Adapter，但 MUST不拥有框架toolchain或通用协议实现。

#### Scenario: pik增加领域Sampler

- **WHEN** `pik`为Foot Placement增加自己的诊断Capability与Sampler
- **THEN** 它 MUST只新增`pik`领域Definition、Bridge和Host Adapter
- **AND** 它 MUST复用独立package的Generator、packet、Session、Writer、Reader、codec和Finalizer

### Requirement: 迁移必须原子删除3C旧Owner

3C切换到KK package的同一迁移 MUST删除原`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、`Tools/ThirdPersonGeneratedDiagnosticSampling`及其Repository Policy allowlist，并重建全部受影响Generated Program与manifest identity。系统 MUST不保留旧新package并存、双Generator生成或运行时fallback。

#### Scenario: 3C Foot完成迁移

- **WHEN** 3C Foot插件已用KK package重新生成并通过Unity编译
- **THEN** 3C仓库 MUST不再跟踪旧package、旧Tools或ThirdPerson采样namespace
- **AND** Foot Capture Program、Schema、Player manifest与Capability Set MUST只保存新identity

### Requirement: 独立package必须保持领域中立

独立仓库 MUST不包含Foot、FinalIK、PoseGraph、Character、Performance Player、3C配置、`pik`算法或领域专属字段。框架扩展新领域时 MUST继续只通过具体View／Metadata、Attribute、Generated Program、typed packet和Host Adapter合同接入，不得在中央代码增加领域switch。

#### Scenario: 第三个项目消费package

- **WHEN** 与3C和`pik`无关的Unity项目声明新的Camera或AI诊断Capability
- **THEN** 它 MUST能够只依赖KK package生成自己的AOT采样程序
- **AND** 独立框架源码 MUST不因该领域名称或字段语义而修改
