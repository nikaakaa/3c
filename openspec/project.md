# Project Context

## Purpose
本项目是在现有 BBBNexus 3C 客户端基础上构建复杂动画 3C demo，并逐步接入 Fantasy 网络同步、客户端预测和回滚。目标不是另起一套角色控制器，而是把现有角色聚合点、配置模块、状态机、运动驱动和动画外观层扩展成可展示、可测试、可同步的动画技术样板。

## Tech Stack
- Unity 6000 系列项目：`3cDemo/Client/3C_Client`
- C# / Unity Test Framework / EditMode tests
- Animancer 作为动画播放外观层
- ScriptableObject 配置驱动角色能力
- Fantasy.Net 服务端与 Fantasy protocol export tool
- OpenSpec 用于能力设计、审批和归档

## Project Conventions

### Code Style
- 生成代码尽量少写注释；关键复杂逻辑可用少量中文注释解释。
- 保持抽象和实现分离，避免把业务规则写死在 MonoBehaviour 细节里。
- 配置驱动优先，动画资源和动作链路从 SO 模块进入运行时。
- 不删除现有 log，除非用户明确要求。

### Architecture Patterns
- 角色聚合点是 `BBBCharacterController`。
- 物理位移权威是 `MotionDriver`。
- 动画播放外观是 `AnimationFacadeBase` / `AnimancerFacade`。
- 全身状态通过 `PlayerStateRegistry`、`PlayerBaseState` 和 `StateMachine` 管理。
- 上半身状态通过 `UpperBodyController` 和对应状态机管理。
- 全身接管动作通过 `ActionController -> ActionArbiter -> OverrideState -> AnimancerFacade` 主线进入。
- 网络同步不得直接同步 Unity 对象、Animancer 内部对象或场景实例引用，必须先映射为稳定 ID 和纯数据快照。

### Testing Strategy
- 实现代码必须配套 Unity EditMode 测试。
- 每个 OpenSpec proposal 的任务清单必须包含自动测试和手动端到端验证步骤。
- 优先使用 Unity MCP 运行定向 EditMode 测试；全量测试若初始化超时，不视为替代定向测试失败。
- OpenSpec 变更必须通过 `openspec validate <change-id> --strict --no-interactive`。

### Git Workflow
- 工作树可能包含用户或其他 agent 的未提交变更，不能回退未确认的改动。
- 不使用破坏性 git 命令。

## Domain Context
- 复杂动画 demo 需要展示移动、起步、循环、急停、跳跃、落地、闪避、翻滚、翻越、装备、瞄准、近战连招、远程射击、受击、死亡、表情、音效、IK、RootQ 姿态和镜头事件。
- 后续网络能力基于 `AnimationStateSnapshot`、Fantasy 协议 DTO、输入历史、快照历史、预测、回滚和事件去重。
- `Ref` 目录可作为参考，不直接复制实现。可参考 UE/Animancer/ZZZ 类项目的动作节点、预输入、取消窗口、notify state、root motion 采样和服务端同步思路。

## Important Constraints
- 新能力和架构变化必须先走 OpenSpec proposal，未经审批不得直接实现大功能。
- 任务颗粒度要细。
- 所有需要绕过当前系统额外做的路径必须停止，等待审批。
- Demo 不新增未审批的独立角色控制器路径。
- OpenSpec 内容使用中文书写。

## External Dependencies
- Fantasy.Net 网络框架。
- `3cDemo/Tools/NetworkProtocol` 下的 proto 协议。
- `3cDemo/Tools/ProtocolExportTool` 用于生成客户端和服务端协议代码。
