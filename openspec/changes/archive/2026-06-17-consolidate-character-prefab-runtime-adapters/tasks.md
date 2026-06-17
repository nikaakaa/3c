## 1. Baseline and Scope
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 读取 `extract-character-runtime-core-from-mono-adapters` delta，确认 core/module ownership 不重复实现。
- [x] 1.3 读取 `character-runtime-ports` 当前 spec，确认 prefab 装配要求写入现有 capability。
- [x] 1.4 读取 `character-config-root` 当前 spec，确认不新增 fallback 配置入口。
- [x] 1.5 列出 `可琳.prefab` 当前 MonoBehaviour 脚本清单。
- [x] 1.6 列出 `可琳_Humanoid.prefab` 当前 MonoBehaviour 脚本清单。
- [x] 1.7 区分每个 MonoBehaviour 是 runtime assembly adapter、Unity-facing adapter、迁移期 facade 还是 debug tooling。

## 2. Characterization Tests
- [x] 2.1 新增或更新 EditMode 静态测试，读取两个 Corin prefab 的脚本清单。
- [x] 2.2 测试记录当前必须保留的 Unity-facing adapter allowlist。
- [x] 2.3 测试标记 `PlayerLocomotionController` 不应作为最终正式 prefab 组件。
- [x] 2.4 测试标记 `FullBodyActionRuntime` 不应作为最终正式 prefab 组件。
- [x] 2.5 测试确认 prefab 没有 rollback debug runner、history recorder 或 replay adapter。
- [x] 2.6 测试确认 prefab 没有第二 motion executor。
- [x] 2.7 测试确认 prefab 没有第二 animation presenter。
- [x] 2.8 测试确认 prefab 没有第二 gameplay runtime assembly adapter。

## 3. Runtime Assembly Adapter
- [x] 3.1 选择保留 `CharacterFrameRuntimeController` 名称或新增批准的等价 runtime assembly adapter。
- [x] 3.2 让 runtime assembly adapter 显式序列化所有 core dependency 所需 Unity-facing adapter 引用。
- [x] 3.3 让 runtime assembly adapter 成为 `CharacterRuntimeCore` 的唯一正式 prefab owner。
- [x] 3.4 让 runtime assembly adapter 直接绑定 Locomotion module 所需 Unity adapter Interface。
- [x] 3.5 让 runtime assembly adapter 直接绑定 Action module 所需 Unity adapter Interface。
- [x] 3.6 保持 runtime assembly adapter 不执行 motion、animation、input consume 或状态机业务逻辑。
- [x] 3.7 保持 runtime assembly adapter 不创建 fallback config。
- [x] 3.8 增加静态测试确认 runtime assembly adapter 不创建第二 pipeline、runner、motion executor 或 presenter。

## 4. Locomotion Facade Retirement
- [x] 4.1 盘点 `PlayerLocomotionController` 在 prefab 上仍提供的序列化引用和 Unity-facing 能力。
- [x] 4.2 将正式主线仍需要的引用移动到 runtime assembly adapter 或现有窄 Unity-facing adapter。
- [x] 4.3 确认 `PlayerLocomotionController` 不再是正式 prefab 依赖。
- [x] 4.4 从 `可琳.prefab` 移除 `PlayerLocomotionController` 组件。
- [x] 4.5 从 `可琳_Humanoid.prefab` 移除 `PlayerLocomotionController` 组件。
- [x] 4.6 增加静态测试阻止正式 prefab 或正式 scene 重新挂载 `PlayerLocomotionController` 作为 gameplay runtime。
- [x] 4.7 保留或删除代码兼容面前，确认没有未经审批的替代 tick 路径。

## 5. Action Facade Retirement
- [x] 5.1 盘点 `FullBodyActionRuntime` 在 prefab 上仍提供的序列化引用和 Unity-facing 能力。
- [x] 5.2 将正式主线仍需要的引用移动到 runtime assembly adapter 或现有窄 Unity-facing adapter。
- [x] 5.3 确认 `FullBodyActionRuntime` 不再是正式 prefab 依赖。
- [x] 5.4 从 `可琳.prefab` 移除 `FullBodyActionRuntime` 组件。
- [x] 5.5 从 `可琳_Humanoid.prefab` 移除 `FullBodyActionRuntime` 组件。
- [x] 5.6 增加静态测试阻止正式 prefab 或正式 scene 重新挂载 `FullBodyActionRuntime` 作为 gameplay runtime。
- [x] 5.7 保留或删除代码兼容面前，确认 Action lifecycle 仍由 core-owned module 持有。

## 6. Prefab and Scene Assembly
- [x] 6.1 更新 `可琳.prefab`，只保留批准的 runtime assembly adapter 和 Unity-facing adapters。
- [x] 6.2 更新 `可琳_Humanoid.prefab`，只保留批准的 runtime assembly adapter 和 Unity-facing adapters。
- [x] 6.3 确认两个 prefab 都引用同一个正式 `CorinCharacterConfig.asset`。
- [x] 6.4 确认两个 prefab 都没有旧平铺配置字段作为正式入口。
- [x] 6.5 确认正式 scene override 不恢复退场 facade 或 debug tooling。
- [x] 6.6 确认 `CharacterFrameRuntimeTickAdapter` 只转发同一个 runtime assembly adapter。

## 7. Regression Tests
- [x] 7.1 运行 Character Frame Pipeline 相关 EditMode 测试。
- [x] 7.2 运行 Character Runtime Ports 相关 EditMode 测试。
- [x] 7.3 运行 Corin prefab/scene binding 相关 EditMode 测试。
- [x] 7.4 运行 Locomotion runtime capture/restore 相关 EditMode 测试。
- [x] 7.5 运行 Action runtime capture/restore 相关 EditMode 测试。
- [x] 7.6 运行 Dodge 完整播放和 claim 释放相关 EditMode 测试。
- [x] 7.7 运行 rollback debug rig boundary 相关 EditMode 测试。

## 8. Validation
- [x] 8.1 运行 `openspec validate consolidate-character-prefab-runtime-adapters --strict --no-interactive`。
- [x] 8.2 运行 `dotnet build Assembly-CSharp.csproj --no-restore`。
- [x] 8.3 运行 `dotnet build Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 8.4 运行 GitNexus impact 分析将要修改的 runtime adapter 符号并记录风险。
- [x] 8.5 运行 GitNexus `detect_changes()`；当前工作区已有大量其它变更，结果为 critical，已结合定向测试、旧 guid 扫描和编译确认本变更的 prefab adapter 装配边界。

## 9. Deprecated Runtime Surface Deletion
- [x] 9.1 删除 `PlayerLocomotionController`、`FullBodyActionRuntime`、`LocomotionTickAdapter`、`FullBodyActionTickAdapter` 代码和 `.meta`。
- [x] 9.2 移除 runtime、rollback、assembler、diagnostics 中对旧 facade 和 retired tick adapter 的引用。
- [x] 9.3 删除以旧 facade / retired tick adapter 为对象的旧测试。
- [x] 9.4 将仍属于当前主线的 rollback、prefab binding 和 runtime module 测试迁移到 `CharacterFrameRuntimeController` 或 pure C# module seam。
- [x] 9.5 运行代码名和旧脚本 guid 静态扫描，确认生产代码、测试和正式资源不再引用被删除 surface。
- [x] 9.6 运行编译、OpenSpec strict 校验和 GitNexus 变更检查。
- [x] 9.7 运行相关 EditMode 测试。
