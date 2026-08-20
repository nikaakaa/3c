# Change: 增加响应式脚掌接触提案并统一预测与响应裁决

## Why

当前 `complete-character-predictive-foot-ik` 正在补齐未来落点、Ground Path、摆动净空、支撑锁脚、骨盆和唯一 FinalIK。预测式能够提前越过楼梯踢面，但输入急转、落点临近换踏面、动画脚与真实地面存在残差时，未来预测本身不能替代脚掌附近的当前接触观察。

`Assets/_HoaxGames/iStep` 已经实现脚掌 BoxCast、SphereCast 法线修复、脚掌尺寸、坡度过滤和接触稳定，不需要重新实现这部分算法。但它当前把这些计算与 `OnAnimatorIK`、Grounded、脚目标平滑、锁脚、骨盆和 Animator IK 写入放在同一个 `FootIK` 组件内；直接挂载仍会形成第二套状态与求解链。

本 change 增加独立的响应式接触提案模块，并在最后把预测与响应提案接入现有唯一 Foot Placement Goal 事务。模块实现可以与预测式收口并行进行；最终接入必须以单一预测基线为输入，不能让两个 active change 同时拥有最终 Goal 组装。

## What Changes

- 直接修改 iStep：把 `FootIK.findNewIKPos` 及其接触测量稳定代码从 Animator 应用流程中抽成唯一可调用的 `HoaxGames` Contact Solver，保留原有 BoxCast、SphereCast、坡面修正和接触点数学，不另写一份项目算法。原 `FootIK` 如继续服务其 Demo，也必须调用同一个 Solver，不能保留第二份计算。
- 新增项目侧每脚 `Reactive Foot Contact Adapter`。它只把同帧原生 Component Pose、Rig Calibration、正式 Profile、当前 PhysicsScene 与自碰撞过滤送给修改后的 iStep Solver，并把 iStep Result 转成 typed 接触提案；Adapter 不读取预测内部状态，不写 Goal、骨盆、Animator、Transform 或 Physical Bone。
- 脚掌查询使用由 Heel/Toe Calibration 和显式 Sole Half Width 定义的定向 footprint BoxCast；选中合法 footprint 命中后，允许执行同 Surface、有限邻域内的 SphereCast 法线修复。两者共同组成一次正式响应式接触查询，不是第二 Grounded 或备用求解器。
- 响应模块只稳定原始测量：同 Surface 的微小点位和法线变化可复用 Committed 测量；Surface、事件或超出死区的几何变化必须发布新事实。模块不平滑最终脚目标，不拥有 glue、Locked/Sliding/Unlocked、骨盆弹簧或 GoalTransition。
- 新增统一 `Foot Goal Proposal` 与每脚唯一 Arbiter。现有预测链降为 Predictive Proposal，响应模块发布 Reactive Proposal；Arbiter 只输出一个 Resolved Foot Proposal，再由现有 Support Lock、GoalTransition、Pelvis 与唯一 GoalSet 消费。
- 新增一条共享作者曲线 `ReactiveOwnershipCurve`。同一曲线分别使用左右脚各自的生物力学接触权重采样；它控制预测与响应的目标所有权，不替代现有 `animation.foot-placement-weight`，也不成为第二 Lock Curve 或独立时钟。
- 两个提案几何兼容时，Arbiter 对相对同帧原生踝骨的修正做混合；Surface 或几何不兼容时不得把两个世界点插值到空中或台阶内部，而是延续唯一 Committed Owner，并在响应接触满足正式接管条件时执行 typed handoff。最终连续性仍只由现有唯一 GoalTransition 处理。
- 落地完成时把本事件最后一个 Resolved Contact 晋级为唯一 `LastLanding`；下一 Ground Path 从该已提交真实支撑接触出发，`NextSwingLanding` 继续表达未来预测。支撑锁定后不得追随当前动画脚反复改写锚点。
- 骨盆只能在左右脚完成预测/响应裁决、Support Lock 与最终 Goal 权重后，从同一对最终 Sole 结果计算；不得让预测与响应各算一份 Pelvis。
- Scene 与 CSV 增加响应查询、提案、曲线输入、所有权、兼容性和 handoff lineage，并继续与唯一 FullBodyIK 和 final writer 的同一 Completion 对账。
- 现有 Foot Placement 调试面板增加 `Predictive Only`、`Reactive Only`、`Hybrid` 三种Editor/Development对比模式。三种模式只改变同一个Arbiter的来源策略，继续经过同一Support Lock、GoalTransition、Pelvis、GoalSet、FullBodyIK和writer；正式角色配置固定为Hybrid，调试选择不写入Profile、Projection、Prefab、Gameplay或网络状态。
- `_HoaxGames/iStep` 的修改后 Contact Solver 是正式响应几何的唯一实现。Character Runtime 通过窄 Adapter 直接调用它，但不挂载原始 `FootIK` Animator 应用流程，不启用 `OnAnimatorIK`、iStep Body Placement 或 Demo 内容，也不把同一算法复制进 GameScripts。

## Parallel Delivery Boundary

### 第一段：可立即并行实现

- 从 iStep 原实现抽出的 Contact Solver、请求、结果、拒绝原因、Surface lineage 与测量 Pending/Committed 合同。
- Rig footprint Calibration、Profile、曲线不可变运行时设置和严格校验。
- footprint BoxCast、法线修复与纯接触几何 Builder。
- 独立只读诊断摘要。

这一段不得修改 `CharacterFootPlacementRuntime.EvaluateFrame` 的最终 Goal 组装，不得接触预测 Landing、Ground Path、Support Lock、Pelvis 或 FinalIK 所有权。

### 第二段：最后统一接入

- 先把当时工作区的预测链收敛为唯一 Predictive Proposal 输入。
- 加入每脚 Arbiter、Resolved Contact 晋级和统一 Support Lock。
- 只在裁决后调用一次 GoalTransition、一次 Pelvis Builder、一次 GoalSet 写入、一次 FullBodyIK 和一次 final writer。
- 删除任何为模块独立演示而产生的临时 writer、组件开关或并行 Prefab 路径；本 change 不允许保留未接线正式模块作为 current truth。

## Impact

- Affected specs: `character-reactive-foot-contact`、`character-foot-placement-presentation`
- Affected code: `_HoaxGames/iStep/Scripts` 中 FootIK 接触计算拆分、项目侧响应 Adapter、Foot Placement 响应式合同、Rig Calibration、Foot Placement Profile、预测/响应 Proposal Arbiter、Landing/Support 提交、diagnostics、Gizmo、CSV
- 最终接入时会修改 `CharacterFootPlacementRuntime`，但不修改 Pose Graph Goal ABI、FinalIK FBBIK、KCC、Gameplay Body、VisualRoot、网络状态或 Physical Bone writer
- Corin 与 TrainingEnemy 继续使用同一个正式 Foot Placement Profile，不新增角色级响应式旁路配置

## Dependency And Active Change Ownership

- 独立响应模块可以与 active `complete-character-predictive-foot-ik` 并行实施。
- 最终接入依赖该 change 的唯一 GoalTransition、Support Lock、Pelvis 和 Landing 生命周期已经形成可对账基线。若它届时仍为 active，必须先完成并归档，或把未完成的最终 Goal 所有权整体移交给本 change；不得让两个 change 同时修改同一最终组装语义。
- `add-discrete-stair-presentation` 中的传统 FootGrounding、Predictive Modifier 和 VisualRoot 高度方案继续不得接入。

## Current Spec Comparison

- current `character-foot-placement-presentation` 明确禁止“响应式结果补洞”和第二脚下 Trace。本 change 修改该边界：仍禁止第二 Grounded、第二 Goal 链、第二 Solver 与第二 writer，但允许 Foot Placement 同一事务内每脚一次正式 Reactive Contact Proposal。
- current `Foot Placement必须是唯一Goal事务` 只列出预测所需输入。本 change 增加响应式 Proposal 作为该事务内部输入，输出仍严格只有 Pelvis、LeftFoot、RightFoot 三个 Goal。
- current `Ground Path必须使用上一已提交落点与下一事件落点` 和 active predictive delta 都把最后预测落点直接晋级为 `LastLanding`。本 change 将最终语义改为：未来预测保持 `NextSwingLanding`，落地时晋级经过 Arbiter 的 `Resolved Contact`，使下一段 Path 从真实已提交支撑点开始。该重叠 Requirement 必须在最终接入前基于预测 change 的最终版本重排，不能分别归档出矛盾文本。
- active predictive change 明确“不新增 Lock Curve”。本 change 不改变这条锁入时钟合同；`ReactiveOwnershipCurve` 只重映射已存在的每脚生物力学接触权重，控制候选来源，不控制锁入计时或最终 IK 总权重。
- `openspec/project.md` 当前写明正式链不得使用响应式结果。归档本 change 时必须把它更新为“允许同一 Foot Placement 事务内的响应式候选，仍禁止独立响应式 IK 和第二 writer”。

## Decisions And Tradeoffs

### 采用：直接修改 iStep 并抽出唯一 Contact Solver

直接移动并参数化 `FootIK.findNewIKPos` 及其必要接触稳定代码，使它脱离 Animator writer 后由项目 Adapter 调用。好处是保留已经购买和验证过的 iStep 几何行为，不承担重新实现误差；代价是项目正式 Runtime 会依赖一份经过修改的第三方源码，后续升级 iStep 时必须维护这处分叉。

### 不采用：直接挂载 iStep 并调组件权重

该方案实现最快，但 iStep 自己拥有 Grounded、Body Placement、脚目标平滑和 Animator IK 写入。即使曲线能改变权重，两套内部状态仍会同时推进，无法证明最终脚和骨盆来自哪一条链。

### 不采用：只替换 iStep 最后的骨骼写入

删除 `SetIKPosition` 和 `Animator.bodyPosition` 仍会保留 iStep 自己的 Grounded、glue、外推、Reset Lerp 与 Body Offset。它们会在预测/响应裁决前形成第二份隐藏状态，因此不是合格的 Proposal Provider。

### 不采用：在 GameScripts 重新实现 iStep 接触算法

项目不再根据 iStep 公式另写 BoxCast、SphereCast 或坡面修正。这样会产生两份行为近似但以后会分叉的实现，也违背本轮直接复用现成响应式实现的目标。

### 不采用：对两个绝对世界目标始终直接 Lerp

当预测和响应命中不同台阶时，绝对点插值会把脚和骨盆带进两级台阶之间。正式方案只在几何兼容时混合相对同帧原生踝骨的修正；不兼容时进行 typed owner handoff。

## Non-Goals

- 不直接启用原始 `FootIK` 的 Animator IK、LegIK、TwoBoneIK、GrounderFBBIK、Body Placement 或骨骼 writer；只适配修改后抽出的 Contact Solver。
- 不增加第二 Grounded、第二 Support Lock、第二 Pelvis、第二 GoalTransition 或第二 final writer。
- 不让响应式结果写回 Gameplay、KCC、VisualRoot、网络或 rollback state。
- 不在本 change 处理移动平台局部锚定、跳跃空中贴脚、攀爬、手部 IK、专用上下楼动画或跑步上下楼特化。
- 不把响应式查询结果当成预测 Ground Path 的临时默认点；没有合法提案时发布 typed rejection。
- 不把面板对比模式做成三套Foot Placement节点、三个Profile、运行时fallback或正式Player业务配置。
