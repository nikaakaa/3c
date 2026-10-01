from pathlib import Path
import json,math,hashlib,shutil
ROOT=Path(__file__).resolve().parents[2]
TEMP=ROOT/'3cDemo/Client/3C_Client/Temp/FootReleasingBoundary'
OUT=ROOT/'docs/diagnostics/foot-placement/ik-tests'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def mag(v):return math.sqrt(sum(x*x for x in v))
result={'status':'execution-failed','baselineCommit':'fbed4b192','firstFrame':2024,'lastFrame':2056,'seedFrame':2023,
    'scope':'直接来源修正版基线与仅两采样文件候选；完整Native→预测/查询→双脚Lifecycle/骨盆/Goal；未重跑FBBIK，历史0.240472mm校准失败保留',
    'variants':{},'qualityReasons':[]}
for variant in ['baseline','candidate']:
    folder=TEMP/variant
    if not (folder/'native.json').exists() or not (folder/'goal.json').exists():
        result['qualityReasons'].append(variant+'未取得完整Native/Goal结果')
        result['variants'][variant]={'command':read(folder/'command.json')}
        continue
    native=read(folder/'native.json');goal=read(folder/'goal.json')
    for kind in ['native','goal']:shutil.copyfile(folder/(kind+'.json'),OUT/('releasing-boundary-'+variant+'-'+kind+'.json'))
    if native['status']!='passed' or goal['status']!='passed':result['qualityReasons'].append(variant+'函数执行或断言失败')
    rows=goal['current']['rows'];metrics={}
    if len(rows)!=33:result['qualityReasons'].append(variant+'未保存全33帧')
    for side in ['left','right']:
        reach='fixedHipReachRatio' if side=='right' else 'pairedReachRatio';penetration='finalPenetration' if side=='right' else 'pairedFinalPenetration'
        position='positionWeight' if side=='right' else 'pairedPositionWeight';observed='finalSupportAvailable' if side=='right' else 'pairedFinalSupportAvailable'
        deltas=[mag(r[side+'CorrectionDelta']) for r in rows[1:]]
        metrics[side]={'overreachFrames':sum(r[reach]>1 for r in rows),'overreachDuration':sum(r['dt'] for r in rows if r[reach]>1),
            'maximumReach':max(r[reach] for r in rows),'maximumCorrectionDelta':max(deltas),'totalCorrectionVariation':sum(deltas),
            'maximumPositivePenetration':max([max(0,r[penetration]) for r in rows if r[penetration] is not None]),
            'observedFrames':sum(r[observed] for r in rows),'activeGoalFrames':sum(r[position]>0 for r in rows)}
        metrics[side]['maximumObservedSoleClearance']=max([-r[penetration] for r in rows if r[penetration] is not None])
    anchors=[r for r in rows if r['state']=='Locked' and r['hasContactAnchor']]
    metrics['right']['lockedAnchorObservedFrames']=len(anchors)
    metrics['right']['maximumLockedAnchorDrift']=max([math.hypot(r['sole'][0]-r['contactAnchor'][0],r['sole'][2]-r['contactAnchor'][2]) for r in anchors]) if anchors else None
    result['variants'][variant]={'nativeStatus':native['status'],'footStatus':goal['status'],'nativeAllocatedBytes':native['allocatedBytesInWarmedSequence'],
        'footAllocatedBytes':goal['current']['allocatedBytes'],'metrics':metrics,'rows':rows,'nativeRows':native['rows']}
if not result['qualityReasons']:
    b=result['variants']['baseline'];c=result['variants']['candidate']
    for side in ['left','right']:
        old=b['metrics'][side];new=c['metrics'][side]
        for key,tolerance in [('maximumReach',0),('maximumCorrectionDelta',0.0002),('maximumPositivePenetration',0.0002)]:
            if new[key]>old[key]+tolerance:result['qualityReasons'].append(side+' '+key+'劣化')
        if new['overreachFrames']>old['overreachFrames']:result['qualityReasons'].append(side+'超长帧增多')
        if new['observedFrames']<new['activeGoalFrames']:result['qualityReasons'].append(side+'有效Goal缺少实际净空观测')
    if c['nativeAllocatedBytes']!=0 or c['footAllocatedBytes']!=0:result['qualityReasons'].append('暖机分配不是0B')
    if any(x['authorWeight']!=y['authorWeight'] for x,y in zip(b['rows'],c['rows'])):result['qualityReasons'].append('作者预算变化')
    result['authorBudgetsIdentical']=all(x['authorWeight']==y['authorWeight'] for x,y in zip(b['rows'],c['rows']))
    result['goalWeightsIdentical']=all(x['positionWeight']==y['positionWeight'] and x['pairedPositionWeight']==y['pairedPositionWeight'] for x,y in zip(b['rows'],c['rows']))
    result['nativeAnklesIdentical']=all(x['currentAnkle']==y['currentAnkle'] for x,y in zip(b['nativeRows'],c['nativeRows']))
    result['continuousChannelsIdentical']=all(x[name][field]==y[name][field] for x,y in zip(b['nativeRows'],c['nativeRows']) for name in ['currentLeftSample','currentRightSample'] for field in ['footHeight','toeHeight','toeSpeed','positionError','rotationError','contact','lockWeight','support'])
    result['earlyLandingRegressionFrames']=[{'frame':x['frame'],'baseline':x['fixedHipReachRatio'],'candidate':y['fixedHipReachRatio'],
        'baselineTargetDistance':math.dist(x['fixedHip'],x['ankle']),'candidateTargetDistance':math.dist(y['fixedHip'],y['ankle']),
        'targetDistanceIncrease':math.dist(y['fixedHip'],y['ankle'])-math.dist(x['fixedHip'],x['ankle'])} for x,y in zip(b['rows'],c['rows']) if y['state']=='Landing' and y['fixedHipReachRatio']>x['fixedHipReachRatio']+0.000001]
    for frame in result['earlyLandingRegressionFrames']:
        if frame['baseline']>1 and frame['targetDistanceIncrease']>0.0002:result['qualityReasons'].append(str(frame['frame'])+' Landing已有超长进一步恶化')
    result['nativePreRollFrames']={v:read(TEMP/v/'native.json').get('nativePreRoll',{}).get('frame') for v in ['baseline','candidate']}
    result['status']='rejected-quality-regression' if result['qualityReasons'] else 'completed-goal-comparison'
    result['acceptance']='只有完整Goal质量改善才支持保留；completed只表示比较完成，FBBIK未校准且既有超伸可能残留'
result['productionSnapshotSha256']=hashlib.sha256((OUT/'releasing-boundary-production-snapshot.zip').read_bytes()).hexdigest()
result['sealedZipSha256']={name:hashlib.sha256((OUT/name).read_bytes()).hexdigest() for name in ['releasing-boundary-production-snapshot.zip','releasing-boundary-build-inputs.zip','releasing-boundary-executed-assemblies.zip']}
result['jobs']='两版原Unity作业均已结束；未持有DisallowAutoRefresh；build-server已关闭；只维护报告，不再调用Unity。'
(OUT/'releasing-boundary-result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(result['status'],result['qualityReasons'])
