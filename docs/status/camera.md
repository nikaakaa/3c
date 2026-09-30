# 相机进展与证据入口

整理日期：2026-09-30。完整可琳相机复刻仍未完成。本页组织读取入口，资源数量、公式、配置和详细消费者核对由下面的原文拥有。

## 当前读取顺序

1. [最新实施记录](../diagnostics/camera/corin-camera-implementation-20260929.md)：当前交付、各批次修正和剩余范围。
2. [完整链路审计](../diagnostics/camera/corin-camera-completion-audit-20260929.md)：区分 Profile 常驻资源、静态引用和真正进入求值器的资源；这是审计时点的快照。
3. [震动时钟与升降资格](../diagnostics/camera/corin-camera-clock-and-vertical-20260929.md)：原生时钟、取消和资格证据。
4. [震动原生消费者依据](../reference/camera/corin-camera-shake-source-parity-20260928.md)：信号、空间衰减、仲裁与取证边界。

## 未闭环范围与职责

默认轨道 Follow/Aim 偏移的运行消费、Delay 原生模式资格与完整构图时序、震动静默/保持/取消的业务来源，以及 Zoom/Stretch 完整生命周期仍以最新实施和审计为准。A 类命中震动需要正式攻击查询、命中结果及相机请求生产者，不能由相机自行推断命中。支援、反击、切人和演出镜头还依赖相应上层业务入口。

相机实施由 [ZZZ Camera 重建](../../openspec/changes/rebuild-character-camera-from-zzz/proposal.md)拥有；正式合同见[相机管线](../../openspec/specs/character-camera-pipeline/spec.md)、[作者入口](../../openspec/specs/character-camera-authoring/spec.md)和[源数据对齐](../../openspec/specs/character-camera-source-parity/spec.md)。

早期接入、消费初查与分工证据移至[相机阶段记录](../archive/records/camera/)。实施报告、基础取证与震动取证统一从[相机诊断分类](../diagnostics/camera/README.md)读取；原始证据包内部内容未改。记录里的历史编译成功不代替当前画面、GC 或完整业务验收。
