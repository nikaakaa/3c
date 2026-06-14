# Change: 新增局部热力扭曲预览

## Why
高速 ACT 需要在冲刺、闪避、蓄力和冲击瞬间提供局部空气扭曲反馈。当前项目已有 URP Glitch 和径向模糊后处理，但还没有一个可在局部区域预览多种热力/风压扭曲风格的能力，无法快速比较哪种视觉语言更适合角色高速动作。

## What Changes
- 新增一个 URP 区域型热力扭曲预览能力，通过场景中的区域源定义一片空气扰动区域，并通过 Volume 参数控制全局强度和候选模式。
- 候选模式先包含热浪折射、螺旋风压、脉冲冲击、纵向上升气流四类，用同一套渲染入口切换，不新增分裂路径。
- 区域源使用独立的范围载体定义扭曲区域，不复用 Glitch 的目标 Renderer 遮罩语义。
- 支持可选 ParticleSystem 作为可见气流、尘雾、热浪边缘和冲击线提示；后处理负责折射，粒子负责让玩家看见这片空气区域。
- 支持不透明场景遮挡：当区域源位于墙体、地形或其他写入相机深度的不透明物体后方时，热力扭曲不会穿墙覆盖前景。
- 在 Sandbox 场景 Volume Profile 中提供可手动切换的预览配置，方便用户逐个比较效果。
- 提供 EditMode 测试覆盖参数归一化、激活判定、区域源配置、Renderer Feature 配置、质量档引用和示例预览配置。

## Impact
- Affected specs: `urp-local-heat-distortion-post-processing`
- Affected code: `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime`
- Affected shaders: `3cDemo/Client/3C_Client/Assets/Shader/PostProcessing`
- Affected prefabs/effects: `3cDemo/Client/3C_Client/Assets/Prefabs`、`3cDemo/Client/3C_Client/Assets/Effects`
- Affected tests: `3cDemo/Client/3C_Client/Assets/Tests/Editor/Rendering`
- Affected settings: URP Renderer Data、Sandbox Volume Profile
