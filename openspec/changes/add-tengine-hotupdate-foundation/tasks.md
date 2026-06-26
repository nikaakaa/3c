# Tasks

## 1. 依赖落位

- [ ] 1.1 备份当前 `Packages/manifest.json` 的依赖列表用于人工对照。
- [ ] 1.2 将 `TEngine` 核心迁移到 `Packages/com.alex.tengine`。
- [ ] 1.3 删除迁移后包内不属于框架源码的示例业务目录。
- [ ] 1.4 将 `UniTask` 作为 embedded package 放入 `Packages/UniTask`。
- [ ] 1.5 将 `YooAsset` 作为 embedded package 放入 `Packages/YooAsset`。
- [ ] 1.6 在 `manifest.json` 增加 `com.code-philosophy.hybridclr` 正式依赖。
- [ ] 1.7 在 `manifest.json` 增加 `com.unity.nuget.newtonsoft-json` 正式依赖。
- [ ] 1.8 保留现有 Fantasy、InputSystem、Cinemachine、Timeline、URP、Animancer、KCC 依赖。
- [ ] 1.9 检查 `packages-lock.json` 是否只反映正式依赖变化。

## 2. TEngine 包整理

- [ ] 2.1 确认 `TEngine.Runtime.asmdef` 在新路径下引用 YooAsset、UniTask、Newtonsoft、UGUI 仍可解析。
- [ ] 2.2 确认 `TEngine.Editor.asmdef` 在新路径下只进入 Editor 平台。
- [ ] 2.3 保留 `SourceGenerator.dll` 和 `GameEventAnalyzer.dll` 的 Unity import meta。
- [ ] 2.4 保留 `System.Buffers.dll` 和 `System.Runtime.CompilerServices.Unsafe.dll` 的 Unity import meta。
- [ ] 2.5 删除或不迁移 TEngine demo 的 `Assets/GameScripts` 示例业务。
- [ ] 2.6 删除或不迁移 TEngine demo 的 `Assets/Launcher` 示例 UI。
- [ ] 2.7 删除或不迁移 TEngine demo 的 `AssetRaw` 示例资源。

## 3. 项目配置迁移

- [ ] 3.1 创建 `Assets/Settings/TEngine`。
- [ ] 3.2 迁移并改名 `AudioSetting.asset`。
- [ ] 3.3 迁移并改名 `ProcedureSetting.asset`。
- [ ] 3.4 迁移并改名 `UpdateSetting.asset`。
- [ ] 3.5 迁移 `YooAssetSettings.asset` 到项目设置目录。
- [ ] 3.6 创建 `Assets/Prefabs/TEngine`。
- [ ] 3.7 迁移并改名 `GameEntry.prefab` 为本项目 bootstrap prefab。
- [ ] 3.8 迁移并改名 `UIRoot.prefab` 为本项目 UI root prefab。
- [ ] 3.9 确认主资源地址和备用资源地址都是正式资源端点。

## 4. 启动层实现

- [ ] 4.1 创建 `Assets/Scripts/Bootstrap/Runtime`。
- [ ] 4.2 创建 `Project.Bootstrap.asmdef`。
- [ ] 4.3 新增 `TEngineBootstrapper`，负责进入 TEngine ModuleSystem。
- [ ] 4.4 新增项目 Procedure 基类或适配层。
- [ ] 4.5 新增 `ProcedureLaunch`。
- [ ] 4.6 新增 `ProcedureInitializeResourcePackage`。
- [ ] 4.7 新增 `ProcedureCheckHotUpdate`。
- [ ] 4.8 新增 `ProcedureLoadHotUpdateAssemblies`。
- [ ] 4.9 新增 `ProcedureEnterProjectRuntime`。
- [ ] 4.10 将项目 Procedure 列表写入本项目 `TEngineProcedureSettings.asset`。

## 5. 热更入口实现

- [ ] 5.1 创建 `Assets/Scripts/HotUpdate/Runtime`。
- [ ] 5.2 创建 `Project.HotUpdate.asmdef`。
- [ ] 5.3 新增 `HotUpdateAssemblyManifest`。
- [ ] 5.4 新增 `HotUpdateEntry`。
- [ ] 5.5 将 `ProcedureLoadHotUpdateAssemblies` 指向 `HotUpdateEntry.Enter()`。
- [ ] 5.6 明确 Taco 资产类型不放入第一版热更程序集。
- [ ] 5.7 明确角色 pipeline 业务入口由 `HotUpdateEntry` 调用，不由 TEngine 示例 `GameApp` 调用。

## 6. YooAsset 资源管线

- [ ] 6.1 设置默认资源包名。
- [ ] 6.2 设置 EditorSimulateMode 的编辑器运行路径。
- [ ] 6.3 设置构建模式下的正式资源端点。
- [ ] 6.4 保留 TEngine/YooAsset 正式备用资源地址配置。
- [ ] 6.5 确认备用资源地址不指向旧目录、旧配置或测试资源链路。
- [ ] 6.6 确认不新增 Addressables 资源路径。
- [ ] 6.7 规划首批进入 YooAsset 的资源标签：启动资源、UI root、热更程序集文本资产。

## 7. Fantasy 边界

- [ ] 7.1 创建 `Assets/Scripts/Network/Fantasy/Runtime`。
- [ ] 7.2 创建 `Project.Network.Fantasy.asmdef` 或记录延后到网络实现 change。
- [ ] 7.3 明确客户端 Fantasy 初始化从项目入口触发。
- [ ] 7.4 保持 `3cDemo/Server` 现有 Fantasy 骨架不被 TEngine 迁移覆盖。
- [ ] 7.5 不导入 TEngine 示例网络路径。

## 8. 旧路径清理

- [ ] 8.1 确认没有运行时引用 `Assets/TEngine` 旧路径。
- [ ] 8.2 确认没有运行时引用 TEngine 示例 `GameScripts/HotFix/GameLogic`。
- [ ] 8.3 确认没有运行时引用 TEngine 示例 `Launcher`。
- [ ] 8.4 确认没有新增 `Ref` 运行时依赖。
- [ ] 8.5 确认没有新增 `Charactor` 拼写的新路径。

## 9. 编译收口

- [ ] 9.1 处理 asmdef 引用错误。
- [ ] 9.2 处理 analyzer DLL import 错误。
- [ ] 9.3 处理 HybridCLR 宏和程序集列表错误。
- [ ] 9.4 处理 YooAsset 初始化 API 版本差异。
- [ ] 9.5 处理 TEngine ResourceModule 单端点改造后的编译错误。
- [ ] 9.6 处理 Bootstrap 到 HotUpdate 入口反射或直接调用错误。
- [ ] 9.7 更新 OpenSpec task 勾选状态。
