import {clamp,smooth,tangent,PALETTES,hopHeight} from './core.mjs';
const P=s=>new Path2D(s);
const mouths={shy:'M-8,1 Q0,8 8,1',sleep:'M-6,3 Q0,0 6,3',angry:'M-10,2 Q0,-4 10,2',sad:'M-9,4 Q0,-4 9,4',curious:'M-5,1 Q1,5 8,0',smug:'M-8,3 Q1,8 12,-3',confused:'M-9,2 Q-3,-2 3,2 Q7,5 11,2',peace:'M-10,-1 Q0,11 10,-1',calm:'M-9,0 Q0,7 9,0'};
const star='M0,-15 L5,-5 15,-4 7,4 10,15 0,9 -10,15 -7,4 -15,-4 -5,-5 Z';
function ellipse(c,x,y,rx,ry,fill,stroke,width=1){c.beginPath();c.ellipse(x,y,Math.max(.01,rx),Math.max(.01,ry),0,0,Math.PI*2);if(fill){c.fillStyle=fill;c.fill();}if(stroke){c.strokeStyle=stroke;c.lineWidth=width;c.stroke();}}
function shape(c,s,fill,stroke,width=2){const p=P(s);if(fill){c.fillStyle=fill;c.fill(p);}if(stroke){c.strokeStyle=stroke;c.lineWidth=width;c.lineCap='round';c.lineJoin='round';c.stroke(p);}}
function pose(c,x,y,depth,part,s){c.transform(...tangent(x,y,depth,part,s.rx,s.ry));}
function imageLoad(url){return new Promise((resolve,reject)=>{const i=new Image();i.onload=()=>resolve(i);i.onerror=()=>reject(new Error('素材无法载入：'+url));i.src=url;});}
export class Drawing {
  constructor(boot){this.boot=boot;this.cache=new Map();this.prepared=new Map();this.skin=null;this.key='';}
  async image(file){if(!this.cache.has(file))this.cache.set(file,imageLoad(this.boot.imageBase+file.split('/').map(encodeURIComponent).join('/')));return this.cache.get(file);}
  async init(){this.propsImage=await this.image('assets/props.png');this.decorImage=await this.image('assets/decorations-v1.png');this.pendantImages={};for(const p of this.boot.pendants)this.pendantImages[p.atlas]=await this.image('assets/'+p.atlas);this.mangoImage=await this.image('assets/'+this.boot.mango.texture);}
  async loadSkin(id){if(this.prepared.has(id))return this.prepared.get(id);const skin=this.boot.skins[id];if(!skin)throw new Error('未知皮肤：'+id);
    const data={...skin,layers:[]};
    for(const layer of skin.layers){let file=layer.texture||skin.atlas;if(file==='@shared-neutral-face')file='skins/original-no-bangs/face-no-bangs.png';else if(file==='@shared-bald-base')file='assets/bald-base-v1.png';else file=`skins/${id}/${file}`;
      const source=await this.image(file);if(layer.sx<0||layer.sy<0||layer.sw<1||layer.sh<1||layer.sx+layer.sw>source.naturalWidth||layer.sy+layer.sh>source.naturalHeight)throw new Error('图层裁切范围错误：'+id+'/'+layer.name);
      const canvas=document.createElement('canvas');canvas.width=Math.ceil(layer.w*3);canvas.height=Math.ceil(layer.h*3);const c=canvas.getContext('2d');c.scale(3,3);c.translate(-layer.x,-layer.y);
      if(layer.clip)c.clip(P(layer.clip));if(layer.clipTop!==undefined){c.beginPath();c.rect(-500,layer.clipTop,1500,1500);c.clip();}if(layer.clipBottom!==undefined){c.beginPath();c.rect(-500,-500,1500,layer.clipBottom+500);c.clip();}
      if(layer.texture==='@shared-bald-base'){const g=c.createRadialGradient(205,240,2,200,288,150);g.addColorStop(0,'#fdebdc');g.addColorStop(1,'#fde4d5');c.fillStyle=g;c.fillRect(layer.x,layer.y,layer.w,layer.h);}else c.drawImage(source,layer.sx,layer.sy,layer.sw,layer.sh,layer.x,layer.y,layer.w,layer.h);
      data.layers.push({...layer,canvas});
    }
    if(skin.cane){data.caneImage=await this.image(`skins/${id}/${skin.atlas}`);}
    this.prepared.set(id,data);return data;
  }
  async setSkin(id){const token=this.key=id;const s=await this.loadSkin(id);if(this.key===token)this.skin=s;return s;}
  prop(c,id,x,y,w,h){const p=this.boot.props.find(p=>p.id===id);if(p)c.drawImage(this.propsImage,p.x,p.y,p.w,p.h,x,y,w,h);}
  decoration(c,state,front){const p=this.boot.decorations.find(p=>p.id===state.prefs.decoration);if(!p)return;const skin=this.skin;
    const animal=['cat','deer','bunny'].includes(p.id),cake=skin.id.endsWith('cake')&&animal;
    if((cake?true:p.front)!==front)return;let x=p.dx,y=p.dy+(skin.decorationOffsetY||0),w=p.dw,h=p.dh;
    if(cake){[x,y,w,h]=p.id==='cat'?[36,24,328,164]:p.id==='deer'?[40,-7,320,188]:[54,-12,292,215];}
    const factor=state.prefs.decorationSize*(animal?1:1.35),cx=x+w/2,bottom=y+h;w*=factor;h*=factor;
    if(animal){const fit=Math.min(1,376/w,300/h);w*=fit;h*=fit;}x=clamp(cx-w/2,12,388-w);y=clamp(bottom-h,-70,420-h);
    c.save();pose(c,200,45,.8,'hat',state);c.drawImage(this.decorImage,p.x,p.y,p.w,p.h,x,y,w,h);c.restore();
  }
  layer(c,l,s,t){c.save();if(l.name==='hair'){c.translate((s.hx-s.rx)*9,(s.hy-s.ry)*7);c.translate(200,145);c.rotate(Math.sin(t*1.7-s.rx)*.018);c.translate(-200,-145);}pose(c,l.x+l.w/2,l.y+l.h/2,l.depth,l.name,s);c.drawImage(l.canvas,l.x,l.y,l.w,l.h);c.restore();}
  eyes(c,s,t){const skin=this.skin;if(!skin.expressiveEyes)return;const l=skin.layers.find(l=>l.name==='glasses');if(!l)return;
    for(let side=0;side<2;side++){const x=l.x+l.w*(side===0?.24:.76),y=l.y+l.h*.5;c.save();pose(c,x,y,l.depth,'glasses',s);c.translate(x,y);c.scale(l.w/246,l.w/246);c.save();c.beginPath();c.ellipse(0,0,34.2,35,0,0,Math.PI*2);c.clip();
      const g=c.createRadialGradient(-12,-20,1,0,0,48);g.addColorStop(0,skin.lensLight||'#c17c8c');g.addColorStop(1,skin.lensDark||'#8d4d60');ellipse(c,0,0,35,36,g);
      c.save();c.translate(s.ex*8.8,s.ey*6.8);ellipse(c,0,0,27.5,28.2,null,skin.eyeOutline||'#754554',1.4);ellipse(c,-.4,-.6,25.3,26,null,skin.lensLight||'#c17c8c',1.2);
      const closed=['sleep','peace'].includes(s.mood)||(s.mood==='smug'&&side===1)||(t%6.7>6.52&&!['surprise','sad','expect'].includes(s.mood));const mark=skin.eyeMark||'#fff0dc',outline=skin.eyeOutline||'#754554';
      if(closed)shape(c,'M-13,1 Q0,10 13,1',null,'#f8e9d7',4.5);
      else if(s.mood==='drowsy')shape(c,'M-13,3 Q0,5 13,3',null,mark,4.5);
      else if(s.mood==='happy')shape(c,'M-14,6 Q0,-13 14,6',null,mark,5.5);
      else if(s.mood==='expect'||skin.eyeStyle==='star')shape(c,star,mark,outline,1.3);
      else if(skin.eyeStyle==='flower'){for(let i=0;i<4;i++){c.save();c.rotate(i*Math.PI/2);ellipse(c,0,-9,6,9,mark,outline,1.3);c.restore();}ellipse(c,0,0,4,4,'#fff9e8',outline);}
      else if(skin.eyeStyle==='spiral')shape(c,'M-13,1 C-16,-20 14,-23 19,-3 C25,20 -13,24 -16,8 C-20,-8 4,-15 9,-3 C15,10 -5,12 -4,2 C-4,-3 2,-3 3,0',null,mark,3.2);
      else if(skin.eyeStyle==='gear'){ellipse(c,0,0,16,16,'#30363e',mark,1.7);ellipse(c,0,0,11,11,null,'#727780',1.8);ellipse(c,0,0,5,5,'#171e24');}
      else{const size=s.mood==='confused'?(side===0?.7:1.2):1;c.scale(size,size);c.rotate((s.mood==='angry'?(side===0?-14:14):s.mood==='sad'?(side===0?12:-12):0)*Math.PI/180);const cross=skin.eyeStyle==='plus'?'M-13,0 L13,0 M0,-13 L0,13':'M-8,-8 L8,8 M-8,8 L8,-8';shape(c,cross,null,outline,8);shape(c,cross,null,mark,5.6);}
      c.restore();if(s.mood==='angry')shape(c,side===0?'M-39,-39 L39,-39 L39,-6 Q0,-15 -39,-29 Z':'M-39,-39 L39,-39 L39,-29 Q0,-15 -39,-6 Z','#aa6f80','#754854',1.8);
      if(s.mood==='sad')shape(c,side===0?'M-25,-12 Q-7,-30 20,-29':'M-20,-29 Q7,-30 25,-12',null,'#704956',3.6);
      if(s.mood==='confused')shape(c,side===0?'M-20,-21 Q0,-15 20,-24':'M-20,-22 Q0,-35 20,-21',null,'#704956',2.8);
      c.globalAlpha=.38;ellipse(c,-17+s.ex*2,-21+s.ey*2,6,2.6,'#fff7ec');c.restore();
      if(s.mood==='sad'){const drop=(t*.6+side*.35)%1;c.globalAlpha=.95-drop*.35;c.translate(side===0?-27:27,46+drop*16);shape(c,'M0,-7 Q-9,4 0,8 Q9,4 0,-7 Z','#b4e1ec','#6d98ae',1.2);}c.restore();
    }
  }
  pendant(c,s,t,behind,envelope){const p=this.boot.pendants.find(p=>p.id===s.prefs.pendant);if(!p)return;const a=t*.38-.4,depth=Math.sin(a);if((depth<0)!==behind)return;const size=64*s.prefs.pendantSize*(.88+.12*(depth+1)/2);let x=220+(177-30*envelope)*Math.cos(a)+s.rx*6,y=316+(68-9*envelope)*depth-48*envelope;x=clamp(x,size*.55+4,440-size*.55-4);
    c.save();c.globalAlpha=depth<0?.66:.98;c.translate(x,y);c.rotate(Math.sin(t*.8)*.087);const ratio=p.w/p.h,w=ratio>1?size:size*ratio,h=ratio>1?size/ratio:size;c.drawImage(this.pendantImages[p.atlas],p.x,p.y,p.w,p.h,-w/2,-h/2,w,h);c.restore();
  }
  mitten(c,x,y,size=1){const g=c.createRadialGradient(x-4,y-6,1,x,y,18*size);g.addColorStop(0,'#fffdf6');g.addColorStop(1,'#e9dac9');ellipse(c,x,y,15*size,15*size,g,'#795b60',1.5);}
  action(c,s,t,envelope){if(!s.action)return;const a=s.action,age=t-a.start;c.save();c.globalAlpha=envelope;c.translate(0,(1-envelope)*90);
    if(a.kind==='read'){this.prop(c,'book',119,382,198,120);this.mitten(c,125,470,.78);const trace=(Math.sin(age*1.4)+1)/2,row=Math.floor(age/2.7)%3;this.mitten(c,151+trace*117,418+row*12,.6);const turn=t-a.pageAt;if(a.page>0&&turn<.85){const span=Math.cos(turn/.85*Math.PI)*78;shape(c,`M218,408 L${218+span},${401-14*Math.sin(turn/.85*Math.PI)} L${218+span},${468-14*Math.sin(turn/.85*Math.PI)} L218,477 Z`,'#fff4da','#b59b7a',1);}}
    else if(a.kind==='eat'){const food=['sugar','cookie','cake','soup'][a.food],cycle=Math.max(0,t-a.cycleAt)%4,approach=cycle<1?smooth(cycle):cycle<1.5?1:cycle<2.4?1-smooth((cycle-1.5)/.9):0;const rect=food==='cake'?[77,388,103,111]:food==='soup'?[62,406,129,98]:[65,410,130,93];this.prop(c,food,...rect);this.mitten(c,rect[0]+10,rect[1]+rect[3]-18,.72);const x=298-77*approach,y=464-72*approach-Math.sin(approach*Math.PI)*15;if(food==='sugar'||food==='cookie'){this.prop(c,food==='sugar'?'piece':'cookie-piece',x-12,y-12,24,24);this.mitten(c,x+8,y+17,.8);}else{this.prop(c,'spoon',x-9,y-10,70,52);if(food==='cake')ellipse(c,x+7,y+6,7,4,'#68416b');this.mitten(c,x+42,y+22,.78);}}
    else if(a.kind==='cane'){const angle=(age<2.9?-13+Math.sin(age*2)*10:Math.sin(age*1.7)*4)*Math.PI/180;c.translate(351,487);c.rotate(angle);const p=this.skin.cane;if(p){const w=Math.min(108,276*p.sw/p.sh);c.drawImage(this.skin.caneImage,p.sx,p.sy,p.sw,p.sh,-4.6-w/2,-276,w,276);}else this.prop(c,'cane',-30,-276,50.8,276);this.mitten(c,-1,-276+276*(this.skin.caneGrip||.286),.84);}
    c.restore();
  }
  waves(c,s,age){const colors=PALETTES[this.skin.id]||['#3994be','#effaff'];for(let hop=1;hop<=s.hop.count;hop++)for(let i=0;i<3;i++){const life=(age-hop*.8-i*.1)/.7;if(life<=0||life>=1)continue;c.save();c.globalAlpha=smooth(life/.08)*(1-smooth((life-.52)/.48));ellipse(c,220,491,25+145*smooth(life),3+10*life,null,colors[i%2],i===0?6.5:4.5);c.restore();}
    for(let hop=1;hop<=s.hop.count;hop++){const life=age-hop*.8;if(life<=0||life>=1)continue;for(let i=0;i<12;i++){const side=i%2===0?-1:1,x=220+side*(76+i%4*13+(28+i%3*13)*life),y=478-(57+i%4*13)*life+27*life*life;c.save();c.globalAlpha=Math.min(1,life/.08)*(1-life);c.translate(x,y);c.rotate(i+life*2);const color=colors[i%2];if(this.skin.id==='gilda')shape(c,'M-6,0 Q0,-9 6,0 Q0,9 -6,0 Z',color);else if(this.skin.id.endsWith('cake')){c.fillStyle=color;c.fillRect(-4,-2,8,4);}else{c.scale(.3,.3);shape(c,star,color);}c.restore();}}
  }
  draw(c,s,t){c.clearRect(0,0,440,650);if(!this.skin)return;c.save();c.translate(0,130);const hopAge=s.hop?t-s.hop.start:-1,height=s.hop?hopHeight(hopAge,s.hop.count):0;
    const envelope=s.action?smooth(Math.min(1,(t-s.action.start)/.7))*smooth(Math.max(0,(s.action.end-t)/.7)):0;
    if(s.mangoUntil>t){const p=this.boot.mango;c.drawImage(this.mangoImage,p.sx,p.sy,p.sw,p.sh,50,180,340,256);c.restore();return;}
    c.save();c.translate(0,-height);this.pendant(c,s,t,true,envelope);
    let tilt=Math.sin(t*1.2)*1.8+s.rx*5.3,bob=Math.sin(t*1.8)*3;
    if(s.mood==='happy'&&!s.action)bob-=Math.abs(Math.sin(t*5))*11;if(s.mood==='shy')tilt+=7;if(s.mood==='sleep'){tilt=11;bob=Math.sin(t*1.1)*3;}if(s.mood==='curious')tilt+=7;if(s.mood==='confused')tilt+=Math.sin(t*.8)*8;if(s.mood==='drowsy')tilt+=5;if(s.mood==='smug')tilt-=6;
    c.save();c.translate(0,-24*envelope);c.translate(220,270);c.scale(1-.16*envelope,1-.16*envelope);c.translate(-220,-270);c.translate(20,93+bob);
    if(s.mood==='echo')for(let i=0;i<3;i++){const life=(t*.58+i/3)%1;c.save();c.globalAlpha=(1-life)*.6;ellipse(c,200,235,80+life*137,70+life*137,null,'#89bac4',2.5);c.restore();}
    c.translate(200,235);c.rotate(tilt*Math.PI/180);const fit=1-Math.max(0,Math.abs(tilt)-8)*.008;c.scale((1+Math.sin(t*1.8)*.006)*fit,(1-Math.sin(t*1.8)*.004)*fit);c.translate(-200,-235);
    this.decoration(c,s,false);for(const l of this.skin.layers)if(l.name!=='hat')this.layer(c,l,s,t);this.eyes(c,s,t);for(const l of this.skin.layers)if(l.name==='hat')this.layer(c,l,s,t);this.decoration(c,s,true);
    c.save();pose(c,this.skin.mouthX,this.skin.mouthY,.9,'face',s);const mx=this.skin.mouthX||200,my=this.skin.mouthY||350;
    if(['shy','happy','peace','expect'].includes(s.mood)){ellipse(c,mx-65,this.skin.cheekY||334,23,9,'#e9a3a35e');ellipse(c,mx+65,this.skin.cheekY||334,23,9,'#e9a3a35e');}
    c.translate(mx,my);if(['surprise','echo','drowsy'].includes(s.mood))ellipse(c,0,0,6,9,'#b56e72','#69494e',2);
    else if(s.mood==='happy')shape(c,'M-13,-5 Q0,1 13,-5 Q11,15 0,17 Q-11,12 -13,-5 Z','#edacaa','#69494e',2);
    else if(s.mood==='expect')shape(c,'M-9,-2 Q0,1 9,-2 Q7,10 0,10 Q-7,10 -9,-2 Z','#edacaa','#875d63',1.8);
    else shape(c,mouths[s.mood]||mouths.calm,null,'#875d63',2.1);c.restore();
    if(this.skin.id==='sound-wave'){c.save();pose(c,145,340,.65,'headwear',s);shape(c,'M110,358 C128,362 153,352 174,350',null,'#503525',6);shape(c,'M110,358 C128,362 153,352 174,350',null,'#deb563',3.3);c.fillStyle='#302c2a';c.fillRect(169,344,24,11);c.restore();}
    c.restore();this.action(c,s,t,envelope);this.pendant(c,s,t,false,envelope);
    c.font='36px -apple-system, sans-serif';c.fillStyle='#738a9b';if(s.mood==='sleep')c.fillText('z z Z',290,107+Math.sin(t)*5);if(s.mood==='think')c.fillText('…',355,104);if(s.mood==='surprise')c.fillText('!',370,104);if(s.mood==='curious'||s.mood==='confused')c.fillText(s.mood==='curious'?'?':'??',350,104);
    if(['happy','shy','peace'].includes(s.mood))for(let i=0;i<3;i++){const a=(t*.45+i*.33)%1;c.save();c.globalAlpha=1-a;c.translate(100+i*116+Math.sin(t+i)*8,112-a*75);c.scale(1.1,1.1);shape(c,'M0,8 C-20,-6 -14,-19 0,-10 C14,-19 20,-6 0,8 Z','#d38e9e');c.restore();}
    c.restore();if(s.hop)this.waves(c,s,hopAge);c.restore();
  }
}
