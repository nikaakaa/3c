## Why

3C已经完成通用采样框架原型，但框架物理源码、Tools和`ThirdPerson.*`身份仍由3C持有，无法让独立`pik`或未来项目消费同一正式Owner。独立KK package由关联仓库change建立后，3C必须一次切换为纯消费者并删除本地框架Owner，避免复制、兼容层或双Generator。

## What Changes

- 本change只实施3C仓库内的消费者迁移；独立仓库由`establish-generated-diagnostic-sampling-package`负责，`pik`由`consume-generated-diagnostic-sampling-package`负责，三者共享同一migration identity和精确commit对账。
- 将3C Unity manifest与packages lock从`com.thirdperson.generated-diagnostic-sampling`切换到独立仓库的`com.kk.generated-diagnostic-sampling`正式file dependency。
- **BREAKING**：把3C Foot领域Definition、asmdef、using和Generated Program全部迁到`KK.GeneratedDiagnosticSampling*`，重建Generator／Schema／Program／Capability Set identity，不兼容旧请求或旧packet。
- Foot采样领域只保留三个typed生命周期Event、具体View／Metadata、Field／Table／Sampler／Program Attribute和下游Analyzer／Publisher；删除Foot Bridge、`hostAdapterId`、每Sampler Host Adapter、手写Column／CsvBinding和Session／Writer控制面。
- **BREAKING**：删除3C本地`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、`Tools/ThirdPersonGeneratedDiagnosticSampling`及Repository Policy allowlist，不保留snapshot、同步脚本、type forwarder或fallback。
- 更新3C的`add-generated-diagnostic-sampling-framework`、Foot与Performance active change，使框架Owner固定指向独立仓库，3C只描述领域消费与顶层编排。

## Capabilities

### New Capabilities

- `generated-diagnostic-sampling-external-consumption`: 定义3C如何只通过独立KK package消费生成采样能力、迁移Foot生命周期事件并彻底删除本地框架Owner。

### Modified Capabilities

无。Generated Diagnostic Sampling Framework尚未安装进current specs；本change只安装3C消费者边界，不复制独立package内部需求。

## Impact

- 3C manifest：`3cDemo/Client/3C_Client/Packages/manifest.json`与`packages-lock.json`。
- 3C Foot插件：`Assets/GameScripts/Main/Runtime/Character/Pipeline/Diagnostics/FootIkSampling`中的using、asmdef、生命周期Event和Generated Program。
- 删除路径：`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、`Tools/ThirdPersonGeneratedDiagnosticSampling`。
- 策略与文档：`Tools/CI/Test-RepositoryPolicy.ps1`及相关active changes。
- 外部前置：独立仓库必须先提供已构建、hash闭合且严格校验的`com.kk.generated-diagnostic-sampling` package；本change不创建或修改外部仓库和`pik`。
