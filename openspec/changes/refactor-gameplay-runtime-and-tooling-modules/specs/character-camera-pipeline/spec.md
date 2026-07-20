## MODIFIED Requirements

### Requirement: BTSMTL 和 Timeline 必须只提交相机请求

系统 MUST让 BTSMTL 自定义节点和 Timeline 相机轨道只提交强类型相机输出，包括 `CameraStateRequest`、`CameraCue`、`CameraResponsePolicy`、`CameraTargetRequest` 或读取 `CameraBasisSnapshot`。每个已公开 Camera Graph node MUST由唯一 Compiler emitter 降低为 versioned Program operation，并保留 Graph/Node authoring identity、端口与 Source Map；Float32与Fixed Target MUST按同一 operation 语义将其提交为现有 PresentationCommand。BTSMTL 节点、Timeline clip、compiled Camera operation 和 Action operation MUST NOT直接控制 Cinemachine、Unity Camera、camera Transform 或 virtual camera priority，也 MUST不把 Camera runtime state写入 Character/World simulation state。缺失字段、未知 operation 或 Target 未实现 MUST在 build/composition 明确失败，不得跳过或使用 runtime fallback。

#### Scenario: BTSMTL 请求瞄准相机

- **WHEN** Aim 状态中的 RequestCameraState node 通过 Character Simulation Compiler 编译
- **THEN** emitter MUST生成带稳定 Source Map 的 `CameraStateRequest(Aim)` Program operation
- **AND** Target leaf MUST通过 PresentationCommand 提交该请求
- **AND** 节点 MUST NOT调用 `CinemachineFreeLook`、`Camera.main` 或 scene camera object

#### Scenario: Timeline 触发技能特写

- **WHEN** Timeline camera clip 采样到 SkillCloseup 窗口
- **THEN** clip MUST输出 `CameraStateRequest(SkillCloseup)` 或等价 sample
- **AND** Timeline MUST NOT直接修改 Cinemachine virtual camera priority

#### Scenario: Camera node 缺少目标配置

- **WHEN** SetCameraTarget node 缺少正式 target identity或包含未知 target kind
- **THEN** Compiler preflight MUST报告 node source identity并拒绝生成 Program
- **AND** runtime MUST不把该 node 当成 Success 或选择默认 CameraTarget

#### Scenario: Fixed Target 编译 Camera operation

- **WHEN** Fixed Program 包含当前 operation-set version 的 Camera operation
- **THEN** Fixed Target MUST输出与 Float32 相同语义的强类型 PresentationCommand
- **AND** Camera request MUST不进入 deterministic CharacterState、WorldState或Snapshot
