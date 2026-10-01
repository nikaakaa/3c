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
