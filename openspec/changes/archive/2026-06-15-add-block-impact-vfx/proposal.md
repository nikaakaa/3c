# Change: 新增开箱即用格挡冲击特效 Prefab

## Why
需要基于 `Assets/Art/Tex/绝区零贴图` 中已导入的特效贴图，做一个可直接拖入场景或由代码实例化播放的格挡冲击特效。第一版目标是表现层开箱即用：包含命中爆闪、火花、弧线、横向光带和可选屏幕冲击后处理，但不接动画事件、格挡判定、状态机或输入系统。

## What Changes
- 新增 `BlockImpactVfx` 开箱即用 Prefab，包含默认配置、材质、粒子、贴片层、生命周期控制和公开播放入口。
- 新增格挡冲击素材配置资产，正式引用 `Assets/Art/Tex/绝区零贴图` 下的爆闪、火花、弧线、光带、扭曲贴图，不写死运行时路径，不提供临时 fallback 配置。
- 新增 Additive 爆闪/光带 shader、火花/拖尾 shader、能量弧线 shader，用于 Prefab 的分层表现。
- 新增纯表现层播放请求和控制组件，使外部代码可以通过世界命中点、方向、强度和持续时间播放一次特效。
- 新增 Sandbox 手动预览对象，用于不接战斗逻辑时验证 Prefab 的开箱即用效果。
- 新增可选 URP 格挡屏幕冲击后处理，通过当前 `ScriptableRendererFeature -> ScriptableRenderPass -> Hidden shader` 链路实现短白闪、径向冲击、横向 streak 和轻微色散。
- 提供 EditMode 测试覆盖配置校验、shader 关键属性、Prefab 结构、播放请求、生命周期、URP Renderer Data 接入和 Sandbox 手动预览配置。
- 提供用户可执行的手动验证步骤，验证如何拖入、触发、调参、关闭效果。

## Impact
- Affected specs: `block-impact-vfx`, `urp-block-impact-post-processing`
- Affected code: `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime`，以及表现层 VFX 运行时代码目录
- Affected shaders: `3cDemo/Client/3C_Client/Assets/Shader/VFX/BlockImpact`, `3cDemo/Client/3C_Client/Assets/Shader/PostProcessing/BlockImpact`
- Affected assets: `3cDemo/Client/3C_Client/Assets/Art/Tex/绝区零贴图`, `3cDemo/Client/3C_Client/Assets/Prefabs/Rendering`, `3cDemo/Client/3C_Client/Assets/Settings`
- Affected tests: `3cDemo/Client/3C_Client/Assets/Tests/Editor/Rendering`
- Related active change: `add-urp-edge-scan-post-processing` 也会修改三档 URP Renderer Data，实施时需要避免覆盖彼此配置。

## Non-Goals
- 不在本变更中实现真实格挡判定、完美格挡窗口、受击盒、伤害结算或敌人攻击系统。
- 不在本变更中接入动画事件、Timeline Signal、AnimationClip Event、FullBody Action 状态机或输入缓冲。
- 不在本变更中实现 hitstop、时间缩放、动作暂停或相机模式切换。
- 不新增独立相机、相机叠加、`OnRenderImage`、独立 CommandBuffer 管线或绕过当前 URP 的后处理路径。
- 不删除现有 log，不改动现有动作、移动、回滚或网络同步逻辑。
