# UnityHFSM 使用指南

本文给后续 agent 和自己继续做状态机相关任务时先读。项目已经通过 UPM 引入 `com.inspiaaa.unityhfsm`，版本为 `2.3.0`，来源见 `3cDemo/Client/3C_Client/Packages/manifest.json`。

本项目后续状态机优先使用 UnityHFSM，不再扩展自研 `Assets/Scripts/FSM/Core/HFSM.cs` 作为角色业务主线。自研 HFSM 可暂时保留为实验代码，除非用户明确要求清理。

## 项目接入原则

- UnityHFSM 只是状态机内核，不直接承载角色业务边界。
- 角色聚合点仍应是当前项目自己的角色入口，不新增未审批的独立角色控制器。
- 动画播放仍通过 Animancer 外观层收敛，状态不要散落大量 Animancer 细节。
- 位移权威仍进入统一运动驱动出口，状态不要直接四处调用 `CharacterController.Move`。
- 输入、意图、状态、动画命令、运动命令分层处理，不把所有逻辑塞进一个 MonoBehaviour。
- 新能力接入角色主状态机前必须走 OpenSpec，中文说明、细任务、测试和验证步骤齐全。

## 最小 API

常用命名空间：

```csharp
using UnityHFSM;
```

最小状态机：

```csharp
StateMachine fsm = new StateMachine();

fsm.AddState("Idle",
    onEnter: state => { },
    onLogic: state => { },
    onExit: state => { });

fsm.AddState("Move");
fsm.AddTransition("Idle", "Move", transition => ShouldMove());
fsm.AddTransition("Move", "Idle", transition => ShouldStop());

fsm.SetStartState("Idle");
fsm.Init();

// 每帧驱动
fsm.OnLogic();
```

`AddState("Name")` 会创建空状态。第一个加入的状态会自动成为 start state，但项目代码应显式调用 `SetStartState`，避免配置顺序变化导致入口漂移。

## 推荐类型 ID

简单原型可以用 string：

```csharp
StateMachine fsm = new StateMachine();
fsm.AddState("Idle");
```

角色业务推荐用 enum，避免字符串拼错：

```csharp
public enum CharacterStateId
{
    Idle,
    MoveStart,
    MoveLoop,
    MoveStop,
    Jump,
    Fall,
    Land,
    Dodge,
    Override
}

StateMachine<CharacterStateId> fsm = new StateMachine<CharacterStateId>();
fsm.AddState(CharacterStateId.Idle);
fsm.SetStartState(CharacterStateId.Idle);
```

如果需要自定义事件类型，可用三泛型：

```csharp
StateMachine<string, CharacterStateId, CharacterEventId> fsm =
    new StateMachine<string, CharacterStateId, CharacterEventId>();
```

## Transition 规则

普通 transition 每次 `OnLogic()` 会被检查：

```csharp
fsm.AddTransition(
    CharacterStateId.Idle,
    CharacterStateId.MoveStart,
    transition => context.MoveIntent.HasMove);
```

`AddTransitionFromAny` 是全局 transition，适合死亡、硬中断等高优先级状态：

```csharp
fsm.AddTransitionFromAny(
    CharacterStateId.Override,
    transition => context.ActionRequest.HasRequest);
```

注意：UnityHFSM 每次 `OnLogic()` 先检查 global transitions，再检查当前状态的 direct transitions，发生 transition 后仍会调用新 active state 的 `OnLogic()`。状态逻辑要能承受“刚 Enter 后同帧 Logic”。

不要把大量业务优先级藏在多个 `AddTransitionFromAny` 的注册顺序里。需要优先级时，优先做一个项目自己的仲裁层，把最高优先级意图写入 context，再让 FSM 只读最终事实。

## Trigger Transition

触发器适合离散事件，例如动画事件、输入边沿、受击事件：

```csharp
fsm.AddTriggerTransition(
    CharacterEventId.JumpPressed,
    CharacterStateId.Idle,
    CharacterStateId.Jump,
    transition => context.CanJump);

fsm.Trigger(CharacterEventId.JumpPressed);
```

`Trigger` 会向当前层级和子层级传播；`TriggerLocally` 只在当前 FSM 本层处理。角色主状态机默认用 `Trigger`，只有明确要限制在某个子状态机时才用 `TriggerLocally`。

## 层级状态机

`StateMachine` 本身也是 `StateBase`，可作为另一个 FSM 的状态：

```csharp
StateMachine root = new StateMachine();
StateMachine locomotion = new StateMachine(rememberLastState: true);

locomotion.AddState("Idle");
locomotion.AddState("Move");
locomotion.AddTransition("Idle", "Move", t => context.MoveIntent.HasMove);
locomotion.AddTransition("Move", "Idle", t => !context.MoveIntent.HasMove);
locomotion.SetStartState("Idle");

root.AddState("Locomotion", locomotion);
root.AddState("Action");
root.SetStartState("Locomotion");
root.Init();
```

`rememberLastState: true` 表示子 FSM 再次进入时回到上次 active state，适合 Locomotion 子树。

如果父状态切出一个 `needsExitTime: true` 的子 FSM，子 FSM 需要通过 exit transition 或内部状态 `StateCanExit()` 允许退出。

## needsExitTime

`needsExitTime` 用于动作不能立刻退出的状态，例如起步、急停、落地、闪避、攻击段：

```csharp
fsm.AddState(CharacterStateId.Dodge,
    onEnter: state => context.Animation.PlayDodge(),
    onLogic: state => context.Motion.ApplyDodgeMotion(),
    canExit: state => context.Animation.NormalizedTime >= 0.8f,
    needsExitTime: true);

fsm.AddTransition(
    CharacterStateId.Dodge,
    CharacterStateId.Idle,
    transition => true);
```

当 transition 条件成立但 active state 不能退出时，UnityHFSM 会保留 pending transition。之后 `canExit` 返回 true，或状态调用 `state.fsm.StateCanExit()`，才真正切换。

需要强制忽略退出时间时，transition 可传 `forceInstantly: true`。项目中只建议死亡、硬打断、对象回收等少数路径使用。

## Ghost State

`isGhostState: true` 表示中转状态，进入后立刻检查 outgoing transitions，不等待下一帧：

```csharp
fsm.AddState("ResolveLanding", isGhostState: true);
fsm.AddTransition("ResolveLanding", "LandHard", t => context.LandLevel >= 2);
fsm.AddTransition("ResolveLanding", "LandLight", t => context.LandLevel < 2);
```

Ghost state 适合决策分发，不适合放动画播放或位移逻辑。

## HybridStateMachine

`HybridStateMachine` 适合层级节点需要公共 Enter/Logic/Exit 的场景，例如 Locomotion 父层每帧统一刷新地面和速度，再让子状态处理动画：

```csharp
HybridStateMachine<CharacterStateId> locomotion =
    new HybridStateMachine<CharacterStateId>(
        beforeOnLogic: fsm => context.Motion.RefreshGrounding(),
        rememberLastState: true);
```

普通子树没有公共逻辑时，用 `StateMachine` 即可。

## Action 系统

`AddAction` / `OnAction` 是库提供的“向 active state 发送动作”的轻量通道：

```csharp
State attack = new State()
    .AddAction("AnimEvent", () => context.NotifyAttackWindow());

fsm.AddState("Attack", attack);
fsm.OnAction("AnimEvent");
```

项目中更推荐把动画事件先写入 context 或事件缓冲，再由状态机在下一次驱动中消费。只有需要同步传入 active state 的轻量事件时再用 `OnAction`。

## 调试

常用调试信息：

```csharp
fsm.ActiveStateName;
fsm.GetActiveHierarchyPath(); // 例如 /Locomotion/Move
fsm.HasPendingTransition;
fsm.PendingStateName;
```

可监听：

```csharp
fsm.StateChanged += state => Debug.Log(fsm.GetActiveHierarchyPath());
```

项目不主动删除现有 log。新增 log 要能帮助定位状态路径、输入事实、transition 原因。

## 本项目推荐封装

不要让 MonoBehaviour 直接堆一堆 `AddState`。推荐后续按这个形状落地：

```text
CharacterHfsmDriver
  - 拥有 UnityHFSM StateMachine
  - 每帧从输入/运动/动画外观层收集事实到 Context
  - 调用 Trigger / OnLogic
  - 将状态输出提交给动画外观层和运动驱动

CharacterHfsmContext
  - 纯运行时事实和输出端口
  - 不持有 UnityHFSM 内部状态作为网络同步事实

CharacterHfsmBuilder
  - 从配置或硬编码第一版定义构建 FSM
  - 统一注册状态、transition、trigger transition

CharacterStateId / CharacterEventId
  - enum ID
  - 后续可映射到网络稳定 ID
```

## 和 BBB 参考的关系

BBB 的 `StateMachine` 很薄，核心是 `Exit -> CurrentState -> Enter`。它的解耦主要靠：

- `PlayerBrainSO.AvailableStates` 决定装配哪些状态。
- `PlayerStateRegistry` 用 enum switch 创建状态实例。
- `GlobalInterruptProcessor` 先跑全局 interceptor。
- 普通状态内部仍有不少 `ChangeState(GetState<T>())`。

本项目若使用 UnityHFSM，不建议复制 BBB 的“状态内到处直接 ChangeState”风格。更推荐：

- 高优先级动作先走仲裁层，写入 context。
- FSM transitions 统一读取 context 决定流转。
- 状态负责 Enter/Logic/Exit 的业务输出，不负责到处查 Registry 切别人。

## 测试要求

任何接入角色状态机的变更至少补 EditMode 测试：

- 初始状态进入正确。
- 普通 transition 条件成立/不成立。
- trigger transition 只在触发时切换。
- `needsExitTime` 会等待，`forceInstantly` 只在批准路径使用。
- 层级子 FSM 的 `rememberLastState` 或 start state 符合预期。
- `GetActiveHierarchyPath()` 输出可诊断路径。
- 状态输出只写入约定端口，不绕过动画外观层或运动驱动。

Unity 测试优先用 MCP 跑定向 EditMode。若 Unity MCP 不可用，必须说明未执行，不伪造结果。

## 常见坑

- 忘记 `Init()`：`OnLogic()` 前必须初始化。
- 隐式 start state：第一个 `AddState` 会成为 start state，但项目代码要显式 `SetStartState`。
- transition 同帧进入后会跑新状态 `OnLogic()`：Enter 和 Logic 不能互相假设隔一帧。
- `needsExitTime` 没有 `canExit` 或 `StateCanExit()`：会让 pending transition 卡住。
- `AddTransitionFromAny` 过多：容易把优先级藏在注册顺序里，后续难维护。
- 业务状态直接引用 Animancer、CharacterController、Camera：会绕过本项目的外观层、运动驱动和相机边界。
- 用 string ID 写大型角色状态机：拼写错误运行时才炸，优先用 enum。

