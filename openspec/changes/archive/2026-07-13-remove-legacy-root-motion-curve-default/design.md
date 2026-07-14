# Design: Root Motion 曲线显式模式硬切换

## Context

当前独立曲线资产的代码链为：

```text
AnimationClip + 采样对象
-> RootMotionCurveBaker
-> RootMotionCurveAsset
-> RootMotionCurveEvaluator
```

但这条链当前没有接入 Corin 的正式运行时位移。Corin RootTree 中的 `MotionCurveTrack` 直接持有 `MotionCurveClip.PositionX/Y/Z/Yaw`，由 Timeline 采样为 motion contribution。`RootMotionCurveAsset` 没有被 Timeline 序列化引用，求值器也没有生产调用者。

因此，本 change 的目标是收紧离线曲线资产的语义，不把它伪装成当前 Timeline 的运行时输入，也不新增一条自动同步或旁路。Timeline 内联曲线继续是唯一的运行时 motion fact。

## Baseline

| 资产组 | 当前状态 | 本 change 的处理 |
| --- | --- | --- |
| `Corin/Pipeline/Motion/Curves` 的 Attack1、Attack2、DodgeBack、DodgeForward、MovingTurn | 均为 `evaluationMode: 0`；累计 XYZ/yaw 已表达完整本地轨迹 | 一次性写为显式 `FullLocalDelta` |
| `BakedAnimation` 的两个 TurnBack 输出 | 缺少模式字段；无非 `.meta` 引用 | 删除，不迁移 |
| Corin RootTree 内联 `MotionCurveClip` | 当前 Runtime 正式位移事实 | 不改数据、不建立对曲线资产的运行时引用 |

`ForwardDistanceYaw` 当前 Baker 将每帧平面位移的 magnitude 累计为正前向距离；它不能表达后撤的 signed Z，也会丢掉横向轨迹。因此本次保留的五个资产全部使用 `FullLocalDelta`，而不是根据资产名称或零值猜测前向模式。

## Decisions

### Decision: 零值只表示未指定

`RootMotionCurveEvaluationMode.Unspecified` 占用零值，`FullLocalDelta` 与 `ForwardDistanceYaw` 使用稳定非零值。只有后两者可进入正式烘焙结果或求值。

业务取舍：现有零值资产必须被一次性处理，但缺失配置不会再伪装成有意的运动设计。攻击、闪避和转身的位移语义可追踪。

### Decision: 保留五个正式 Corin 烘焙输出，删除两个失效旧输出

五个 `Corin/Pipeline/Motion/Curves` 资产的累计 local XYZ 与当前用途一致，硬切时显式写入 `FullLocalDelta`。两个 `BakedAnimation` TurnBack 输出没有模式字段、没有引用，并且与 MovingTurn 的源动画和数据重叠，因此直接删除。

业务取舍：保留仍有作者价值的曲线资产，删除没有消费者的历史输出。不会用 runtime 兼容代码掩盖错误，也不会把重复数据继续当成可用资产。

### Decision: RootMotionCurveAsset 是离线 authoring 数据，Timeline 内联曲线是 Runtime 真相

独立资产可以由 Baker 创建、检查和重新烘焙；`MotionCurveClip` 负责 Timeline 实际提交的 motion contribution。两者之间没有运行时读取、按名称查找、自动复制或双向同步。

业务取舍：当前 Runtime 只有一条位移输入链，调试和网络表现不会受到未引用烘焙资产影响。代价是本 change 不提供“从资产一键转写为 Timeline clip”的编辑器体验；需要该体验时应单独设计正式导入命令、覆盖规则和 source identity，不能偷接在求值器或 Timeline 运行时。

### Decision: 正式 Baker 必须要求作者选择模式

Baker 的创建状态使用 `Unspecified`。未选择有效模式时，UI 禁止或拒绝烘焙；模式转换使用穷尽分支，不保留默认返回 `FullLocalDelta` 的路径。`SetBakedData` 只接受有效模式。

业务取舍：作者多一次选择，但新资产不会因 UI 默认值或默认分支带入隐式运动策略。

### Decision: 求值器拒绝非法模式而不推断曲线

资产校验和求值器将 `Unspecified`、未知数值和缺失模式视为错误。求值器不根据 XYZ 曲线、前向距离曲线、资产名称或目录推断模式；无效资产不会产生 sample 或 delta。

业务取舍：错误资产不会贡献位移，可能暴露作者配置问题；这比沿错误坐标语义推进角色逻辑位置更安全。

## Alternatives

### 方案一：保留零值为 FullLocalDelta

短期能继续读取旧资产，但缺失字段仍会伪装成有效设计，无法定位错误；不采用。

### 方案二：把全部七个资产迁移为 FullLocalDelta

能避免删除数据，但会把两个无引用、缺字段的旧 TurnBack 输出继续保留为表面可用的重复来源；不采用。

### 方案三：根据曲线内容或资产名称推断模式

前向距离和 XYZ 曲线可同时存在，后退曲线也无法由 magnitude 恢复符号；推断不可靠且不可追溯；不采用。

### 方案四：在本 change 中自动把 RootMotionCurveAsset 导入 Timeline

可以减少手动转写，但会新增 authoring 编译/覆盖规则，且当前 Timeline 资产并没有该引用模型。把它塞进本 change 会制造隐式双数据源；不采用，后续独立规划。

### 方案五：删除整个 RootMotionCurveAsset/Baker 子系统

可以彻底减少一类 authoring 数据，但会失去离线采样、检查和重烘焙动画位移的工具。当前仍保留五个 Corin 曲线资产作为有效 authoring 输出，因此不在本 change 删除该子系统。

## Risks

- 任何遗留的 `else -> FullLocalDelta`、字段初始化或 Baker 默认转换都会重新引入隐式语义，实施时必须全局扫描。
- 直接改变枚举数值而不先写入五个显式 `FullLocalDelta` 值，会让这些资产变成 `Unspecified`；资产迁移和枚举硬切必须在同一次实施完成。
- 两个删除资产虽无非 `.meta` 引用，实施时仍须再次扫描引用，避免覆盖用户刚加入的配置。
- 当前没有正式资产到 Timeline 的导入器。该缺口不应由本 change 用 runtime fallback 填补。
