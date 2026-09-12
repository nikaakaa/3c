# Corin 技能 FlowCanvas 迁移计划

## 输入闭包

根 Definition：

- Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset
- 旧作者根：Assets/Configs/Character/Corin/Pipeline/Graphs/CorinPlayableRootTree.asset

迁移只处理 Definition 中三项 Character Skill，旧 RootTree 的其它 Character/AI/Presentation 图不在本批 Skill 闭包内。

| Skill | 旧入口 identity | 旧可达 Graph 数 | 旧可达 Node 数 | 旧内联 Timeline 数 | 新主资产目标 |
|---|---|---:|---:|---:|---|
| Attack | 3ae19d5e-dd52-4f44-a80d-e32d2474e7ec | 68 | 419 | 4 | Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.Skill.f642ec00edd595e1dcf13e4e27fe79339915e188264180f92f10350c5baf3060.asset |
| DodgeBack | ee909991-5be0-4961-838a-a2854baca30d | 9 | 61 | 1 | Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.Skill.405b6c5d13e66ae379c647152961ea33db573cbb25270013699cd5b5b1b68c0c.asset |
| DodgeForward | b2328afd-40a8-467d-91f8-784c461f6137 | 9 | 61 | 1 | Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.Skill.9b38fd12dc097f01acc9250c3f9a4ff3b919bf10536a1e004132ad755f59a127.asset |

目标路径由 local:<SkillId> 的 SHA-256 前 16 字节确定，apply 前必须检查 asset 与 meta 均不存在；存在即报告冲突并停止。

## 资源引用闭包

Attack 还引用共享 Timeline：

- 旧路径：Assets/Configs/Character/Corin/Pipeline/Graphs/SharedTimelines/CorinAttack1Timeline.asset
- GUID：be588770448d17444828b566954e2709
- 处理：保持 Shared Asset 引用，不复制成多个私有 Timeline。

每个旧 Graph 的节点、Flow Edge、Property Edge、条件 Graph、StateMachine/State 子图、Timeline/TreeClip 和 Blackboard declaration 必须进入同一 Skill Document 闭包。未被三项入口可达的旧对象不自动迁移、不自动删除。

## 业务映射

| 旧作者能力 | 新作者能力 | 处理 |
|---|---|---|
| Root/StateMachine Enter/Any/Exit/State OnEnter/OnExit/Timeline Hook/Condition Result | 原生系统 anchor | 由页面工厂创建，不能作为普通节点复制 |
| Sequence/Selector/Parallel/Loop/Succeed | 原生技能结构节点 | 每条子边转成独立稳定步骤 Flow 输出，不使用 FlowCanvas Flip Flop 语义 |
| StateMachine/State | 技能状态页与状态主体页 | 私有页面随根文件保存 |
| Timeline/TimelineEnter | 原生 Timeline 节点与 TimelineAsset | Timeline 保持独立编辑；TreeClip 页面进入 TimelineBody |
| Compare/And/Or/Not | 原生 Simplex wrapper | Port Shape 从唯一 Capability Catalog 投影 |
| Input/Action/Window/Blackboard/Locomotion | 对应技能 FlowNode | 字段、默认值、引用和作用域通过 typed Mutation 导入 |
| ActivateActionInstance | 无新节点 | 删除二次激活；保留正式 Action runtime 入口及原有边语义 |

旧 Node/Edge 的 identity 先进入迁移 diff 的 source identity，目标新增 Node/Edge 使用 local: 临时身份；apply 后由正式 FlowCanvas owner 生成并反向导出实际身份。Root/Macro Graph AuthoringId 使用确定性 local identity，重复 apply 不随机换根。

## 应用顺序

1. checkout 当前精确 Definition，生成 v7 package。
2. 从旧 Skill 闭包生成 graph/layout/timeline/macro migration target；不修改 Unity asset。
3. dry-run 检查 capability、owner、Port Shape、资源 GUID、Root/Macro 文件归属及 identity 冲突。
4. 由作者确认实际冲突后，使用同一 document hash apply。
5. apply 先创建根主资产，再创建私有 Graph/Macro/Timeline 子资产，随后写 Node/Edge/Blackboard/Timeline 引用。
6. 保存失败或反向导出失败时回滚全部 owner，并删除本次创建且 apply 前不存在的根文件。
7. 重新 checkout，核对 root identity、Node/Edge/Port、owner、layout、Timeline 引用和 hash。
8. 仅在 Document Clean 且闭包验证成功后触发唯一 Character Build。

## 禁止项

- 不从 Character Definition 恢复 Open Root Tree 作为新 Skill 作者入口。
- 不保留旧图镜像、旧图转换运行路径、FlowCanvas runtime clone 或 fallback。
- 不覆盖已有 asset/meta，不自动改名，不自动选择第二路径。
- 不迁移 Pose、AI、动画、IK、移动或网络模型。
