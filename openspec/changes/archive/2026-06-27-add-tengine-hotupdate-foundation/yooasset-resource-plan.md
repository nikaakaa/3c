# YooAsset 首批资源规划

默认包名：`DefaultPackage`

首批资源类别：

- `boot`: `Assets/Prefabs/TEngine/TEngineBootstrap.prefab`
- `ui-root`: `Assets/Prefabs/TEngine/TEngineUIRoot.prefab`
- `hotupdate-dll`: `Assets/AssetRaw/HotUpdate/DLL/*.bytes`

目录职责：

- `Assets/AssetArt`: 美术资产工作区，承载动画、模型、材质、贴图、第三方美术包和后续图集输出。
- `Assets/AssetRaw`: 热更资源采集目录，承载运行时原始资源入口和 HybridCLR DLL bytes。
- `Assets/AssetRaw/HotUpdate/DLL`: `GameBase.dll.bytes`、`GameProto.dll.bytes`、`BattleCore.dll.bytes`、`GameLogic.dll.bytes`。

资源定位：

- 不新增 Addressables 路径。
- 热更程序集文本资产使用 `Assets/AssetRaw/HotUpdate/DLL/{AssemblyName}.dll.bytes`。
- 主资源地址由 `TEngineUpdateSettings.asset` 的 `ResDownLoadPath + projectName + platform` 生成。
- 备用资源地址由 `FallbackResDownLoadPath + projectName + platform` 生成，作为正式 CDN 备用地址，不用于旧配置兼容。
- 默认不填本地测试 URL；未配置真实 CDN 时 Host/Web 模式直接报错，EditorSimulateMode 不依赖远端端点。
