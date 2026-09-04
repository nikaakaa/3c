# character-toon-rendering Specification

## Purpose

定义 Corin 在现有 URP 中使用 ZZZMiyabi Toon 的唯一材质接入、纹理与色表语义、面部朝向输入和场景主光边界；不把当前角色接入等同于完整复刻 ZZZ 原始渲染。

## Requirements

### Requirement: Corin MUST 使用唯一 ZZZMiyabi Toon 渲染链

所有正式 Corin Runtime Profile MUST 通过 `CelShaders/ZZZShader` 材质与现有项目 URP Renderer 渲染。Corin MUST NOT 同时保留 Genshin、ASP 或旧 Simple Toon 回退路径。

#### Scenario: 任一正式 Corin 角色进入 Gameplay

- **WHEN** Local、Rollback 或 ServerAuthoritative Runtime Profile 实例化 Corin
- **THEN** 身体、面部、头发和武器 MUST 使用保持原 GUID 的 ZZZShader 材质
- **AND** 角色轮廓 MUST 由 ZZZShader 的 Outline Pass 产生

### Requirement: Corin 材质 MUST 按 ZZZMiyabi 语义消费贴图

Body、Hair 和 Weapon MUST 使用各自 D、N、M 图。Face MUST 使用 Corin Face D 与 face lightmap。N 图 MUST 作为 Linear Default Texture 供 Shader 自行解码，M 图 MUST 作为 Linear 数据读取。

#### Scenario: Corin 各表面完成材质采样

- **WHEN** ZZZShader 计算 Corin 表面
- **THEN** D 图 MUST 提供 Albedo
- **AND** N 图 RGB MUST 提供切线法线与 Diffuse Bias
- **AND** M 图 R/G/B/A MUST 分别按仓库定义提供区域 ID、金属度、光滑度和高光
- **AND** 阴影色与浅阴色 MUST 来自 Corin 自己的五区色表，不得复用 Miyabi 色表

### Requirement: 面部方向 MUST 属于 Unity 表现

每个正式 Corin Runtime Profile MUST 只存在一个 CorinZZZFaceOrientation，并显式绑定 Head Bone 与 Face Renderer。方向更新 MUST 通过 MaterialPropertyBlock 完成，且 MUST NOT 创建材质实例或写入 Gameplay、Simulation、Rollback 状态。

#### Scenario: 动画旋转 Corin 头部

- **WHEN** Head Bone 方向随 Presentation 动画变化
- **THEN** Face Renderer 的 `_HeadForward` 与 `_HeadLeft` MUST 随表现帧更新
- **AND** 共享材质与 Gameplay 状态 MUST 不因该更新发生变化

### Requirement: ZZZ 光照 MUST 来自场景主光

录像场景 MUST 配置一个可用 Directional Light。ZZZShader MUST 使用 URP Main Light 的方向和颜色，不得通过材质内固定方向或无光 fallback 代替场景光。

#### Scenario: 调整录像场景 Directional Light

- **WHEN** 作者旋转 Directional Light
- **THEN** Corin 身体分区明暗与 Face SDF 阴影 MUST 一致随主光改变
