# character-camera-pipeline Specification

## MODIFIED Requirements

### Requirement: Camera 必须是本地表现管线

系统 MUST将角色相机保持为 local-only Presentation/Committer port。Camera runtime state、mode、FOV、orbit、shake、recoil、blend progress 和 Cinemachine priority MUST不进入 CharacterSimulationState、WorldSimulationState、SimulationIngress、SimulationWorldSnapshot 或 model output port。明确需要复制的表现事件 MAY作为 PresentationSyncDomain fact 由具体 Model adapter消费，但 Camera resolver state 本身 MUST不成为同步事实。

#### Scenario: 本地技能特写

- **WHEN** committed command 请求 SkillCloseup
- **THEN** Camera port MUST在 PresentationFrame 本地切换或混合镜头
- **AND** model output adapter MUST不发送当前 Camera state

#### Scenario: 可复制表现事件

- **WHEN** 某个 cue policy 要求复制表现事件
- **THEN** model adapter MAY消费对应 PresentationSyncDomain fact
- **AND** CameraStage 本地状态 MUST不被复制
