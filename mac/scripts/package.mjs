import packager from '@electron/packager';
import fs from 'node:fs/promises';import path from 'node:path';import {fileURLToPath}from'node:url';import {execFileSync}from'node:child_process';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..'),repo=path.dirname(root),dist=path.join(root,'dist');
const arch=process.env.TARGET_ARCH||process.arch;
if(process.platform!=='darwin')throw new Error('Build this app on a Mac or the macOS GitHub Actions runner.');
if(!['arm64','x64'].includes(arch))throw new Error('Unsupported Mac architecture');
const data=path.join(dist,'pet-data');await fs.mkdir(data,{recursive:true});
for(const folder of['assets','skins'])await fs.cp(path.join(repo,folder),path.join(data,folder),{recursive:true});
for(const file of['dialogue.json','interaction.json'])await fs.copyFile(path.join(repo,file),path.join(data,file));
const iconset=path.join(dist,'helena.iconset');await fs.mkdir(iconset,{recursive:true});
for(const size of[16,32,128,256,512])for(const multiplier of[1,2])execFileSync('sips',['-z',String(size*multiplier),String(size*multiplier),path.join(root,'app/icon.png'),'--out',path.join(iconset,`icon_${size}x${size}${multiplier===2?'@2x':''}.png`)],{stdio:'ignore'});
const icon=path.join(dist,'helena.icns');execFileSync('iconutil',['-c','icns',iconset,'-o',icon]);
const built=await packager({dir:root,name:'HelenaPet',platform:'darwin',arch,electronVersion:'45.0.0',appVersion:'0.1.0-beta.1',appBundleId:'io.github.edwardedison.helenapet',out:dist,asar:true,prune:true,overwrite:true,icon,extraResource:[data],ignore:[/^\/dist(?:\/|$)/,/^\/scripts(?:\/|$)/,/^\/test(?:\/|$)/,/^\/smoke-output(?:\/|$)/,/^\/pnpm-lock\.yaml$/,/^\/README\.md$/],extendInfo:{LSMinimumSystemVersion:'13.0',LSUIElement:true,NSHighResolutionCapable:true}});
const appPath=path.join(built[0],'HelenaPet.app');
// Ad-hoc sealing protects bundle integrity; this is NOT Developer ID signing or notarization.
const entitlements=path.join(root,'scripts/entitlements.plist');
execFileSync('codesign',['--force','--deep','--sign','-','--entitlements',entitlements,appPath],{stdio:'inherit'});
execFileSync('codesign',['--verify','--deep','--strict',appPath],{stdio:'inherit'});
const bundle=path.join(dist,`HelenaPet-Mac-${arch}`);await fs.mkdir(bundle,{recursive:true});
execFileSync('ditto',[appPath,path.join(bundle,'HelenaPet.app')]);
await fs.copyFile(path.join(root,'README.md'),path.join(bundle,'开始使用.md'));
const executable=path.join(bundle,'HelenaPet.app/Contents/MacOS/HelenaPet');
execFileSync('file',[executable],{stdio:'inherit'});
console.log('MAC_APP='+path.join(bundle,'HelenaPet.app'));
await fs.writeFile(path.join(dist,`app-${arch}.txt`),path.join(bundle,'HelenaPet.app'));
// Archive only after running the packaged app's smoke check in CI.
