## MODIFIED Requirements

### Requirement: Runtime、Preview和Live Debug必须使用同一事实源

完整场景预览 MUST使用真实 Actor 的正式输入、committed Body/Intent、Action lifecycle 与 state-local source，经匹配 Projection、source backend、Routing Plan、Pose Plan 和 completion 语义产生结果。Action Timeline、Pose Graph、Blend Space 与 MM 作者页面 MUST不为完整角色输出提供独立 Action command、Fact 或 query fixture。有限技能动画 Diagnostics MUST结合 Actor、ActionInstance、SkillProgram、调用路径/运行 generation 与 playback 区分，state-local Pose MUST仍按 Actor/Provider/Player/Source/generation 区分；MUST显示真实生命周期、effective sample、transition、release 与 Pose contribution，不得从 Animancer weight 或骨骼反推第二份事实。

#### Scenario: Projection变为Stale

- **WHEN** 拓扑或装配改变而 Projection 尚未显式 Build
- **THEN** 受控预览与新的正式运行准备 MUST停止使用旧产物
- **AND** MUST不创建临时 Plan、旧产物补充路径或独立 PlayableGraph

#### Scenario: 合法参数已经修改

- **WHEN** 作者仅修改正式运行调参合同支持的字段
- **THEN** 当前 Actor MUST通过原子参数协议按规定时机采用
- **AND** Diagnostics MUST显示实际采用状态，不把提交中的值当成已提交事实
