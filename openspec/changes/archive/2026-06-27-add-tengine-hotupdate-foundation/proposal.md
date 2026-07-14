# Proposal

## Why

当前客户端还没有正式热更、资源版本和资源生命周期底座。后续 `CharacterPipeline`、BTSMTL Timeline、表现资源、局内 UI 和最小 Fantasy 网络压力场景都会需要统一的异步资源加载、启动流程和热更程序集边界。

TEngine 的价值不在于替代本项目 gameplay 架构，而在于提供一条已经组合好的客户端工程底座：HybridCLR 热更、YooAsset 资源管理、UniTask 异步、Procedure 启动流程、GameEvent/MemoryPool/ObjectPool 等基础模块。它适合作为项目底层能力接入，但不能接管 BTSMTL authoring、角色 pipeline 或 Fantasy 服务端边界。

## What Changes

- 新增 `tengine-hotupdate-foundation` 能力，用 TEngine 作为客户端热更和资源底座。
- 将 TEngine 核心代码迁移为客户端内的正式第三方/框架包，不作为 `Ref` 运行时依赖。
- 引入 HybridCLR、YooAsset、UniTask、Newtonsoft Json 和 TEngine GameEvent SourceGenerator 所需依赖。
- 采用 TEngine 原始 `Assets/GameScripts` 目录口径承载启动流程和热更入口，TEngine Procedure 只负责启动资源、热更程序集和本项目入口。
- 保留 BTSMTL 作为 authoring 和 gameplay graph runtime 的主线，不用 TEngine FSM 替代 `StateMachineGraphRuntime`。
- 保留 Fantasy 作为最小权威服务端和 Unity 客户端网络依赖，客户端边界进入 `GameLogic` 热更程序集，不导入 TEngine 示例项目里的第二套网络路径。
- 建立正式资源端点配置，允许 TEngine/YooAsset 的主资源地址和备用资源地址作为 CDN 容灾配置；禁止的是旧配置、旧数据源和旧目录的兼容 fallback 链路。
- 明确 TEngine 示例目录结构作为正式项目结构使用，但不整包迁移 `Launcher`、示例 UI、示例 BattleMainUI、示例 `GameModule/UIModule` 或示例配置表业务。

## Folder Migration

目标目录按“第三方框架、项目设置、项目业务”分层：

```text
3cDemo/Client/3C_Client/Packages/com.alex.tengine
3cDemo/Client/3C_Client/Packages/UniTask
3cDemo/Client/3C_Client/Packages/YooAsset
3cDemo/Client/3C_Client/Assets/AssetArt
3cDemo/Client/3C_Client/Assets/AssetRaw
3cDemo/Client/3C_Client/Assets/Settings/TEngine
3cDemo/Client/3C_Client/Assets/Prefabs/TEngine
3cDemo/Client/3C_Client/Assets/GameScripts/Main/GameEntry.cs
3cDemo/Client/3C_Client/Assets/GameScripts/Main/Procedure
3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL
3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline
3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Camera
3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Rendering
3cDemo/Client/3C_Client/Assets/GameScripts/HotFix/GameBase
3cDemo/Client/3C_Client/Assets/GameScripts/HotFix/GameProto
3cDemo/Client/3C_Client/Assets/GameScripts/HotFix/BattleCore
3cDemo/Client/3C_Client/Assets/GameScripts/HotFix/GameLogic
3cDemo/Client/3C_Client/Assets/GameScripts/HotFix/GameLogic/Network/Fantasy
```

迁移规则：

- `UnityProject/Assets/TEngine/Runtime`、`Editor`、`Extension`、`Libraries`、`package.json` 进入 `Packages/com.alex.tengine`。
- `UnityProject/Assets/TEngine/Settings/*.asset` 和 `Settings/Prefab/*.prefab` 不放进包内，迁入项目自己的 `Assets/Settings/TEngine` 和 `Assets/Prefabs/TEngine`，并按本项目流程改名。
- `UnityProject/Packages/UniTask` 和 `UnityProject/Packages/YooAsset` 作为 embedded packages 迁入项目 `Packages`。
- `com.code-philosophy.hybridclr` 通过 `Packages/manifest.json` 正式声明。
- 示例 `Assets/Launcher` 不整包迁移；更新 UI 可后续按本项目命名重建到项目 UI 模块。
- 示例 `Assets/GameScripts/Main` 的目录和流程命名作为正式结构使用；流程代码按本项目需求重写，不迁移 Launcher/UI 进度界面。
- 旧 `Assets/Scripts` 稳定代码迁入 `Assets/GameScripts/Main/Runtime`，不再保留 `Assets/Scripts` 作为正式代码根目录。
- 示例 `Assets/GameScripts/HotFix/GameBase`、`GameProto`、`BattleCore`、`GameLogic` 的热更分层作为正式结构使用；第一版只让 `GameLogic` 承载项目入口和 Fantasy 客户端边界。
- Fantasy 客户端边界迁入 `Assets/GameScripts/HotFix/GameLogic/Network/Fantasy`，不再创建独立 `Project.Network.Fantasy` 程序集。
- 示例 `GameProto/LubanLib` 不迁移为当前依赖；Luban 配置链路另行规划，避免和 BTSMTL 节点资产数据源分裂。
- 旧 `Assets/Art` 根目录迁移为 `Assets/AssetArt`，作为美术资产工作区；`Assets/AssetRaw` 承载热更资源采集入口、DLL bytes 和后续 YooAsset 采集入口。

## Non-Goals

- 不在本变更中实现角色动作、战斗命中、Timeline 事实轨道或完整网络同步。
- 不把 TEngine FSM 用作动作状态机主线。
- 不把 TEngine GameEvent 作为 Fantasy 协议消息、服务端事件或 gameplay 权威裁决的替代品。
- 不恢复旧 `PlayerSO/LocomotionSO/ActionSO/footphase/bodyclaim` 配置。
- 不新增旧配置 fallback、兼容路径、示例路径或临时桥接路径。
- 不新增测试任务；用户会在 Unity 中做端到端验证。

## Business Tradeoffs

- 选 TEngine 热更底座的收益是展示商业客户端基本功：资源版本、热更程序集、启动流程、资源释放和异步加载；代价是接入复杂度会上升，需要先把程序集边界和资源端点定干净。
- 选 embedded `Packages/com.alex.tengine` 而不是把 `Assets/TEngine` 直接放入项目，是为了让第三方框架边界更清晰；代价是需要确认 asmdef GUID 和 analyzer DLL import meta 在迁移后仍然有效。
- 选直接采用 TEngine `GameScripts/Main` 和 `GameScripts/HotFix` 目录，是为了降低后续热更文档、工具链和团队对照成本；代价是项目入口会保留 `GameEntry`、`GameLogic`、`GameApp` 这些 TEngine 约定名，需要通过代码内容而不是目录名表达项目业务边界。
- 选把稳定 runtime 从 `Assets/Scripts` 迁入 `GameScripts/Main/Runtime`，是为了让 AOT 稳定层和热更层都收敛在 TEngine 目录树下；代价是历史文档和工具硬编码路径必须同步改掉，不再保留旧根路径兼容。
- 选把美术根目录从 `Art` 迁到 `AssetArt`，是为了贴合 YooAsset/TEngine 工具对美术资产扫描和图集输出的命名习惯；代价是历史报告文件里的旧路径需要随迁移刷新，不再继续保留旧 `Assets/Art` 入口。
- 选把 Fantasy Unity 客户端边界放进 `GameLogic` 而不是独立热更程序集，是为了减少第一版 asmdef 断点和编译错误面；代价是后续网络协议稳定后，是否拆出稳定协议程序集需要单独评估。
- 选保留主资源地址和备用资源地址，是为了符合热更资源的 CDN 容灾需求；取舍是这两个地址都必须是正式资源配置，不能借备用地址承载旧环境、旧目录或测试资源链路。

## Spec Conflicts To Resolve

- `openspec/project.md` 当前写明客户端主脚本模块是 `Camera`、`Rendering`、`BTSMTL`，本变更会新增 `GameScripts/Main`、`GameScripts/HotFix` 和 `GameLogic/Network/Fantasy` 作为基础设施模块。
- `add-character-pipeline-runtime-entry` 规划 `CharacterPipelineRunner` 作为 gameplay tick 权威，本变更必须保证 TEngine Procedure 只负责启动和装配，不创建第二套 gameplay tick。
- `add-btsmtl-transition-rule-graph-authoring` 正在把 Transition 条件下钻到规则图，本变更不能引入 TEngine FSM 或事件系统来绕过 BTSMTL TransitionRuleGraph。
- 当前项目规则禁止旧配置兼容 fallback，而 TEngine ResourceModule 暴露的 `FallbackHostServerURL` 属于资源下载容灾；实现时可以保留，但必须作为正式资源备用地址，不得指向旧资源目录或旧配置数据。
