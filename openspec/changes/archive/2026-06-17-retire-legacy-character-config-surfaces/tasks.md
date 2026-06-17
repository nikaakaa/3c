## 1. 基线与清单

- [x] 1.1 阅读本变更的 `proposal.md`、`design.md` 与全部 spec delta，确认范围只包含废弃配置/兼容字段风险清理。
- [x] 1.2 盘点正式 prefab/scene/config 中旧字段名、旧 GUID、旧目录与退役组件引用，产出实现用清单。
- [x] 1.3 将清单按“删除”“只读保留”“仅测试/迁移可见”分类，避免产生未经审批的分裂路径。

## 2. 测试先行

- [x] 2.1 扩展配置根测试，断言 `PlayerLocomotionController` 与 `FullBodyActionRuntime` 不从旧平铺字段解析配置。
- [x] 2.2 扩展 prefab/scene 绑定测试，断言正式资产不存在旧字段非空值和旧字段序列化键残留。
- [x] 2.3 扩展作者ing 布局测试，断言旧 FullBody 配置目录、旧 FullBody 状态机目录、旧 FullBody 动画目录不能作为正式路径存在。
- [x] 2.4 扩展运行时端口测试，断言退役 tick adapter/presenter 不会注册或驱动正式 runtime。
- [x] 2.5 扩展兼容视图测试，断言 `FullBodyStateView` 只能作为只读状态观察面使用。

## 3. 资产清理

- [x] 3.1 清理 Corin prefab 中旧 locomotion/action 平铺字段的序列化残留。
- [x] 3.2 清理正式 scene 中旧 locomotion/action 平铺字段的序列化残留。
- [x] 3.3 确认 Corin action/request policy 资产继续通过正式新路径和现有 GUID 被引用。
- [x] 3.4 确认旧 FullBody 状态机资产 GUID 不再被正式 prefab/scene/config 引用。

## 4. 运行时代码清理

- [x] 4.1 从 `PlayerLocomotionController` 移除旧平铺配置字段和相关 fallback/兼容读取面。
- [x] 4.2 从 `FullBodyActionRuntime` 移除旧平铺配置字段和相关 fallback/兼容读取面。
- [x] 4.3 删除或硬隔离 `FullBodyActionTickAdapter` 的正式运行时使用面。
- [x] 4.4 删除或硬隔离 `LocomotionTickAdapter` 的正式运行时使用面。
- [x] 4.5 删除或硬隔离旧 locomotion/action Animancer presenter 的正式运行时使用面。
- [x] 4.6 保留 `FullBodyStateView` 的只读诊断职责，并移除任何写入、仲裁或推进正式动作生命周期的路径。

## 5. 规格与文档同步

- [x] 5.1 更新相关 active specs，使正式路径只指向角色根配置、角色专属 Action 配置、Locomotion 状态图与 CharacterFrame runtime。
- [x] 5.2 标记旧目录、旧 Host Adapter、旧 tick adapter、旧 presenter 为废弃或移除，不再作为未来动作开发参考。
- [x] 5.3 确认本变更与 `refactor-locomotion-action-state-graphs` 的归档顺序不会产生规格冲突。

## 6. 验证

- [x] 6.1 运行 `openspec validate retire-legacy-character-config-surfaces --strict --no-interactive`。
- [x] 6.2 运行相关 EditMode 测试集合：配置根、prefab/scene 绑定、作者ing 布局、运行时端口、兼容视图。
- [x] 6.3 运行 C# 编译验证，确认移除旧字段后无编译错误。
- [x] 6.4 运行 `git diff --check`。
- [x] 6.5 更新本 checklist，只有完成且验证通过的任务标记为 `- [x]`。
