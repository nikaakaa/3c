# Tasks

## 1. 现有资产身份与迁移输入

- [x] 1.1 导出 Corin RootTree 的 Agent v8 Snapshot并记录Root、Action、Locomotion、Attack Combo和Dodge状态的稳定identity。
- [x] 1.2 记录现有 Attack1/2 state body、Timeline、track、clip、TreeClip、Action Context与lifecycle identity。
- [x] 1.3 记录 DodgeBack/DodgeForward state body、Timeline、TreeClip、rule graph与lifecycle identity。
- [x] 1.4 记录 `IsDodging` declaration、写入节点、读取节点和全部Locomotion rule引用。
- [x] 1.5 记录现有 Attack1/2 PipelineInplace animation与main root-motion curve的来源、帧数和归零口径。
- [x] 1.6 记录 WithWeaponInplace Normal01..05、对应End、03Explode与05B的GUID、60fps时长和loop配置。
- [x] 1.7 记录 WithWeaponRootmotion Normal01..05及对应End的GUID、时长和root transform binding。

## 2. 规范化五段攻击表现资产

- [x] 2.1 保持现有 Pipeline Attack1主动画GUID与归零结果不变。
- [x] 2.2 保持现有 Pipeline Attack2主动画GUID与归零结果不变。
- [x] 2.3 从 Normal03 WithWeaponInplace创建唯一 Pipeline Attack3主动画资产。
- [x] 2.4 从 Normal04 WithWeaponInplace创建唯一 Pipeline Attack4主动画资产。
- [x] 2.5 从 Normal05 WithWeaponInplace创建唯一 Pipeline Attack5主动画资产。
- [x] 2.6 从 Normal01 End WithWeaponInplace创建唯一 Pipeline Attack1 End资产。
- [x] 2.7 从 Normal02 End WithWeaponInplace创建唯一 Pipeline Attack2 End资产。
- [x] 2.8 从 Normal03 End WithWeaponInplace创建唯一 Pipeline Attack3 End资产。
- [x] 2.9 从 Normal04 End WithWeaponInplace创建唯一 Pipeline Attack4 End资产。
- [x] 2.10 从 Normal05 End WithWeaponInplace创建唯一 Pipeline Attack5 End资产。
- [x] 2.11 对新增八份资产应用与Pipeline Attack1/2相同的根节点初始X/Z归零规则。
- [x] 2.12 保持新增资产60fps、完整时长、非循环和全部3328个WithWeapon curve path。
- [x] 2.13 确认普通五连不引用普通Inplace、Humanoid Inplace或WithWeaponRootmotion表现clip。
- [x] 2.14 将03Explode与05B登记为未接入特殊资源，不创建猜测性状态、Timeline或fallback。

## 3. 五段攻击 gameplay motion curve

- [x] 3.1 保持 Attack1 main motion curve继续来源于Normal01 WithWeaponRootmotion。
- [x] 3.2 保持 Attack2 main motion curve继续来源于Normal02 WithWeaponRootmotion。
- [x] 3.3 从Normal03 WithWeaponRootmotion烘焙Attack3 main motion curve。
- [x] 3.4 从Normal04 WithWeaponRootmotion烘焙Attack4 main motion curve。
- [x] 3.5 从Normal05 WithWeaponRootmotion烘焙Attack5 main motion curve。
- [x] 3.6 从Normal01 End WithWeaponRootmotion烘焙Attack1 End motion curve。
- [x] 3.7 从Normal02 End WithWeaponRootmotion烘焙Attack2 End motion curve。
- [x] 3.8 从Normal03 End WithWeaponRootmotion烘焙Attack3 End motion curve。
- [x] 3.9 从Normal04 End WithWeaponRootmotion烘焙Attack4 End motion curve。
- [x] 3.10 从Normal05 End WithWeaponRootmotion烘焙Attack5 End motion curve。
- [x] 3.11 确认所有motion curve从零累计、保留signed yaw并使用正式60fps采样口径。
- [x] 3.12 确认动画pose不提供第二条runtime root-motion位移路径。

## 4. Attack1 与 Attack2 Timeline 后摇修正

- [x] 4.1 保留Attack1 inline Timeline与现有AnimationTrack producer identity。
- [x] 4.2 将Attack1主动画clip的extrapolation从Hold改为None。
- [x] 4.3 在Attack1 AnimationTrack添加Attack1 End clip并设置完整End时长。
- [x] 4.4 为Attack1主段与End段配置一次明确、有限且无空采样区间的overlap/ease。
- [x] 4.5 在Attack1 MotionCurveTrack摆放main与End motion clip并使用各自真实CurveEndFrame。
- [x] 4.6 保持Attack1Hit、Attack1Cancel、Cue和Action Context identity。
- [x] 4.7 保留Attack2 inline Timeline与现有AnimationTrack producer identity。
- [x] 4.8 将Attack2主动画clip的extrapolation从Hold改为None。
- [x] 4.9 在Attack2 AnimationTrack添加Attack2 End clip并设置完整End时长。
- [x] 4.10 为Attack2主段与End段配置一次明确、有限且无空采样区间的overlap/ease。
- [x] 4.11 在Attack2 MotionCurveTrack摆放main与End motion clip并使用各自真实CurveEndFrame。
- [x] 4.12 保持Attack2Hit、Attack2Cancel、Cue和Action Context identity。

## 5. Attack3、Attack4 与 Attack5 leaf闭环

- [x] 5.1 在Attack Combo StateMachine创建唯一Attack3 StateNode及inline state body。
- [x] 5.2 在Attack Combo StateMachine创建唯一Attack4 StateNode及inline state body。
- [x] 5.3 在Attack Combo StateMachine创建唯一Attack5 StateNode及inline state body。
- [x] 5.4 为Attack3配置request consume、Attack ActionProfile、独立Action Context、OnExit lifecycle和inline TimelineNode。
- [x] 5.5 为Attack4配置request consume、Attack ActionProfile、独立Action Context、OnExit lifecycle和inline TimelineNode。
- [x] 5.6 为Attack5配置request consume、Attack ActionProfile、独立Action Context、OnExit lifecycle和inline TimelineNode。
- [x] 5.7 为Attack3 inline Timeline创建Animation、MotionCurve、Cue与Decision Tree tracks。
- [x] 5.8 在Attack3 AnimationTrack摆放主攻击与End clip并绑定规范化Pipeline资产。
- [x] 5.9 在Attack3 MotionCurveTrack摆放main与End motion clip。
- [x] 5.10 为Attack3创建root-owned Attack3Hit与Attack3Cancel Frame declaration及Decision TreeClip。
- [x] 5.11 为Attack4 inline Timeline创建Animation、MotionCurve、Cue与Decision Tree tracks。
- [x] 5.12 在Attack4 AnimationTrack摆放主攻击与End clip并绑定规范化Pipeline资产。
- [x] 5.13 在Attack4 MotionCurveTrack摆放main与End motion clip。
- [x] 5.14 为Attack4创建root-owned Attack4Hit与Attack4Cancel Frame declaration及Decision TreeClip。
- [x] 5.15 为Attack5 inline Timeline创建Animation、MotionCurve、Cue与Decision Tree tracks。
- [x] 5.16 在Attack5 AnimationTrack摆放主攻击与End clip并绑定规范化Pipeline资产。
- [x] 5.17 在Attack5 MotionCurveTrack摆放main与End motion clip。
- [x] 5.18 为Attack5创建root-owned Attack5Hit Frame declaration及Decision TreeClip。
- [x] 5.19 为Attack3/4/5 Hit declaration配置唯一WindowId、Digest和ActionWindow projection。
- [x] 5.20 为Attack3/4 Cancel declaration配置唯一WindowId、Digest和ActionWindow projection。
- [x] 5.21 为Attack3/4/5配置唯一GameplayCue与CameraCue identity。
- [x] 5.22 为Attack3/4/5 AnimationTrack producer增加正式Presentation Profile binding。

## 6. 五段连段状态转移

- [x] 6.1 保持Attack1Cancel + Attack request的Attack1-to-Attack2 transition。
- [x] 6.2 将Attack2Cancel + Attack request的target从Attack1改为Attack3。
- [x] 6.3 创建Attack3Cancel + Attack request的Attack3-to-Attack4 transition。
- [x] 6.4 创建Attack4Cancel + Attack request的Attack4-to-Attack5 transition。
- [x] 6.5 删除Attack2-to-Attack1循环edge及其旧rule引用。
- [x] 6.6 为Attack3、Attack4、Attack5分别创建StateRootCompleted-to-Exit transition。
- [x] 6.7 清理无条件Attack5-to-Attack1 edge与没有Timeline fact的虚假Cancel window。
- [x] 6.8 保持每个combo source OnExit只提交一次取消terminal transition。
- [x] 6.9 保持每个combo target activation唯一消费request并创建新的Action Context。
- [x] 6.10 保持任一阶段无combo request时继续播放本段End clip到自然完成。
- [x] 6.11 保持leaf自然完成只提交一次Complete并进入nested Exit。
- [x] 6.12 保持outer Attack root completed回到None且不提交第二条terminal transition。

## 7. 外层 Action 与 Dodge 层级

- [x] 7.1 在外层Action StateMachine创建唯一Dodge StateNode与inline state body。
- [x] 7.2 在Dodge state body创建inline Dodge Direction StateMachine。
- [x] 7.3 将DodgeBack StateNode、inline body、Timeline和lifecycle移动到nested graph而不克隆。
- [x] 7.4 将DodgeForward StateNode、inline body、Timeline和lifecycle移动到nested graph而不克隆。
- [x] 7.5 将方向选择rule移动到nested Entry transitions，删除内层对瞬时Dodge request的重复门禁，并保持target leaf唯一消费Dodge request。
- [x] 7.6 将DodgeBack/DodgeForward完成与move-cancel transitions接到nested Exit。
- [x] 7.7 将outer None-to-Dodge收敛为只查询Dodge request的动作大类入口。
- [x] 7.8 将outer Dodge-to-None绑定nested StateRootCompleted。
- [x] 7.9 删除outer旧DodgeBack/DodgeForward states、edges和orphan rules。
- [x] 7.10 确认outer Action最终只包含None、Attack、Dodge。

## 8. Full-body Action locomotion ownership

- [x] 8.1 创建root-owned internal Bool declaration `HasActionLocomotionOwnership`。
- [x] 8.2 创建root-owned internal Bool declaration `ResumeLocomotionThroughRunEnd`。
- [x] 8.3 在outer Attack OnEnter设置ownership=true、resume-run-end=false。
- [x] 8.4 在outer Attack OnExit设置ownership=false且不复制leaf terminal lifecycle。
- [x] 8.5 在outer Dodge OnEnter设置ownership=true、resume-run-end=true。
- [x] 8.6 在outer Dodge OnExit设置ownership=false且不复制leaf terminal lifecycle。
- [x] 8.7 删除`IsDodging` declaration及Dodge leaf旧写入节点。
- [x] 8.8 删除全部Locomotion rule中的`IsDodging`读取与Not节点。
- [x] 8.9 确认没有`IsDodging`序列化引用、source map或Agent schema残留。

## 9. Locomotion ActionOverride交接

- [x] 9.1 将Idle、WalkStart、WalkLoop、WalkEnd到ActionOverride的edge改为统一ownership条件。
- [x] 9.2 将RunStart、RunLoop、RunEnd、MovingTurn到ActionOverride的edge改为统一ownership条件。
- [x] 9.3 保持全部ownership edge稳定高优先级并删除重复source-target edge。
- [x] 9.4 配置ActionOverride在ownership=false且有输入时直接进入RunLoop。
- [x] 9.5 配置ActionOverride在ownership=false、无输入且resume-run-end=true时进入RunEnd。
- [x] 9.6 配置ActionOverride在ownership=false、无输入且resume-run-end=false时进入Idle。
- [x] 9.7 保持ActionOverride body无Timeline、ActionProfile、animation、motion或request consume。

## 10. 同tick ownership与分层停止

- [x] 10.1 将Gameplay Parallel的Action child flow order调整到Locomotion之前。
- [x] 10.2 保持Action与Locomotion复用现有Parallel/StateMachine operation而不新增仲裁节点。
- [x] 10.3 确认Action激活tick先写ownership再由Locomotion进入ActionOverride。
- [x] 10.4 确认Action完成tick先清ownership再由Locomotion选择恢复producer。
- [x] 10.5 确认active Attack/Dodge与普通Locomotion Timeline不在同tick提交不同Base selection。
- [x] 10.6 保持parent graceful stop沿outer state、nested SM、leaf state和TimelineNode传播。
- [x] 10.7 保持ForceStop只释放runtime资源而不伪造gameplay terminal transition。

## 11. Agent、编译产物与文档收口

- [x] 11.1 更新Agent synthesis macro与业务样例coverage中的五段Attack、outer Action集合和Dodge层级约束，保持通用Validator不硬编码Corin。
- [x] 11.2 在Agent synthesis业务样例coverage中检查ActionOverride ownership declaration名称与职责，并删除旧`IsDodging`样例语义。
- [x] 11.3 通过Agent v8 Snapshot确认五段Attack、nested Dodge、唯一ownership和全部Timeline clips。
- [x] 11.4 运行Agent validate_graph并修复全部authoring/semantic错误。
- [x] 11.5 运行Agent dry-run compile并确认nested graph、Timeline、TreeClip、motion和source map可编译。
- [x] 11.6 使用正式Character Simulation compile workflow重新生成Semantic IR。
- [x] 11.7 使用正式Float32 Target workflow重新生成Corin Program与Presentation Projection。
- [x] 11.8 确认Program、Projection、source revision、ProgramHash、producer binding和source map一致。
- [x] 11.9 确认Presentation Profile只保留当前Attack producer binding且不存在stale binding。
- [x] 11.10 更新change文档中的最终GUID、帧范围、WindowId、Digest和迁移结果。
- [x] 11.11 运行`openspec validate refactor-corin-action-combo-authoring --strict --no-interactive`。

## 12. 循环连段与移动取消扩展

- [x] 12.1 修正文档中Attack5终止语义并记录显式循环、MoveCancel与同Tick优先级。
- [x] 12.2 为Attack1..5创建root-owned Frame/Frame MoveCancel declaration与ActionWindow投影身份。
- [x] 12.3 为Attack5创建ComboCancel declaration并放置同Timeline Decision TreeClip。
- [x] 12.4 将Attack1..5 ComboCancel窗口对齐各自End后摇的前段。
- [x] 12.5 在Attack1..5 Timeline后摇晚段放置MoveCancel Decision TreeClip。
- [x] 12.6 创建Attack5-to-Attack1显式combo transition并保持Attack request查询纯度。
- [x] 12.7 为Attack1..5创建MoveCancel-to-Exit transition并排除同TickAttack request。
- [x] 12.8 设置combo transition稳定高于MoveCancel transition。
- [x] 12.9 将普通攻击取消lifecycle收敛为覆盖combo与移动取消的唯一RecoveryCancel分支。

## 13. Dodge通用后摇取消

- [x] 13.1 将`CanDodgeMoveCancel` declaration迁移为`DodgeRecoveryCancel`并删除旧名称。
- [x] 13.2 保持DodgeBack与DodgeForward Timeline仅在后摇段写入DodgeRecoveryCancel。
- [x] 13.3 为两个Dodge leaf增加后摇内Attack category handoff条件。
- [x] 13.4 为两个Dodge leaf增加按当前MoveAxis选择DodgeBack/DodgeForward的重入条件。
- [x] 13.5 保持新Dodge target leaf为Dodge request唯一消费点。
- [x] 13.6 将Dodge move-cancel条件改为排除同TickAttack与Dodge request。
- [x] 13.7 设置Dodge后摇优先级为Attack、Dodge、移动、自然完成。
- [x] 13.8 将Dodge leaf取消lifecycle reason收敛为`DodgeRecoveryCancel`。

## 14. RushAttack作者闭环

- [x] 14.1 规范化RushAttack主段与End inplace动画资产并保持60fps非循环。
- [x] 14.2 从匹配root-motion资源烘焙RushAttack主段与End motion curve。
- [x] 14.3 在Attack内层创建RushAttack State与inline StateBehaviorSubTree。
- [x] 14.4 在RushAttack body创建唯一Action activation、Action Context、Timeline与terminal lifecycle。
- [x] 14.5 在RushAttack Timeline放置主动画、End动画、主motion、End motion与有限重叠。
- [x] 14.6 创建RushAttack Hit、ComboCancel与MoveCancel declaration及Decision TreeClip。
- [x] 14.7 使用同Tick`DodgeRecoveryCancel` Frame fact使Dodge recovery Attack进入RushAttack，普通None-to-Attack仍进入Attack1。
- [x] 14.8 配置RushAttack后段Attack request进入Attack1，移动输入进入Exit且Attack优先。
- [x] 14.9 为RushAttack producer增加唯一Presentation binding并确认Explode变体未连接。

## 15. Agent与编译收口

- [x] 15.1 扩展Agent intent使combo loop为显式选项并支持MoveCancel。
- [x] 15.2 扩展action_combo Macro生成显式末段回首段与MoveCancel条件。
- [x] 15.3 扩展directional_dodge Macro表达后摇重入与RushAttack handoff。
- [x] 15.4 更新Macro coverage与Corin synthesis业务覆盖，不向通用Validator加入角色硬编码。
- [x] 15.5 删除一次性迁移入口并确认没有旧`CanDodgeMoveCancel`或末段终止残留。
- [x] 15.6 使用正式Character Simulation compile workflow重新生成Semantic IR、Float32 Program与Presentation Projection。
- [x] 15.7 运行Agent Snapshot、validate_graph与dry-run compile并修复全部错误。
- [x] 15.8 核对ProgramHash、source revision、producer binding、source map与资产引用一致。
- [x] 15.9 运行`openspec validate refactor-corin-action-combo-authoring --strict --no-interactive`。
