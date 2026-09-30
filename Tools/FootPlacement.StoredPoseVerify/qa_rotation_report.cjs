const fs=require('fs'),vm=require('vm'),assert=require('assert');
const path=process.argv[2]||'docs/diagnostics/foot-placement/ik-tests/live-rotation-response.html';
const html=fs.readFileSync(path,'utf8'),payload=html.match(/<script id="data" type="application\/json">([\s\S]*?)<\/script>/)[1],code=html.match(/<script>([\s\S]*?)<\/script>/)[1];
const elements={};for(const match of html.matchAll(/id="([^"]+)"/g))elements[match[1]]={value:0,textContent:'',innerHTML:'',listeners:{},addEventListener(name,fn){this.listeners[name]=fn;}};
elements.data.textContent=payload;let callback;
const context={document:{getElementById:id=>elements[id]},setTimeout:fn=>{callback=fn;return 1;},clearTimeout:()=>{callback=null;}};
vm.createContext(context);vm.runInContext(code,context);const data=JSON.parse(payload);let visited=0;
for(let c=0;c<data.cases.length;c++){
 elements.case.listeners.change({target:{value:c}});const rows=data.cases[c].current.current.rows;
 for(let i=0;i<rows.length;i++){
  elements.seek.listeners.input({target:{value:i}});assert(elements.frame.textContent.startsWith(String(rows[i].frame)));
  assert((elements.curves.innerHTML.match(/<polyline/g)||[]).length>=11);
  for(const id of ['flow','curves','bend','metrics','bend-values','checks'])assert(!/NaN|undefined/.test(elements[id].innerHTML));
  if(data.cases[c].status==='failed')assert(elements.checks.innerHTML.includes('失败'));
  visited++;
 }
 elements.play.listeners.click();callback();elements.play.listeners.click();
}
assert.equal(visited,data.cases.reduce((n,c)=>n+c.current.current.frames,0));
console.log(`Checked ${visited} frame renders, ${data.cases.length} cases, actual failure labels and playback; browser visual acceptance remains unverified`);
