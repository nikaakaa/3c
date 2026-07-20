## 1. 建立 Profile 数据模型

- [x] 1.1 将 `CharacterAnimationPresentationDefinition` 重命名并改造为 `CharacterAnimationPresentationProfile : ScriptableObject`。
- [x] 1.2 为 Profile 建立 CreateAssetMenu、Layer catalog、TransitionLibrary 和 producer binding 正式序列化字段。
- [x] 1.3 保留 `AnimationProducerPresentationBinding` 的稳定 Timeline/Track identity、Transition 与 Easing 合同。
- [x] 1.4 将 `CharacterPipelineDefinition` 的内联 presentation 字段替换为唯一 Profile 资产引用。
- [x] 1.5 将 Definition configuration validation 改为缺失 Profile、空 Layer、缺失 Library 和 binding 错误的明确报告。
- [x] 1.6 删除旧 `AnimationPresentation` 内联属性与旧类型名，不添加 `FormerlySerializedAs` 或兼容 getter。

## 2. 迁移 Compiler 与 Projection 输入

- [x] 2.1 将 Character Simulation Compiler 改为只从 Definition 的 Profile 引用读取表现配置。
- [x] 2.2 将 `CharacterPresentationProjection.Build` 输入改为正式 Profile 类型。
- [x] 2.3 保持 Projection 继续复制 Layer catalog、TransitionLibrary 和稳定 producer binding。
- [x] 2.4 将 Profile asset 纳入 Definition source revision dependency。
- [x] 2.5 保持动画 `.anim` 二进制内容不进入 Program source revision。
- [x] 2.6 保持 Program/Projection ProgramId、ProgramHash、SemanticHash、NumericProfile 与 source revision 严格匹配。
- [x] 2.7 提升 compiler version 并使旧内联产物明确 stale。

## 3. 收敛 Definition Inspector

- [x] 3.1 将 Definition Inspector 分为 Pipeline、Config References、Artifact Status 和 Navigation 四个紧凑区块。
- [x] 3.2 只在默认视图显示 RootTree、TickRate、Input、GameplayEffect、Action、Behavior 与 Animation Presentation Profile 引用。
- [x] 3.3 将 Action/Behavior 引用列表改为默认紧凑显示，不展开其内部业务字段。
- [x] 3.4 将 Program/Projection 对象引用与 identity 放入默认折叠的 Generated Artifacts 区域。
- [x] 3.5 将默认 Artifact Status 限制为轻量 metadata 检查与 Compile 命令。
- [x] 3.6 保持完整 compiler diagnostics 只由显式命令执行。
- [x] 3.7 从 Definition Inspector 删除 Layer、TransitionLibrary 和 producer binding 写 UI。
- [x] 3.8 从 Definition Inspector 删除逐 producer Graph/Timeline 投影与导航循环。
- [x] 3.9 保持选中、Repaint 和折叠切换不运行 Compiler、完整 source revision 或 Program 解码。

## 4. 建立 Profile Inspector

- [x] 4.1 新增 `CharacterAnimationPresentationProfileEditor`，编辑 Layer catalog 与 TransitionLibrary。
- [x] 4.2 建立引用当前 Profile 的 CharacterPipelineDefinition context 发现逻辑。
- [x] 4.3 在多个 Definition 引用同一 Profile 时提供显式 editor-only context 选择。
- [x] 4.4 在无 Definition context 时禁用 producer 来源投影与新增 binding 命令，并显示明确错误。
- [x] 4.5 从选定 Definition 的正式 Projection 缓存读取 producer identity、LayerId 与来源信息。
- [x] 4.6 在 Profile Inspector 按 stable producer identity 显示 transition 与 easing binding。
- [x] 4.7 保持 Profile Inspector 不推导 StateMachine flow、Priority、Driver 或 runtime lifecycle。
- [x] 4.8 将 Open Graph、Open Timeline 与 Open Transition 导航迁入 Profile Inspector。
- [x] 4.9 保持 Graph 与 Timeline 为两个独立窗口，不新增 Presentation EditorWindow。

## 5. 迁移 Authoring Service、Preview 与 Agent

- [x] 5.1 将 producer binding authoring service 的输入和 Undo/dirty owner 改为 Profile asset。
- [x] 5.2 保持 binding 写入前使用选定 Definition 的正式 Projection 校验 producer identity。
- [x] 5.3 将 Timeline Preview target 改为沿 Definition.Profile 引用取得正式表现配置。
- [x] 5.4 保持 Timeline Preview 只消费正式 Projection 与共享 AnimationPlaybackLifecycle。
- [x] 5.5 将 Agent Snapshot presentation section 改为输出 Profile asset path/GUID、Layer、Library 与 binding。
- [x] 5.6 保持 Agent Patch schema 拒绝 Layer、TransitionLibrary 与 producer binding 写操作。
- [x] 5.7 更新 Validator 与 snapshot error 文案，删除“内联 Presentation Definition”口径。

## 6. 迁移 Corin 资产

- [x] 6.1 清点 Corin 当前 Base Layer、OutputPolicy、TransitionLibrary 与全部 producer binding identity。
- [x] 6.2 创建正式 `CorinAnimationPresentationProfile.asset`。
- [x] 6.3 将现有 Layer、TransitionLibrary、Transition GUID/fileID 与 Easing 原样迁入 Profile。
- [x] 6.4 将 Corin Definition 的内联 presentation YAML 替换为 Profile 资产引用。
- [x] 6.5 重建 Corin SimulationProgram 与 PresentationProjection。
- [x] 6.6 核对 Profile producer binding 集合与重建 Projection producer 集合一致。
- [x] 6.7 确认 Corin Definition 资产不再保存 `m_AnimationPresentation` 内联块。

## 7. 激进清理旧路径

- [x] 7.1 删除旧 `CharacterAnimationPresentationDefinition` 类型、文件名与全部引用。
- [x] 7.2 删除 Definition Inspector 旧 presentation foldout、producer binding 列表和导航实现。
- [x] 7.3 删除 authoring service 对 Definition 的 Undo、dirty 和内联对象写入。
- [x] 7.4 删除 Agent、Preview、Compiler 对 `definition.AnimationPresentation` 的读取。
- [x] 7.5 使用 `rg` 确认不存在内联 presentation 字段、兼容 getter、FormerlySerializedAs、lazy migration 或双写。
- [x] 7.6 删除一次性迁移辅助代码与废弃序列化数据。

## 8. 文档、编译与校验

- [x] 8.1 更新 `openspec/project.md` 的 Authoring、Presentation 与 Code Organization 口径。
- [x] 8.2 同步受影响 current specs，删除 Definition 内联 Presentation 与 Definition Inspector 唯一编辑入口旧真相。
- [x] 8.3 使用规定参数编译 Runtime 与 Simulation 相关工程。
- [x] 8.4 编译后立即执行 `dotnet build-server shutdown`。
- [x] 8.5 使用规定参数编译 Editor 与 Agent 相关工程。
- [x] 8.6 Editor 编译后立即执行 `dotnet build-server shutdown`。
- [x] 8.7 运行 `openspec validate refactor-character-pipeline-definition-config-boundary --strict --no-interactive`。
