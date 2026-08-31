# 接触位置仅由完整世界残差接管实验

## 最终处置补记

后续193957采样及用户反馈已将eb5fb05／5d858bc选为Landing／Swing的固定比较版本，不是本文件末尾18:20时点的“仍待加载”。20260830-193957-489-9f55422ed479473682fc01f0583625db为facts57／diagnosis26，1043帧／2086脚行，完整12文件ZIP在Diagnostics/FootPlacementReplayArchives/20260830-contact-world-residual，保存提交7f7b66d。

持久归档README及retention-manifest记录：持续Gap12/60降至3/60、Landing未闭合11/60降至2/60、普通Swing最大额外步98.285降至36.401毫米。没有找到绑定193957的官方Replay Proof，不补造matched；历史可比性按已核对的原始Body／动画／Foot输入及表现时钟限定。骨盆硬夹紧、膝盖翻侧及部分端点平面负距仍存在，不把该参照叫全身合格。

之后212054提前卸载候选被拒绝，3436cf6／27dbef4撤销，221050的1119个非身份CSV列对193957逐值相同，37项规则／计数／score恢复。脚目标有效性及骨盆三步属于后续独立change，索引见[骨盆实验历史](../../refine-character-pelvis-response/experiments/README.md)。本段只补齐最终处置，不重写193957原数据或把后续改进记到本实验。

## 用户范围与对照

用户要求先处理贴出的历史回溯：Contact目标已在地面，但世界残差保留旧高位，随后相对动画scalar又把输出抬高。只修改这条位置历史链，不改Swing目标、脚掌倾斜、Rotation Weight、膝盖、查询、Anchor、作者曲线或数值配置。

历史效果参照为`23578bb61d823939368df0940d7efb75b03f0bd6`（2026-08-29 03:53:49 +0800）之后的`20260829-035426-702-d8d0a4fd8b48434db82470bfd5308625`采样；提交时间接近不是独立的产物身份证明，仍须核对Program/Projection及共同原始输入。155326恢复包只作近期同输入控制，不称质量合格；173423为未通过的Sliding实验前驱，不把它的收益当作已验收基础。

## 单变量

VerifiedSupport的VerifiedAnchor、LockedFullAnchor和LockedSliding统一采用`ContactWorldResidual`域。位置只由已有PlantWorldResidual保存完整世界误差：正式换代捕获`R=O_previous-SelectedWorldTarget`，本帧按原HalfLife推进一次并按原完整向量完成容差清零，输出`O=SelectedWorldTarget+R_after`。不再把这个D串联到动画相对scalar，不再保留173423的另一份SlidingWorldError。

保留原Target Height政策、HalfLife、Capture reason、Completion、Direction角历史与最终Goal权重。Contact不执行scalar，scalar事实为分域未执行；输出与Desired相同不是伪造scalar已到位。Swing及Release仍使用AnimationRelativeScalar。Contact正常退出Release时，原Release入口先以完整上一O捕获并推进，再令scalar同步本帧q，退出帧不加第二个响应步长。硬失权、Source/Profile/World失效仍按原Reset处理。

## 取舍与失败边界

这一小步直接去除1031样例中D到O的额外约93毫米抬高，不承诺所有踏空立刻消失。478–480若只有Plant残差，原尾差仍可能保留。959的旧scalar曾向下抵消Plant残差，取消它可能使离面增加；必须真实记录，不把所有旧scalar贡献都称为坏误差。保留完整世界捕获，不扣FormalFootHeight、不使用当前动画基准重建旧世界输出、不清Y、不硬贴地、不恢复WeightedGoal转移实验。

173423已测得Sliding中心回到平面时倾斜脚尖产生新的负距离；用户将旋转几何列为低优先级，本轮保持旋转不动，但穿透与Release大步仍原样计入结果，不改评分掩盖。

## 规格对账

本切片替换active spec的Sliding双世界历史要求。其它scalar速度/位置轴公式只适用于AnimationRelativeScalar，Contact域位置Owner为PlantWorldResidual；Direction仍同帧执行。current spec要求唯一Interpolation/Resolved/Goal/Writer，没有要求Contact必须串联两份位置历史，本切片不增加Owner。旧失败位置basis/WeightedGoal要求仍属未实施任务，不能因本切片误称恢复。

## 证据要求

使用原Record `43357ff3cd384e5cba75d2c31175b116`，核对完整输入及表现时钟，再比较1031、476–484、959及全部Contact间隙、穿透、真实位移、Release、Pelvis、Reach和原37项规则。历史旧schema只比较语义一致的原始值，不补列、不伪造新facts、不把不同规则总分作改善证据。

Runtime与唯一公开DTO已经完成：Contact位置不再消费scalar，原Plant Capture/Decay不变，Support Direction保持原10度历史，退出Release按实际DomainTransferred同步q。删除Sliding误差状态、12标量出口及Tangential枚举；只保留三个分域事实。Runtime规定flags构建27个既有依赖/项目警告、0错误，build-server shutdown完成；Editor迁移和Replay尚未完成，不声明效果。

03:54包的2086行按Frame/Side与155326一一对应，OriginalSole XYZ逐值完全相同；1031右脚原状态UnlockedSupport，物理Heel/Toe中心距1.44平面32.547毫米。旧samples SHA256为`A5EB967B9FEDEB5953DEDB2CB98814326A275CFDB1E57C9BD137E2617D655A8E`。旧包ProjectionRevision=`808cf24fefe613e5e9ef8944cb3e671720efa52b7d64c0b8bef0331cb72489df`，已用`git grep`核对`23578bb`内`CorinCharacterPipelineDefinition.PresentationProjection.asset:27`完全相同，不只依据采样时间。

历史可比性边界：03:54与155326共同Body/时钟字段相同，但Foot Motion已跨迁移；Step Time有1182行变化，Step Distance432行，Contact Event availability/ordinal/位置与Phase各206行变化，Foot Height129行变化且最大0.313毫米。不能声称历史版所有正式Foot输入与当前完全相同，也不能把旧696列/旧规则评分与当前规则直接比成单变量收益。当前候选仍须与155326同一正式输入控制核对。

## 当时的交付等待状态（18:20，后续结果见开头）

Runtime候选`eb5fb05`，Diagnostics独立提交`5d858bc`，唯一版本facts57/Analyzer57/diagnosis26。Editor规定flags构建57个既有警告、0错误，构建后shutdown完成；change与全量strict为94/94。删除12个SlidingResponse列，只保留3个Domain事实，旧56废弃列与旧52缺列均按typed拒绝，不覆写旧证据。原37项质量与七维评分不变。

2026-08-30 18:20前后的正式工具状态仍为Unity Play、Trace Idle、无采样/Finalizing。此Play不属于本轮启动的回放，主任务没有停止、Refresh或加载新代码，已请求用户回到Edit。新Replay尚未运行，173423只是旧前驱，不能引用它宣称本次候选效果。下一步仅在Edit加载完整候选并用既有精确Record采样，不新增角色、测试或查询入口。
