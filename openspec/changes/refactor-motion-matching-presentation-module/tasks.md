## 1. 现状清单与迁移约束

- [ ] 1.1 列出Factory当前创建trajectory Adapter的全部调用点。
- [ ] 1.2 列出Simulation Presentation当前保存Intent与Selected sequence的全部字段。
- [ ] 1.3 列出Simulation Presentation当前具体Adapter类型判断的全部调用点。
- [ ] 1.4 列出Playback当前MM producer、sampling、output、frame selection与cleanup字段。
- [ ] 1.5 列出Playback当前MM Resolve、retention、history、Replay、Reset与Dispose helper。
- [ ] 1.6 确认Timeline sampling、通用Lifecycle、显式Player节点、Pose Graph Plan与FootPlacement owner不迁移。
- [ ] 1.7 定义旧分散owner删除清单，禁止保留兼容wrapper与双写状态。

## 2. Module合同与固定帧数据

- [ ] 2.1 定义`CharacterMotionMatchingPresentationModule`唯一生命周期。
- [ ] 2.2 定义Module启用状态只来自Projection MM payload。
- [ ] 2.3 定义Module锁定ActorId与Body SourceMode。
- [ ] 2.4 定义正式Body frame输入。
- [ ] 2.5 定义可选Accepted Intent输入及其Actor、sequence与reset约束。
- [ ] 2.6 定义固定容量`MotionMatchingPlaybackDemand`。
- [ ] 2.7 定义固定容量Animation Selection结果集合。
- [ ] 2.8 定义非零单调frame completion identity。
- [ ] 2.9 定义Resolve结果中的selection count、resolved producer与history completion信息。
- [ ] 2.10 定义同帧重复Resolve的typed失败。
- [ ] 2.11 定义缺失Resolve、重复Complete与跨帧Complete的typed失败。
- [ ] 2.12 定义reset identity不匹配的typed失败。

## 3. Trajectory Adapter内聚

- [ ] 3.1 把Accepted Intent Adapter构造移动进Module。
- [ ] 3.2 把Selected Body Adapter构造移动进Module。
- [ ] 3.3 把最新Accepted Intent状态移动进Module。
- [ ] 3.4 把Accepted Intent单调sequence校验移动进Module。
- [ ] 3.5 把Selected Body trajectory sequence移动进Module。
- [ ] 3.6 让Accepted Intent Adapter从正式Body visible pose建立frame。
- [ ] 3.7 让Selected Body Adapter从正式Body target cursor建立frame。
- [ ] 3.8 让Selected Body Adapter继续传播selected tick与真实sample age。
- [ ] 3.9 让Module按锁定SourceMode选择唯一Adapter。
- [ ] 3.10 删除外部`ICharacterMotionMatchingTrajectorySource`读帧入口。
- [ ] 3.11 删除Factory中的`CreateMotionMatchingTrajectorySource`。
- [ ] 3.12 删除Simulation Presentation中的具体Adapter类型判断。
- [ ] 3.13 删除Simulation Presentation中的Intent缓存与Selected sequence。
- [ ] 3.14 让无MM payload配置不构造trajectory Adapter。

## 4. Producer与Selection所有权迁移

- [ ] 4.1 把MM producer runtime字典移动进Module。
- [ ] 4.2 把Projection producer binding校验移动进Module构造。
- [ ] 4.3 把projection exact identity传入Module拥有的producer runtime。
- [ ] 4.4 把MM sampling映射移动进Module。
- [ ] 4.5 让Module接收MM producer sample publish。
- [ ] 4.6 让Module接收MM producer replace。
- [ ] 4.7 让Module接收MM producer retire与release。
- [ ] 4.8 让Module按正式Playback demand解析当前producer。
- [ ] 4.9 保持AnimationChannel winner只由通用Playback selection提供。
- [ ] 4.10 禁止Module读取State、Action、Tree route、Priority或候选列表。
- [ ] 4.11 把跨Database Search、Plan与Selection继续委托给现有producer runtime。
- [ ] 4.12 删除Playback直接查找`CharacterMotionMatchingProducerRuntime`的入口。

## 5. Resolve阶段与Selection生成

- [ ] 5.1 在Module BeginFrame清理固定Selection workspace。
- [ ] 5.2 在Module内从Body/Intent解析唯一Trajectory Source Frame。
- [ ] 5.3 在Module内处理ResetSequence推进。
- [ ] 5.4 对每个selected MM demand执行一次producer Resolve。
- [ ] 5.5 拒绝同一producer同一PresentationFrame重复Resolve。
- [ ] 5.6 把Pose Source output转换为完整`AnimationPoseSourceId`。
- [ ] 5.7 让正式MM Selection lowerer生成`AnimationSelectionFrame`。
- [ ] 5.8 保证Selection不携带transition、PoseSlot或隐藏Stack字段。
- [ ] 5.9 把Selection写入固定容量结果集合。
- [ ] 5.10 保存本帧待完成producer与PlaybackId。
- [ ] 5.11 对未被本帧解析的producer执行唯一Domain release。
- [ ] 5.12 让Module返回Selection集合与completion identity。
- [ ] 5.13 让Playback只把返回Selection合并到公共Animation Selection集合。
- [ ] 5.14 让Playback只把新Selection提交给匹配的MotionMatchingSelectionInput。
- [ ] 5.15 删除Playback中的MM Resolve循环。
- [ ] 5.16 删除Playback中的`AddMotionMatchingRequest`实现。

## 6. Retention与Frozen Output

- [ ] 6.1 把MM frozen output字典移动进Module。
- [ ] 6.2 把当前帧resolved producer集合移动进Module。
- [ ] 6.3 让Module从Pose Plan completion读取全部Player发布的MM source usage identity。
- [ ] 6.4 用frozen output维持仍被Player使用的正式Selection source descriptor。
- [ ] 6.5 retained Selection不得重新Search或提升Selection Generation。
- [ ] 6.6 Player引用缺失frozen output时产生typed Invalid。
- [ ] 6.7 只在全部Player正式release source后删除frozen output，并让Playback sampling继续服从Lifecycle Retired。
- [ ] 6.8 禁止Module保存Player usage、Stack entry、transition clock或weight副本，只消费Pose Plan completion发布的source usage结果。
- [ ] 6.9 禁止Module保存Stored Pose或Inertial residual副本。
- [ ] 6.10 删除Playback中的retained MM request恢复helper。
- [ ] 6.11 删除Playback中的MM output prune helper。

## 7. Complete阶段与Pose History

- [ ] 7.1 在唯一Pose Graph Evaluate完成后调用Module CompleteFrame。
- [ ] 7.2 校验completion identity、presentation frame与reset identity。
- [ ] 7.3 从正式Pose Runtime复制绑定PoseNode的完成骨骼位置。
- [ ] 7.4 校验绑定PoseNode与Pose Plan completion identity属于本帧Selection batch。
- [ ] 7.5 从正式Pose Runtime读取同帧Foot Feature aggregate。
- [ ] 7.6 把完成Pose追加到对应producer Pose History。
- [ ] 7.7 保持History只供下一帧Query读取。
- [ ] 7.8 绑定PoseNode无合法Pose时记录typed history gap。
- [ ] 7.9 禁止用上一帧Pose、bind pose或当前candidate伪造history。
- [ ] 7.10 完成全部producer后关闭frame completion。
- [ ] 7.11 Complete后按显式Player source usage执行retained frozen output清理。
- [ ] 7.12 删除Playback中的MM frame selection集合。
- [ ] 7.13 删除Playback直接追加Base Slot Pose History的helper。

## 8. Playback与Simulation Presentation接线

- [ ] 8.1 让Playback只保存一个可空MM Module字段。
- [ ] 8.2 让Factory按Projection显式构造或省略MM Module。
- [ ] 8.3 把ActorId与Body SourceMode正式传入Module构造。
- [ ] 8.4 把Module所有权原子转移给Playback。
- [ ] 8.5 让Simulation Presentation的`AcceptsTrajectoryIntent`委托Module能力。
- [ ] 8.6 让Simulation Presentation的Intent capture直接提交Module。
- [ ] 8.7 让Simulation Presentation每帧只提交Body frame与表现时钟。
- [ ] 8.8 让Playback从通用Lifecycle建立固定MM demand buffer。
- [ ] 8.9 让Playback在Timeline request前调用Module ResolveFrame。
- [ ] 8.10 让Playback在Pose Graph完成后调用Module CompleteFrame。
- [ ] 8.11 保持同一PlayableGraph每帧只Evaluate一次。
- [ ] 8.12 保持Foot Placement只消费最终`FinalAnimationPoseFrame`。
- [ ] 8.13 保持Camera在Foot Placement后推进。
- [ ] 8.14 保持无MM payload时Timeline链不经过空MM工作循环。
- [ ] 8.15 删除Playback中的MM producer、sampling、output与cleanup字段。

## 9. Reset、Replacement与Lifetime

- [ ] 9.1 让Body ResetSequence变化原子Reset Module。
- [ ] 9.2 让Committed branch replacement清理Module trajectory与history。
- [ ] 9.3 让Selected stream reset清理Module trajectory与history。
- [ ] 9.4 让EventId Replace影响MM producer时清理对应selection lifecycle。
- [ ] 9.5 让EventId Retire影响MM producer时释放对应domain与frozen output。
- [ ] 9.6 让Presentation Reset清理Module全部frame状态。
- [ ] 9.7 让Projection replacement Dispose旧Module后再构造新Module。
- [ ] 9.8 让Dispose先完成未完成Pose jobs再释放Module workspace。
- [ ] 9.9 让Module Dispose释放全部Database Runtime。
- [ ] 9.10 让Module Dispose清理Replay与diagnostics引用。
- [ ] 9.11 删除Simulation Presentation单独Reset trajectory Adapter的调用。
- [ ] 9.12 删除Simulation Presentation单独Dispose trajectory Adapter的调用。
- [ ] 9.13 删除Playback逐producer Reset与Dispose循环。

## 10. Diagnostics与Replay

- [ ] 10.1 把Search Replay producer查找移动进Module。
- [ ] 10.2 保持Replay exact Projection与Database identity校验。
- [ ] 10.3 让Playback snapshot provider只委托Module Replay入口。
- [ ] 10.4 发布Module Resolve completion identity。
- [ ] 10.5 发布Module selection count。
- [ ] 10.6 发布Module Complete identity。
- [ ] 10.7 发布History appended或typed gap状态。
- [ ] 10.8 发布retained frozen output count。
- [ ] 10.9 发布Module reset reason与前后reset sequence。
- [ ] 10.10 保持diagnostics interest关闭时不构造candidate detail集合。
- [ ] 10.11 无MM Module时不发布伪MM snapshot或capability状态。

## 11. Query Fixture Preview

- [ ] 11.1 让Query Fixture显式选择正式Definition与MM producer。
- [ ] 11.2 让Preview构造与Runtime相同的MM Module。
- [ ] 11.3 让Fixture query进入Module内部正式query seam。
- [ ] 11.4 让Preview复用正式Runtime Database、Admission、Search与Plan。
- [ ] 11.5 让Preview复用正式Animation Selection lowering。
- [ ] 11.6 让Preview Selection进入正式编译Pose Plan与显式Player节点。
- [ ] 11.7 让Preview按图复用SelectedPosePlayer、局部Inertialization或BlendStack，再进入正式Pose Graph composition。
- [ ] 11.8 让Preview通过同一CompleteFrame合同处理history。
- [ ] 11.9 禁止Preview执行Program与WorldSolver。
- [ ] 11.10 禁止Preview执行Foot Physics与Camera。
- [ ] 11.11 删除任何直接Animancer Play或临时PlayableGraph入口。

## 12. 激进清理与文档同步

- [ ] 12.1 删除外部可见的trajectory Adapter具体类型判断。
- [ ] 12.2 删除旧trajectory source Factory方法。
- [ ] 12.3 删除Simulation Presentation旧Intent与sequence字段。
- [ ] 12.4 删除Playback旧MM producer与sampling字段。
- [ ] 12.5 删除Playback旧MM frozen output与frame selection字段。
- [ ] 12.6 删除Playback旧MM request、retention、history与prune helper。
- [ ] 12.7 删除旧Reset、Replay与Dispose分裂调用。
- [ ] 12.8 搜索并删除任何新旧Module双写路径。
- [ ] 12.9 搜索并拒绝MM私有Player、Blend Stack、Inertialization、Pose Graph或PlayableGraph。
- [ ] 12.10 更新`add-character-motion-matching-pose-source`旧owner设计描述。
- [ ] 12.11 更新`add-character-motion-matching-pose-source`受本change完成的Preview任务状态。
- [ ] 12.12 更新`openspec/project.md`的MM表现Module正式链路。
- [ ] 12.13 对比current specs与全部相关active delta并记录剩余矛盾。
- [ ] 12.14 严格校验本change与全部OpenSpec文档。
