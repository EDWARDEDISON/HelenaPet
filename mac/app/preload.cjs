const {contextBridge,ipcRenderer}=require('electron');
contextBridge.exposeInMainWorld('helena',{
  bootstrap:()=>ipcRenderer.invoke('helena:bootstrap'),
  update:patch=>ipcRenderer.invoke('helena:update',patch),
  command:(name,value)=>ipcRenderer.send('helena:command',name,value),
  hit:opaque=>ipcRenderer.send('helena:hit',opaque===true),
  pointer:pressed=>ipcRenderer.send('helena:pointer',pressed===true),
  drag:active=>ipcRenderer.send('helena:drag',active===true),
  onState:cb=>{const fn=(_e,data)=>cb(data);ipcRenderer.on('helena:state',fn);return()=>ipcRenderer.removeListener('helena:state',fn);},
  onCursor:cb=>{const fn=(_e,data)=>cb(data);ipcRenderer.on('helena:cursor',fn);return()=>ipcRenderer.removeListener('helena:cursor',fn);},
  onCommand:cb=>{const fn=(_e,data)=>cb(data);ipcRenderer.on('helena:action',fn);return()=>ipcRenderer.removeListener('helena:action',fn);}
});
