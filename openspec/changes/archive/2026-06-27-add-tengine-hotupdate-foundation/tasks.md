# Tasks

## 1. 依赖落位

- [x] 1.1 备份当前 `Packages/manifest.json` 的依赖列表用于人工对照。
- [x] 1.2 将 TEngine 核心迁移到 `Packages/com.alex.tengine`。
- [x] 1.3 将 TEngine `package.json` 的 `unity` 字段修正为 UPM 合法版本。
- [x] 1.4 删除迁移后包内不属于框架源码的示例业务目录。
- [x] 1.5 将 UniTask 作为 embedded package 放入 `Packages/UniTask`。
- [x] 1.6 将 YooAsset 作为 embedded package 放入 `Packages/YooAsset`。
- [x] 1.7 在 `manifest.json` 增加 HybridCLR 正式依赖。
- [x] 1.8 在 `manifest.json` 增加 Newtonsoft Json 正式依赖。
- [x] 1.9 保留现有 Fantasy、InputSystem、Cinemachine、Timeline、URP、Animancer、KCC 依赖。
- [x] 1.10 检查 `packages-lock.json` 是否只反映正式依赖变化。

## 2. TEngine 包整理

- [x] 2.1 确认 `TEngine.Runtime.asmdef` 在新路径下引用 YooAsset、UniTask、InputSystem、UGUI 仍可解析。
- [x] 2.2 确认 `TEngine.Editor.asmdef` 在新路径下只进入 Editor 平台。
- [x] 2.3 保留 SourceGenerator 和 GameEventAnalyzer 的 Unity import meta。
- [x] 2.4 保留 `System.Buffers.dll` 和 `System.Runtime.CompilerServices.Unsafe.dll` 的 Unity import meta。
- [x] 2.5 确认没有运行时引用 `Assets/TEngine` 旧路径。
- [x] 2.6 确认没有把 TEngine 示例 `Launcher` 作为运行时目录。
- [x] 2.7 确认没有把 TEngine 示例 `AssetRaw` 作为运行时资源目录。

## 3. 项目配置迁移

- [x] 3.1 创建 `Assets/Settings/TEngine`。
- [x] 3.2 迁移并改名 `AudioSetting.asset`。
- [x] 3.3 迁移并改名 `ProcedureSetting.asset`。
- [x] 3.4 迁移并改名 `UpdateSetting.asset`。
- [x] 3.5 迁移 `YooAssetSettings.asset` 到项目设置目录。
- [x] 3.6 创建 `Assets/Prefabs/TEngine`。
- [x] 3.7 迁移并改名 `GameEntry.prefab` 为 `TEngineBootstrap.prefab`。
- [x] 3.8 迁移并改名 `UIRoot.prefab` 为 `TEngineUIRoot.prefab`。
- [x] 3.9 清空本地测试资源端点，未配置真实 CDN 时 Host/Web 模式直接报错。
- [x] 3.10 将旧 `Assets/Art` 根目录迁移为 `Assets/AssetArt`。
- [x] 3.11 保留 `Assets/AssetRaw` 作为热更资源采集目录。
- [x] 3.12 创建 `Assets/AssetRaw/HotUpdate/DLL` 作为热更程序集 bytes 采集目录。

## 4. TEngine 原目录结构落位

- [x] 4.1 创建 `Assets/GameScripts`。
- [x] 4.2 创建 `Assets/GameScripts/Main`。
- [x] 4.3 将启动 MonoBehaviour 落到 `Assets/GameScripts/Main/GameEntry.cs`。
- [x] 4.4 创建 `Assets/GameScripts/Main/Procedure`。
- [x] 4.5 将启动流程类改为 `Procedure` 命名空间。
- [x] 4.6 新增 `ProcedureBase`。
- [x] 4.7 新增 `ProcedureLaunch`。
- [x] 4.8 新增 `ProcedureInitPackage`。
- [x] 4.9 新增 `ProcedureInitResources`。
- [x] 4.10 新增 `ProcedureLoadAssembly`。
- [x] 4.11 新增 `ProcedureStartGame`。
- [x] 4.12 新增 `HotUpdateAssemblyLoader`。
- [x] 4.13 新增 `HybridClrRuntimeBridge`。
- [x] 4.14 将 Procedure 列表写入 `TEngineProcedureSettings.asset`。
- [x] 4.15 删除旧 `Assets/Scripts/Bootstrap` 路径。
- [x] 4.16 删除旧 `Project.Bootstrap.asmdef`。
- [x] 4.17 创建 `Assets/GameScripts/Main/Runtime`。
- [x] 4.18 将 `Assets/Scripts/BTSMTL` 迁入 `Assets/GameScripts/Main/Runtime/BTSMTL`。
- [x] 4.19 将 `Assets/Scripts/Character` 迁入 `Assets/GameScripts/Main/Runtime/Character`。
- [x] 4.20 将 `Assets/Scripts/Camera` 迁入 `Assets/GameScripts/Main/Runtime/Camera`。
- [x] 4.21 将 `Assets/Scripts/Rendering` 迁入 `Assets/GameScripts/Main/Runtime/Rendering`。
- [x] 4.22 删除旧 `Assets/Scripts` 根目录。
- [x] 4.23 修正 BTSMTL editor 中硬编码的脚本扫描路径。

## 5. GameLogic 热更入口

- [x] 5.1 创建 `Assets/GameScripts/HotFix/GameBase`。
- [x] 5.2 创建 `Assets/GameScripts/HotFix/GameProto`。
- [x] 5.3 创建 `Assets/GameScripts/HotFix/BattleCore`。
- [x] 5.4 创建 `Assets/GameScripts/HotFix/GameLogic`。
- [x] 5.5 创建 `GameBase.asmdef`。
- [x] 5.6 创建 `GameProto.asmdef`。
- [x] 5.7 创建 `BattleCore.asmdef`。
- [x] 5.8 创建 `GameLogic.asmdef`。
- [x] 5.9 新增 `GameApp.Entrance()` 作为热更入口。
- [x] 5.10 新增 `HotUpdateAssemblyManifest`。
- [x] 5.11 将 `TEngineUpdateSettings.asset` 指向 `GameBase.dll`、`GameProto.dll`、`BattleCore.dll` 和 `GameLogic.dll`。
- [x] 5.12 将 TEngine `UpdateSetting` 默认热更程序集改为 `GameBase.dll`、`GameProto.dll`、`BattleCore.dll` 和 `GameLogic.dll`。
- [x] 5.13 删除旧 `Assets/Scripts/HotUpdate` 路径。
- [x] 5.14 删除旧 `Project.HotUpdate.asmdef`。
- [x] 5.15 明确 BTSMTL 资产类型不放入第一版热更程序集。

## 6. Fantasy 客户端边界

- [x] 6.1 创建 `Assets/GameScripts/HotFix/GameLogic/Network/Fantasy`。
- [x] 6.2 将 Fantasy 客户端初始化代码迁入 `GameLogic`。
- [x] 6.3 将 Fantasy 命名空间改为 `GameLogic.Network.Fantasy`。
- [x] 6.4 删除旧 `Assets/Scripts/Network/Fantasy` 路径。
- [x] 6.5 删除旧 `Project.Network.Fantasy.asmdef`。
- [x] 6.6 保持 `3cDemo/Server` 现有 Fantasy 骨架不被 TEngine 迁移覆盖。
- [x] 6.7 不导入 TEngine 示例网络路径。

## 7. 角色控制代码归属规划

- [x] 7.1 明确 `GameEntry` 和 `Procedure` 只负责启动、资源和热更装配。
- [x] 7.2 明确 `GameApp` 只负责进入项目 runtime。
- [x] 7.3 明确 `CharacterPipelineRunner` 继续作为 gameplay tick 权威。
- [x] 7.4 明确 BTSMTL 节点、图资产和 Unity 序列化稳定类型不放进 `GameLogic`。
- [x] 7.5 明确角色可热更业务进入 `GameLogic/Character` 或后续 `Project.Character` 稳定程序集。

## 8. 编译收口

- [x] 8.1 处理 TEngine package manifest 错误。
- [x] 8.2 处理 `TEngine.Runtime` 不产出导致的引用错误。
- [x] 8.3 处理 asmdef 引用错误。
- [x] 8.4 处理 HybridCLR 宏和程序集列表错误。
- [x] 8.5 处理 YooAsset 初始化 API 版本差异。
- [x] 8.6 确认没有旧 `Project.Bootstrap`、`Project.HotUpdate`、`Project.Network.Fantasy` 运行时引用。
- [x] 8.7 确认没有新增 Addressables 资源路径。
- [x] 8.8 更新 OpenSpec task 勾选状态。
