# Change: 新增纯 HFSM Core 框架

## Why
当前 `Assets/Scripts/FSM/Core/SimpleHFSM.cs` 已经能表达组合状态、叶子状态、同层 transition 和 Builder，但它仍偏 demo：缺少状态路径、跨层转移、历史恢复、下推栈、事件队列、切换保护、构建校验和调试快照。后续角色移动、战斗、覆盖动作和临时状态如果直接堆在现有 demo 版上，会把业务规则和状态机语义混在一起。

本变更先把纯 HFSM 框架落到 `Assets/Scripts/FSM/Core/HFSM.cs`，只定义状态结构和切换语义，不接入角色移动、Animancer、输入或相机。

## What Changes
- 在 `Assets/Scripts/FSM/Core/HFSM.cs` 新增独立纯 HFSM Core，保留 `SimpleHFSM.cs` 作为旧 demo/参考，不在本变更中删除。
- 提供受控的 `StateMachine<TContext>` 运行入口，统一管理启动、停止、Tick、当前路径、上一条转移和调试事件。
- 支持组合状态与叶子状态组成一棵树，并以完整状态路径作为运行时事实。
- 支持基于最近公共父节点的跨层转移，避免跨层切换时错误退出公共祖先。
- 支持浅历史、深历史，以及 push/pop 下推栈，用于恢复先前状态路径。
- 支持 transition priority、注册顺序、深度作用域的确定性冲突解决。
- 支持事件队列和轮询 transition 两种触发方式，但事件处理不得绑定 Unity 输入或具体业务。
- 支持 `CanEnter`、`CanExit`、transition action、transition reason/payload 和转移拒绝反馈。
- 提供构建期校验和运行时调试快照，便于定位非法 target、孤儿状态、重复挂载、缺失 initial 等问题。

## Non-Goals
- 不把 `BasicMovementStateMachine`、WASD、战斗、Animancer、相机或 BBB 角色系统迁入 HFSM。
- 不新增角色状态、移动状态、动画配置或新的 MonoBehaviour 主入口。
- 不在本阶段实现正交并行区域；需要并行状态时先通过多个 HFSM 实例协作。
- 不删除现有 `SimpleHFSM.cs` 和 `HFSMTest.cs`，除非后续实施阶段发现编译冲突并经确认。
- 不新增未审批的编辑器工具或运行时配置资产。

## Impact
- Affected specs: `pure-hfsm-core`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/FSM/Core/HFSM.cs`
  - 可能仅在必要时调整 `3cDemo/Client/3C_Client/Assets/Scripts/FSM/Core/SimpleHFSM.cs` 的命名空间冲突，不改变其 demo 行为
- Validation:
  - `openspec validate add-pure-hfsm-core --strict --no-interactive`
  - `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore -v:minimal`
  - 通过最小用例手动或临时调试验证：同层转移、跨层 LCA 转移、浅/深历史恢复、push/pop 恢复、事件队列顺序、转移拒绝日志/回调
