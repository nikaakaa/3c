# 相机来源事件到工程 Cue 的映射

修订 v1，2026-09-13；属于协调 PLAN 的映射交付合同与初始线索，不是已经写入 Corin 资产的结果。此页由 Camera 任务维护；曲线迁移任务消费已确认行，统一写入本批 Timeline 资产及生成源码。

## 记录规则

一条可写入的映射必须同时具备：原始来源文件/对象/事件身份与证据位置，源动作名，工程 Timeline 精确资产路径及稳定身份，Clip/Cue 身份或由正式 API 创建的明确落点，效果类型、正式 ResourceId、原时钟与帧率、开始时刻、持续/更新条件、权重/曲线引用、目标与取消/自然结束规则。资源文件名、m_ZoomId 对齐或动作简称均不能替代其余字段。

字段不明时保留该资源的精确缺口和已查证据位置，不造默认持续时间、不用另一效果代替、不要求用户凭空填整表。Camera 继续取证并提供映射；不能越过同批 Timeline/生成源码的单一写入分工。

## 当前线索及边界

来源动作分组和 Normal_01 的单一 Shake 线索来自用户协调通知 `camera-preview-timeline-domain-runtime-r1`。本次只核对工程资产中的 m_ZoomId，没有重新读取这些动作的原始外部事件文件；以下不是完整来源闭包。

| 源动作线索 | 效果 / ResourceId | 本轮工程证据 | 工程 Timeline/Clip | 时刻/持续/取消 | 当前缺口 |
|---|---|---|---|---|---|
| Attack_Counter | Zoom / Corin_Attack_Counter_CamZoom_01 | [正式资源](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Counter_CamZoom_01.asset:16) 的 m_ZoomId 一致 | 未确认精确身份 | 未确认 | 补原事件证据、工程动作身份和生命周期，不能直接认定 Counter 对应某个现有 Clip |
| Attack_Normal_05 | Zoom / Corin_Attack_Normal_05_CamZoom_01 | [正式资源](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Normal_05_CamZoom_01.asset:16) 的 m_ZoomId 一致 | 未确认精确身份 | 未确认 | 补原事件到工程 Timeline/Clip 的完整路径 |
| Attack_Normal_05 | Zoom / Corin_Attack_Normal_05_CamZoom_02 | [正式资源](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Normal_05_CamZoom_02.asset:16) 的 m_ZoomId 一致 | 未确认精确身份 | 未确认 | 区分两个 Zoom 的真实触发/持续/取消，不按序号猜时点 |
| Attack_Normal_01 | Shake / Corin_Attack_Normal_01_CamShake_A_01 | 协调输入只列此 Shake；本轮未完成正式资源与消费者闭包 | 不得按 Attack1 名称认定 | 未确认 | 补指定 Shake 的资源、原消费者和工程落点；禁止以 Zoom 替代 |

这些行只覆盖本次协调提供的线索，不宣称是原动作的完整事件清单，也不代表所有 81 Shake、18 Zoom、18 Stretch、4 Override 均有对应工程 Cue。

## 对接结果

Camera 提供已确认映射行和相机资源/运行绑定。曲线迁移任务在其拥有的同批 Timeline 与生成源码中落实对应 Cue，双方不分别重建同一资产。Camera 的绑定结果须报告实际采用的资源与请求身份；预览展示这一真实结果，不从表中推测运行镜头已经生效。

此页本轮未产生任何资产或代码修改。旧 Projection 和旧域 DLL 的调用结果均不能补充这里缺失的工程身份或来源证据。
