# Change: 重构格挡冲击特效为立体分层表现

## Why
当前 `add-block-impact-vfx` 已经提供了能播放的 Prefab、Profile、材质、shader、后处理和 Sandbox 入口，但实际观感仍偏“贴图板子”：横向亮光、中心 Bloom、弧线和火花看起来像几张透明图叠在相机前，没有参考图里的空间层次、冲击方向和强烈屏幕光感。

参考图里的完整格挡冲击是多层表现组合，但当前阶段不追完整复刻，先收缩为最关键的轻量反馈：中心过曝 Bloom、屏幕空间横向强光、方向性速度火花。这个变更用于纠正现有“贴图板子”问题，继续使用同一个 `BlockImpactVfx` 入口，不新增分裂的第二套格挡特效。

## What Changes
- 将 `BlockImpactVfx` 从“若干贴图面片叠加”收缩为“中心 Bloom + 屏幕空间横光 + 方向火花”的轻量表现。
- 横条强光改为 URP 屏幕空间 anamorphic streak，由 Render Feature/Render Pass 的 Hidden shader 根据屏幕中心、强度、宽度和衰减生成，不再依赖一个世界空间矩形板子承担主效果。
- 中心亮光改为 HDR 核心闪光 + 屏幕脉冲共同驱动 Bloom，保留可选贴图 mask，但避免可见方形边界。
- 火花改为方向性 ParticleSystem burst，使用 Stretched Billboard、速度对齐、Trail 模块、重力和速度衰减表现有物理感的高速金属火花，而不是静态点状贴图。
- 贴图定位为 mask/noise/spark/trail/arc alpha，不作为完整最终画面的“截图贴片”。
- 保持 Prefab 开箱即用、Inspector 预览、无动画事件依赖、无真实格挡判定依赖。
- 增加自动测试和手动验证，明确如何检查“不是方片”“横条来自后处理”“火花有方向”“弧线有深度”。

## 会用到的技术
- URP `ScriptableRendererFeature` / `ScriptableRenderPass`：复用现有后处理链路生成屏幕空间横向强光、短白闪、轻微色散和冲击脉冲。
- Hidden full-screen shader：用屏幕 UV、冲击中心、水平高斯衰减、HDR 强度和时间包络生成横条亮光。
- ParticleSystem Renderer `Stretched Billboard`、Trails、gravity modifier 和 velocity damping：做有方向、有拖尾、有下坠/衰减的火花。
- Unlit Additive HDR shader：中心核和火花使用加色、生命周期 fade、软边 mask。
- ScriptableObject Profile：正式保存贴图引用、颜色、强度、层级开关和生命周期参数，不写运行时路径，不做临时 fallback。
- EditMode 测试：验证 Prefab 结构、材质/shader 属性、Renderer Feature 配置、粒子渲染模式、无动画事件依赖和手动验证入口。

## Impact
- Affected active change: `add-block-impact-vfx`，本变更是它的表现架构纠偏，实施时应改同一套 Prefab/Profile/Controller/PostProcess，不创建并行的 `BlockImpactVfx2`。
- Affected specs: `block-impact-vfx`
- Affected code: `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime`
- Affected shaders: `3cDemo/Client/3C_Client/Assets/Shader/VFX/BlockImpact`, `3cDemo/Client/3C_Client/Assets/Shader/PostProcessing/BlockImpact`
- Affected assets: `3cDemo/Client/3C_Client/Assets/Prefabs/Rendering`, `3cDemo/Client/3C_Client/Assets/Settings`, `3cDemo/Client/3C_Client/Assets/Art/Tex/绝区零贴图`
- Affected tests: `3cDemo/Client/3C_Client/Assets/Tests/Editor/Rendering`

## Non-Goals
- 不实现真实格挡判定、完美格挡窗口、受击盒、伤害结算或敌人攻击系统。
- 不接动画事件、Timeline Signal、AnimationClip Event、FullBody Action 状态机或输入系统。
- 不实现 hitstop、时间缩放、动作暂停或相机模式切换。
- 不新增第二套格挡 VFX 入口、第二个 Prefab 主线或绕过现有渲染系统的配置。
- 不新增独立相机、相机叠加、`OnRenderImage` 或独立 CommandBuffer 主路径。
- 不实现 arc/ribbon、ring、切片等完整参考图层；这些层以后需要再单独审批。
- 不强制引入 VFX Graph；除非后续单独审批，否则第一轮用 ParticleSystem 和 shader 完成。
- 不删除现有 log。

## Open Questions
- 当前按“已有贴图足够做 mask/noise/spark/trail/arc”规划；如果用户后续能提供更接近参考图的横向 flare mask、星芒 mask 或火花序列图，可以作为正式 Profile 贴图替换，但不是本方案的阻塞项。
- 当前按 URP 后处理在内置 Bloom 前执行规划；实施时需要检查项目当前 Renderer Feature 顺序，确保横向强光能被 Bloom 接住。
