const {app,BrowserWindow,ipcMain,Menu,Tray,nativeImage,screen,session}=require('electron');
const fs=require('node:fs');const path=require('node:path');const os=require('node:os');
const {pathToFileURL,fileURLToPath}=require('node:url');
const smoke=process.argv.includes('--smoke-test');
if(smoke)app.setPath('userData',fs.mkdtempSync(path.join(os.tmpdir(),'helena-smoke-')));
const dataRoot=app.isPackaged?path.join(process.resourcesPath,'pet-data'):path.resolve(__dirname,'../..');
const readJSON=name=>JSON.parse(fs.readFileSync(path.join(dataRoot,name),'utf8').replace(/^\uFEFF/,''));
let pet,panel,tray,prefs,catalog,boot,core,cursorTimer,saveTimer,dragging=null,pressed=false,ignoring=false;
let errors=[];let ending=false;
const configFile=()=>path.join(app.getPath('userData'),'preferences.json');
function save(){clearTimeout(saveTimer);saveTimer=setTimeout(()=>{try{fs.mkdirSync(path.dirname(configFile()),{recursive:true});fs.writeFileSync(configFile()+'.new',JSON.stringify(prefs,null,2));fs.renameSync(configFile()+'.new',configFile());}catch(e){console.error('Save failed',e.message);}},350);}
function trusted(e){return [pet,panel].some(w=>w&&!w.isDestroyed()&&e.sender===w.webContents&&e.senderFrame===w.webContents.mainFrame);}
function state(){for(const w of [pet,panel])if(w&&!w.isDestroyed())w.webContents.send('helena:state',prefs);}
function sendAction(name,value){if(pet&&!pet.isDestroyed())pet.webContents.send('helena:action',{name,value});}
function bounds(x,y){const size={width:Math.round(440*prefs.scale),height:Math.round(650*prefs.scale)};
  const area=screen.getDisplayNearestPoint({x:Math.round(x),y:Math.round(y)}).workArea;
  return {...size,x:Math.round(Math.max(area.x,Math.min(area.x+area.width-size.width,x))),y:Math.round(Math.max(area.y,Math.min(area.y+area.height-size.height,y)))};
}
function recover(){const a=screen.getPrimaryDisplay().workArea;const b=bounds(a.x+a.width-440*prefs.scale-40,a.y+a.height-650*prefs.scale-20);pet.setBounds(b);pet.showInactive();prefs.x=b.x;prefs.y=b.y;save();}
function secureWindow(w){w.webContents.setWindowOpenHandler(()=>({action:'deny'}));w.webContents.on('will-navigate',e=>e.preventDefault());w.webContents.on('console-message',(_e,...args)=>{const msg=typeof _e==='object'&&'message'in _e?_e.message:args[1];if(msg){console.log(msg);if(_e.level==='error'||args[0]===3)errors.push(msg);}});w.webContents.on('render-process-gone',(_e,d)=>{errors.push(d.reason);console.error('Renderer exited',d);});}
function openPanel(){if(panel&&!panel.isDestroyed()){panel.show();panel.focus();return;}
  panel=new BrowserWindow({width:550,height:820,minWidth:470,minHeight:540,title:'听见你 · 海伦娜桌宠',backgroundColor:'#edf2f5',autoHideMenuBar:true,show:false,webPreferences:{preload:path.join(__dirname,'preload.cjs'),nodeIntegration:false,contextIsolation:true,sandbox:true,webSecurity:true,backgroundThrottling:false}});
  secureWindow(panel);panel.loadFile(path.join(__dirname,'index.html'),{query:{panel:'1'}});panel.once('ready-to-show',()=>{if(!smoke)panel.show();});panel.on('closed',()=>panel=null);
}
async function start(){core=await import(pathToFileURL(path.join(__dirname,'core.mjs')).href);
  const skins={};for(const id of fs.readdirSync(path.join(dataRoot,'skins'))){const f=path.join(dataRoot,'skins',id,'rig.json');if(fs.existsSync(f))skins[id]=readJSON(`skins/${id}/rig.json`);}
  const pendants=readJSON('assets/pendants.json');catalog={skins:Object.keys(skins),pendants:pendants.map(p=>p.id)};
  let loaded={};try{loaded=JSON.parse(fs.readFileSync(configFile(),'utf8'));}catch{}
  prefs=core.cleanPreferences(loaded,catalog);
  boot={skins,pendants,decorations:readJSON('assets/decorations.json'),props:readJSON('assets/props.json'),mango:readJSON('assets/mango.json'),dialogue:readJSON('dialogue.json'),interaction:readJSON('interaction.json'),imageBase:pathToFileURL(dataRoot+path.sep).href,settings:prefs,testMode:smoke,version:app.getVersion()};
  const permitted=[path.resolve(__dirname)+path.sep,path.resolve(dataRoot,'assets')+path.sep,path.resolve(dataRoot,'skins')+path.sep];
  session.defaultSession.setPermissionRequestHandler((_wc,_p,cb)=>cb(false));
  session.defaultSession.webRequest.onBeforeRequest((d,cb)=>{try{const local=fileURLToPath(d.url);cb({cancel:!permitted.some(p=>path.resolve(local).startsWith(p))});}catch{cb({cancel:!d.url.startsWith('data:')});}});
  const a=screen.getPrimaryDisplay().workArea;
  const pos=bounds(prefs.x??a.x+a.width-440*prefs.scale-40,prefs.y??a.y+a.height-650*prefs.scale-20);
  pet=new BrowserWindow({...pos,transparent:true,frame:false,hasShadow:false,resizable:false,skipTaskbar:true,show:false,focusable:false,acceptFirstMouse:true,alwaysOnTop:prefs.topmost,backgroundColor:'#00000000',webPreferences:{preload:path.join(__dirname,'preload.cjs'),contextIsolation:true,nodeIntegration:false,sandbox:true,webSecurity:true,backgroundThrottling:false}});
  secureWindow(pet);pet.setAlwaysOnTop(prefs.topmost,'floating');if(process.platform==='darwin'){pet.setVisibleOnAllWorkspaces(true,{visibleOnFullScreen:true});app.dock?.hide();}
  pet.webContents.on('did-fail-load',(_e,code,desc)=>{errors.push(`${code}: ${desc}`);});
  pet.loadFile(path.join(__dirname,'index.html'));pet.once('ready-to-show',()=>{if(!smoke)pet.showInactive();});
  pet.on('closed',()=>{pet=null;if(!ending)app.quit();});
  const icon=nativeImage.createFromPath(path.join(__dirname,'icon.png')).resize({width:20,height:20});
  if(!smoke){tray=new Tray(icon);tray.setToolTip('听见你 · 海伦娜桌宠');tray.setContextMenu(Menu.buildFromTemplate([{label:'打开互动面板',click:openPanel},{label:'找回桌宠',click:recover},{label:'小憩 / 醒来',click:()=>sendAction('toggle-sleep')},{type:'separator'},{label:'退出',click:()=>app.quit()}]));tray.on('click',openPanel);}
  Menu.setApplicationMenu(Menu.buildFromTemplate([{label:'海伦娜',submenu:[{label:'互动面板',click:openPanel},{label:'找回桌宠',click:recover},{type:'separator'},{role:'quit',label:'退出'}]},{role:'editMenu'}]));
  cursorTimer=setInterval(()=>{if(!pet||pet.isDestroyed())return;const c=screen.getCursorScreenPoint();if(dragging){if(Date.now()-dragging.at>30000){dragging=null;pressed=false;}else pet.setPosition(Math.round(dragging.x+c.x-dragging.cx),Math.round(dragging.y+c.y-dragging.cy));}
    const b=pet.getBounds();pet.webContents.send('helena:cursor',{x:(c.x-b.x)/prefs.scale,y:(c.y-b.y)/prefs.scale});},33);
  screen.on('display-removed',recover);screen.on('display-metrics-changed',()=>{if(pet){const b=pet.getBounds();pet.setBounds(bounds(b.x,b.y));}});
  if(smoke){const out=process.env.HELENA_TEST_OUTPUT||path.resolve(process.cwd(),'smoke-output');fs.mkdirSync(out,{recursive:true});
    const deadline=setTimeout(()=>{console.error('Smoke test timed out');app.exit(2);},90000);
    try{await new Promise((resolve,reject)=>{pet.webContents.once('did-finish-load',resolve);pet.webContents.once('did-fail-load',(_e,_c,m)=>reject(new Error(m)));});
      const result=await pet.webContents.executeJavaScript('window.runHelenaSmokeTest()');
      fs.writeFileSync(path.join(out,'pet.png'),Buffer.from(result.preview.split(',')[1],'base64'));delete result.preview;
      openPanel();await new Promise((resolve,reject)=>{panel.webContents.once('did-finish-load',resolve);panel.webContents.once('did-fail-load',(_e,_c,m)=>reject(new Error(m)));});
      const panelResult=await panel.webContents.executeJavaScript('window.runHelenaPanelTest()');
      await panel.webContents.executeJavaScript('new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))');
      fs.writeFileSync(path.join(out,'panel.png'),(await panel.webContents.capturePage()).toPNG());
      fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({...result,panel:panelResult,platform:process.platform,arch:process.arch,electron:process.versions.electron,errors},null,2));
      console.log('HELENA_SMOKE',JSON.stringify({...result,panel:panelResult,errors}));clearTimeout(deadline);app.exit(result.ok&&panelResult.ok&&!errors.length?0:1);
    }catch(e){console.error(e);app.exit(1);}
  }
}
ipcMain.handle('helena:bootstrap',e=>{if(!trusted(e))throw new Error('Untrusted window');return {...boot,settings:prefs};});
ipcMain.handle('helena:update',(e,patch)=>{if(!trusted(e)||!patch||typeof patch!=='object')throw new Error('Invalid update');const before=prefs.scale;prefs=core.cleanPreferences({...prefs,...patch},catalog);if(before!==prefs.scale){const b=pet.getBounds();pet.setBounds(bounds(b.x,b.y));}pet.setAlwaysOnTop(prefs.topmost,'floating');save();state();return prefs;});
ipcMain.on('helena:command',(e,name,value)=>{if(!trusted(e))return;if(name==='panel')openPanel();else if(name==='recover')recover();else if(name==='quit')app.quit();else if(['mood','talk','action','stop','hop','toggle-sleep','mango','clear-history'].includes(name)){if(typeof value==='string'&&value.length>100)return;sendAction(name,value);}});
ipcMain.on('helena:hit',(e,opaque)=>{if(!pet||e.sender!==pet.webContents||pressed||dragging)return;const next=!opaque;if(next!==ignoring){ignoring=next;pet.setIgnoreMouseEvents(next,{forward:true});}});
ipcMain.on('helena:pointer',(e,down)=>{if(!pet||e.sender!==pet.webContents)return;pressed=down===true;if(pressed){ignoring=false;pet.setIgnoreMouseEvents(false);}});
ipcMain.on('helena:drag',(e,active)=>{if(!pet||e.sender!==pet.webContents)return;if(active){const c=screen.getCursorScreenPoint(),b=pet.getBounds();dragging={x:b.x,y:b.y,cx:c.x,cy:c.y,at:Date.now()};}else{dragging=null;const b=pet.getBounds(),next=bounds(b.x,b.y);pet.setBounds(next);prefs.x=next.x;prefs.y=next.y;save();}});
if(!smoke&&!app.requestSingleInstanceLock())app.quit();else{
  app.on('second-instance',()=>{if(pet)recover();});app.whenReady().then(start).catch(e=>{console.error(e);app.exit(1);});
}
app.on('window-all-closed',()=>{if(!tray)app.quit();});
app.on('before-quit',()=>{ending=true;clearInterval(cursorTimer);clearTimeout(saveTimer);if(prefs&&!smoke){try{fs.mkdirSync(path.dirname(configFile()),{recursive:true});fs.writeFileSync(configFile(),JSON.stringify(prefs,null,2));}catch{}}tray?.destroy();});
