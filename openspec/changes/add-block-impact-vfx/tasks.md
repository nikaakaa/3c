## 1. 规格和边界
- [x] 1.1 确认第一版交付开箱即用 `BlockImpactVfx` Prefab。
- [x] 1.2 确认 Prefab 不依赖 AnimationClip Event。
- [x] 1.3 确认 Prefab 不依赖 Timeline Signal。
- [x] 1.4 确认 Prefab 不依赖真实格挡判定。
- [x] 1.5 确认 Prefab 不接入输入系统。
- [x] 1.6 确认 Prefab 不修改 FullBody、Locomotion、Action、回滚或网络。
- [x] 1.7 确认屏幕冲击只走 URP Renderer Feature 链路。
- [x] 1.8 确认不新增相机脚本、相机叠加、`OnRenderImage` 或独立 CommandBuffer 主路径。
- [x] 1.9 确认不删除现有 log。
- [x] 1.10 确认与 `add-urp-edge-scan-post-processing` 的 Renderer Data 修改不互相覆盖。

## 2. 贴图和配置资产
- [x] 2.1 盘点 `Assets/Art/Tex/绝区零贴图` 中适合格挡冲击的贴图。
- [x] 2.2 选择中心爆闪贴图。
- [x] 2.3 选择火花或线状拖尾贴图。
- [x] 2.4 选择能量弧线贴图。
- [x] 2.5 选择冲击圆环或裂纹贴图。
- [x] 2.6 选择横向光带或相机 glow 贴图。
- [x] 2.7 选择噪声或扭曲贴图。
- [x] 2.8 新增 `BlockImpactVfxProfile` 配置资产类型。
- [x] 2.9 在配置中暴露爆闪贴图字段。
- [x] 2.10 在配置中暴露火花贴图字段。
- [x] 2.11 在配置中暴露弧线贴图字段。
- [x] 2.12 在配置中暴露圆环贴图字段。
- [x] 2.13 在配置中暴露光带贴图字段。
- [x] 2.14 在配置中暴露噪声或扭曲贴图字段。
- [x] 2.15 在配置中暴露颜色和 HDR 强度参数。
- [x] 2.16 在配置中暴露持续时间参数。
- [x] 2.17 在配置中暴露粒子数量参数。
- [x] 2.18 在配置中暴露粒子速度参数。
- [x] 2.19 在配置中暴露粒子寿命参数。
- [x] 2.20 在配置中暴露火花喷射角度参数。
- [x] 2.21 在配置中暴露弧线数量参数。
- [x] 2.22 在配置中暴露光带尺寸参数。
- [x] 2.23 增加配置校验：必需贴图为空时报错。
- [x] 2.24 增加配置校验：数值参数钳制到安全范围。
- [x] 2.25 创建正式默认 `BlockImpactVfxProfile` 资产。

## 3. 播放请求和控制组件
- [x] 3.1 新增 `BlockImpactVfxRequest`。
- [x] 3.2 请求包含世界命中点。
- [x] 3.3 请求包含攻击方向。
- [x] 3.4 请求包含屏幕中心。
- [x] 3.5 请求包含强度。
- [x] 3.6 请求包含持续时间。
- [x] 3.7 请求包含随机种子。
- [x] 3.8 请求包含爆闪层启用开关。
- [x] 3.9 请求包含火花层启用开关。
- [x] 3.10 请求包含弧线层启用开关。
- [x] 3.11 请求包含光带层启用开关。
- [x] 3.12 请求包含屏幕冲击启用开关。
- [x] 3.13 提供请求默认值。
- [x] 3.14 提供请求参数规范化。
- [x] 3.15 确认请求不包含 Unity 场景对象引用。
- [x] 3.16 新增 `BlockImpactVfxController`。
- [x] 3.17 Controller 暴露 `Play(BlockImpactVfxRequest request)`。
- [x] 3.18 Controller 暴露 Inspector 手动预览入口。
- [x] 3.19 Controller 支持 `PlayOnEnable` 可选项。
- [x] 3.20 Controller 支持 `Stop`。
- [x] 3.21 Controller 支持重播。
- [x] 3.22 Controller 播放结束后进入待机。

## 4. 世界空间 shader
- [x] 4.1 新增 `BlockImpactAdditive.shader`。
- [x] 4.2 使用 Unlit 透明渲染。
- [x] 4.3 使用 Additive 混合。
- [x] 4.4 关闭 ZWrite。
- [x] 4.5 暴露 `_BaseMap`。
- [x] 4.6 暴露 `_TintColor`。
- [x] 4.7 暴露 `_Intensity`。
- [x] 4.8 暴露 `_Alpha`。
- [x] 4.9 暴露 `_UvScaleOffset`。
- [x] 4.10 新增 `BlockImpactSpark.shader`。
- [x] 4.11 支持 ParticleSystem 顶点颜色。
- [x] 4.12 支持拖尾渐隐。
- [x] 4.13 新增 `BlockImpactArc.shader`。
- [x] 4.14 支持噪声滚动。
- [x] 4.15 支持 dissolve。
- [x] 4.16 支持边缘强度。
- [x] 4.17 确认 shader 不使用缺失贴图 fallback。

## 5. 材质和 Prefab
- [x] 5.1 创建爆闪材质。
- [x] 5.2 创建横向光带材质。
- [x] 5.3 创建火花材质。
- [x] 5.4 创建火花拖尾材质。
- [x] 5.5 创建弧线材质。
- [x] 5.6 创建冲击环材质。
- [x] 5.7 创建 `BlockImpactVfx.prefab`。
- [x] 5.8 Prefab 添加中心爆闪子对象。
- [x] 5.9 Prefab 添加横向光带子对象。
- [x] 5.10 Prefab 添加冲击环子对象。
- [x] 5.11 Prefab 添加弧线子对象。
- [x] 5.12 Prefab 添加火花 ParticleSystem。
- [x] 5.13 Prefab 添加火花 Trail 配置。
- [x] 5.14 Prefab 添加 `BlockImpactVfxController`。
- [x] 5.15 Prefab 绑定默认 `BlockImpactVfxProfile`。
- [x] 5.16 Prefab 默认参数能直接播放可见效果。
- [x] 5.17 Prefab 不需要动画事件即可播放。

## 6. 实例调度
- [x] 6.1 新增可选 `BlockImpactVfxSpawner`。
- [x] 6.2 Spawner 支持绑定 Prefab。
- [x] 6.3 Spawner 支持提交播放请求。
- [x] 6.4 Spawner 根据请求生成或复用实例。
- [x] 6.5 Spawner 限制同时活跃实例数量。
- [x] 6.6 Spawner 缺 Prefab 时报告错误。
- [x] 6.7 Spawner 不读取或修改状态机对象。

## 7. Sandbox 预览
- [x] 7.1 新增 `BlockImpactVfxPreview`。
- [x] 7.2 支持 Inspector 手动触发。
- [x] 7.3 支持配置预览命中点。
- [x] 7.4 支持配置预览方向。
- [x] 7.5 支持配置预览强度。
- [x] 7.6 支持配置预览持续时间。
- [x] 7.7 支持自动重复预览开关。
- [x] 7.8 自动重复预览默认关闭。
- [x] 7.9 将预览对象加入 Sandbox。
- [x] 7.10 预览对象默认禁用或不自动触发。
- [x] 7.11 预览对象不接入输入系统。

## 8. 屏幕冲击设置和 Volume
- [x] 8.1 新增 `BlockImpactPostProcessSettings`。
- [x] 8.2 定义全局强度范围。
- [x] 8.3 定义白闪强度范围。
- [x] 8.4 定义径向冲击强度范围。
- [x] 8.5 定义横向光带强度范围。
- [x] 8.6 定义色散强度范围。
- [x] 8.7 定义冲击半径范围。
- [x] 8.8 定义采样次数范围。
- [x] 8.9 定义默认关闭 settings。
- [x] 8.10 实现 `IsActive` 判定。
- [x] 8.11 新增 `BlockImpactPostProcess` VolumeComponent。
- [x] 8.12 暴露全局强度。
- [x] 8.13 暴露白闪强度。
- [x] 8.14 暴露径向冲击强度。
- [x] 8.15 暴露横向光带强度。
- [x] 8.16 暴露色散强度。
- [x] 8.17 暴露冲击半径。
- [x] 8.18 暴露采样次数。
- [x] 8.19 默认不激活。

## 9. 屏幕冲击运行时和 Render Pass
- [x] 9.1 新增运行时脉冲状态类。
- [x] 9.2 支持提交屏幕中心。
- [x] 9.3 支持提交脉冲强度。
- [x] 9.4 支持提交持续时间。
- [x] 9.5 支持测试可控 Tick。
- [x] 9.6 新增 `BlockImpactPostProcessRendererFeature`。
- [x] 9.7 新增 `BlockImpactPostProcessRenderPass`。
- [x] 9.8 settings 或脉冲无效时不入队。
- [x] 9.9 shader 缺失时不入队。
- [x] 9.10 RenderPass 使用当前相机颜色。
- [x] 9.11 RenderPass 写回当前相机颜色目标。
- [x] 9.12 RenderPass 在内置后处理之前执行。
- [x] 9.13 新增 `Hidden/3C/PostProcessing/BlockImpact` shader。
- [x] 9.14 shader 支持径向采样。
- [x] 9.15 shader 支持白闪。
- [x] 9.16 shader 支持横向 streak。
- [x] 9.17 shader 支持 RGB 色散。
- [x] 9.18 强度为 0 时输出原始颜色。

## 10. URP 配置资产
- [x] 10.1 将格挡屏幕冲击 Renderer Feature 接入 High Fidelity Renderer Data。
- [x] 10.2 将格挡屏幕冲击 Renderer Feature 接入 Balanced Renderer Data。
- [x] 10.3 将格挡屏幕冲击 Renderer Feature 接入 Performant Renderer Data。
- [x] 10.4 保留已有 Radial Blur Feature。
- [x] 10.5 保留已有 Local Heat Distortion Feature。
- [x] 10.6 保留已有 Glitch Feature。
- [x] 10.7 保留已有或进行中的 Edge Scan Feature。
- [x] 10.8 在 SampleSceneProfile 或 Sandbox 使用的 Volume Profile 中加入默认关闭参数。

## 11. 自动测试
- [x] 11.1 新增 `BlockImpactVfxTests`。
- [x] 11.2 测试请求默认值。
- [x] 11.3 测试请求强度钳制。
- [x] 11.4 测试请求持续时间钳制。
- [x] 11.5 测试请求方向归一化。
- [x] 11.6 测试请求不包含 Unity 场景对象引用。
- [x] 11.7 测试 profile 必需贴图校验。
- [x] 11.8 测试 profile 数值参数钳制。
- [x] 11.9 测试 shader 关键属性。
- [x] 11.10 测试 Prefab 包含爆闪、光带、火花、弧线或冲击环。
- [x] 11.11 测试 Prefab 包含 Controller 和默认 Profile。
- [x] 11.12 测试 Prefab 不依赖动画事件。
- [x] 11.13 测试 Sandbox 包含默认关闭预览入口。
- [x] 11.14 新增 `BlockImpactPostProcessingTests`。
- [x] 11.15 测试屏幕冲击默认 settings 不激活。
- [x] 11.16 测试屏幕冲击正强度激活。
- [x] 11.17 测试屏幕冲击参数钳制。
- [x] 11.18 测试运行时脉冲提交和衰减。
- [x] 11.19 测试 RendererFeature 缺 shader 时不渲染。
- [x] 11.20 测试三档 Renderer Data 引用 Feature 和 shader。
- [x] 11.21 测试 shader 包含径向采样、白闪、streak、色散关键路径。

## 12. 手动验证
- [x] 12.1 打开 `Assets/Scenes/Sandbox.unity`。
- [x] 12.2 确认 Main Camera 开启 Post Processing。
- [x] 12.3 启用格挡冲击预览对象。
- [x] 12.4 确认预览对象绑定 `BlockImpactVfx.prefab`。
- [x] 12.5 触发一次格挡冲击预览。
- [x] 12.6 确认命中点出现强爆闪。
- [x] 12.7 确认火花沿攻击反方向或切线喷射。
- [x] 12.8 确认弧线或冲击环短暂出现。
- [x] 12.9 确认横向光带短暂出现。
- [x] 12.10 启用屏幕冲击全局强度。
- [x] 12.11 再次触发预览，确认短白闪、径向冲击、横向光带或轻微色散出现。
- [x] 12.12 将 VFX 强度设为 0，确认世界空间特效消失。
- [x] 12.13 将屏幕冲击强度设为 0，确认后处理恢复无冲击。
- [x] 12.14 将 `BlockImpactVfx.prefab` 拖入空场景，确认无需动画事件也能通过 Inspector 预览播放。

## 13. 校验
- [x] 13.1 运行 `openspec validate add-block-impact-vfx --strict --no-interactive`。
- [x] 13.2 使用 Unity Test Runner 运行 `ThirdPersonRendering.Tests.BlockImpactVfxTests`。
- [x] 13.3 使用 Unity Test Runner 运行 `ThirdPersonRendering.Tests.BlockImpactPostProcessingTests`。
- [x] 13.4 不运行 Unity batchmode。
- [x] 13.5 把手动验证方式告诉用户，包括怎么触发、怎么关闭、怎么确认不依赖动画事件。
