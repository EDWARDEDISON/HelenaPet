export const MOODS = ['calm','listen','happy','shy','echo','sleep','surprise','think','angry','sad','curious','expect','smug','confused','drowsy','peace'];
export const MOOD_NAMES = ['安静陪伴','侧耳倾听','开心','害羞','回声感知','小憩','惊讶','思考','生气','委屈','好奇','期待','得意','困惑','困困','安心'];
export const DEFAULTS = Object.freeze({skin:'original-no-bangs',decoration:'cat',pendant:'shell',scale:0.72,decorationSize:1,pendantSize:1,textSize:18,gaze:'follow',topmost:true,auto:true,dailyHop:true,typewriter:true});
export const clamp = (v,a,b) => Math.max(a,Math.min(b,v));
export const smooth = v => { v=clamp(v,0,1); return v*v*(3-2*v); };
export const chargeCount = seconds => seconds < .45 ? 0 : seconds < .9 ? 1 : seconds < 1.6 ? 2 : 3;
export const hopHeight = (age,count) => age<0 || age>=count*.8-1e-9 ? 0 : Math.sin((age% .8)/.8*Math.PI)*70*Math.pow(.82,Math.floor(age/.8));
export function cleanPreferences(input={},catalog={}) {
  const p={...DEFAULTS};
  for(const [k,min,max] of [['scale',.42,1.25],['decorationSize',.7,2],['pendantSize',.7,2.2],['textSize',16,28]])
    if(Number.isFinite(input[k]))p[k]=clamp(input[k],min,max);
  for(const k of ['topmost','auto','dailyHop','typewriter'])if(typeof input[k]==='boolean')p[k]=input[k];
  if(catalog.skins?.includes(input.skin))p.skin=input.skin;
  if(['none','cat','deer','bunny','crown','bow','star'].includes(input.decoration))p.decoration=input.decoration;
  if(['none',...(catalog.pendants||[])].includes(input.pendant))p.pendant=input.pendant;
  if(['follow','center','up','down','left','right','up-left','up-right','down-left','down-right'].includes(input.gaze))p.gaze=input.gaze;
  if(Number.isFinite(input.x)&&Number.isFinite(input.y)){p.x=Math.round(input.x);p.y=Math.round(input.y);}
  return p;
}
export function project(x,y,depth,part,rx,ry) {
  const u=(x-200)/195,curve=Math.max(0,1-u*u),dx=x-200,dy=y-260;
  return [200+dx*(1-.085*Math.abs(rx))+rx*(depth*18+curve*17),260+dy*(1-.045*Math.abs(ry)+(part==='hat'?ry*.015:0))+ry*(depth*15+curve*10)+rx*dx*.022];
}
export function tangent(x,y,depth,part,rx,ry) {
  const q=project(x,y,depth,part,rx,ry),a=project(x+1,y,depth,part,rx,ry),b=project(x,y+1,depth,part,rx,ry);
  const xx=a[0]-q[0],xy=a[1]-q[1],yx=b[0]-q[0],yy=b[1]-q[1];
  return [xx,xy,yx,yy,q[0]-xx*x-yx*y,q[1]-xy*x-yy*y];
}
export const PALETTES={gilda:['#315C47','#A6D4AD'],architect:['#4A9EA9','#DFF9FA'],clerk:['#884752','#F2DFC0'],'dark-cake':['#6558A5','#C6BAEC'],'gold-cake':['#B67D2D','#F3D68D'],haixi:['#A44654','#E7B99A'],maria:['#527EC4','#E6CE8C'],'redeem-star':['#66ADBE','#E8D4AE'],'sea-witch':['#287D87','#9ADDD4'],seaside:['#62A99D','#D8F0E8'],'sleeping-girl':['#C5789C','#D8CCF2'],'sound-wave':['#BA7545','#D7B184'],'star-girl':['#416AAC','#C5D9FA'],'sweet-cake':['#C86E9C','#B7DDA6']};
