# URP Glitch 后处理使用与排查

本文给后续继续维护 Glitch 后处理时先读。当前实现属于 URP 自定义后处理路径，不新增相机脚本，不使用 `OnRenderImage`，不新增并行渲染出口。

## 相关文件

- `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime/Glitch.cs`
- `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime/GlitchSettings.cs`
- `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime/GlitchRendererFeature.cs`
- `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime/GlitchRenderPass.cs`
- `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime/GlitchMaskRenderPass.cs`
- `3cDemo/Client/3C_Client/Assets/Shader/PostProcessing/Glitch.shader`
- `3cDemo/Client/3C_Client/Assets/Shader/PostProcessing/GlitchMask.shader`
- `3cDemo/Client/3C_Client/Assets/Settings/URP-HighFidelity-Renderer.asset`
- `3cDemo/Client/3C_Client/Assets/Settings/URP-Balanced-Renderer.asset`
- `3cDemo/Client/3C_Client/Assets/Settings/URP-Performant-Renderer.asset`
- `3cDemo/Client/3C_Client/Assets/Settings/SampleSceneProfile.asset`

## 启用顺序

先验证全屏，再验证局部。

1. Main Camera 的 `Post Processing` 必须开启。
2. 当前 URP Pipeline 使用的 Renderer Data 必须包含启用状态的 `3C Glitch` Renderer Feature。
3. Volume Profile 中 `Glitch` 组件必须启用。
4. `Intensity` 勾选 override，并设置为大于 0。验证时可直接设为 `1`。
5. 先关闭 `Use Target Mask`，确认 Game View 出现全屏故障效果。
6. 全屏确认有效后，再开启 `Use Target Mask`。
7. 局部目标 Renderer 的 `Rendering Layer Mask` 必须勾选 `Glitch Target`。

注意：局部筛选用的是 Renderer 的 `Rendering Layer Mask`，不是普通 GameObject Layer。

## 推荐验证参数

全屏验证：

```text
Intensity = 1
Block Size = 80-120
Horizontal Jitter = 0.02-0.08
Rgb Split = 0.01-0.04
Scan Line Intensity = 0.2-1
Speed = 15-60
Use Target Mask = false
```

局部验证：

```text
Use Target Mask = true
Mask Influence = 1
Mask Expansion = 0.04-0.12
目标 Renderer: Rendering Layer Mask 勾选 Glitch Target
```

## 当前实现约束

- `GlitchRendererFeature` 的 injection point 应保持为 `BeforeRenderingPostProcessing`。
- 三档 Renderer asset 中 Glitch 的 `injectionPoint` 应保持为 `550`。
- `GlitchRenderPass` 构造时应调用 `ConfigureInput(ScriptableRenderPassInput.Color)`。
- 主 pass 使用 `Blitter.BlitCameraTexture(copyTexture, source, material, 0)` 写回相机颜色目标。
- 不要把主 pass 移回 `AfterRenderingPostProcessing`，之前该阶段会导致效果看起来完全没有写到画面。
- `GlitchMaskRenderPass` 的 Mask RT 保持 `R8_UNorm`、`msaaSamples = 1`、`depthBufferBits = 0`。
- Mask pass 只绑定自己的 Mask RT 并清黑，不绑定相机 depth target。
- `GlitchMask.shader` 使用 `ZTest Always`，只负责把目标 Renderer 轮廓写入 Mask RT。
- 局部 mask 通过 `FilteringSettings` 的 Rendering Layer mask 过滤，目前默认目标位是 `2`，对应 `Glitch Target`。

## 排查顺序

### 完全没有效果

优先按这个顺序查：

1. Volume stack 中 `Glitch.IsActive()` 是否为 true。
2. `Intensity` 是否 override 且大于 0。
3. Main Camera 的 `Post Processing` 是否开启。
4. 当前激活的 URP Pipeline 是否使用包含 `3C Glitch` 的 Renderer Data。
5. `3C Glitch` Renderer Feature 是否 active。
6. Glitch 的 `injectionPoint` 是否为 `550`。
7. `GlitchRenderPass` 是否仍使用 `Blitter.BlitCameraTexture` 写回。
8. Console 是否有 shader 或编译错误。

可在 Unity Editor 中用临时探针确认当前状态：

```csharp
var cam = UnityEngine.Camera.main;
var camData = cam ? cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() : null;
var glitch = UnityEngine.Rendering.VolumeManager.instance.stack?.GetComponent<ThirdPersonRendering.Glitch>();
UnityEngine.Debug.Log($"postProcess={camData?.renderPostProcessing}, glitchActive={glitch?.IsActive()}, intensity={glitch?.intensity.value}, mask={glitch?.useTargetMask.value}");
```

### 全屏有效，局部无效

按这个顺序查：

1. `Use Target Mask` 是否 override 且为 true。
2. 目标角色所有参与显示的 `Renderer` / `SkinnedMeshRenderer` 是否勾选 `Glitch Target`。
3. Renderer asset 中 `targetRenderingLayerMask` 是否为 `2`。
4. Mask shader 是否仍是 `Hidden/3C/PostProcessing/GlitchMask`。
5. `GlitchMask.shader` 是否仍为 `ZTest Always`。
6. Mask pass 是否没有绑定相机 depth target。

### 画面变白或 mask 材质露到相机颜色里

这通常说明 mask pass 的目标绑定错了。重点查：

1. Mask pass 是否只 `SetRenderTarget(maskTexture, ClearFlag.Color, Color.black)`。
2. Mask pass 绘制后是否调用 `ResetTarget()`。
3. 主 pass 和 mask pass 是否处在同一个有效的 injection point。
4. 是否有人把 mask pass 又改回依赖 `ConfigureTarget` 或相机 depth target。

## 不要绕开的边界

- 不要新增相机专用脚本来驱动 Glitch。
- 不要用 `OnRenderImage`。
- 不要新增第二条渲染出口。
- 不要把局部目标改成普通 GameObject Layer，避免和碰撞、射线、AI、交互层冲突。
- 不要删除现有 log，除非用户明确要求。

## 手动验收

全屏验收：

1. 打开 `Assets/Scenes/Sandbox.unity`。
2. 确认 Main Camera 开启 `Post Processing`。
3. 在 `SampleSceneProfile` 中启用 `Glitch`，设置 `Intensity = 1`。
4. 关闭 `Use Target Mask`。
5. Game View 应出现明显横向错位、RGB 分离或扫描线变化。

局部验收：

1. 保持全屏验收已通过。
2. 开启 `Use Target Mask`。
3. 目标 Renderer 勾选 `Glitch Target`。
4. Game View 应只在目标物体附近出现故障效果，背景不应全屏污染。
