# 曲线迁移操作单：相机请求写入 Corin TreeClip

修订 v1，2026-09-17。本页是曲线迁移任务写入相机请求时的逐步操作单，消费 [source-cue-mapping.md](source-cue-mapping.md) 的已确认行。只描述当前可执行步骤与验收，不重写映射合同。

## 工厂能力现状（2026-09-17 对账）

- `btsmtl.export_code` / `btsmtl.generate_assets` 已支持 Timeline 生成：产物 `CorinAttackGameplayAbilityAuthoringCode/Attack.cs` 含 `EnsureTimeline`、Timeline 资产 `ResolveExternalAsset` 引用与 `TimelineAuthoringPropertyContract.Apply` 属性写入。
- 运行编译侧相机 Node emitter 已就绪：`BtsmtlSkillFlowLeafEmitter.cs` 含 `RequestCameraStateNode`/`RequestCameraEffectNode`/`SetCameraResponseNode` 分支与 `DeclareCameraProducer`。
- 导出侧相机 Node 走 FlowNode 泛化机制，尚无实例验证；首次写入后按下方验收步骤确认。

## 写入步骤（每个来源动作一次）

1. 在对应技能 Graph 的 TreeClip 图内放置 `camera-effect-request` Node（瞬态 Zoom/Shake 用它；持续状态切换才用 `camera-state-request`）。
2. Node 字段按映射表填写：`ResourceId` 填正式资源 Id；效果持续由资源自身字段承载（Zoom 资产的 Delay/End），Node 不另造时长。
3. 保留来源身份：Node 的 sequenceId/actionContext 按所在动作填写，保证循环/取消/seek 只触发一次。
4. 写入完成后删除同片段内的旧通用 `ActionCueClip(CueType: Camera)`（如 `Attack1CameraCue`），不保留双轨。
5. 跑 `btsmtl.export_code` + `btsmtl.generate_assets`，验收产物：生成源码出现该相机 Node 的 `EnsureFlowNode` 行与 ResourceId 字段；资产内旧 Cue 消失。

## 逐条操作（按映射表已确认行）

| 来源动作 | 放 Node 的片段 | ResourceId | 备注 |
|---|---|---|---|
| Attack_Counter | Counter 对应 TreeClip（当前工程尚无该片段时随 Counter 动作片段一并创建） | `Corin_Attack_Counter_CamZoom_01` | 来源开始/结束引用 `battle:23:23/24`，`normalizedTime=0`；持续由 Zoom 资源字段承载 |
| Attack_Normal_05 主动作 | Normal_05 主动作 TreeClip | `Corin_Attack_Normal_05_CamZoom_01` | 替换原 `CorinAttack5Timeline` 内 24→25 帧的通用 Cue 时同步删除旧 Cue |
| Attack_Normal_05 End_2 | End_2 动作 TreeClip | `Corin_Attack_Normal_05_CamZoom_02` | 与主动作 Zoom 01 区分，禁止按序号复用 01 |
| Attack_Normal_01 | **暂不可写入** | —— | Shake 资源 `Corin_Attack_Normal_01_CamShake_A_01` 尚未创建（Camera 目录无此资产）。需先由资源侧按来源创建正式 Shake 资源，再回来写 Node。禁止用 Zoom 代替 |

## 当前缺口（写入前置）

- Normal_01 Shake 资源缺失：资源侧先补 `Corin_Attack_Normal_01_CamShake_A_01` 正式资产（来源 `sm0-038` 只给四个事件时点 `0.412/0.432/0.452/0.471`，无持续/曲线正文，资源字段按 Shake 标准配置填写并在资源内保留来源注释）。
- 来源 JSON 未提供独立效果持续时间/采样率：Zoom 资源字段即持续合同，Node 不补造数值。