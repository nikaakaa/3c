# 动画同步补缺记录

本轮分析 Corin、Unagi、Anbi 的主控制器及同版本 GameAssembly。已读出静态切入参数，并追到一条「数据对象 → 请求队列 → 每层记录 → CrossFade」的目标播放位置链路。尚未恢复出可直接配置的 SyncGroup 成员、主从选择或脚步标记同步规则。

## 三个角色目前读到了什么

| 项目 | Corin | Unagi | Anbi |
| --- | --- | --- | --- |
| 重新导出的状态数 | 105 | 111 | 88 |
| 绑定 TimeParameter 的状态 | 0 | 0 | 0 |
| 非零状态 CycleOffset | 0 | 0 | 0 |
| 绑定 CycleOffsetParameter 的状态 | 0 | 0 | 0 |
| 多子节点 BlendTree 节点 | 0 | 4 | 0 |
| 启用自动切入偏移的转场 | 1 | 5 | 3 |

原解析器读取了 `m_TimeParamID`，但只存进局部变量，结构化导出遗漏了它。现在已正式保留该成员并导出 `TimeParameter` 名称。三个控制器按原 CAB/PathID 重新导出，除每状态新增这两个字段外，既有字段没有变化；304 个状态的该 ID 全是 0。这是排除一种时间参数绑定方式的证据，不是排除所有运行时代码调时。

原解析器里的 `m_SyncGroupID` / `m_SyncGroupRole` 读取属于 `IsArknightsEndfield()` 分支，不能拿来当作 ZZZ 已导出的同步组数据。代码改动保存在 [解析器差异](Classes-AnimatorController.cs.diff) 和 [导出器差异](CorinControllerStructuredExporter-Program.cs.diff)。

Unagi 的四个多子节点来自左右方向闪避，各有两个 MotionSet；参数是 `Float_JoyStickDir_Temp`，不是走跑混合树。它的层 0/3 共享状态机 0，层 1/4 共享状态机 1，分别用 MotionSet 0/1；所有层的 `m_SyncedLayerAffectsTiming` 为 false。共享状态机这件事不能直接解释成脚步同步组。

三名角色的 `Walk_Loop → Run_Loop` 都保存混合时长 0.2、固定时长开启、目标偏移 0、自动偏移关闭。其它移动转场有明确非零目标切入位置，例如 Corin 的 `Walk_Start → Walk_Loop` 偏移约 0.8332343、偏移帧 29；必须连同帧模式等原开关阅读，不能全都改成「继承上一段相位」。

逐角色表格保留全部移动转场、自动偏移例外、完整条件与原始 JSON：

- [Corin 动画同步](../../../corin_replication/replication-guide/动画同步.md)
- [Unagi 动画同步](../../../unagi_replication/replication-guide/动画同步.md)
- [Anbi 动画同步](../../../anbi_replication/replication-guide/动画同步.md)

三份 CharacterScriptConfig 另外核实了混合覆盖根字段：`BlendData=null`、`StartBlendData=null`、`BlendDataByTag={}`。这仅说明所选配置没有在这几个入口填覆盖值。Corin 主资料包尚未纳入全部角色根字段，本次单独核实的来源与原值保存在 [native-evidence.json](native-evidence.json) 的 actors 中。

## 代码里的目标播放位置从哪里来

目前确认的磁盘本机分支如下，混淆名只用于精确对账，不擅自改成业务名称。

| 环节 | 证据 | 已确认的行为 |
| --- | --- | --- |
| 数据对象转请求 | `ILJHPKCIBPH.JCJBCKAHOKC`，RVA `0x15BEC110` | 读取 `IAKAPNCLCNP +0x4C` 的低 16 位，按有符号整数转换，乘文件常量约 `0.000030518815`，写入请求 payload `+0x10` |
| 入队 | 调用点 `0x15BEC34F` | 将 64 字节 `CKHHEGOGGCG` 请求交给 `IILNEIJILBJ +0x30` 的队列；被调函数 `0x0D96A250` |
| 出队并消费 | `GKKGNAMGLBE`，RVA `0x15BDB510` | 从同一队列取请求，在调用点 `0x15BDB78E` 交给 `INEHFNNLNAN`；出队函数 `0x0D96A4D0` |
| 保存每层位置 | `INEHFNNLNAN`，RVA `0x1538A4C0` | 按请求层号取得 `DGIKPMOOLBH`，在 `0x1538A9F2–0x1538A9F8` 把请求 `+0x10` 写到每层对象 `+0x30` |
| 立即指定切入 | 调用点 `0x1538AB79` | 条件满足且层号为 0 时，将请求 `+0x10` 作为 `CrossFade` 的 normalizedTimeOffset |
| 使用保存的位置 | `JOOHDOAJDDP`，RVA `0x15BDA820`，调用点 `0x15BDAE19` | 条件满足时，将每层对象 `+0x30` 作为 `CrossFade` 的 normalizedTimeOffset |

这里的队列消费有状态、层号及其它分支判断；表格不是说所有请求都无条件立即切换。IAKAPNCLCNP 上游如何生成这几个字段、哪些业务会发送它，目前没有解完，暂不把它定性为本地脚步同步或网络校正。

两处显式传入位置的调用都使用 `Animator.CrossFade(int,float,int,float,float)`：归一化混合时长约 0.1、目标层 0、normalizedTransitionTime 为 0。**这里的 0.1 是归一化混合时长，不能抄成 0.1 秒**；也不能拿它覆盖控制器内其它正常转场的时长。没有实际运行采样证明这些分支在三名角色的普通移动时触发。

## NormalizedTime 参数和目标切入位置不是同一个值

| 消费位置 | 请求中的来源 | 数据对象中的来源 |
| --- | --- | --- |
| CrossFade 的 normalizedTimeOffset | `ECNEMHNBJNG`，payload `+0x10` | `ECLOLGMKNKJ`，对象 `+0x4C`，按低 16 位解压 |
| Animator 的 `NormalizedTime` 参数 | `DDHGOMCPLCD`，payload `+0x28` | `BPLPPFDGLLN`，对象 `+0x38`，另一处低 16 位解压 |
| Animator 的 `FrameCount` 参数 | `IDJBKNDLIIP`，payload `+0x2C` | `IBICBGALMID`，对象 `+0x58` |

SetFloatID 和 SetIntegerID 的原生调用槽已分别对上 Animator 方法。静态参数 ID 从同版本 829 快照读取，再匹配主控制器 Tos：`0xF88C9E81 = NormalizedTime`，`0x82587B50 = FrameCount`。没有把 `.bss` 中文件不存在的静态值当成零，也没有按字段名字猜参数 ID。

`CKHHEGOGGCG` 是值类型，上述偏移是未装箱 payload 偏移；不能直接用带 16 字节对象头的 metadata_offset。原始字段、类型、运行时核实标记均保存在证据 JSON。

## 能配置到什么程度，还差什么

| 需要复刻的行为 | 当前已有证据 | 仍缺的证据 |
| --- | --- | --- |
| 普通转场从目标哪里进入 | 每条转场的 Offset、OffsetCount、固定时长、帧模式、中断和退出条件 | ZZZ 特殊字段在引擎中的完整计算与优先顺序 |
| 自动切入 | Corin 1 条、Unagi 5 条、Anbi 3 条的自动值/比例/总帧数已保留 | `m_AutoTransitionOffsetValue/Ratio` 在此版本 UnityPlayer 中的计算公式，尤其 ratio=-1 和 4 |
| 代码强制进入某个位置 | 请求、队列、每层存储到 CrossFade 的传值路径 | 上游值的生成方式、触发条件及实际运行调用 |
| SyncGroup / 脚步相位同步 | 当前主控制器没有恢复出显式组成员或标记表 | 是否存在引擎内部同步，leader/follower 的选择、marker 对齐和循环时间处理 |
| 输入响应窗口 | 原 Zone 起止、Max 标志、转场条件、点击/长按映射，以及已有帧窗口消费者分析 | 缓冲保存多久、何时消费、条件检查先后和中断时是否保留输入 |

这里没有给缺失项填经验参数。已有静态参数可以逐项对照 BTSMTL 的正式作者字段；上表未确认的规则还不能声称可以一比一配置完成。本轮只整理原始证据，没有修改 Unity 作者资产或运行逻辑。

## 证据范围与复查入口

- [native-evidence.json](native-evidence.json)：来源 SHA-256、完整字段、参数 ID、文件常量、控制器对账和选定函数。
- [播放调用扫描](native-calls/index.json)：4 个 Animator 原生槽，78 个已确认指令边界的调用/跳转点，65 个函数；范围是 GameAssembly 的直接调用和已确认槽的 RIP 相对调用，不包括寄存器间接调用或 UnityPlayer 内部调用。
- [数据对象转请求](native-flow/function-15BEC110.asm)、[队列消费](native-flow/function-15BDB510.asm)、[请求处理](native-flow/function-1538A4C0.asm)、[每层位置消费](native-flow/function-15BDA820.asm)。

GameAssembly SHA-256：`4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4`。元数据与 829 快照使用同一模块版本；重建分析时再次核对 GameAssembly、快照二进制和页映射的哈希。磁盘函数保留热更新重定向分支，所以这里记录的是已解出的本机代码行为，没有把它冒充为当前游戏实例的执行轨迹。
