const fs=require('fs'),vm=require('vm'),assert=require('assert');
const html=fs.readFileSync('docs/diagnostics/foot-placement/ik-tests/releasing-action-native.html','utf8');
const payload=html.match(/<script id="data" type="application\/json">([\s\S]*?)<\/script>/)[1],code=html.match(/<script>([\s\S]*?)<\/script>/)[1];
const elements={};for(const match of html.matchAll(/id="([^"]+)"/g))elements[match[1]]={value:0,textContent:'',innerHTML:'',listeners:{},addEventListener(name,fn){this.listeners[name]=fn;}};
elements.data.textContent=payload;let callback;
const context={document:{getElementById:id=>elements[id]},setTimeout:fn=>{callback=fn;return 1;},clearTimeout:()=>{callback=null;}};
vm.createContext(context);vm.runInContext(code,context);
for(let i=0;i<33;i++){
 elements.seek.listeners.input({target:{value:i}});assert(elements.frame.textContent.startsWith(String(2024+i)));
 for(const key of ['scene','curves','metrics','moment','legs','businessCurves','businessMetrics','heightCurves','heightMetrics'])assert(!/NaN|undefined/.test(elements[key].innerHTML+elements[key].textContent));
 assert.equal((elements.scene.innerHTML.match(/<circle/g)||[]).length,3);
 assert.equal((elements.legs.innerHTML.match(/<circle/g)||[]).length,18);
}
elements.seek.listeners.input({target:{value:17}});assert(elements.metrics.innerHTML.includes('0.0233'));
assert(elements.metrics.innerHTML.includes('122473249c778fb49aa583ed0d45bc65'));
assert(elements.verdict.textContent.includes('历史校准失败'));
assert(elements.businessMetrics.innerHTML.includes('NewEventContactAcquired'));
assert(elements.businessMetrics.innerHTML.includes('Accepted'));
assert(elements.heightVerdict.textContent.includes('业务劣化'));
elements.next.listeners.click();assert.equal(Number(elements.seek.value),18);
elements.back.listeners.click();assert.equal(Number(elements.seek.value),17);
elements.play.listeners.click();callback();assert.equal(Number(elements.seek.value),18);elements.play.listeners.click();
console.log('Checked 33 actual frames, source handover, bilateral solved legs, per-frame path and transition snapshots, failure label and synchronized controls; no browser visual acceptance claimed');
