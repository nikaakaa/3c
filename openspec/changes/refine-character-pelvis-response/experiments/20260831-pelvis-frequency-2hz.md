# 持续Goal第二轮：Corin骨盆2Hz实验与拒绝

## 裁决

拒绝ae10348的2Hz参数，恢复原3Hz及匹配产品。它确实减少了部分骨盆大步，且保持原Foot输出；但以更久的回正迟滞为代价，实际Solved Knee中大步也增加。不能仅凭最大峰下降或37项质量分不变采纳。

固定效果对照仍为193957，直接前驱为020243/9a36c4e。本轮不改变已有Foot、Bend、共同高度公式、硬Reach或Handoff来补救，不在此失败候选上继续叠加变量。持续质量Goal未完成。

## 假设、改动与构建

33个世界Y绝对步长超过50毫米的前驱窗口中，17个当帧触及硬边界，0个当帧触发Handoff速度清零。266/591/789的前一帧正向修正仍偏高，随后硬上界下降而被迫夹紧，形成延迟下压。

修改前先用020243固定Foot/Pose/Reach输入复算原3Hz响应，Output最大偏差5.04e-8米。只读反事实中，直接把历史换成Root/World高度或取消Handoff清速度均更差；触界全部清速度未解决关键窗口。这些均未进入Runtime，不能概括为所有世界坐标响应方案必定无效。

本轮选择现有Corin正式频率3→2，尝试降低短暂正向需求的采用速度；同时预先记录回正变慢、负偏移驻留延长和R826深压缩风险。不是ZZZ参数复原，也没有增加配置、开关、第二Spring或外部滤波。

ae103485704c171a910ccf7079e42aefd3b60c64只提交CorinFootPlacementProfile的一处数值、正式Float32/Fixed Program与Projection、本change的设计和任务描述。Runtime及Diagnostics算法逐字符不变，TrainingEnemy没有修改。

Runtime规定flags构建27个既有警告、0错误，结束立即shutdown。Unity在Edit加载参数后依次执行正式Float32与Fixed Build，两者共享SourceRevision 478eb360f64f3440352fdf6ba92f61f7e3900389d7b7678f648967e5a362d54a、SemanticHash 6382c0cff9ef6b120289294c965f50a3d1ac54dca1a4c78664f8a97ce9971919、Projection ed34503d67fd89b692f7da59b46afed1b10cfbe4e3271cfa0e046c8018f1a478。随后再次正式加载并以原Record采样，没有batchmode。

## 原包、Proof与可比较性

候选原包20260831-023618-000-a5928c1f85c3409f80d7e5258e3d2fb1，facts62/Analyzer62/diagnosis31、1221列、1043共享帧/2086脚行。所有共享帧实际公开频率为2；Analyzer原公式消费Frequency，没有固定3Hz校验或schema迁移。

023756 Proof对020417官方matched=false：七个Program/Projection身份变化、DivergentFrameCount=0。正式工作流保留这条identity mismatch错误；没有清改比较器或重写Proof。独立核1044完整frames数组以及trace/start/input/body hash逐值相同，时钟仍one-fixed-tick-per-presentation-frame/logic-locked，samples SHA与Proof一致。

143个共同目标/硬Reach/Posture/动画/Body输入列与020243逐值相同，50195行geometry只有四个采样/Surface/Path身份列变化。193957没有直接官方Proof，不伪造它的匹配记录。

## Foot保护

两包Foot State、Goal位置与权重无差异；Ankle/Heel最大实际版本差1.063微米，Toe1.022微米，ResolvedEffectiveSole逐值相同。全部加权Pelvis输出满足本帧正式共同硬区间，无越界。原525个固定Contact行保持，穿透34/90、持续Gap3/60不变。

全部37项规则、scorePolicy、eligible/matched/rate、Health/Evidence及occurrence与直接前驱相同；measurements不是全部相同，膝盖及物理微量舍入有变化。七维61.9分不变不能裁决全程骨盆和Knee。

目标硬夹255次不变，输出硬夹171→153、边界清朝外速度5→6、Handoff reset99不变。Pelvis自身PositionWeight从1039个1/4个0变为1043个1，是慢响应在四帧仍未回到原可见容差；Foot作者权重没有改变。

## 骨盆收益与代价必须分开

下表顺序为固定193957、直接前驱020243/3Hz、候选023618/2Hz。世界列在后两包来自Writer直接点；193957仅使用已有单位T条件下的历史重建，不补旧CSV字段。

| 指标 | 193957 | 020243 | 023618 |
| --- | ---: | ---: | ---: |
| 加权Correction单步超过50毫米 | 30 | 30 | 23 |
| Correction P90 | 24.267 mm | 24.774 mm | 17.722 mm |
| Correction最大 | 89.362 mm | 89.362 mm | 89.362 mm |
| 世界Y绝对步超过50毫米 | 45 | 33 | 30 |
| 世界Y绝对步P90 | 44.673 mm | 42.948 mm | 39.853 mm |
| 世界Y绝对步最大 | 80.210 mm | 80.210 mm | 80.210 mm |

Correction没有新增超过50毫米的帧，移出231/266/285/303/591/610/789。266下降50.122→9.585毫米、591下降60.101→24.047毫米、789下降57.856→46.018毫米。675继续下降11.053→5.012毫米、711下降2.097→1.016毫米。267仍下降70.808毫米，322/466仍89.362/79.742毫米，不能说关键窗口全部解决。

世界Y净少三帧，却有九个新增、十二个移出。新增229/230/284/301/302/320/483/500/994；其中前八项是上移变大，不全叫下陷。994下降48.813→66.476毫米、995已有下降59.507→64.375毫米。266世界运动从下降3.519变上升37.018毫米，说明Correction变小不等于最终位移变小。

全部1043帧平均加权偏移+6.890→-4.653毫米，最负值仍-172.658毫米。但低于-5毫米的采样帧479→544，按各帧dt累计7.983→9.067秒；负偏移时间积分0.34104→0.40848米秒，约增19.8%。初始负段左截断，不能把97→104帧叫完整起始时长。

只对相同的755帧资格进一步检查：目标不低于0、正式硬区间允许0、实际加权输出却低于-5毫米。这个辅助条件用现有EndpointTolerance，不改质量规则，也不把必要下压归为错误。命中208→270，累计时间3.467→4.500秒，最深-144.062→-157.199毫米；Accepted子集58→111，Releasing150→159。回正负偏移积分0.13509→0.19721米秒，约增加46%。471–477延至480、506–513延至515、794–801延至804。

因此本次不仅压低短促上抬，也延长了目标与硬边界都已允许回正时的下压。本轮不把这个代价解释成更正确的站姿。

## Knee外溢

这些是FBBIK记录的Solved Knee，不是新增的最终Physical Knee测量。按同侧2084对计算，extra为相对Original Knee的额外变化，actual step为Solved Knee本身的Component单步，两者不能互换。

| 指标 | 193957 | 020243 | 023618 |
| --- | ---: | ---: | ---: |
| extra超过50毫米 | 415 | 431 | 428 |
| extra超过100毫米 | 103 | 89 | 84 |
| actual step超过50毫米 | 713 | 686 | 700 |
| actual step超过100毫米 | 173 | 160 | 174 |
| actual step最大 | 541.183 mm | 626.929 mm | 593.333 mm |

2Hz的actual超过100毫米新增28、移出14，净增14。R826峰下降33.596毫米，但仍高于固定对照，该帧骨盆再低6.552毫米、目标腿长比6.304%→6.206%、BendWeight仍0，深压缩没有解除。R827314.168→353.181毫米、R934416.284→449.760毫米、R952258.945→308.836毫米。

R867大步移至868，R882提前至881，R994提前至993；不能把原帧峰消失当整段问题消失。R1044变小只证明采样窗口内结果，记录止于1045，不能承诺之后不换侧。已有landing-leg-extension只有2个eligible/Evidence4，不覆盖这些普通Swing窗口。

## 保存与恢复

12文件ZIP与独立Proof保存于Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/step-5-pelvis-frequency-2hz，逐条SHA256与原包一致；原始Record、193957、020243及所有失败包都未覆盖或清理。

- ZIP SHA256：60DA29C5C8A3EF4B69B0212C3A173F23CF3624D045716FE6363DE82781D32AC2。
- Proof SHA256：F28F3F054B085CA37805399BC8B9ACF0AEE7CAE907EF0508964E5241F09585E9。
- samples SHA256：DF9D67ED159FE2926BDE874FAE14A6544D465AAA3A569FF92674110FEEB36DA5。

8833b4cde3811bd43a5927ec0961a6511dd2373f只逆向撤销ae10348的参数和对应产品，保留新的拒绝记录。不修改用户既有proposal/project或仅行尾状态变化的FinalIkFullBodySolver文件。

恢复时三个Program/Projection及Corin Profile对9a36c4e内容无差异；再次以规定flags构建Runtime，27既有警告、0错误并立即shutdown。Unity重新Refresh、编译完成、Console无编译错误后执行同Record。恢复新包20260831-025450-166-8db33f64db784e89b406d332ce778ab8已封口，随后本任务退出自己启动的Play，回到Edit。

恢复包1043帧实际频率全3，2086×1221列中1197个非身份列与020243逐值相同，涵盖Foot/Pelvis/Knee/Reach/Spring/Body/Input/最终World。剩余24列采样/实例/Surface/Path身份双向映射无冲突；geometry50195行仅四身份列不同。37项规则、计数、完整score/occurrence/measurements相同，总分61.9不变，独立诊断任务与本任务核对一致。

025625官方Proof自动对前一个2Hz样本比较，故继续保留matched=false、七身份变化、Divergent0；另行与020417核对Runtime identity、1044完整frames及trace/start/input/body hash全相同，证明恢复关系而不篡改官方结论。原始Proof和12文件恢复ZIP另存在本实验archive目录的restored-3hz子目录。

- 恢复ZIP SHA256：FCFF8AC366DCFEFDC7DF22A34490F6F32497468281F6355B34D784440CEB9539。
- 恢复Proof SHA256：3EC5B05976651BAB0410648251F52051F5A5139705DC5F9FDF842B77C35F99AA。
- 恢复samples SHA256：669562DE894822D3FBCBDDBBC7CA65A6568A50AB44A867E793F11A66A44EBACC。

全量OpenSpec strict95/95通过，未新增测试或诊断规则。本轮完整执行了候选、自动Replay、独立质量检查、拒绝、精确撤销及恢复Replay；其进展是排除这个有实测代价的修法，不是骨盆质量已达标。193957继续固定效果对照，下一轮不得重新把2Hz或直接World历史搬运当作已通过起点。
