# Change: 通用化角色动作请求解析接口

## Why
当前项目已经有 `InputRequestKind.Attack`、`InputRequestKind.Dodge`、`InputRequestKind.Jump`、`ActionRequestType`、输入缓冲和本地回滚输入事实，但正式 FullBody Action 闭环仍主要围绕 Dodge 的 builder/provider 组织。Dodge provider 现在会直接把输入缓冲解析成带目标状态含义的 request fact；后续轻攻击如果直接在 provider 里生成 `Action.Attack01/02/03`，Attack、Dodge、Jump 会继续把“输入键”“动作请求”和“目标状态”混在一起。

这个变更先收敛接口化边界，让模块并行扩展时只提交请求和纯数据解析结果，不新增第二套 controller、runner、resolver、presenter 或 pipeline。

## What Changes
- 定义 request-first 的 FullBody Action 请求解析口径：输入 provider 只提交动作请求候选，不决定最终目标状态、动画 key 或 motion spec。
- 定义 `CharacterActionRequest`、request provider、request resolver、resolve context 和 resolved action 的职责边界。
- 保留 `InputRequestKind`，但明确它只表示输入缓冲键，不表示动作语义、目标状态或连段阶段。
- 让 Dodge 迁移到同一套 provider/resolver 路径，并保持现有行为。
- 让 Attack、Jump、HitReact 等后续动作只能通过同一套接口扩展，不能在 arbiter 主流程或 frame pipeline 中新增硬编码分支。
- 约束 `add-light-attack-combo-action` 后续实现必须消费解析后的动作结果，不在输入 provider 中直接决定 `Attack01/02/03`。

## Non-Goals
- 不实现轻攻击、跳跃、受击、伤害、hitbox、hurtbox、命中停顿或网络同步。
- 不修改 `.asset`、`.prefab`、`.unity`。
- 不新增 CharacterFramePipeline、state machine runner、motion executor、animation presenter 或播放 facade。
- 不把 action resolver 变成读取 Unity scene object、Animancer runtime 或 InputAction 的大杂烩。
- 不新增 fallback 配置；缺少正式配置时必须校验失败。

## Impact
- 主要影响 `fullbody-action-framework`、`local-preinput-buffer`、`character-frame-pipeline` 三个能力规格。
- 该 change 是 `add-light-attack-combo-action` 的前置边界收敛：攻击动作实现应在本 change apply 后再基于 resolved action 接入。
- 运行时代码实现阶段需要对 `CharacterActionRequestSubmissionArbiter`、`CommittedActionRequestSubmissionProviders`、`CommittedActionInputRequestBuilder`、Dodge request/config 相关符号分别做 GitNexus impact analysis。

## Open Questions
- 第一版 resolved action 是否需要把 combo stage 作为通用 payload，还是由 Attack 专用 resolver 输出具体 target state。本文档倾向后者：request 保持通用，resolver 输出具体纯数据结果。
- Jump 是否和 Attack 一样进入 FullBody Action 请求解析，还是后续拆出 Locomotion vertical action。本文档只要求 Jump 不绕过同一请求接口，具体状态归属留给 Jump proposal。
