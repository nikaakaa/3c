# Change: 屏幕空间点阵透明角色材质

## Why
当前需要一种角色移动时点阵固定在屏幕上的透明表现，用于做“角色经过固定网点遮罩”的视觉效果。普通半透明会引入排序和深度问题，世界/模型空间点阵又会跟着角色表面移动，不符合这次需求。

## What Changes
- 新增正式角色材质的屏幕空间点阵透明能力，覆盖当前场景使用的 Haste Diffuse 风格角色 shader，并保留 Toon shader 支持；点阵以屏幕像素网格为锚点，不跟随角色骨骼、UV 或世界位置移动。
- 使用 alpha clip / cutout 思路实现点状透明，不把角色材质切到普通 Transparent 混合队列。
- 通过正式配置和运行时参数驱动点阵间距、覆盖强度、半径和硬度，shader 只消费归一化参数。
- Forward、Outline、相机 DepthOnly 和 DepthNormals 路径使用同一套点阵裁剪语义，避免身体、描边和相机深度不一致。
- ShadowCaster 第一版保持现有实心阴影，不新增屏幕空间点阵阴影路径。
- 提供默认关闭的预览入口和 EditMode 自动测试，覆盖参数、shader 属性、材质/预览配置和边界限制。

## Impact
- Affected specs: `screen-space-dot-transparency`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Shader/二次元/UnityGenshinToonShader-main/Shaders/URPGenshinToon.shader`
  - `3cDemo/Client/3C_Client/Assets/Shader/二次元/UnityGenshinToonShader-main/Shaders/ToonInput.hlsl`
  - `3cDemo/Client/3C_Client/Assets/Shader/二次元/UnityGenshinToonShader-main/Shaders/ToonForwardPass.hlsl`
  - `3cDemo/Client/3C_Client/Assets/Shader/二次元/UnityGenshinToonShader-main/Shaders/ToonOutlinePass.hlsl`
  - `3cDemo/Client/3C_Client/Assets/Shader/ScreenSpaceDotTransparency/...`
  - `3cDemo/Client/3C_Client/Assets/Art/Animation/Haste/HasteMainCharacter/Shader/W_savCharacterNEW.shader`
  - `3cDemo/Client/3C_Client/Assets/Art/Animation/Haste/HasteMainCharacter/Shader/sav_CHAREYESHELLS.shader`
  - `3cDemo/Client/3C_Client/Assets/Art/Animation/Haste/HasteMainCharacter/Shader/savglasstest.shader`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime/...`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Rendering/...`
  - 可选默认关闭预览资产：`Assets/Scenes/Sandbox.unity`、`Assets/Settings/...`、`Assets/Art/Mat/...`

## Grounding
- 项目当前 Unity 客户端路径是 `3cDemo/Client/3C_Client`，渲染效果已有 `Assets/Scripts/Rendering/Runtime` 和 `Assets/Tests/Editor/Rendering` 组织方式。
- 当前场景角色材质主要引用 Haste 角色 shader：`W/savCharacterNEW`、`sav_CHAREYESHELLS` 和 `savglasstest`；项目中也存在 `URPGenshinToon.shader`，并包含 Forward、ShadowCaster、DepthOnly、DepthNormals 和 Outline pass。
- 现有 URP 后处理 specs 强调不得使用额外相机、`OnRenderImage` 或并行渲染路径；本需求不是全屏后处理，而是角色材质自身的可见性裁剪。
- 当前活跃 OpenSpec 主要集中在 locomotion、FullBody、rollback 和动作链路，没有发现正在进行的渲染点阵透明变更。

## User Verification After Implementation
- 在 Sandbox 中启用默认关闭的点阵透明预览对象，或把正式点阵 Profile 绑定到使用 Haste Diffuse 风格材质的测试角色。
- 调整点阵覆盖强度后，角色应以圆点网格形式露出背景。
- 移动角色时，点阵网格应固定在屏幕像素位置，不跟随角色表面滑动。
- 移动相机时，角色投影会变化，但屏幕点阵锚点仍固定在画面网格上。
- 观察描边和角色本体，二者应使用同一套点阵裁剪。
- 观察阴影，第一版预期仍保持实心角色阴影。

## Non-Goals
- 不新增全屏后处理、额外相机、独立 Renderer Feature 或第二套角色渲染路径。
- 不实现点状透明阴影；如果需要，单独审批新的 shadow pass 语义。
- 不把点阵透明接入具体 gameplay 触发条件；本变更只提供正式表现能力和默认关闭预览。
- 不批量替换所有角色材质资产；本变更只让现有正式角色 shader 能消费同一套点阵参数。
