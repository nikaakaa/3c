# Change: 将 Corin 统一接入 ZZZMiyabi Toon 渲染

## 归档结果（2026-09-04）

已完成 15/15 项任务：Corin 四个正式材质统一使用 ZZZMiyabi Toon，配置 Corin 自己的色表和纹理语义，以 Head Bone 经 MaterialPropertyBlock 提供面部朝向，并消费场景主光。范围是当前本地 Corin 材质接入，不代表已经完整复刻 ZZZ 原始渲染，也不扩大原文中的素材使用范围。本次只归档文档，不提交 Shader、材质、贴图或角色代码。

本次按用户指令视为已验收并归档。下文 Why、方案过程及原始验证记录保留为变更历史；被后续方案替代的实现不再作为当前实施要求。


## Why

Corin 最初已有的 Toon 路线无法得到目标的《绝区零》材质层次。直接使用 ZZZMiyabi 后，Miyabi 的五区颜色参数被错误套给 Corin，造成身体、头发和武器偏色；Corin 又没有参考模型的朝向辅助骨骼，面部方向数据缺失。问题属于角色材质配置和接入数据，不应通过退回原 Shader 规避。

## What Changes

- 将 ZZZMiyabi 的 ZZZShader、Forward Pass、Outline Pass 与所需 ShaderLib 原始源码放入项目。
- Corin 身体、面部、头发和武器统一使用 `CelShaders/ZZZShader`，保留四个正式材质原 GUID。
- D 图作为 Albedo，N 图按 ZZZShader 的原始 RGB 解码方式导入，M 图按线性数据提供区域 ID、金属度、光滑度和高光。
- 不复用 Miyabi 的角色色表，为 Corin 的 Body、Hair、Weapon 与 Face 配置独立五区阴影色、浅阴色和轮廓参数。
- 使用 Corin 现有 Head Bone 计算 Forward/Left，通过 MaterialPropertyBlock 只写入 Face Renderer。
- 对 2D Face SDF 的七段衰减权重进行归一，避免 Corin face lightmap 在部分角度损失亮度。
- 恢复项目原有 Genshin Shader 与 Renderer 配置，不改变其他角色既有渲染链。

## Impact

影响 Corin 四个正式材质、三张 N 图与三张 M 图导入设置、六个 Corin Runtime Profile Prefab，以及新增的 ZZZMiyabi Shader 源码和 Corin 面部朝向组件。Gameplay、动画求值、Simulation、Rollback 和网络状态不变。

## 现行合同对照

- 当前 specs 没有角色 Toon Rendering capability，本变更新增 `character-toon-rendering`。
- 渲染只消费 Presentation 的 Head Bone 与 Renderer，不进入 Simulation Program 或 Rollback Snapshot。
- 项目继续使用现有 URP 14 Pipeline，不引入第二套正式 Pipeline Asset。
- ZZZMiyabi 仓库未提供许可证文件，本接入仅用于用户明确说明的不提交、本地录制场景，不声明可再分发授权。
