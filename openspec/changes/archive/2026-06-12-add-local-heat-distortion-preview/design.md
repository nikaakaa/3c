## Context
项目已有 `urp-glitch-post-processing` 和 `urp-radial-blur-post-processing` 两条 URP 后处理能力。局部热力扭曲应沿用当前 `VolumeComponent -> Settings -> ScriptableRendererFeature -> ScriptableRenderPass -> Hidden shader` 的结构，避免 `OnRenderImage`、额外相机、独立 CommandBuffer 管线或临时角色脚本绕过现有系统。

## Goals / Non-Goals
- Goals: 提供可在 Unity Editor 中快速切换的局部热力扭曲候选效果。
- Goals: 保持配置层和渲染实现分离，Volume 只表达参数，RenderPass 只消费归一化后的 settings。
- Goals: 使用场景中的区域源表达一片空气扰动范围，让效果区别于 Glitch 的目标物体遮罩。
- Goals: 支持 ParticleSystem 作为可选可见层，用于展示热浪边缘、尘雾、风线和冲击线。
- Non-Goals: 本变更不把效果接入闪避、冲刺、攻击或状态机事件。
- Non-Goals: 本变更不新增角色控制器、动作系统入口或独立摄像机渲染路径。
- Non-Goals: 本变更不删除现有 log 或改动已有 Glitch/RadialBlur 行为。
- Non-Goals: 本变更不新增 VFX Graph 依赖；第一版使用 Unity 内置 ParticleSystem。

## Decisions
- Decision: 使用一个 `LocalHeatDistortion` VolumeComponent 表达所有候选模式。
- Alternatives considered: 为每种扭曲创建独立 VolumeComponent。该方案会增加配置碎片，并让后续接入动作事件时难以统一调度。

- Decision: 使用一个枚举参数选择候选模式，第一批包含 `HeatHaze`、`SpiralPressure`、`PulseShockwave`、`VerticalFlow`。
- Alternatives considered: 先只做一个热浪效果。该方案不满足“可以做多种扭曲我选一下”的预览目标。

- Decision: 使用 `LocalHeatDistortionAreaSource` 表达世界空间区域源，区域源负责范围、形状、模式覆盖和粒子引用，RenderPass 只消费区域源生成的屏幕空间区域参数或区域 Mask RT。
- Alternatives considered: 复用 Glitch 的目标 Renderer 遮罩。该方案会让热力扭曲看起来像物体局部故障，不符合“一片空气区域”的目标。

- Decision: 使用相机深度贴图对区域源中心深度做遮挡比较，墙体、地形等不透明物体在区域源前方时裁掉后处理扭曲。
- Alternatives considered: 将折射改成场景粒子 shader。该方案适合独立 VFX，但会绕过当前 `LocalHeatDistortion` 后处理链路，形成第二套热力扭曲路径。

- Decision: 区域源第一版支持圆形/椭圆屏幕投影和圆柱风压载体，满足地面热浪、角色周围风压和冲击圈预览。
- Alternatives considered: 一开始就支持任意 Mesh 体积。该方案更灵活，但会扩大实现和测试面。

- Decision: 粒子层使用内置 ParticleSystem，作为区域源的可选子对象或引用，不作为折射计算的唯一依据。
- Alternatives considered: 引入 VFX Graph。当前 manifest 未包含 VFX Graph 包，为预览阶段引入新依赖不合适。

- Decision: 不引入外部噪声贴图依赖，第一版 shader 内部生成噪声和流动扰动。
- Alternatives considered: 使用美术噪声贴图。该方案更利于最终质感，但会阻塞第一轮预览选择。

## Risks / Trade-offs
- 风格过强会影响玩家读招：默认参数必须很轻，手动预览时再提高强度。
- 区域 Mask 和全屏采样会增加一次颜色拷贝和一次区域绘制：通过激活判定、无区域源不入队、强度为 0 不入队降低成本。
- 深度遮挡会增加一次相机深度采样：仅在热力扭曲 pass 激活时发生，且沿用当前 URP 深度输入，不新增相机或独立渲染管线。
- 粒子过多会干扰战斗读招：预览 prefab 的默认发射率必须低，且能单独关闭粒子可见层。
- 多模式 shader 可能产生分支：第一版使用固定枚举参数控制，后续如果性能需要再拆 pass 或 shader variant。

## Migration Plan
1. 新增能力时不修改现有后处理行为。
2. 在三个质量档 URP Renderer Data 上追加局部热力扭曲 Feature。
3. 新增默认关闭的区域源预览 prefab，不接入角色动作。
4. 在 Sandbox Volume Profile 添加默认关闭的 `Local Heat Distortion` 配置。
5. 用户选定效果后，另起变更把选中的效果接入动作事件。

## Open Questions
- 最终默认候选模式由手动预览后决定。
- 区域源最终是否跟随角色骨骼、武器或动作事件，等选定效果后另起变更决定。
