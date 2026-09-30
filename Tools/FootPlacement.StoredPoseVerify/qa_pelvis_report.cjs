const fs=require('fs'),vm=require('vm'),assert=require('assert');
const html=fs.readFileSync('docs/diagnostics/foot-placement/ik-tests/live-rotation-response.html','utf8');
const payload=html.match(/<script id="data" type="application\/json">([\s\S]*?)<\/script>/)[1],code=html.match(/<script>([\s\S]*?)<\/script>/)[1];
const elements={};for(const match of html.matchAll(/id="([^"]+)"/g))elements[match[1]]={value:0,textContent:'',innerHTML:'',listeners:{},addEventListener(name,fn){this.listeners[name]=fn;}};
elements.data.textContent=payload;let callback;
const context={document:{getElementById:id=>elements[id]},setTimeout:fn=>{callback=fn;return 1;},clearTimeout:()=>{callback=null;}};
vm.createContext(context);vm.runInContext(code,context);let visited=0;
for(const side of ['right','left']){
 elements.side.listeners.change({target:{value:side}});
 for(let i=0;i<58;i++){
  elements.seek.listeners.input({target:{value:i}});assert(elements.frame.textContent.startsWith(String(2192+i)));
  assert((elements.curves.innerHTML.match(/<polyline/g)||[]).length>=23);
  for(const id of ['curves','metrics','moment','exit','checks'])assert(!/NaN|undefined/.test(elements[id].innerHTML));
  assert(elements.checks.innerHTML.includes('失败'));visited++;
 }
 elements.seek.listeners.input({target:{value:20}});
 elements.next.listeners.click();assert.equal(Number(elements.seek.value),21);
 elements.back.listeners.click();assert.equal(Number(elements.seek.value),20);
 elements.play.listeners.click();callback();assert.equal(Number(elements.seek.value),21);elements.play.listeners.click();
}
assert.equal(visited,116);console.log('Checked both feet across 58 actual frames, three real versions, failure labels, null gaps and playback; no browser visual acceptance claimed');
