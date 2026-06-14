# Design: 角色配置根 SO

## Context
当前 `PlayerLocomotionController` 的配置加载路径：

```csharp
// 当前：4 个平行序列化字段
[SerializeField] CharacterStateMachineDefinitionSO stateMachineDefinition;
[SerializeField] RunLocomotionAnimationConfigSO runAnimationConfig;
[SerializeField] TurnInPlaceAnimationConfigSO turnInPlaceAnimationConfig;
[SerializeField] BasicMovementConfigSO config;
```

BBB 的 `PlayerSO` 将所有子模块收敛到一个根 SO：

```csharp
public class PlayerSO : ScriptableObject {
    public PlayerBrainSO Brain;           // 状态机
    public CoreSO Core;                   // 基础参数
    public LocomotionSO LocomotionAnims;  // 动画配置
    public AimingSO Aiming;
    public ActionSO Action;
    ...
}
```

## Goals
- `PlayerLocomotionController` 只持有一个 `CharacterConfigSO` 引用
- 所有子 SO 的配置路径通过根 SO 可见
- 新增子模块时不需要在 Controller 上开新字段
- 不引入额外运行时开销（解引用子 SO 是零开销的指针拷贝）

## Non-Goals
- 不改子 SO 的数量或内容
- 不引入标签、排序、LOD 或运行时热重载
- 不为子 SO 增加依赖图或自动图验证

## Decisions
- Decision: 使用单独的 `CharacterConfigSO` 类型，不把子模块直接嵌入状态机定义
 - Reason: 逻辑状态机 `CharacterStateMachineDefinitionSO` 应保持只关心状态拓扑；运动参数和动画配置是不同关注点，分离更干净
 - BBB 参考: `PlayerSO` 同样是独立于 `PlayerBrainSO` 的根节点

- Decision: 子 SO 仍保留独立 CreateAssetMenu
 - Reason: 设计者需要能单独创建和编辑子 SO，再通过根 SO 引用组装
 - BBB 参考: `LocomotionSO`、`JumpSO` 等同样保留独立入口

- Decision: `CharacterConfigSO` 放在 `Assets/Configs/3C/` 根目录
 - Reason: 作为最高层配置入口，不应藏到子目录；BBB 的 `PlayerSO` 同样放在 `ConfigData/` 根

## Proposed Code

### CharacterConfigSO
```csharp
[CreateAssetMenu(fileName = "CharacterConfig", menuName = "3C/Character/CharacterConfig")]
public sealed class CharacterConfigSO : ScriptableObject
{
    [SerializeField] CharacterStateMachineDefinitionSO stateMachine;
    [SerializeField] BasicMovementConfigSO movement;
    [SerializeField] RunLocomotionAnimationConfigSO locomotionAnimation;
    [SerializeField] TurnInPlaceAnimationConfigSO turnInPlace;

    public CharacterStateMachineDefinitionSO StateMachine => stateMachine;
    public BasicMovementConfigSO Movement => movement;
    public RunLocomotionAnimationConfigSO LocomotionAnimation => locomotionAnimation;
    public TurnInPlaceAnimationConfigSO TurnInPlace => turnInPlace;
}
```

### PlayerLocomotionController 改动
```csharp
// 旧
[SerializeField] RunLocomotionAnimationConfigSO runAnimationConfig;
[SerializeField] TurnInPlaceAnimationConfigSO turnInPlaceAnimationConfig;
[SerializeField] BasicMovementConfigSO config;
[SerializeField] CharacterStateMachineDefinitionSO stateMachineDefinition;

// 新
[SerializeField] CharacterConfigSO characterConfig;
```

Controller 内所有引用子 SO 的外部接口改为从 `characterConfig` 解引用。内部 `Resolve*` 方法保持签名不变，只需改变从哪个字段获取。

## Migration
1. 创建 `CharacterConfigSO` 类
2. `PlayerLocomotionController` 增加 `characterConfig` 字段，旧字段标记 `[Obsolete]`
3. 将内部 `ResolveTurnInPlaceAnimationConfig()`、`ResolveRunAnimationConfig()`、`config` 引用改为读入根 SO
4. 在 `Assets/Configs/3C/` 下创建 `CharacterConfig.asset`，引用已有子 SO 资产
5. 在场景中为 `PlayerLocomotionController` 赋值新根 SO
6. 验证运行时行为不变
7. 删除旧序列化字段（清理序列化数据后）

## Risks / Trade-offs
- Risk: 旧字段序列化数据残留导致 Controller 在升级后加载两份配置
 - Mitigation: 实现时先增加新字段、逻辑走新字段；确认无误后在 Editor 中清除旧字段序列化数据

- Risk: `CharacterConfigSO` 被过度填充，变成巨型万能配置
 - Mitigation: 严格只收敛当前已有子模块；后续新增模块时按需扩展，不预填空位
