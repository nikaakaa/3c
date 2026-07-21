## 1. 现状清单与依赖确认

- [x] 1.1 记录 `CharacterSimulationPresentationRuntime` 的字段、公开方法、构造重载和直接调用点
- [x] 1.2 记录 local owner、Deterministic Rollback simulated actor和ServerAuthoritative observed actor的当前创建参数
- [x] 1.3 记录 `ServerAuthoritativeRemoteVisualPoseFilter` 的输入、状态、输出和diagnostics字段
- [x] 1.4 记录remote visual profile资产路径、`.meta` GUID和全部场景引用
- [x] 1.5 确认 `refactor-gameplay-runtime-and-tooling-modules` 的selected Body与Camera实现已稳定且不再并发编辑相同文件
- [x] 1.6 确认本change不修改Action、Tag、GameplayEffect、Graph、Timeline、Agent schema、Program operation和Corin动作资产
- [x] 1.7 确认 `refactor-simulation-tick-hot-path` 只改变最终result生产方式时，本change仍只消费committed result

## 2. Presentation Body合同

- [x] 2.1 定义 `CharacterPresentationBodyInterval` 的Actor、previous/current tick和pose字段
- [x] 2.2 定义 `Append` 与 `Reset` 的显式body stream update kind
- [x] 2.3 校验Body interval Actor identity、tick有效性和前后顺序
- [x] 2.4 定义 `CommittedStream` 与 `SelectedStream` body策略
- [x] 2.5 定义只读 `CharacterBodyPresentationFrame`
- [x] 2.6 在Body frame中包含previous/current tick、sample alpha和visible pose
- [x] 2.7 在Body frame中包含animation sample tick与sample alpha
- [x] 2.8 在Body frame中包含reset reason、target pose与visual error诊断数据
- [x] 2.9 从公开合同移除以camera ownership推断Body策略的参数
- [x] 2.10 让Body策略在runtime创建后不可切换

## 3. Committed Body Runtime

- [x] 3.1 创建内部 `CharacterBodyPresentationRuntime`
- [x] 3.2 迁移Actor、tick rate、visual root和bind pose校验
- [x] 3.3 迁移committed body历史所有权
- [x] 3.4 迁移presentation delta驱动的本地表现时钟
- [x] 3.5 迁移零step、单step和多step body interval消费
- [x] 3.6 迁移同tickcanonical pose替换识别
- [x] 3.7 迁移tick回退后的stream reset处理
- [x] 3.8 迁移visible pose保留与recovery offset初始化
- [x] 3.9 迁移position和rotation recovery连续推进
- [x] 3.10 迁移Body history裁剪且保持所需相邻interval
- [x] 3.11 让CommittedStream内部识别已提交分支替换并拒绝仅SelectedStream允许的外部Reset
- [x] 3.12 让Body Runtime唯一应用visual root position与rotation
- [x] 3.13 让Body Runtime输出同一visible pose给Camera而不读取VisualRoot回值
- [x] 3.14 迁移Body interpolation profiler marker和trace发布
- [x] 3.15 让committed branch replacement只按旧分支与新分支在同一presentation sample tick的姿态差生成recovery offset
- [x] 3.16 保留replacement起点之前的有效body历史并只删除被replay覆盖的tick区间
- [x] 3.17 让已有recovery与新分支差连续合成，先应用本帧offset再衰减后续帧

## 4. Selected Body收敛

- [x] 4.1 将selected interval elapsed time迁入Body Runtime
- [x] 4.2 将selected target position和yaw区间采样迁入Body Runtime
- [x] 4.3 将position velocity和有界SmoothDamp迁入Body Runtime
- [x] 4.4 将yaw velocity和有界SmoothDampAngle迁入Body Runtime
- [x] 4.5 让selected stream replacement只响应显式Reset
- [x] 4.6 保持selected tick来自Model Egress提交而不是Presentation自行选择
- [x] 4.7 保持零新interval的PresentationFrame继续收敛到已提交target
- [x] 4.8 保持canonical selected Body不被visual pose覆盖
- [x] 4.9 将target、visual、error、reset sequence和reset reason diagnostics迁入Presentation
- [x] 4.10 删除visual body伪装成前后相同canonical interval的路径

## 5. Remote Presentation Profile所有权

- [x] 5.1 在Character Presentation模块定义正式 `CharacterRemotePresentationProfile`
- [x] 5.2 保留并校验position smooth time、max speed、yaw smooth time和max yaw speed字段
- [x] 5.3 将profile settings降低为Presentation内部不可变配置
- [x] 5.4 移动并重命名现有profile脚本且保留原`.meta` identity
- [x] 5.5 将Corin remote profile资产迁入角色Presentation配置目录且保留原`.meta` identity
- [x] 5.6 更新唯一资产显示名与正式类型
- [x] 5.7 保持两个ServerAuthoritative客户端Scene引用同一迁移后资产
- [x] 5.8 删除 `ServerAuthoritativeRemotePresentationProfile` 类型、namespace和旧路径
- [x] 5.9 搜索并拒绝默认remote profile、兼容wrapper和第二份收敛配置

## 6. Camera内部模块

- [x] 6.1 创建内部 `CharacterCameraPresentationRuntime`
- [x] 6.2 迁移Camera state producer实例容器
- [x] 6.3 迁移Camera response producer实例容器
- [x] 6.4 迁移Camera target producer实例容器
- [x] 6.5 迁移pending Camera cue生命周期
- [x] 6.6 迁移State、Response和Modifier resolver所有权
- [x] 6.7 迁移Camera target binding预检
- [x] 6.8 迁移follow和aim bind offset计算
- [x] 6.9 迁移look input采样与response应用
- [x] 6.10 让Camera Runtime只消费Body Runtime输出的visible pose
- [x] 6.11 迁移CameraPosePlan生成与rig adapter调用
- [x] 6.12 保持无Camera组合收到Camera command时明确失败
- [x] 6.13 保持无Camera Actor不创建Camera容器和resolver
- [x] 6.14 迁移Camera Reset和Dispose状态清理

## 7. 协调器与动画生命周期

- [x] 7.1 收窄 `CharacterSimulationPresentationRuntime` 为唯一公开协调器
- [x] 7.2 保留Actor和Program/Projection identity校验
- [x] 7.3 保留producer existence与kind校验
- [x] 7.4 将playback command唯一转发给 `CharacterAnimationPlaybackRuntime`
- [x] 7.5 将Camera command唯一转发给Camera Runtime
- [x] 7.6 保持现有Cue/VFX/UI信号路由且不宣称新增最终consumer
- [x] 7.7 定义 `RequireCommittedSelection` 动画启动策略
- [x] 7.8 定义 `AwaitCommittedSelection` 动画启动策略
- [x] 7.9 让owner和simulated actor缺少required selection时保持明确错误
- [x] 7.10 让observed actor在可靠selection前只推进Body
- [x] 7.11 让observed actor首个合法selection后进入现有PendingFirstSample生命周期
- [x] 7.12 统一Body、Animation、Camera和frame cleanup的固定调用顺序
- [x] 7.13 让Animation sampling使用Body frame的sample tick和alpha
- [x] 7.14 从外部合同删除 `HasRequiredAnimationOutput`
- [x] 7.15 删除外部 `PresentBody` 入口
- [x] 7.16 删除无行为的Presentation `BeginTick`入口及调用
- [x] 7.17 保持AnimationPlaybackLifecycle与Animancer adapter实现不变
- [x] 7.18 保持一个PresentationFrame只调用一次协调器 `Present`

## 8. Factory与正式组合

- [x] 8.1 让 `CharacterPresentationRuntimeFactory` 成为唯一runtime创建入口
- [x] 8.2 定义 `CreateLocalOwner` 的CommittedStream、RequireCommittedSelection和Camera组合
- [x] 8.3 定义 `CreateSimulatedActor` 的CommittedStream、RequireCommittedSelection和无Camera组合
- [x] 8.4 定义 `CreateObservedActor` 的SelectedStream、AwaitCommittedSelection和remote profile组合
- [x] 8.5 集中Program/Projection identity和tick rate校验
- [x] 8.6 集中Body、Animation和可选Camera模块构造失败清理
- [x] 8.7 将 `CharacterPipelineHost` 迁移到 `CreateLocalOwner`
- [x] 8.8 将Deterministic Rollback本地Actor迁移到 `CreateLocalOwner`
- [x] 8.9 将Deterministic Rollback无相机simulated actor迁移到 `CreateSimulatedActor`
- [x] 8.10 将ServerAuthoritative remote site迁移到 `CreateObservedActor`
- [x] 8.11 删除六个直接构造重载和全部直接 `new CharacterSimulationPresentationRuntime`
- [x] 8.12 搜索并删除按class、namespace、Actor名称或Network Model猜测组合的路径

## 9. ServerAuthoritative适配清理

- [x] 9.1 让Remote Presentation Registration只提交selected Body interval和显式Reset
- [x] 9.2 保持selected Body horizon仍由Prediction Schedule和Model Egress决定
- [x] 9.3 保持SampleProducer可提前缓存且reliable event服从selected horizon
- [x] 9.4 保持Remote Gameplay output buffer与可靠事件顺序
- [x] 9.5 删除 `ServerAuthoritativeRemoteVisualPoseFilter`
- [x] 9.6 删除ServerAuthoritative visual position、yaw和velocity状态
- [x] 9.7 删除ServerAuthoritative对 `HasRequiredAnimationOutput` 的读取
- [x] 9.8 删除ServerAuthoritative对 `PresentBody` 的调用
- [x] 9.9 将model diagnostics收窄为selected tick、queue和reliable horizon
- [x] 9.10 确认ServerAuthoritative模块不再引用SmoothDamp或remote visual settings

## 10. Lifecycle、清理与文档

- [x] 10.1 统一runtime Reset顺序并清空Body、Animation、Camera和signals状态
- [x] 10.2 统一runtime Dispose顺序并保证每个内部模块只释放一次
- [x] 10.3 保持外部registration先注销PresentationFrame target再Dispose runtime
- [x] 10.4 删除不再使用的Body sample、producer instance和Camera辅助类型
- [x] 10.5 删除不再使用的公开property、构造API和using引用
- [x] 10.6 搜索并删除旧profile名、旧filter名、`PresentBody`和`HasRequiredAnimationOutput`
- [x] 10.7 搜索并确认只有Body Runtime写visual root Transform
- [x] 10.8 搜索并确认只有Camera Runtime调用camera rig adapter
- [x] 10.9 搜索并确认只有Animation Playback链调用Animancer
- [x] 10.10 更新 `openspec/project.md` 的Presentation目录与运行链说明
- [x] 10.11 更新受影响current spec文字且不覆盖并行Action change的语义

## 11. 编译与OpenSpec校验

- [x] 11.1 使用规定参数编译 `ThirdPersonClient.Runtime.csproj`
- [x] 11.2 编译后立即执行 `dotnet build-server shutdown`
- [x] 11.3 使用规定参数编译 `ThirdPersonSimulation.ServerAuthoritative.Unity.csproj`
- [x] 11.4 编译后立即执行 `dotnet build-server shutdown`
- [x] 11.5 使用规定参数编译 `ThirdPersonSimulation.DeterministicRollback.Unity.csproj`
- [x] 11.6 编译后立即执行 `dotnet build-server shutdown`
- [x] 11.7 使用规定参数编译 `ThirdPersonClient.Editor.csproj` 以确认资产类型与Inspector引用
- [x] 11.8 编译后立即执行 `dotnet build-server shutdown`
- [x] 11.9 运行 `openspec validate refactor-character-presentation-runtime-modules --strict --no-interactive`
- [x] 11.10 确认全部任务真实完成后再将本文件所有任务标记为 `[x]`
- [x] 11.11 使用规定参数重新编译 `Assembly-CSharp.csproj` 并在完成后关闭build server
