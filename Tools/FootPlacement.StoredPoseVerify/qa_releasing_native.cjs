const fs=require('fs'),vm=require('vm'),assert=require('assert');
const html=fs.readFileSync('docs/diagnostics/foot-placement/ik-tests/releasing-action-native.html','utf8');
const payload=html.match(/<script id="data" type="application\/json">([\s\S]*?)<\/script>/)[1],code=html.match(/<script>([\s\S]*?)<\/script>/)[1];
const elements={};for(const match of html.matchAll(/id="([^"]+)"/g))elements[match[1]]={value:0,textContent:'',innerHTML:'',listeners:{},addEventListener(name,fn){this.listeners[name]=fn;}};
elements.data.textContent=payload;let callback;
const context={document:{getElementById:id=>elements[id]},setTimeout:fn=>{callback=fn;return 1;},clearTimeout:()=>{callback=null;}};
vm.createContext(context);vm.runInContext(code,context);
for(let i=0;i<12;i++){
 elements.seek.listeners.input({target:{value:i}});assert(elements.frame.textContent.startsWith(String(2035+i)));
 for(const key of ['scene','curves','metrics','moment'])assert(!/NaN|undefined/.test(elements[key].innerHTML+elements[key].textContent));
 assert.equal((elements.scene.innerHTML.match(/<circle/g)||[]).length,3);
}
elements.seek.listeners.input({target:{value:6}});assert(elements.metrics.innerHTML.includes('0.0233'));
assert(elements.metrics.innerHTML.includes('122473249c778fb49aa583ed0d45bc65'));
assert(elements.verdict.textContent.includes('仍未验证'));
elements.next.listeners.click();assert.equal(Number(elements.seek.value),7);
elements.back.listeners.click();assert.equal(Number(elements.seek.value),6);
elements.play.listeners.click();callback();assert.equal(Number(elements.seek.value),7);elements.play.listeners.click();
console.log('Checked 12 actual frames, real source handover, scene coordinates and synchronized controls; no browser visual acceptance claimed');
