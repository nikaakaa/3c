# 相机来源事件到工程相机请求的映射

修订 v3，2026-09-17；属于协调 PLAN 的映射交付合同与来源核对结果，不是已经写入 Corin 资产的结果。动作相机请求的正式工程落点改为技能 Graph 内的 TreeClip 特殊 Node，不再以 CameraCueClip 作为动作链最终 owner。此页由 Camera 任务维护；曲线迁移任务消费已确认行，统一写入本批 TreeClip 资产及生成源码。

## 记录规则

一条可写入的映射必须同时具备：原始来源文件/对象/事件身份与证据位置，源动作名，工程 TreeClip/Node 精确资产路径及稳定身份，Node 请求身份或由正式 API 创建的明确落点，效果类型、正式 ResourceId、原时钟与帧率、开始时刻、持续/更新条件、权重/曲线引用、目标与取消/自然结束规则。资源文件名、m_ZoomId 对齐或动作简称均不能替代其余字段。

字段不明时保留该资源的精确缺口和已查证据位置，不造默认持续时间、不用另一效果代替、不要求用户凭空填整表。Camera 继续取证并提供映射；不能越过同批 Timeline/生成源码的单一写入分工。

## 当前线索及边界

来源动作分组和 Normal_01 的单一 Shake 线索来自用户协调通知 `camera-preview-timeline-domain-runtime-r1`。本轮读取了 `D:/ZZZ_Dump/output/corin_replication/replication-guide/data/actions/` 中的对应动作 JSON，并把来源事件身份、归一化时间和导出动作片段记录到下表；来源 JSON 没有为独立 Camera key 提供效果持续时间或显式采样率，所以这些字段仍保持缺口。

| 源动作线索 | 效果 / ResourceId | 本轮工程证据 | 工程 TreeClip/Node 请求 | 时刻/持续/取消 | 当前缺口 |
|---|---|---|---|---|---|
| Attack_Counter | Zoom / `Corin_Attack_Counter_CamZoom_01` | 来源 `sm0-011-Attack_Counter.json`：`$.cameraKeys[4]`；`$.events[id=battle:23:23]` 为 `CameraZoomKey`，`battle:23:24` 为 `EndCameraZoomKey`，均 `resolved=true`、`normalizedTime=0`；动作片段 `Avatar_Female_Size01_Corin_Ani_Attack_Counter.anim` 的 `StopTime=0.9666667`。工程 [正式资源](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Counter_CamZoom_01.asset:16) 的 `m_ZoomId` 一致 | 当前工程没有 Counter 对应的正式 TreeClip/Camera Node 请求；只有资源和生成 Projection 中的资源条目 | 来源只给出 key 的开始/结束引用，未给独立效果持续时间、采样率或工程时钟映射 | 补来源事件到工程 TreeClip/Node 的完整路径；不能按资源同名直接认定 Counter 已接线 |
| Attack_Normal_05 | Zoom / `Corin_Attack_Normal_05_CamZoom_01` | 来源 `sm0-047-Attack_Normal_05.json`：`$.cameraKeys[4]`；`$.events[id=battle:36:19]` 为 `CameraZoomKey`，`battle:36:20` 为 `EndCameraZoomKey`，均 `resolved=true`、`normalizedTime=0`；动作片段 `Avatar_Female_Size01_Corin_Ani_Attack_Normal_05.anim` 的 `StopTime=1.5000001`。工程 [正式资源](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Normal_05_CamZoom_01.asset:16) 的 `m_ZoomId` 一致 | 工程候选为嵌入 [CorinAttack5Timeline](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Abilities/CorinAttackGameplayAbilityDefinition.asset:8347)，现有 Camera ActionCue authoring id `3b26dcef-1d55-478e-a59a-5312a72f9bb4`，24→25 帧；当前仍是 `ActionCueClip`，没有正式 TreeClip/Node 请求或 `ResourceId` | 来源 Camera key 是独立开始/结束引用，工程 24→25 只是现有通用 Cue 的帧范围，不能据此宣布两者时钟相同 | 需要曲线迁移任务把已确认 ResourceId 写入 TreeClip 特殊 Node 并保留来源身份；在此之前不认定映射闭合 |
| Attack_Normal_05_End_2 | Zoom / `Corin_Attack_Normal_05_CamZoom_02` | 来源 `sm0-049-Attack_Normal_05_End_2.json`：`$.cameraKeys[3]`；`$.events[id=battle:38:18]` 为 `CameraZoomKey`，`battle:38:19` 为 `EndCameraZoomKey`，均 `resolved=true`、`normalizedTime=0`；动作片段 `Avatar_Female_Size01_Corin_Ani_Attack_Normal_05_B.anim` 的 `StopTime=3.0666666`。工程 [正式资源](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Normal_05_CamZoom_02.asset:16) 的 `m_ZoomId` 一致 | 没有独立 End_2 TreeClip/Node 请求身份；现有 `CorinAttack5Timeline` 的通用 Camera Cue 未携带 `ResourceId`，不能选择 Zoom 01 或 Zoom 02 | 必须区分 End_2 的来源动作和主动作的结束规则；来源未给独立效果持续时间/采样率 | 补 End_2 到工程 TreeClip/Node 的正式落点；不能按 `01/02` 序号或现有 Attack5 Cue 猜接线 |
| Attack_Normal_01 | Shake / `Corin_Attack_Normal_01_CamShake_A_01` | 来源 `sm0-038-Attack_Normal_01.json`：`$.cameraKeys` 只有这一项；`battle:27:0`、`:1`、`:2`、`:3` 的 `CameraShake.shakeConfigKey` 均指向它，归一化时间分别为 `0.4120000005`、`0.4320000112`、`0.4519999921`、`0.4709999859`；动作片段 `Avatar_Female_Size01_Corin_Ani_Attack_Normal_01.anim` 的 `StopTime=0.86666673` | 工程 Camera 目录当前没有该 Shake 的正式资源；不得把 [CorinAttack1Timeline](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Graphs/SharedTimelines/CorinAttack1Timeline.asset:249) 的通用 `Attack1CameraCue` 认作 TreeClip Node，现有 ActionCue 也没有 `ResourceId` | 来源给出四个攻击事件时间点，但没有 Camera Shake 持续时间/曲线正文；工程没有可消费 ResourceId | 补指定 Shake 的正式资源、原消费者和 TreeClip/Node 落点；禁止以 Zoom 替代 |

这些行只覆盖本次协调提供的线索，不宣称是原动作的完整事件清单，也不代表所有 81 Shake、18 Zoom、18 Stretch、4 Override 均有对应工程 Cue。

## 本轮来源证据

- `sm0-011-Attack_Counter.json` 的 `cameraKeys` 同时声明 Counter 的 Shake A、Shake E1/E2、Stretch 01 和 Zoom 01；本轮只把 Zoom 01 的 `battle:23:23/24` 作为 Zoom 映射证据，不能把其它 key 的存在转换成工程请求。
- `sm0-047-Attack_Normal_05.json` 的主动作声明 Zoom 01/Stretch 01；`sm0-049-Attack_Normal_05_End_2.json` 的 End_2 动作声明 Zoom 02/Stretch 02。两份来源的独立 Camera key 事件都用 `normalizedTime=0` 表示开始/结束引用，不能直接换算为工程 Timeline 帧。
- `sm0-038-Attack_Normal_01.json` 的 Camera key 只有 Shake A，四个 `AttackProperty` 事件时间点均指向同一 key；这条来源证据不支持 Normal_01 使用任何 Zoom 资源。
- 工程侧 `CorinAttack5Timeline` 的 `Attack5CameraCue` 当前仍为通用 `ActionCueClip`，`CorinAttack1Timeline` 的 `Attack1CameraCue` 也同样没有 `ResourceId`。两者只能作为待迁移的历史事件线索，不能作为 TreeClip/Node 或已采用的 Camera Resource 证据。

## 对接结果

Camera 提供已确认映射行和相机资源/运行绑定。曲线迁移任务在其拥有的同批 TreeClip 与生成源码中落实对应 Node 请求，双方不分别重建同一资产。Camera 的绑定结果须报告实际采用的资源与请求身份；预览展示这一真实结果，不从表中推测运行镜头已经生效。

本轮没有写入 Timeline、生成源码或 Camera 资源；旧 Projection 和旧域 DLL 的调用结果均不能补充这里缺失的工程身份或来源证据。
