## 1. 范围确认
- [x] 1.1 确认第一版只收敛当前存在的 4 个子模块（stateMachine、movement、locomotionAnimation、turnInPlace）
- [x] 1.2 确认不改逻辑状态机拓扑
- [x] 1.3 确认不合并动画配置入状态机
- [x] 1.4 确认保留子 SO 的独立 CreateAssetMenu

## 2. CharacterConfigSO 类
- [x] 2.1 新增 `CharacterConfigSO` ScriptableObject，放在 `Assets/Scripts/Character/Config/` 或等价目录
- [x] 2.2 包含 4 个子模块引用字段：stateMachine、movement、locomotionAnimation、turnInPlace
- [x] 2.3 为每个字段添加属性访问器
- [x] 2.4 增加 CreateAssetMenu：`3C/Character/CharacterConfig`

## 3. PlayerLocomotionController 改造
- [x] 3.1 新增 `characterConfig` 序列化字段
- [x] 3.2 新建子模块获取本地方法：`ResolveCharacterStateMachineDefinition()`、`ResolveMovementConfig()`、`ResolveRunAnimationConfig()`、`ResolveTurnInPlaceAnimationConfig()`
- [x] 3.3 将所有子模块引用点改为走 `characterConfig` 解引用
- [x] 3.4 旧字段标记 `[Obsolete]`，留作降级 fallback
- [x] 3.5 验证编译通过

## 4. 资产创建与赋值
- [x] 4.1 在 `Assets/Configs/3C/` 下创建 `CharacterConfig.asset`
- [x] 4.2 asset 引用已有 DefaultCharacterStateMachine、BasicMovementConfig、DefaultRunLocomotionAnimationConfig、CorinTurnInPlaceAnimationConfig
- [x] 4.3 在场景中为 可琳 的 PlayerLocomotionController 赋值新 asset

## 5. 测试
- [x] 5.1 添加 EditMode 测试验证 `CharacterConfigSO` 的空引用保护
- [x] 5.2 添加 EditMode 测试验证 `PlayerLocomotionController` 从根 SO 解析子配置
- [x] 5.3 添加 EditMode 测试验证旧字段 fallback 路径

## 6. 验证
- [x] 6.1 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`
- [x] 6.2 运行定向 EditMode 测试
- [x] 6.3 运行 `openspec validate consolidate-character-config-root --strict --no-interactive`
- [x] 6.4 不运行 Unity batchmode
- [x] 6.5 Play Mode 手动验证 WASD / RunEnd / Dodge / Turn 行为不变

