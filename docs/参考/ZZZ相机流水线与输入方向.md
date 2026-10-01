# ZZZ 相机流水线与输入方向

核对日期：2026-10-01。范围是 829 快照中的战斗相机 `NapVirtual3DActionCamera_1`，不是整个游戏所有相机。当前项目完整复刻仍未完成。

## 来源

原生代码来自 `D:/Normal_Software/HoYoPlay/games/ZenlessZoneZero Game/GameAssembly.dll`，证据包对应 SHA-256 为 `4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4`。类型、布局和运行 MethodInfo 使用 829 元数据与快照页核对；读取入口为 `D:/ZZZ_Dump/PIK分析包/元数据/export_gameplay_metadata.py` 中的 `GameplaySession`、`exported_type`，原生函数边界来自 PE 的 `.pdata`，指令用 Capstone 解码。

既有主函数证据位于 `docs/diagnostics/camera/camera-basis-runtime-20260929/camera-update.json` 与 `camera-stage-0x134db160.json`、`camera-stage-0x134db1e0.json`、`camera-stage-0x134db300.json`、`camera-stage-0x134db450.json`。本轮重新解码初始化函数 `0x134DEBD0` 和通用模块创建函数 `0x06C72C70`，通过各次调用的运行 MethodInfo 泛型上下文取得实际模块类型，不按枚举字段声明顺序猜注册顺序。

## 两阶段模块执行

`InternalUpdateCameraState` 位于 `0x134DAB80`，正常路径依次执行：

| 阶段 | 原生入口 | 输入、处理与输出 |
| --- | --- | --- |
| 帧前准备 | `0x134DB160`，调用 `0x134E3910` | 接收本帧 delta，更新后续模块读取的相机上下文。 |
| 模块状态更新 | `0x134DB1E0` | 按注册列表遍历所有模块，传入 delta、`AvatarInfoData` 的引用与 `AvatarPrepareData`，调用虚表 `+0x110`。这一步不等于每个模块都直接写最终位置。 |
| 模块求解 | `0x134DB300` | 按同一列表先调用虚表 `+0x108` 判断资格，合格后调用 `+0x118`，传入同一 `CameraState` 与 `AvatarInfoData` 引用。 |
| 最终输出 | `0x134DB450` | 完成输出转换、混合与附加位置／旋转修正，再写 Transform；不能把模块内部中间角度当成最终操作方向。 |

初始化 `0x134DEBD0` 按以下顺序创建并加入 `+0x240` 的唯一模块列表；`0x06C72C70` 先装配相机及 accessor，再将同一实例加入列表。

| 顺序 | 类型与职责 | 状态更新 RVA | 求解 RVA |
| --- | --- | --- | --- |
| 1 | `PLNPMBCCNGE`，状态参数与混合 | `0x1A667400` | 继承基类 `0x178025B0` |
| 2 | `BEMOIHLCNEM`，Zoom | 继承基类 `0x17802550` | `0x11FE5630` |
| 3 | `DFLBCIKIEPE`，转镜控制，持有手动拖动实例 `FEBAFIHIDGP` | `0x1A282E70` | `0x1A283560` |
| 4 | `EJKKEBPOLME`，Delay | `0x14A39AA0` | `0x14A39B00` |
| 5 | `NHEFILHBNND`，Shake | `0x174DD360` | `0x174DD3C0` |

Zoom 的职责还由既有 `CameraDataAccessor.Zoom` 消费者 `0x11FE63B0` 核对；Shake 的职责由既有 `AONHPMMKECA → CameraDataAccessor.Shake` 证据核对。Stretch 和 Pitch 的实例入口不应仅凭此表被另算成常驻模块或移到 Shake 之后。

## 上下方向的边界

原作鼠标 action 为 `InLevelCameraMousePositionDelta`，绑定 `<Mouse>/delta`，action processor 为 `ScaleVector2(x=0.1,y=0.1)`。原始轴 `m_InvertInput=false` 与玩家反向设置只描述输入配置，不能据此推断当前项目的最终镜头方向。

当前项目调用链是：Look action 原始 Y → 输入适配器锁存 → 相机域 → `CharacterCameraFramePlanner.ResolveLook` 的 `AxisDirection` → `m_ElevationAxis.Step(-axisInput.y, ...)` → 轨道 → Override／Zoom／Stretch 构图 → Aim 解析 → Delay → Shake／Shot → 限位 → 碰撞 → Rig。只有进入手动轨道比例的边界取负，效果的归一化仰角不经过此转换。

三个量不能混用：

| 量 | 当前项目约定 |
| --- | --- |
| 轨道比例 | 从低轨道到高轨道；比例增大使相机位置升高，朝向目标时通常表现为低头。 |
| `CameraFramePlan.Pitch`、Rig Pitch | 由最终前向 Y 求得，朝上为正。 |
| Unity 欧拉 X | 正值使前向朝下，符号与方向 Pitch 相反。 |

方向检查必须看最终 `Rotation * Vector3.forward` 与方向 Pitch。`b6dad249d` 曾用相机位置升高判断正 Y 正确，实际前向 Y 从 `-0.147755116` 降到 `-0.405829638`，该结论已在检查记录中更正。`be5a30aeb` 仅修正手动 Y 到轨道比例的转换，本轮保留此行为。

## Follow、Aim 与碰撞的职责

原作轨道消费者 `0x134DD0C0` 分别将 Follow、LookAt 偏移加入绑定点，先由 Follow 和轨道半径求位置，再由 LookAt 减相机位置求前向。`AvatarPrepareData` 中 `CameraBaseRootTrans` 为 `+0x80`，`CameraLookTrans` 为 `+0x88`。本轮进一步核对到 `0x134DAEF1` 使用的字符串是 `CameraLook`，经模型查找后写入 `+0x88`；这尚未确定原作绑定点的局部位置或高度。

项目 Delay 负责跟随和构图阻尼；环境约束接收已完成的最终期望计划，只负责安全位置和碰撞缩回／恢复。修前环境层对每个 `Clear` 帧都执行三维 `SmoothDamp`，导致转镜角度已更新而位置仍沿上一帧世界路径追赶。修后只有实际缩短距离尚未恢复时，才沿本帧 Pivot 到期望位置的射线衰减缩短比例，并用同一环境查询确认恢复位置安全。比例由本帧安全距离与期望距离确定；同时推镜时以新的期望距离计算，不携带旧轨道的固定米数。碰撞不另存方向、不额外平滑正常跟随或技能距离。

上述顺序和职责核对不证明 Follow／Aim 高度、完整 Delay 资格、全部效果生命周期或实机手感已完成原作对齐。

## 战斗相机碰撞的实际消费者

2026-10-01 继续对照碰撞。`Default_Normal` 的 `MUTE_CAMERA_COLLIDER=false`、`CollisionStrategyAlgorithmType=0`。本节以实际调用与 829 快照的分支字节为依据，不把同时存在的 Custom／Cinemachine 配置或类型视为已启用的执行路径。

| 原生入口 | 已确认的输入、处理与输出 |
| --- | --- |
| `0x134DBAFF → 0x134E3260` | 最终输出阶段把同一份 `CameraState` 和本帧 delta 交给碰撞处理，之后才写 Transform。 |
| `0x134E332A → 0x1069C800` | 主相机 `+0xC8` 的 `OEEFKMHDJGM` 接收 `CameraState&`，沿目标到镜头的方向写回 `PositionCorrection`。 |
| `0x1069C8F1 → 0x1229D1D0` | 读取当前 Avatar 配置；检查 `+0xC8` 的 `CinemachineCollisionConfig` 与 `+0xC1` 的静默开关。该原生路径没有读取 `+0xC4` 的算法枚举或 `+0xD0` 的 Custom 配置。 |
| `0x1069C9E4 → 0x106AB020` | 先用射线求视线可用距离。普通相机半径进入查询距离，命中后以距离减 `0.0001` 米计算位置；这与后面的体积球扫是两个阶段。 |
| `0x1069CBC5 → 0x106AB350` | 再处理镜头附近的体积保护，输入包含旋转前向、FOV、近裁剪值及配置；先查询保护球，重叠时再沿当前目标方向球扫求距离。保护体随镜头参数变化，不能用固定半径加近裁剪值宣称等价。 |
| `0x1069CBE9 → 0x1069B470` | 消费当前／上一帧命中平面和保存的距离修正，经 `0x1F949C40` 与配置 `m_Damping` 推进恢复，再把安全距离写回同一 `PositionCorrection`。 |

上述快照中的改写分支均关闭：`0x053CA817`、`0x053FED5D`、`0x053FED5E`、`0x053FED5C`、`0x053FED5B`、`0x053FED5F` 为 0；`0x0536AC67` 为 0，主相机临时采用角色参考位置求解后恢复原 `ReferenceLookAt`。原生函数按来源一节的 PE／元数据入口重新解码。

原作视线查询 `0x129642D0` 还消费运行配置的 `RunningIgnoreTag`（`+0x6C`）和 `RunningTransparentTag`（`+0x70`）；体积球扫进入 `0x12969C20`。这与项目仅有自身／层／触发器过滤仍有差异。原作资源保存普通／地面半径 `0.02`／`0.05`、最小目标距离 `0.1`、恢复 damping `1.5` 等字段，但仅有序列化值不证明每一项都被这条战斗路径消费；地面分类与其它阻尼字段仍需沿消费者核对。

本批修正项目球扫的三项独立错误：`RaycastHit.distance` 已是球心沿扫掠方向的行进距离，不能再次减半径；上一帧重叠不能否决本帧已经无遮挡的目标位置；返回安全结果前必须确认本帧候选保护球没有重叠。当前查询复用同一球扫函数处理已有的目标线段和移动线段，Reset 或旧位置重叠时不消费旧移动段。目标线段无遮挡时仍不执行移动扫掠；原作上述路径也没有上一帧位置到本帧期望位置的独立扫掠，完整转角策略仍未对齐，不能仅补调用就标记复刻完成。

当前项目仍使用 `0.2 + 0.05` 米保护球、`SmoothTime=0.08` 的缩短比例恢复和项目层过滤 `-5`。这些是现有配置，不是已完成的 ZZZ 参数移植；完整近裁剪保护体、特殊过滤、命中平面恢复与转角行为尚未闭环。
