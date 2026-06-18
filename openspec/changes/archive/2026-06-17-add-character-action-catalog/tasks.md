# Tasks

## 1. 实施前检查
- [x] 1.1 读取本变更的 `proposal.md`、`design.md` 和所有 spec delta。
- [x] 1.2 读取 `formalize-character-frame-module-architecture`、`add-light-attack-combo-action` 的 proposal/design/tasks，确认实施顺序。
- [x] 1.3 搜索当前代码与资产中的旧 ActionSet 半成品、旧 Dodge 平铺配置和旧根配置 Dodge 字段。
- [x] 1.4 对 `CharacterConfigSO` 运行 GitNexus impact analysis，并记录 direct callers、affected processes 和 risk。
- [x] 1.5 对 `CharacterActionRequestResolution` 运行 GitNexus impact analysis，并记录 direct callers、affected processes 和 risk。
- [x] 1.6 对 `CommittedActionRequestSubmissionProviderCollection` 运行 GitNexus impact analysis，并记录 direct callers、affected processes 和 risk。

## 2. Action Catalog 数据合同
- [x] 2.1 定义角色动作目录 runtime model，按稳定 `ActionStateId` 查询动作定义。
- [x] 2.2 定义动作定义 runtime model，包含 request type、source input kind、priority、resistance、motion seed、animation key seed 和 variant 数据。
- [x] 2.3 定义 catalog 查询失败、重复 action id、重复 request binding 的错误结果。
- [x] 2.4 定义 Dodge variant 数据结构，覆盖 Directional 与 Backstep 的 duration、distance、rotateToDirection 和方向策略。
- [x] 2.5 确认 runtime model 不引用 `ScriptableObject`、`MonoBehaviour`、`Transform`、`AnimationClip`、Animancer runtime 或 InputAction。

## 3. SO 作者配置
- [x] 3.1 新增或迁移为单一正式 `CharacterActionCatalogSO` 或等价 `ActionSetSO` 类型。
- [x] 3.2 新增 `CharacterActionDefinitionSO` 或等价动作定义 SO 类型。
- [x] 3.3 为 Dodge 定义 SO 提供 Directional 与 Backstep variant 配置字段。
- [x] 3.4 为 catalog 和 definition 增加显式校验 API。
- [x] 3.5 确认 Action Catalog SO 不直接引用动作动画 Profile、AnimationClip、Animancer Transition 或 Locomotion graph。
- [x] 3.6 确认 Action Definition SO 不提供代码默认手感参数。

## 4. CharacterConfigSO 接入
- [x] 4.1 在 `CharacterConfigSO` 增加正式 Action Catalog 子模块引用。
- [x] 4.2 将正式 runtime 解析从 `DodgeAction` 字段迁移到 Action Catalog 查询。
- [x] 4.3 移除 `DodgeAction` 字段，禁止作为 fallback。
- [x] 4.4 更新 Corin 根配置资产，使其引用正式 Action Catalog。
- [x] 4.5 增加根配置校验，覆盖缺失 Action Catalog、缺失 Dodge entry 和重复 action id。

## 5. Dodge 行为迁移
- [x] 5.1 创建 Corin Action Catalog 资产。
- [x] 5.2 创建或迁移 `Action.Dodge` definition 资产。
- [x] 5.3 将现有 Corin Dodge Directional/Backstep 数值迁入 Dodge definition。
- [x] 5.4 保持 `BodyClaimPolicySO` 仍作为独立 body/channel claim 规则入口。
- [x] 5.5 保持动作动画 Profile 仍由动作动画绑定配置解析。
- [x] 5.6 验证 Directional Dodge 输出的 variant、world direction、duration、distance、rotateToDirection 与迁移前一致。
- [x] 5.7 验证 Backstep Dodge 输出的 variant、world direction、duration、distance、rotateToDirection 与迁移前一致。

## 6. Request provider 和 resolver 接入
- [x] 6.1 将 Action resolve context 扩展为可访问纯 runtime Action Catalog。
- [x] 6.2 将 Dodge resolver 改为从 catalog entry 获取正式 Dodge 定义和数值。
- [x] 6.3 将 provider/resolver 装配从静态 Dodge-only 默认集合迁向 catalog 驱动入口。
- [x] 6.4 确认 External 与 TurnBack 请求保留现有语义，不被错误纳入 Dodge catalog entry。
- [x] 6.5 确认新增简单 Action 不需要修改 `CharacterActionRequestSubmissionArbiter` 主流程。

## 7. 自动测试
- [x] 7.1 增加 EditMode 测试：Action Catalog 能解析 `Action.Dodge` definition。
- [x] 7.2 增加 EditMode 测试：重复 action id 会报告配置错误。
- [x] 7.3 增加 EditMode 测试：缺失 Dodge definition 会报告配置错误且不 fallback 到 `CharacterConfigSO.DodgeAction`。
- [x] 7.4 增加 EditMode 测试：Dodge resolver 使用 catalog 数值生成 Directional resolved action。
- [x] 7.5 增加 EditMode 测试：Dodge resolver 使用 catalog 数值生成 Backstep resolved action。
- [x] 7.6 增加 EditMode 测试：rejected Dodge 请求不消费输入。
- [x] 7.7 增加 EditMode 测试：Action Catalog 不引用动作动画 Profile 或 Animancer runtime 对象。
- [x] 7.8 增加静态边界测试：正式代码不读取 `CharacterConfigSO.DodgeAction` 作为 gameplay fallback。
- [x] 7.9 增加静态边界测试：正式 Corin 根配置引用链包含 Action Catalog 且不包含 `Assets/Configs/3C/Action/FullBody/`。

## 8. 工具验证
- [x] 8.1 运行 `openspec validate add-character-action-catalog --strict --no-interactive`。
- [x] 8.2 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 8.3 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 8.4 运行 Action Catalog、CharacterConfigRoot、CharacterActionRequestResolution、Dodge 行为相关定向 EditMode 测试。
- [x] 8.5 运行 GitNexus `detect_changes()`；当前 dirty worktree 包含既有无关改动，本变更目标文件范围限定在 Action Catalog、Dodge 配置迁移和角色配置根接入。

## 9. 收尾
- [x] 9.1 更新或暂停 `add-light-attack-combo-action` 中仍要求全局状态树叶子的段落。
- [x] 9.2 确认旧根配置 Dodge 字段、旧 Dodge 平铺配置类型和旧 Dodge config 资产不再保留。
- [x] 9.3 确认 tasks 全部完成后再勾选完成项。

## 10. 命名统一收尾
- [x] 10.1 将根配置上的 interrupt policy 字段和公开属性统一命名为 `ActionInterruptPolicy`。
- [x] 10.2 将 Corin interrupt policy 资产目录和资产名统一为 `InterruptPolicy/CorinActionInterruptPolicySet.asset`。
- [x] 10.3 更新 runtime、测试和当前 OpenSpec 事实/变更文案中的旧 interrupt policy 命名。
- [x] 10.4 运行构建、OpenSpec 校验、Unity 定向测试和旧名残留扫描。

## 11. Dodge runtime tuning 命名统一
- [x] 11.1 将纯 runtime Dodge 数值类型从旧 config 语义统一为 `DodgeActionTuning`。
- [x] 11.2 将查询和测试辅助方法中的 Dodge config 命名统一为 Dodge tuning。
- [x] 11.3 更新编译项、测试和当前 OpenSpec 文案中的旧 Dodge config 命名。
- [x] 11.4 运行构建、OpenSpec 校验、Unity 定向测试和旧名残留扫描。
