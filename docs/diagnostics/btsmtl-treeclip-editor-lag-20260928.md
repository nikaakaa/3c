# TreeClip 子图参数编辑卡顿

## 定位

当前打开的 `Corin_Attack_Normal_04_CamShake_E_02` 只有 5 个节点，归属 `CorinAttackGameplayAbilityDefinition.asset`。

调用链：`BtsmtlSkillNodeInspector.Change` → `BtsmtlSkillFlowEditorMutation.Apply` → 修改前 `BtsmtlSkillOwnedAssets.Collect` → 修改后 `ReleaseUnreferenced` 再次 `Collect`。

旧收集实现从主资产开始遍历。每进入一张子图，又调用整张子图的递归校验；父级已走过的后代会被重复校验。当前普通攻击资产收集结果为 100 个私有子资产，连续三次只读测量为 100.9003、106.7694、96.1927 ms。同一叶子图的单独校验为 0.2924、0.2866、0.1362 ms。上述值是收集/校验方法耗时，不是完整 UI 帧耗时。

## 修改

- Inspector 普通参数修改不再请求私有资产清理；替换 FSM、状态内容、Timeline 引用仍执行清理。
- 引用收集直接遍历 Macro、FSM、StateBody、Timeline、步骤条件、转移条件、TreeClip 和 Marker；保留同一资产文件范围与 visited 去重，不再借用整图校验完成收集。
- 正式编辑事务保留当前图校验、Undo 和最终保存；移除修改前一次重复序列化和修改后校验前一次重复序列化。
- 无资产数据迁移，无运行时代码或 IK 修改。

## 检查与限制

- diff 检查通过。
- 使用当前 Unity Editor 的 `ThirdPersonClient.Runtime.rsp` 和 Unity 自带 Roslyn 编译器独立编译，输出至系统临时目录，退出码 0。仅出现已有的 `PipelineBlackboardValueInfoNode.m_ReportedSourceError` 未使用警告。
- 未覆盖 Unity 的 Library 编译产物，未运行 Play、回放或新增测试代码。
- 修改期间 Unity MCP 会话间歇消失或不响应。尚未确认 Editor 完成导入，也未完成修改后的 UI 耗时测量；不能将静态调用减少当作窗口性能验收。
