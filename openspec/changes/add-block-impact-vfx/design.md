## Context
参考截图中的格挡冲击不是单个素材，而是多个表现层叠加：命中点高亮爆闪、方向性火花、能量弧线或冲击环、横向强光带，以及屏幕空间短白闪、径向冲击和轻微色散。用户已经在 `Assets/Art/Tex/绝区零贴图` 导入了大量贴图，其中包含 `Eff_Block_01.png`、`Distortion_Hit_01.png`、`Distortion_Circle_*.png`、`Distortion_Trail_*.png`、`Eff_Arc_01.png`、`Eff_Camglow_01.png`、`Eff_Noise_*.png` 等候选素材。

当前工程已有 `ThirdPersonRendering` 命名空间下的 URP 后处理模式，例如 Radial Blur、Local Heat Distortion、Glitch，以及进行中的 Edge Scan。屏幕冲击部分应复用现有 URP Renderer Feature 模式。世界空间特效部分应作为表现层 Prefab，不读取状态机、动画事件或战斗对象。

## Goals / Non-Goals
- Goals: 提供一个开箱即用的 `BlockImpactVfx` Prefab。
- Goals: Prefab 可拖入场景手动触发，也可由后续代码实例化后调用公开播放入口。
- Goals: Prefab 自带正式默认配置和材质引用。
- Goals: 默认配置能看到命中爆闪、火花、弧线或冲击环、横向光带。
- Goals: 可选联动 URP 屏幕冲击后处理。
- Goals: 默认不会改变现有玩法逻辑。
- Non-Goals: 不实现真实格挡事件来源。
- Non-Goals: 不接动画事件或状态机。
- Non-Goals: 不强制引入 VFX Graph。

## Decisions
- Decision: 第一版交付 Prefab + Profile + 公开 Play 入口。
  - 原因: 用户需要之后开箱即用，Prefab 能把材质、粒子、贴片、生命周期和默认参数打包在一起，外部只负责传入命中点和方向。
  - Alternatives considered: 只写 shader 和后处理。该方案不能满足拖入场景直接使用，也缺少火花和贴片层。

- Decision: 播放入口不依赖动画事件。
  - 原因: 当前请求明确不要动画事件之类的接入。公开方法、Inspector 按钮和可选 PlayOnEnable 能覆盖预览和后续代码调用。
  - Alternatives considered: 在 AnimationClip Event 中触发。该方案会提前绑定动作资源和战斗流程，超出当前范围。

- Decision: 世界空间 VFX 和屏幕空间后处理分开建模。
  - 原因: 火花、星芒、弧线需要位于命中点附近；短白闪、径向冲击、横向 streak、色散需要全屏 pass。拆开后可以分别启停、调参和测试。
  - Alternatives considered: 全部做成全屏后处理。该方案无法表现方向性火花和命中点局部爆闪。

- Decision: URP 屏幕冲击沿用 Volume + Renderer Feature 链路。
  - 原因: 当前项目已经有同类后处理能力，统一链路能避免 `OnRenderImage`、相机叠加和独立渲染路径。
  - Alternatives considered: 在 Prefab 中挂相机脚本直接 Blit。该方案会分裂渲染路径，不符合当前后处理规格。

- Decision: 第一版使用 ParticleSystem、Billboard/Quad/Ribbon Mesh 和普通 shader，不强制 VFX Graph。
  - 原因: ParticleSystem 更容易和 Prefab 绑定，也更容易做 EditMode 结构测试和 Sandbox 验证。
  - Alternatives considered: 全部使用 VFX Graph。该方案表现上限更高，但会引入新的资源和运行时依赖，第一版不需要。

## Risks / Trade-offs
- Risk: Bloom 强度依赖当前 URP 后处理配置，手动预览可能在 Bloom 关闭时显得不够亮。
  - Mitigation: 手动验证中要求确认 Main Camera 和 Volume Profile 后处理状态，并提供强度归零恢复步骤。

- Risk: `add-urp-edge-scan-post-processing` 与本变更都会改三档 Renderer Data。
  - Mitigation: 实施时先检查 active change 的 Renderer Feature 序列化结果，修改时保留已有 feature，不覆盖同一 asset 中的其他变更。

- Risk: 贴图数量多，可能误用 UI、角色或非特效贴图。
  - Mitigation: 通过 `BlockImpactVfxProfile` 显式列出正式使用的贴图字段，并用测试检查必须引用的纹理不为空。

- Risk: Prefab 自动播放可能干扰场景。
  - Mitigation: Prefab 可以支持 `PlayOnEnable`，但 Sandbox 中的预览对象必须默认禁用或不自动重复触发。

## Migration Plan
1. 新增格挡冲击 Profile、shader、材质和 Prefab，不接现有战斗逻辑。
2. 新增 Sandbox 预览对象，用 Inspector 或测试入口手动触发。
3. 新增 URP 屏幕冲击后处理，默认关闭。
4. 后续若已有格挡事件规格，再另开 change 将真实格挡事件映射到 `BlockImpactVfx.Play(...)`。

## Open Questions
- 后续真实格挡事件来源尚未审批：需要另行确认由哪个战斗模块提供命中点、攻击方向、防御角色和完美格挡标记。
- hitstop 是否要纳入同一效果包尚未审批：第一版不暂停 simulation、不改动画速度。
