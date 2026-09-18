## MODIFIED Requirements

### Requirement: 公共Unity Composition必须由程序集依赖强制模型无关

公共 Unity Session Composition、Float32 request lowering 与标准 Local authoring MUST位于不引用具体 Network Model 的独立 Unity 程序集。Character Host 与模型 adapter MUST单向引用公共程序集，不得通过预定义程序集、friend assembly、反射、字符串查找或补充 registry 绕过依赖方向。

独立场景中的角色预览 MUST经同一公共 Composition 入口和正式 Host 建立真实 Session，明确选择已经存在的 Source、Pipeline、Backend、Solver 与 Actor registration；预览编辑器 MUST只管理启动请求和公开输入/控制端口。角色 Active 前 MUST消费主重构的正式控制模块binding/语义与状态schema、SkillProgram目录和完整发布闭包；C#控制、唯一动作服务与技能解释器仍在同一正式Pipeline推进，不另开Update或恢复链。系统 MUST不新增 Preview 专用 Composer、Kernel、Pipeline 或模型推断，公共 Composition MUST不反向依赖作者窗口或表现实现。

SessionHost/Actor/WorldSolver 要求 MUST只适用于上述角色路径。Timeline owner 提供的非 Skill 本地表现调用 MUST按其正式业务 owner、帧输入和受限输出合同运行，并消费同一场景生命周期；MUST不为接入预览伪造 Character Session，也不让该独立帧写入 Character/World 状态或替代角色事务。

#### Scenario: ServerAuthoritative Unity adapter被移除

- **WHEN** 构建不包含 ServerAuthoritative Unity 程序集
- **THEN** Local Composition MUST仍可编译并创建正式 Session
- **AND** 公共 Composer MUST不包含该模型类型或分支
- **AND** 明确使用 Local 配置的角色预览 MUST通过同一公共入口运行

#### Scenario: 场景的技能或控制合同尚未就绪

- **WHEN** 所选场景角色接入的正式控制实现、技能目录或对应发布版本不可用
- **THEN** 准备 MUST明确定位缺少的接口或产物
- **AND** MUST不加载旧角色RootTree或临时Preview Kernel使场景继续

#### Scenario: 非Skill表现不包含角色Session

- **WHEN** 场景仅声明 Timeline 的正式非 Skill 表现 owner 及合法内容/目标绑定
- **THEN** 预览 MUST按该 owner 的准备和释放结果管理场景，不要求 Character Composition 或 roster
- **AND** 该接入 MUST不创建 Preview Kernel 或改变角色模拟事务
