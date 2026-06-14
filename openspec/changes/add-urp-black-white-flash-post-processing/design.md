## Context
项目已有 `RadialBlur`、`Glitch`、`LocalHeatDistortion`、`EdgeScan` 和 `BlockImpactPostProcess` 等 URP 后处理能力。它们主要沿用 `VolumeComponent -> Settings -> ScriptableRendererFeature -> ScriptableRenderPass -> Hidden shader` 的路径，并通过 EditMode 测试验证参数和 Renderer 接入。

黑白闪属于屏幕空间风格化冲击帧，不应由角色控制器、动作状态机或相机脚本直接实现。第一版应建立独立、可预览、可测试的渲染能力和手动播放入口；后续再用单独变更把格挡、受击、闪避或连招事件接入该能力。

## Goals / Non-Goals
- Goals: 新增一个默认关闭的 URP 黑白闪后处理。
- Goals: 支持全屏黑白闪和径向局部黑白闪。
- Goals: 支持通过可调曲线播放一次完整黑白闪。
- Goals: 在 Sandbox 中提供默认 Profile 和 Controller，使用户打开场景即可调参和手动播放。
- Goals: 保持配置抽象和渲染实现分离，使 Volume/Settings 负责参数表达和钳制，RenderPass/shader 只消费归一化数据。
- Goals: 提供自动测试和 Sandbox 手动验证。
- Non-Goals: 不接入格挡、受击、闪避、动作状态机、动画事件或网络同步。
- Non-Goals: 不新增输入绑定、UI 按钮、战斗事件桥接或自动命中特效触发。
- Non-Goals: 不新增目标 Rendering Layer Mask、Mask RT、深度法线描边或外部噪声贴图。
- Non-Goals: 不修改现有 `BlockImpactPostProcess`、`Glitch`、`RadialBlur` 的行为。

## Decisions
- Decision: 使用独立 `BlackWhiteFlash` 能力，而不是把黑白闪塞进 `BlockImpactPostProcess`。
  - Alternatives considered: 直接扩展 `BlockImpactPostProcess` 的 flash 分支。该方案会把“格挡/冲击局部表现”和“通用风格化冲击帧”绑死，不利于后续闪避、大招、处决等复用。
- Decision: 使用全屏 Render Pass 实现，视觉范围由 shader 中的全屏/径向 mask 控制。
  - Alternatives considered: 用球体、平面或角色材质表达黑白闪。该方案不能稳定影响整个画面，也容易绕开现有 URP 后处理体系。
- Decision: 使用 `VolumeComponent` 暴露设计参数，并通过 `Settings` 值结构钳制范围。
  - Alternatives considered: 由 MonoBehaviour 直接持有 shader 参数。该方案会让参数来源分散，难以测试，也不符合现有后处理模块模式。
- Decision: 使用 `BlackWhiteFlashProfile` 表达可调曲线和基础视觉参数，使用 `BlackWhiteFlashController` 只负责播放采样并写入已有 Volume。
  - Alternatives considered: 把曲线字段直接塞进 Renderer Feature。该方案会让渲染接入承担播放状态，破坏抽象分离，也不方便在场景中调参。
- Decision: Sandbox 的默认 Controller 挂在已有 `Global Volume` 上，引用当前 `SampleSceneProfile` 中的 Black White Flash 组件。
  - Alternatives considered: 新增一个独立预览场景或额外相机。该方案会形成额外验证路径，且不是当前用户想要的开箱调参方式。
- Decision: Renderer Feature 挂到当前三档 URP Renderer Data，Render Pass 默认使用 `BeforeRenderingPostProcessing`。
  - Alternatives considered: 使用 `AfterRenderingPostProcessing` 或 `OnRenderImage`。前者在当前项目已有经验中不如写回内置后处理前稳定，后者不符合 URP 统一路径。
- Decision: 第一版不做目标遮罩和深度/法线边缘。
  - Alternatives considered: 第一版直接复刻更完整的角色剪影、武器斜切和目标 Mask。该方案范围过大，会同时牵涉 Rendering Layer、Mask RT、动作触发和调参验证，审批与实现风险更高。

## Risks / Trade-offs
- Risk: 单纯黑白阈值可能看起来过硬或丢细节。
  - Mitigation: 暴露阈值、对比度、白场增强、暗部压黑和反白程度，先在 Sandbox 调整视觉基线。
- Risk: 全屏黑白闪可能过亮，影响可读性。
  - Mitigation: 强度默认 0，白场增强和反白程度必须钳制，手动验证包含强度归零恢复原画面。
- Risk: 第一版没有动作触发，不能直接在格挡/闪避时自动播放。
  - Mitigation: 本变更只建立渲染能力和手动曲线播放入口；动作事件接入另走 proposal，避免未审批地把渲染能力绑定到动作系统。
- Risk: Controller 写入共享 Volume Profile 时可能把预览参数留在资源里。
  - Mitigation: Controller 停止时默认把强度写回 0，并保留参数在同一个正式 Volume Profile 中，不创建 fallback 配置。

## Migration Plan
1. 新增黑白闪 Volume、Settings、Renderer Feature、Render Pass 和 Hidden shader。
2. 将 Renderer Feature 正式挂入 High Fidelity、Balanced、Performant 三档 URP Renderer Data。
3. 在 Sandbox Volume Profile 中添加默认关闭的 Black White Flash 参数。
4. 新增默认 `BlackWhiteFlashProfile` 资产和 Sandbox `BlackWhiteFlashController`。
5. 新增 EditMode 测试并运行定向测试。
6. 在 Sandbox 中手动验证全屏模式、径向局部模式、曲线播放和关闭恢复。

## Open Questions
- 是否需要第二阶段把黑白闪接入格挡/受击/闪避事件，由具体动作事件提交屏幕中心、强度和持续时间？
- 是否需要第二阶段增加目标遮罩，使黑白闪只作用于角色、武器或敌人轮廓？
