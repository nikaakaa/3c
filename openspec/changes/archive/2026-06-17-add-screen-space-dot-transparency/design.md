## Context
这次效果要求“点固定在屏幕上，不跟着角色走”。因此点阵坐标不能来自模型 UV、物体空间或世界空间，而应来自当前相机裁剪后的屏幕像素坐标。当前场景角色材质主要使用 Haste 的 Diffuse 风格 shader，项目中也保留 `URPGenshinToon.shader`。

现有渲染能力中，URP 后处理通过 `VolumeComponent -> Settings -> ScriptableRendererFeature -> ScriptableRenderPass -> Hidden shader` 接入；但点状透明只应该影响使用该材质的角色像素，不应该污染整屏画面，也不应该为了局部遮罩新增相机或 Renderer Feature。

## Goals / Non-Goals
- Goal: 点阵固定在屏幕像素网格上，角色移动和动画不会让点阵跟着表面滑动。
- Goal: 透明表现使用 alpha clip，保留不透明/cutout 渲染语义，避免普通透明排序问题。
- Goal: 当前角色可见 pass 使用屏幕点阵裁剪；Toon 的 Forward、Outline、相机 DepthOnly 和 DepthNormals 使用一致裁剪。
- Goal: 参数通过正式配置和运行时写入通道控制，默认关闭，不静默使用 fallback 配置。
- Non-Goal: 不做点状透明阴影。
- Non-Goal: 不做全屏后处理版角色遮罩。
- Non-Goal: 不接入具体技能、隐身、受击或状态机触发。

## Decisions
- Decision: 在正式角色材质路径内实现第一版，而不是新增后处理。
  - Reason: 后处理要先生成角色 mask，再做屏幕裁剪，容易形成额外渲染路径；材质裁剪能直接限制在角色像素上。当前场景实际使用 Haste Diffuse 风格角色 shader，因此必须覆盖该路径，Toon 支持作为已有角色 shader 兼容保留。

- Decision: 使用 alpha clip / cutout，而不是 alpha blend。
  - Reason: 点状透明更接近二值遮罩，cutout 能减少透明排序、深度和角色自遮挡问题。

- Decision: 点阵采样使用屏幕像素坐标。
  - Reason: 像素坐标可以让点间距以屏幕尺寸表达，角色移动时网格不随模型坐标变化。

- Decision: 抽出共享 HLSL 点阵函数给 Haste Diffuse 风格角色 shader 和 Toon pass 使用。
  - Reason: 同一个效果不应在 Toon 和 Diffuse 路径各自维护一份公式；Toon 的身体、描边和相机深度仍必须一致，否则会出现可见洞口和深度/描边残留不匹配。

- Decision: ShadowCaster 第一版不裁剪。
  - Reason: ShadowCaster 的坐标空间来自光源视角，不等同于玩家屏幕空间；强行同步会变成另一套视觉语义，需要单独审批。

- Decision: 运行时使用正式配置和 `MaterialPropertyBlock` 写入目标 renderer。
  - Reason: 不修改 shared material，能让同一材质资产被多个角色复用，同时保持配置和实现分离。

## Parameters
- `_ScreenDotTransparencyEnabled`: 0/1，默认关闭。
- `_ScreenDotCoverage`: 0 到 1，表达整体点状透明强度。
- `_ScreenDotSpacingPixels`: 点阵间距，像素单位。
- `_ScreenDotRadius`: 单元格内点半径比例。
- `_ScreenDotHardness`: 边缘硬度。
- `_ScreenDotOffsetPixels`: 屏幕像素偏移，用于正式调试和镜头构图，不用于跟随角色。

实现阶段可以调整命名，但 MUST 保持语义清晰、默认关闭和测试覆盖。

## Risks / Trade-offs
- 风险: 小点阵会在运动中闪烁或 aliasing。
  - Mitigation: 配置最小点间距和半径范围，测试覆盖参数钳制。

- 风险: DepthOnly 或 DepthNormals 未同步裁剪会导致洞口遮挡背景或后处理误判。
  - Mitigation: 明确要求相机深度相关 pass 使用同一裁剪函数。

- 风险: Outline 未同步裁剪会残留完整描边。
  - Mitigation: Outline pass 接入同一裁剪函数。

- 风险: 直接修改角色材质资产会影响所有使用同一材质的实例。
  - Mitigation: 运行时写入使用 `MaterialPropertyBlock`，默认材质属性保持关闭。

## Validation Strategy
- OpenSpec 必须通过 `openspec validate add-screen-space-dot-transparency --strict --no-interactive`。
- 实现阶段添加 `ThirdPersonRendering.Tests.ScreenSpaceDotTransparencyTests`。
- 自动测试覆盖配置参数钳制、默认关闭、Haste Diffuse 风格角色 shader 与 Toon shader 属性存在、Forward/Outline/Depth pass 共享裁剪关键路径、ShadowCaster 不接入点阵裁剪、运行时参数通过 `MaterialPropertyBlock` 写入、预览入口默认关闭。
- 不使用 Unity batchmode。
