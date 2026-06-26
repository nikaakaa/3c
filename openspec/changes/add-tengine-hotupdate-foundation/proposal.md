# Proposal

## Why

当前客户端还没有正式热更、资源版本和资源生命周期底座。后续 `CharacterPipeline`、Taco Timeline、表现资源、局内 UI 和最小 Fantasy 网络压力场景都会需要统一的异步资源加载、启动流程和热更程序集边界。

TEngine 的价值不在于替代本项目 gameplay 架构，而在于提供一条已经组合好的客户端工程底座：HybridCLR 热更、YooAsset 资源管理、UniTask 异步、Procedure 启动流程、GameEvent/MemoryPool/ObjectPool 等基础模块。它适合作为项目底层能力接入，但不能接管 Taco authoring、角色 pipeline 或 Fantasy 服务端边界。

## What Changes

- 新增 `tengine-hotupdate-foundation` 能力，用 TEngine 作为客户端热更和资源底座。
- 将 TEngine 核心代码迁移为客户端内的正式第三方/框架包，不作为 `Ref` 运行时依赖。
- 引入 HybridCLR、YooAsset、UniTask、Newtonsoft Json 和 TEngine GameEvent SourceGenerator 所需依赖。
- 新增本项目自己的启动流程命名和程序集边界，TEngine Procedure 只负责启动资源、热更程序集和本项目入口。
- 保留 Taco 作为 authoring 和 gameplay graph runtime 的主线，不用 TEngine FSM 替代 `StateMachineGraphRuntime`。
- 保留 Fantasy 作为最小权威服务端和 Unity 客户端网络依赖，不导入 TEngine 示例项目里的第二套网络路径。
- 建立正式资源端点配置，允许 TEngine/YooAsset 的主资源地址和备用资源地址作为 CDN 容灾配置；禁止的是旧配置、旧数据源和旧目录的兼容 fallback 链路。
- 明确 TEngine 示例目录只作为参考：不整包迁移 `Launcher`、`GameScripts/HotFix/GameLogic`、示例 UI、示例 BattleMainUI 或示例配置表业务。

## Folder Migration

目标目录按“第三方框架、项目设置、项目业务”分层：

```text
3cDemo/Client/3C_Client/Packages/com.alex.tengine
3cDemo/Client/3C_Client/Packages/UniTask
3cDemo/Client/3C_Client/Packages/YooAsset
3cDemo/Client/3C_Client/Assets/Settings/TEngine
3cDemo/Client/3C_Client/Assets/Prefabs/TEngine
3cDemo/Client/3C_Client/Assets/Scripts/Bootstrap
3cDemo/Client/3C_Client/Assets/Scripts/HotUpdate
3cDemo/Client/3C_Client/Assets/Scripts/Network/Fantasy
```

迁移规则：

- `UnityProject/Assets/TEngine/Runtime`、`Editor`、`Extension`、`Libraries`、`package.json` 进入 `Packages/com.alex.tengine`。
- `UnityProject/Assets/TEngine/Settings/*.asset` 和 `Settings/Prefab/*.prefab` 不放进包内，迁入项目自己的 `Assets/Settings/TEngine` 和 `Assets/Prefabs/TEngine`，并按本项目流程改名。
- `UnityProject/Packages/UniTask` 和 `UnityProject/Packages/YooAsset` 作为 embedded packages 迁入项目 `Packages`。
- `com.code-philosophy.hybridclr` 通过 `Packages/manifest.json` 正式声明。
- 示例 `Assets/Launcher` 不整包迁移；更新 UI 可后续按本项目命名重建到 `Assets/Scripts/Bootstrap` 或 UI 模块。
- 示例 `Assets/GameScripts/HotFix/GameLogic` 不整包迁移；其中 `GameApp`、`GameModule`、UI 模块只作为结构参考。
- 示例 `Assets/GameScripts/Procedure` 不整包迁移；流程类按本项目命名重写到 `Assets/Scripts/Bootstrap/Procedures`。
- 示例 `GameProto/LubanLib` 不迁移为当前依赖；Luban 配置链路另行规划，避免和 Taco 节点资产数据源分裂。

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
- 选重写本项目 `Bootstrap` 流程而不是搬 TEngine 示例 `GameScripts/Procedure`，是为了避免示例业务命名污染项目；代价是前期要手动对照 TEngine 流程拆小实现。
- 选保留 Fantasy 现有骨架而不是导入 TEngine 示例网络，是为了保持网络只服务 Gameplay 客户端压力场景；代价是客户端热更程序集和 Fantasy 协议程序集边界需要单独设计。
- 选保留主资源地址和备用资源地址，是为了符合热更资源的 CDN 容灾需求；取舍是这两个地址都必须是正式资源配置，不能借备用地址承载旧环境、旧目录或测试资源链路。

## Spec Conflicts To Resolve

- `openspec/project.md` 当前写明客户端主脚本模块是 `Camera`、`Rendering`、`Taco`，本变更会新增 `Bootstrap`、`HotUpdate`、`Network/Fantasy` 作为基础设施模块。
- `add-character-pipeline-runtime-entry` 规划 `CharacterPipelineRunner` 作为 gameplay tick 权威，本变更必须保证 TEngine Procedure 只负责启动和装配，不创建第二套 gameplay tick。
- `add-taco-transition-rule-graph-authoring` 正在把 Transition 条件下钻到规则图，本变更不能引入 TEngine FSM 或事件系统来绕过 Taco TransitionRuleGraph。
- 当前项目规则禁止旧配置兼容 fallback，而 TEngine ResourceModule 暴露的 `FallbackHostServerURL` 属于资源下载容灾；实现时可以保留，但必须作为正式资源备用地址，不得指向旧资源目录或旧配置数据。
