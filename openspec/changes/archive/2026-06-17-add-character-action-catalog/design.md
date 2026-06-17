## Context
当前运行时主线已经从旧 FullBody/Locomotion 控制器推进，转向 `CharacterFrameRuntimeController -> CharacterRuntimeCore -> CharacterFramePipeline`。`CharacterFrameSubmitterGraph` 已经把 Locomotion 和 FullBody Action 作为 sibling submitter，但 Action 配置入口仍然偏 Dodge 特例：

- `CharacterConfigSO` 直接持有旧 Dodge 平铺配置。
- 默认 request provider/resolver 集合仍静态列出 External、TurnBack、Dodge。
- Dodge resolver 硬编码 Dodge action id、动画 key 和 motion spec 构建。
- 现有 `add-light-attack-combo-action` 已经标记旧“全局状态树叶子”方案与当前 Action lifecycle 路线冲突。

因此本变更先补 Action Catalog 纵切，而不是直接做技能编辑器或轻攻击实现。

## Goals / Non-Goals
- Goals:
  - 定义角色级 Action Catalog SO 作为正式动作逻辑入口。
  - 定义动作定义 SO 到纯 runtime action definition 的转换边界。
  - 先让 Dodge 通过 catalog entry 解析，保留现有 Dodge 行为。
  - 让后续 LightAttack、Jump、Skill 能沿同一个数据入口扩展。
  - 增加 EditMode 测试、静态边界验证、编译验证和 OpenSpec 校验。
- Non-Goals:
  - 不实现完整技能编辑器窗口。
  - 不实现轻攻击连段、伤害、hitbox、受击或死亡。
  - 不实现 VFX、SFX、Camera event、IK 或动作 Timeline 轨道。
  - 不引入新的状态机 engine。
  - 不让 Root Motion、Animancer callback 或 Transform 写入成为动作位移权威。
  - 不修改 Fantasy 协议或真实网络流程。

## Decisions
- Decision: 使用 `CharacterActionCatalogSO` 作为角色动作目录，而不是继续在 `CharacterConfigSO` 增加 `DodgeAction`、`LightAttack`、`Skill01` 等平铺字段。
  - Reason: 根配置只负责追踪子模块；动作数量会增长，平铺字段会很快变成第二套技能编辑器。
  - Alternative considered: 继续沿用旧 Dodge 平铺配置字段再加 LightAttack 平铺配置字段。不采用，因为会把每个动作都变成根配置特例。

- Decision: `CharacterActionDefinitionSO` 只表达动作逻辑，不直接持有动作动画 Profile。
  - Reason: 现有规格已经要求动作逻辑配置和动作动画绑定分离。动作定义可以输出稳定 animation key seed，但具体 clip、fade、TransitionLibrary 仍归动画配置。
  - Alternative considered: 动作定义直接引用动作动画 Profile。暂不采用，因为会让逻辑配置和表现配置重新耦合。

- Decision: 第一条迁移目标是 Dodge，不是 LightAttack。
  - Reason: Dodge 已经在现有管线跑通，迁移它可以验证 catalog 不改变行为；LightAttack 需要 combo window 和 stage 选择，应该依赖本变更之后再改写旧 proposal。
  - Alternative considered: 直接把 Dodge 和三段普攻一起做。暂不采用，因为会同时引入 catalog、combo 和新动作语义，难以判断失败来源。

- Decision: SO 在 authoring 层存在，运行时核心消费纯 C# runtime model。
  - Reason: rollback、预测、测试和网络同步不能依赖 Unity asset 或场景实例对象。
  - Alternative considered: resolver 每帧直接读取 SO。暂不作为正式 runtime 合同，避免 Unity 资产引用污染 Action solver。

- Decision: 缺失 catalog、缺失 Dodge entry 或非法数值必须报错，不提供隐藏默认配置。
  - Reason: 项目约定不允许 fallback 配置；隐藏默认会让手感来源和编辑器数据不可信。

## Risks / Trade-offs
- Risk: 现有 active change 已经在规划 `ActionSetSO`，本变更如果命名和职责不一致会造成分裂路径。
  - Mitigation: 将 `CharacterActionCatalogSO` 定义为 `ActionSetSO` 的正式等价命名目标；实施时如已有 `ActionSetSO` 半成品，优先迁移/重命名，不保留两个正式入口。
- Risk: Dodge resolver 仍需要方向解析、Directional/Backstep 分支，无法完全由静态数据表达。
  - Mitigation: Catalog 提供正式定义和数值；Dodge 专用方向解析仍可作为 resolver 策略存在，但不得成为配置入口或主流程硬编码。
- Risk: 旧 `add-light-attack-combo-action` 文档仍提到全局状态树路径。
  - Mitigation: 本变更完成后，轻攻击 proposal 必须改写为 Action Catalog + Action lifecycle 路线再实施。

## Migration Plan
1. 增加 Action Catalog 和 Action Definition 的纯数据合同与 SO authoring 类型。
2. 将 `CharacterConfigSO` 增加正式 `actionCatalog` 子模块引用。
3. 将 Corin Dodge 配置迁到 `Assets/Configs/3C/Action/Corin/Actions/Dodge/` 下的 catalog entry。
4. 将旧 Dodge runtime config 解析改为 catalog 查询，保留行为输出一致。
5. 将 provider/resolver 装配改为 catalog 驱动的通用路径，Dodge 专用策略只处理方向和变体解析。
6. 将 `CharacterConfigSO.DodgeAction` 标记为迁移遗留或移除正式读取路径。
7. 增加配置校验、行为保持测试、静态边界测试和编译验证。

## Open Questions
- `ActionSetSO` 是否作为类型名保留，还是统一重命名为 `CharacterActionCatalogSO`。实施前需要优先检查当前代码中是否已有未提交的 `ActionSetSO` 半成品；若存在，应迁移为单一正式入口。
- Dodge 的 Directional/Backstep 是否拆成两个 definition entry，还是作为 `Action.Dodge` 下的 variants。默认建议保留一个 `Action.Dodge` definition，variants 作为子数据。
