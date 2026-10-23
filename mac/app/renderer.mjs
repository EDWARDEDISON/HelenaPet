import {Drawing} from './drawing.mjs';
import {MOODS,MOOD_NAMES,clamp,chargeCount,hopHeight} from './core.mjs';
const api=window.helena,$=id=>document.getElementById(id),isPanel=new URLSearchParams(location.search).has('panel');
let boot,drawing,prefs,state,cursor={x:-1000,y:-1000},drag=null,typing=null,skinPromise=Promise.resolve(),frameId=0;
const canvas=$('canvas'),ctx=canvas.getContext('2d',{willReadFrequently:true});
const time=()=>performance.now()/1000;
let sleepAt=time()+180+Math.random()*120,wakeAt=Infinity,nextHop=time()+45+Math.random()*45,moodUntil=Infinity,bubbleUntil=0;
function engage(){sleepAt=time()+180+Math.random()*120;nextHop=time()+45+Math.random()*45;if(state?.mood==='sleep'){state.mood='calm';wakeAt=Infinity;}}
function mood(id,persistent=false){if(!MOODS.includes(id))return;state.mood=id;moodUntil=persistent||['sleep','calm'].includes(id)?Infinity:time()+6;if(id==='sleep'){state.action=null;wakeAt=time()+1200+Math.random()*600;}}
function say(event){engage();const lines=boot.dialogue.filter(l=>l.event===event);const pool=lines.length?lines:boot.dialogue.filter(l=>l.event==='click');const line=pool[Math.floor(Math.random()*pool.length)]||{text:'听见你啦，我会在这里陪着你。',mood:'calm',kind:'fan'};mood(line.mood);showBubble(line.text,line.kind==='official'?'原作台词 · '+(line.source||'原作资料'):'桌宠互动 · 预置台词');}
function showBubble(text,kind='桌宠互动'){clearInterval(typing);$('bubble').hidden=false;$('bubble-kind').textContent=kind;$('bubble-text').textContent='';bubbleUntil=time()+Math.max(7,text.length*.11+3);let i=0;
  if(prefs.typewriter){typing=setInterval(()=>{$('bubble-text').textContent=text.slice(0,++i);if(i>=text.length)clearInterval(typing);},35);}else $('bubble-text').textContent=text;
}
function action(value){engage();const [kind,foodValue]=String(value||'').split(':');if(kind==='nudge'){if(state.action?.kind==='read'){state.action.page++;state.action.pageAt=time();state.action.end=Math.max(state.action.end,time()+8);}else if(state.action?.kind==='eat'){state.action.cycleAt=time();state.action.end=Math.max(state.action.end,time()+6);}else if(state.action?.kind==='cane'){state.hop={start:time(),count:1};}return;}
  if(!['read','eat','cane'].includes(kind))return;const now=time();state.action={kind,food:clamp(Number(foodValue)||0,0,3),start:now,end:now+(kind==='read'?20:kind==='eat'?12:10),page:0,pageAt:now,cycleAt:now+.8};state.hop=null;say(kind==='read'?'action-read':kind==='cane'?'action-cane':['feed-sugar','feed-cookie','feed-cake','feed-soup'][state.action.food]);}
function command({name,value}){if(!state)return;const wasSleeping=state.mood==='sleep';engage();if(name==='mood')mood(value,true);else if(name==='talk')say(value);else if(name==='action')action(value);else if(name==='stop'){state.action=null;mood('calm');}else if(name==='hop'){state.action=null;state.hop={start:time(),count:3};}else if(name==='toggle-sleep')mood(wasSleeping?'calm':'sleep',true);else if(name==='mango'){state.mangoUntil=time()+6;}else if(name==='clear-history'){$('bubble').hidden=true;}}
function update(p){const prev=prefs;prefs=p;state.prefs=p;$('bubble-text').style.fontSize=p.textSize+'px';if(prev?.skin!==p.skin)skinPromise=drawing.setSkin(p.skin).catch(fail);}
function fail(e){console.error(e);showBubble('素材载入失败。请从完整 Mac 安装包重新解压后打开。','启动提示');}
function tick(){const now=time();if(state.action&&now>=state.action.end)state.action=null;if(state.action?.kind==='read'&&now-state.action.pageAt>5.8){state.action.page++;state.action.pageAt=now;}
  if(state.hop&&now-state.hop.start>state.hop.count*.8+1.2)state.hop=null;
  if(now>=moodUntil){mood('calm');moodUntil=Infinity;}
  if(prefs.auto){if(state.mood==='sleep'&&now>wakeAt){mood('calm');engage();say('wake');}else if(state.mood!=='sleep'&&!state.action&&!drag&&now>sleepAt)mood('sleep',true);}
  if(prefs.dailyHop&&state.mood==='calm'&&!state.action&&!drag&&!state.hop&&now>nextHop){state.hop={start:now,count:1};nextHop=now+45+Math.random()*45;}
  if(drag?.button===0&&!drag.moving&&!drag.handled&&now-drag.start>.55){drag.handled=true;say('pet');}
  if(drag?.button===2){const age=now-drag.start,count=chargeCount(age);$('charge').hidden=age<.35;$('charge').textContent=count?`蓄力 ${count} / 3 · 松开跳跃`:'按住一会儿，积攒回声';}
  let x=0,y=0;if(state.mood!=='sleep'){if(prefs.gaze==='follow'){x=clamp((cursor.x-220)/220,-1,1);y=clamp((cursor.y-470)/190,-1,1);}else{x=prefs.gaze.includes('left')?-1:prefs.gaze.includes('right')?1:0;y=prefs.gaze.includes('up')?-1:prefs.gaze.includes('down')?1:0;}}
  state.rx+=(x-state.rx)*.12;state.ry+=(y-state.ry)*.12;state.ex+=(x-state.ex)*.3;state.ey+=(y-state.ey)*.3;state.hx+=(state.rx-state.hx)*.08;state.hy+=(state.ry-state.hy)*.08;
  ctx.setTransform(2,0,0,2,0,0);drawing.draw(ctx,state,now);
  if(now>bubbleUntil){$('bubble').hidden=true;clearInterval(typing);}
  if(!drag){const inBubble=!$('bubble').hidden&&cursor.y<175&&cursor.x>15&&cursor.x<425;let opaque=false;if(cursor.x>=0&&cursor.x<440&&cursor.y>=0&&cursor.y<650)opaque=ctx.getImageData(Math.floor(cursor.x*2),Math.floor(cursor.y*2),1,1).data[3]>20;api.hit(inBubble||opaque);}
  frameId=requestAnimationFrame(tick);
}
function pointerDown(e){if(!drawing.skin)return;e.preventDefault();engage();canvas.setPointerCapture(e.pointerId);api.pointer(true);drag={button:e.button===2||e.ctrlKey?2:0,start:time(),sx:e.screenX,sy:e.screenY,x:e.offsetX/prefs.scale,y:e.offsetY/prefs.scale,moving:false,handled:false};}
function pointerMove(e){if(!drag||drag.button!==0)return;if(Math.hypot(e.screenX-drag.sx,e.screenY-drag.sy)>5&&!drag.moving){drag.moving=true;api.drag(true);}}
function pointerUp(e){if(!drag)return;e.preventDefault();const d=drag;drag=null;api.pointer(false);$('charge').hidden=true;if(d.moving){api.drag(false);say('drag');return;}if(d.button===2){const count=chargeCount(time()-d.start);if(count){state.action=null;state.hop={start:time(),count};}else api.command('panel');return;}
  if(d.handled)return;if(state.action&&d.y>505&&d.x>75){action('nudge');return;}const x=d.x,y=d.y-130;if(y<300)say('touch-hat');else if(y<422)say('touch-glasses');else if(x<90||x>350)say('touch-hair');else say('touch-cheek');
}
function btn(parent,text,fn){const b=document.createElement('button');b.textContent=text;b.addEventListener('click',fn);parent.append(b);return b;}
function select(id,entries){const el=$(id);for(const [value,label]of entries){const o=document.createElement('option');o.value=value;o.textContent=label;el.append(o);}el.addEventListener('change',()=>api.update({[id]:el.value}));}
function syncPanel(p){prefs=p;for(const key of ['skin','decoration','pendant','gaze','scale','decorationSize','pendantSize','textSize'])$(key).value=p[key];for(const key of ['auto','topmost','dailyHop','typewriter'])$(key).checked=p[key];}
function setupPanel(){document.body.classList.add('panel-mode');$('panel').hidden=false;
  for(const [text,event]of [['打个招呼','greet'],['聊聊诗歌','poetry'],['聊聊甜食','sweet'],['陪我一会儿','company'],['原作台词','official']])btn($('talk-buttons'),text,()=>api.command('talk',event));
  MOODS.forEach((id,i)=>btn($('moods'),MOOD_NAMES[i],()=>{for(const b of $('moods').children)b.classList.remove('selected');$('moods').children[i].classList.add('selected');api.command('mood',id);}));
  for(const [text,kind]of [['一起读书','read'],['吃点甜品','eat'],['探杖听回声','cane']])btn($('actions'),text,()=>api.command('action',kind==='eat'?`eat:${$('food').value}`:kind));
  select('skin',Object.entries(boot.skins).map(([id,s])=>[id,s.name]));
  select('decoration',[['none','不戴装饰'],['cat','小猫耳朵'],['deer','小鹿角'],['bunny','兔耳朵'],['crown','小皇冠'],['bow','蝴蝶结'],['star','星星夹']]);
  select('pendant',[['none','不戴挂件'],...boot.pendants.map(p=>[p.id,p.name])]);
  select('gaze',[['follow','跟随鼠标'],['center','正前方'],['up','向上'],['down','向下'],['left','向左'],['right','向右'],['up-left','左上'],['up-right','右上'],['down-left','左下'],['down-right','右下']]);
  for(const key of ['scale','decorationSize','pendantSize','textSize'])$(key).addEventListener('input',()=>api.update({[key]:Number($(key).value)}));
  for(const key of ['auto','topmost','dailyHop','typewriter'])$(key).addEventListener('change',()=>api.update({[key]:$(key).checked}));
  for(const [id,name,value]of [['page','action','nudge'],['stop','stop'],['hop','hop'],['recover','recover'],['quit','quit']])$(id).addEventListener('click',()=>api.command(name,value));
  api.onState(syncPanel);syncPanel(prefs);
}
async function initialize(){boot=await api.bootstrap();prefs=boot.settings;if(isPanel){setupPanel();return;}
  state={prefs,mood:'calm',rx:0,ry:0,ex:0,ey:0,hx:0,hy:0,action:null,hop:null,mangoUntil:0};drawing=new Drawing(boot);await drawing.init();await drawing.setSkin(prefs.skin);update(prefs);
  api.onState(update);api.onCursor(p=>cursor=p);api.onCommand(command);
  canvas.addEventListener('pointerdown',pointerDown);canvas.addEventListener('pointermove',pointerMove);canvas.addEventListener('pointerup',pointerUp);canvas.addEventListener('pointercancel',()=>{drag=null;api.pointer(false);api.drag(false);$('charge').hidden=true;});canvas.addEventListener('contextmenu',e=>e.preventDefault());
  let wheelAt=0;canvas.addEventListener('wheel',e=>{e.preventDefault();if(time()-wheelAt<.12)return;wheelAt=time();api.update({scale:clamp(prefs.scale+(e.deltaY<0?.05:-.05),.42,1.25)});},{passive:false});
  $('bubble').addEventListener('click',()=>say('company'));tick();
}
const ready=initialize().catch(e=>{console.error(e);$('bubble').hidden=false;$('bubble-kind').textContent='启动失败';$('bubble-text').textContent=String(e.message);throw e;});
window.runHelenaSmokeTest=async()=>{await ready;if(!boot.testMode)throw new Error('Test mode required');cancelAnimationFrame(frameId);const report={ok:true,skins:0,renderCases:0,actions:[],chargeCounts:[chargeCount(.44),chargeCount(.45),chargeCount(.9),chargeCount(1.6)]};const original={...prefs};
  for(const id of Object.keys(boot.skins)){await drawing.setSkin(id);report.skins++;for(const m of MOODS){state.mood=m;state.rx=-.6;state.ry=.4;state.ex=.8;state.ey=-.5;ctx.setTransform(2,0,0,2,0,0);drawing.draw(ctx,state,2.2);const data=ctx.getImageData(0,0,880,1300).data;let visible=0;for(let i=3;i<data.length;i+=16)if(data[i]>30)visible++;if(visible<3000)throw new Error('Invisible skin/mood '+id+'/'+m);report.renderCases++;}
    for(const kind of ['read','eat','cane']){const start=2;state.action={kind,food:2,start,end:22,page:1,pageAt:3,cycleAt:2.8};drawing.draw(ctx,state,3.4);report.renderCases++;}state.action=null;
  }
  for(const p of boot.pendants){state.prefs={...original,pendant:p.id};drawing.draw(ctx,state,3.2);report.renderCases++;}for(const p of boot.decorations){state.prefs={...original,decoration:p.id};drawing.draw(ctx,state,3.2);report.renderCases++;}
  state.prefs=original;await drawing.setSkin('original-no-bangs');state.action=null;state.mood='happy';state.rx=state.ry=state.ex=state.ey=0;drawing.draw(ctx,state,3.2);showBubble('听见你啦，我会在这里陪着你。','Mac 测试版 · 渲染自检');report.actions=['read','eat','cane','pendants','decorations'];report.preview=canvas.toDataURL();report.previewSkin=drawing.skin.id;return report;
};
window.runHelenaPanelTest=async()=>{await ready;if(!boot.testMode)throw new Error('Test mode required');return {ok:$('skin').options.length===16&&$('moods').children.length===16,skinOptions:$('skin').options.length,moodButtons:$('moods').children.length,pendants:$('pendant').options.length};};
