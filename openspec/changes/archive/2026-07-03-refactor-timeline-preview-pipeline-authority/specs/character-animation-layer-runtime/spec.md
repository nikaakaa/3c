# character-animation-layer-runtime Specification

## MODIFIED Requirements

### Requirement: 角色管线不依赖旧动画播放路径
系统 MUST 保持角色管线和 BTSMTL Timeline 编辑器预览只有一条动画播放语义：动画来源写贡献，动画层运行时仲裁，Animancer adapter 应用。角色管线 MUST NOT 读取旧 `AnimationPresentationPolicySO`、旧 locomotion/action SO、旧 bodyclaim policy，MUST NOT 依赖 `TimelinePlayer` autonomous playback。BTSMTL Timeline 编辑器预览如果需要播放角色动画，MUST 复用正式动画贡献、动画层运行时和 Animancer adapter，MUST NOT 继续以 `TimelinePlayer` 或独立 PlayableGraph 作为预览真相。

#### Scenario: 搜索旧直接播放入口
- **WHEN** 实现阶段发现角色管线运行路径仍引用 `Animator.Play`、`Animator.CrossFade`、`TimelinePlayer` autonomous playback 或旧动画策略 SO
- **THEN** 该引用 MUST 删除或迁移到正式动画层
- **AND** 系统 MUST NOT 保留兼容分支让旧路径继续驱动角色动画

#### Scenario: BTSMTL 编辑器预览播放 Timeline
- **WHEN** BTSMTL Timeline 编辑器需要预览角色动画
- **THEN** 预览 MUST 使用正式 Timeline 采样输出动画贡献
- **AND** 预览 MUST 使用 `CharacterAnimationLayerRuntime` 生成播放计划
- **AND** 预览 MUST 使用 `AnimancerAnimationPresenter` 或等价正式 adapter 应用播放计划
- **AND** 预览 MUST NOT 使用 `TimelinePlayer` autonomous playback 或独立 PlayableGraph 作为另一套动画真相

