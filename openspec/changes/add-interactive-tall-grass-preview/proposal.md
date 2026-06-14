# Change: 新增可交互高草丛预览

## Why
场景需要先建立一块可观察、可调参、能和角色产生交互反馈的高草丛，用于验证二次元/风格化场景方向。当前项目没有独立的高草丛渲染和交互预览能力，无法判断草的高度、密度、颜色、摆动和角色穿过时的视觉读数是否适合 3C demo。

## What Changes
- 新增一个可交互高草丛预览能力，第一版用于 Sandbox 或独立预览场景，不接入动作状态机。
- 新增高草丛数据配置，表达草丛尺寸、密度、随机种子、高度范围、颜色、风摆、交互半径和压弯强度。
- 新增高草丛生成器，用配置生成一组可复现的草片实例或网格块，避免手摆大量草对象。
- 新增风格化草 shader，优先走 URP 场景 shader 路径，支持近似二次元色块、轻微描边/边缘强化、风摆和交互压弯。
- 新增草丛交互源，第一版支持指定 Transform 或玩家 Transform 推开/压弯附近草片。
- 新增预览 prefab 和手动验证步骤，便于比较“二次元高草丛”和“较写实高草丛”的取舍。
- 提供 EditMode 测试覆盖配置钳制、生成确定性、交互参数计算、默认不污染场景和 prefab 结构。

## Impact
- Affected specs: `interactive-tall-grass-scene-preview`
- Affected code: `3cDemo/Client/3C_Client/Assets/Scripts/Scene/Runtime`
- Affected shaders: `3cDemo/Client/3C_Client/Assets/Shader/Scene`
- Affected prefabs: `3cDemo/Client/3C_Client/Assets/Prefabs/Env`
- Affected materials/assets: `3cDemo/Client/3C_Client/Assets/Materials/Scene`、`3cDemo/Client/3C_Client/Assets/Configs/3C/Scene`
- Affected tests: `3cDemo/Client/3C_Client/Assets/Tests/Editor/Scene`
