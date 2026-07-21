# Gameplay Lab实机验收记录

## 记录规则

- 只记录真实Unity Play Mode与computer-use操作结果。
- 每条记录写明Scene、Variant、输入、目标条件、障碍条件、Live Debug、runtime diagnostics与Console结果。
- 没有看到的结果写“未确认”，不从代码存在推断实机成立。
- 截图应保留能同时识别Game表现、Variant/diagnostics或Live Debug的关键画面。

## 2026-07-21 StandaloneGameplay基线

### 环境

- Scene：`Assets/Scenes/Standalone/StandaloneGameplay.unity`
- Session：Local Float32 / Unity CharacterController
- 角色：Corin玩家与Neutral训练敌人
- 操作方式：computer-use控制真实Unity 2022.3.62f2c1 Editor

### 已确认

- Unity可以进入Play Mode。
- Game View显示玩家与训练敌人。
- 左键输入会触发Attack表现。
- Attack后玩家位置明显靠近训练目标，说明目标攻击链至少发生了可见位移。
- Timeline Editor存在CorinAttack1Timeline、Motion Warp、Animation与其它Track。
- 本轮Play Mode操作后Console计数为7条普通Log、0 Warning、0 Error；可见Log均为MCP For Unity连接与工具注册信息。
- Live Debug目标菜单列出了`可琳`与`CorinStandaloneTrainingEnemy`两个runtime实例，证明两者都完成了runtime diagnostics注册。
- 两个候选都显示`source missing`；这明确排除了“runtime实例未注册”，问题位于作者Timeline到Program SourceMap的身份与revision合同。

### 未确认

- Live Debug仍显示`Source: Shared Asset / CorinAttack1Timeline`与`None (Timeline)`，没有绑定运行中Timeline实例。
- 未看到MotionWarp trace中的Applied、NoTarget、Clamped或Blocked结果。
- 未同时记录requested displacement与WorldSolver applied displacement。
- 未验证正面、侧面、背面和移动后目标。
- 未验证隔墙、贴墙、墙角、薄墙、狭窄通道和不可达目标。
- 未验证Local Fixed；Source、Pipeline、Host与Variant类型已实现，但Gameplay Lab场景、两个runtime root和Variant资产尚未生成。
- 未建立操作前Console快照，因此当前只能确认操作后的Error计数为0，不能证明没有被Clear或过滤过的历史异常。
- 未观察Walk/Run步态相位匹配与Foot Placement在同一动作中的最终状态。

### SourceMap诊断结论

- Timeline条目携带Timeline/Track/Clip身份时，Runtime Debug旧逻辑仍先取Declaration/Node；Timeline条目因此被归入owner Node，Timeline容器不存在。
- Program SourceMap旧逻辑把ProgramHash当成所有source的ContentHash；Timeline Editor请求的是`TimelineAuthoringFingerprint`，二者不是同一revision口径。
- 正式修复方向是Program SourceMap逐条保存作者容器hash，并按Clip、Track、Timeline、Declaration、Node、Edge、Graph的顺序解析身份；不放宽Editor匹配，不增加名称匹配或运行时猜测。

### 当前复验阻塞

- SourceMap字段、Semantic IR codec、Float/Fixed Program codec与Runtime Debug映射已进入共享工作树。
- `ThirdPersonSimulation.Core.csproj`构建为0 Warning、0 Error。
- Unity通过computer-use刷新后，外部AI、Equipment与Presentation签名错误已清除。当前Console只显示5个DeterministicRollback Unity迁移错误：两个旧`IRollbackLocalInputAdapter`引用、Prepared Source缺少新`BindRuntime`与capacity、旧registration返回类型不匹配。
- `ThirdPersonSimulation.Fixed.Unity.csproj --no-dependencies`为0 Warning、0 Error；`ThirdPersonClient.Runtime.csproj --no-dependencies`包含Gameplay Lab runtime并构建成功，仅保留既有`CharacterInputValueNodes`未使用字段Warning。
- Gameplay Lab runtime已从预定义`Assembly-CSharp`迁入`ThirdPersonClient.Runtime`，Unity生成的脚本与目录`.meta`保持原GUID。Editor launcher已进入`ThirdPersonClient.Editor`。
- Rollback两个旧类仍被并行文件占用保护拒绝写入；本change不复制Prepared Source或增加兼容接口。在这5个错误收口前，不能生成场景资产、重生成正式Program或把Live Debug复验写成通过。

### 操作污染

- Game View捕获鼠标后，computer-use从Timeline或其它面板移动到Game View并点击左键会同时产生较大的Look delta。
- 结果表现为镜头角度突然改变，无法把当前截图当成稳定的距离/角度对照证据。
- 后续Gameplay Lab必须提供进入同一正式Input request链的键盘Attack binding，不能用直接Action调用或Transform控制规避该问题。

## 2026-07-21 Local Fixed启动复验

### 已确认

- `Gameplay Lab`正式资产重建已越过Presentation Projection contract校验并更新Float32与Fixed Variant资产。
- `Local Fixed Q32.32`通过正式Gameplay Lab launcher进入Unity Play Mode。
- 运行场景中同时存在`Gameplay Lab Fixed Player`与`Gameplay Lab Fixed Target`，并且`FixedCharacterHost`实例总数为2。
- 两个Host来自同一Fixed Variant runtime root，没有同时实例化Float32 Character Host。
- 空闲运行10秒后没有项目Error；唯一Warning来自MCP批处理请求被Unity主线程顺序执行，不属于Gameplay运行错误。

### 尚未确认

- 本轮没有发送移动、Attack、Dodge或镜头输入。
- MotionWarp trace、KCC阻挡、Foot Placement与Animation Marker Sync仍未记录。
- 本轮没有执行Deterministic Rollback双端Player Build/Run。

## 待执行矩阵

| Variant | 目标距离 | 目标角度 | 障碍 | Warp trace | Solver结果 | 表现 | Console | 状态 |
|---|---|---|---|---|---|---|---|---|
| Local Float32 | 近 | 正面 | 无 | 未记录 | 未记录 | 攻击与接近可见 | 0 Warning / 0 Error | 部分 |
| Local Float32 | 中 | 侧面 | 无 | 未记录 | 未记录 | 未记录 | 未记录 | 待执行 |
| Local Float32 | Clamp外 | 背面 | 无 | 未记录 | 未记录 | 未记录 | 未记录 | 待执行 |
| Local Float32 | 中 | 正面 | 隔墙 | 未记录 | 未记录 | 未记录 | 未记录 | 待执行 |
| Local Float32 | 近 | 侧面 | 贴墙/墙角 | 未记录 | 未记录 | 未记录 | 未记录 | 待执行 |
| Local Float32 | 中 | 正面 | 薄墙/狭窄通道 | 未记录 | 未记录 | 未记录 | 未记录 | 待执行 |
| Local Fixed | 近 | 正面 | 无 | 未记录 | 未记录 | 空闲Play已进入，Player/Target均实例化 | 0 Error；1条MCP工具Warning | 启动闭环通过，输入待执行 |
| Local Fixed | 中 | 侧面 | 无 | 未记录 | 未记录 | 未记录 | 未记录 | 被场景资产与共享编译阻塞 |
| Local Fixed | Clamp外 | 背面 | 无 | 未记录 | 未记录 | 未记录 | 未记录 | 被场景资产与共享编译阻塞 |
| Local Fixed | 中 | 正面 | 隔墙 | 未记录 | 未记录 | 未记录 | 未记录 | 被场景资产与共享编译阻塞 |
| Local Fixed | 近 | 侧面 | 贴墙/墙角 | 未记录 | 未记录 | 未记录 | 未记录 | 被场景资产与共享编译阻塞 |
| Local Fixed | 中 | 正面 | 薄墙/狭窄通道 | 未记录 | 未记录 | 未记录 | 未记录 | 被场景资产与共享编译阻塞 |
