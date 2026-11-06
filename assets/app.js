/* The Everything World: map viewer. Ink is rendered into cached tiles; labels, songs and covers are drawn fresh over them. */
(function(){
const W=window.WORLD,G=window.GLYPH,SEA=window.SEAART;
const $=id=>document.getElementById(id);
const canvas=$('map'),ctx=canvas.getContext('2d');
const songs=W.songs.map((a,i)=>({i,uri:a[0],t:a[1],a:a[2],al:a[3],y:a[4],ms:a[5],pop:a[6],g:a[7],e:a[8],v:a[9],ac:a[10],d:a[11],tempo:a[12],by:a[13],r:a[14],x:a[15],y2:a[16],c:a[17],iid:a[18]}));
songs.forEach(s=>{s.wy=s.y2});
const regions=W.regions,towns=W.towns,glyphTypes=W.glyphTypes;
const glyphs=W.glyphs;              // sorted by y: [type,x,y,scale,seed]
const FF='"IM Fell English",Georgia,serif',FS='"IM Fell English SC","IM Fell English",Georgia,serif',FD='"Cinzel Decorative","IM Fell English SC",Georgia,serif';
const COVER=24;                     // cover size in world units
// no graphics acceleration? then draw at one pixel per CSS pixel; extra sharpness costs too much on a CPU
const softwareGL=(()=>{try{const g=document.createElement('canvas').getContext('webgl');if(!g)return true;const e=g.getExtension('WEBGL_debug_renderer_info');return /swiftshader|llvmpipe|software|basic render/i.test(e?g.getParameter(e.UNMASKED_RENDERER_WEBGL):'')}catch(_){return true}})();
if(softwareGL)document.documentElement.classList.add('low');
let devDpr=softwareGL?1:Math.min(window.devicePixelRatio||1,2),dpr=devDpr,vw=0,vh=0,moving=false;
let view={x:0,y:0,s:.3},goal={x:0,y:0,s:.3},fitS=.3,dirty=true;
const MAXS=9;
let sel=null,hover=null,panelW=0,touched=false;

/* ---------- images ---------- */
const base=new Image(),atlas=new Image();let atlasC=null;
atlas.onload=()=>{atlasC=atlas;dirty=true};
const hires=new Map();
function hi(iid){let im=hires.get(iid);if(!im&&hires.size<400){im=new Image();im.onload=()=>{dirty=true};im.src='assets/covers/'+iid+'.jpg';hires.set(iid,im)}return im&&im.complete&&im.naturalWidth?im:null}

/* ---------- tiles ---------- */
const TILE=512,LEVELS=[1,2,4,8];
const tiles=new Map();let tick=0,queue=[];
let overview=null;const OVS=.5;
function lineW(px,ts){return px/Math.pow(ts,.72)}
function inBox(b,x0,y0,x1,y1){return b[2]>=x0&&b[0]<=x1&&b[3]>=y0&&b[1]<=y1}
function strokeChunks(c,arr,x0,y0,x1,y1){c.beginPath();let n=0;for(const ch of arr){if(!inBox(ch.b,x0,y0,x1,y1))continue;const p=ch.p;c.moveTo(p[0],p[1]);for(let i=2;i<p.length;i+=2)c.lineTo(p[i],p[i+1]);n++}if(n)c.stroke()}
let decor=[];
function paint(c,x0,y0,x1,y1,ts){
  c.setTransform(ts,0,0,ts,-x0*ts,-y0*ts);
  const sx0=Math.max(0,x0),sy0=Math.max(0,y0),sx1=Math.min(W.W,x1),sy1=Math.min(W.H,y1);
  c.imageSmoothingEnabled=true;c.imageSmoothingQuality='high';
  c.drawImage(base,sx0,sy0,sx1-sx0,sy1-sy0,sx0,sy0,sx1-sx0,sy1-sy0);
  c.lineJoin='round';c.lineCap='round';
  for(const r of W.ripples){c.strokeStyle=`rgba(62,74,70,${r.a})`;c.lineWidth=lineW(.75,ts);strokeChunks(c,r.l,x0,y0,x1,y1)}
  c.strokeStyle='rgba(66,88,96,.8)';
  for(const r of W.rivers){c.lineWidth=lineW(r.w,ts);strokeChunks(c,r.l,x0,y0,x1,y1)}
  c.strokeStyle='rgba(92,58,34,.62)';c.lineWidth=lineW(1.3,ts);c.setLineDash([9,5,2,5]);strokeChunks(c,W.borders,x0,y0,x1,y1);c.setLineDash([]);
  c.strokeStyle='rgba(52,34,18,.95)';c.lineWidth=lineW(1.8,ts);strokeChunks(c,W.coast,x0,y0,x1,y1);
  // symbols, painted north to south so nearer ones overlap farther ones
  const lw=lineW(.95,ts);let a=0,b=glyphs.length;while(a<b){const m=(a+b)>>1;if(glyphs[m][2]<y0-4)a=m+1;else b=m}
  for(let i=a;i<glyphs.length;i++){const g=glyphs[i];if(g[2]>y1+60)break;if(g[1]<x0-50||g[1]>x1+50)continue;G[glyphTypes[g[0]]](c,g[1],g[2],g[3],g[4],lw)}
  for(const d of decor)if(d.x+d.r>=x0&&d.x-d.r<=x1&&d.y+d.r>=y0&&d.y-d.r<=y1)d.draw(c);
  // neat-line around the sheet
  c.strokeStyle='rgba(52,34,18,.9)';c.lineWidth=5;c.strokeRect(26,26,W.W-52,W.H-52);c.lineWidth=1.4;c.strokeRect(38,38,W.W-76,W.H-76);
  c.fillStyle='rgba(52,34,18,.82)';
  for(let x=38;x<W.W-38;x+=160){const on=(((x-38)/160)|0)%2===0;if(on){c.fillRect(x,28,160,9);c.fillRect(x,W.H-37,160,9)}}
  for(let y=38;y<W.H-38;y+=160){const on=(((y-38)/160)|0)%2===0;if(on){c.fillRect(28,y,9,160);c.fillRect(W.W-37,y,9,160)}}
}
function tileKey(l,i,j){return l+'/'+i+'/'+j}
function renderTile(k,l,i,j){
  const cv=document.createElement('canvas');cv.width=cv.height=TILE;const c=cv.getContext('2d',{alpha:false});
  const ws=TILE/l;c.fillStyle='#d9c79c';c.fillRect(0,0,TILE,TILE);
  paint(c,i*ws,j*ws,(i+1)*ws,(j+1)*ws,l);
  tiles.set(k,{cv,t:tick});
  if(tiles.size>110){const old=[...tiles.entries()].sort((p,q)=>p[1].t-q[1].t).slice(0,20);old.forEach(([key])=>tiles.delete(key))}
}
function drawWorld(){
  ctx.setTransform(dpr,0,0,dpr,0,0);
  ctx.fillStyle='#2a1f14';ctx.fillRect(0,0,vw,vh);
  const s=view.s,px=s*devDpr;
  ctx.imageSmoothingEnabled=true;ctx.imageSmoothingQuality=px<.8?'high':'medium';
  ctx.setTransform(s*dpr,0,0,s*dpr,-view.x*s*dpr,-view.y*s*dpr);
  if(!softwareGL){ctx.shadowColor='rgba(0,0,0,.6)';ctx.shadowBlur=30;ctx.fillStyle='#d9c79c';ctx.fillRect(0,0,W.W,W.H);ctx.shadowBlur=0}
  const x0=view.x,y0=view.y,x1=view.x+vw/s,y1=view.y+vh/s;
  if(px<=OVS*1.15){ctx.drawImage(overview,0,0,W.W,W.H);queue=[];return}
  const l=LEVELS.find(v=>v>=px*.92)||8,ws=TILE/l,need=[],have=[];
  const i0=Math.max(0,Math.floor(x0/ws)),j0=Math.max(0,Math.floor(y0/ws)),i1=Math.min(Math.ceil(W.W/ws)-1,Math.floor(x1/ws)),j1=Math.min(Math.ceil(W.H/ws)-1,Math.floor(y1/ws));
  const cx=(x0+x1)/2,cy=(y0+y1)/2;
  for(let j=j0;j<=j1;j++)for(let i=i0;i<=i1;i++){const k=tileKey(l,i,j),tt=tiles.get(k);
    if(tt){tt.t=tick;have.push([tt,i,j])}else need.push({k,l,i,j,d:Math.hypot((i+.5)*ws-cx,(j+.5)*ws-cy)})}
  // only paint what lies underneath if something on top is still missing
  if(need.length){ctx.drawImage(overview,0,0,W.W,W.H);
    for(const pl of LEVELS){if(pl>=l)break;const pws=TILE/pl;
      for(let j=Math.floor(y0/pws);j<=Math.floor(y1/pws);j++)for(let i=Math.floor(x0/pws);i<=Math.floor(x1/pws);i++){const tt=tiles.get(tileKey(pl,i,j));if(tt){tt.t=tick;ctx.drawImage(tt.cv,i*pws,j*pws,pws+.5/s,pws+.5/s)}}}}
  for(const[tt,i,j]of have)ctx.drawImage(tt.cv,i*ws,j*ws,ws+.5/s,ws+.5/s);
  need.sort((a,b)=>a.d-b.d);queue=need;
}
function pump(){ // one tile per frame keeps the page responsive on slow machines
  const t0=performance.now();
  while(queue.length&&performance.now()-t0<14){const q=queue.shift();if(!tiles.has(q.k))renderTile(q.k,q.l,q.i,q.j)}
  dirty=dirty||queue.length>0;
}

/* ---------- labels ---------- */
/* Text is drawn once into a small sprite and reused: redrawing outlined text every frame is the slowest thing a CPU can do here. */
const sprites=new Map(),widths=new Map();
const mc=document.createElement('canvas').getContext('2d');
function mt(text,style,fam,px){const k=style+fam+'|'+text;let w=widths.get(k);if(w===undefined){mc.font=`${style}100px ${fam}`;w=mc.measureText(text).width;widths.set(k,w)}return w*px/100}
function curvedSprite(text,style,fam,px,spR,bendR,rot,fill,halo,lw){
  const q=Math.max(6,moving?Math.round(px):Math.round(px*10)/10);
  const key=[text,style,fam,q,spR,bendR,rot,fill,halo,lw].join('|');let sp=sprites.get(key);
  if(!sp){
    const font=`${style}${q}px ${fam}`,gap=q*spR,bend=q*bendR;mc.font=font;
    const chars=[...text],ws=chars.map(c=>mc.measureText(c).width),tot=ws.reduce((a,b)=>a+b,0)+gap*(chars.length-1);
    const c0=Math.abs(Math.cos(rot)),s0=Math.abs(Math.sin(rot)),pad=q*1.1+bend+(lw||0);
    const w=Math.ceil(tot*c0+pad*2*Math.max(s0,.5)+q),h=Math.ceil(tot*s0+pad*2);
    const cv=document.createElement('canvas');cv.width=Math.ceil(w*devDpr);cv.height=Math.ceil(h*devDpr);
    const x=cv.getContext('2d');x.scale(devDpr,devDpr);x.translate(w/2,h/2);x.rotate(rot);x.font=font;x.textAlign='center';x.textBaseline='middle';x.lineJoin='round';
    const place=[];let p=-tot/2;for(let i=0;i<chars.length;i++){const m=p+ws[i]/2,u=tot?2*m/tot:0;place.push([m,-bend*(1-u*u),bend&&tot?Math.atan(4*bend*m/(tot*tot)):0]);p+=ws[i]+gap}
    if(lw){x.strokeStyle=halo;x.lineWidth=lw;chars.forEach((c,i)=>{x.save();x.translate(place[i][0],place[i][1]);x.rotate(place[i][2]);x.strokeText(c,0,0);x.restore()})}
    x.fillStyle=fill;chars.forEach((c,i)=>{x.save();x.translate(place[i][0],place[i][1]);x.rotate(place[i][2]);x.fillText(c,0,0);x.restore()});
    sp={cv,w,h,tot,q};sprites.set(key,sp);if(sprites.size>1400)sprites.clear();
  }
  return sp;
}
function blit(sp,x,y,px){const k=px/sp.q,w=sp.w*k,h=sp.h*k,a=x-w/2,b=y-h/2;ctx.drawImage(sp.cv,moving?a:Math.round(a*dpr)/dpr,moving?b:Math.round(b*dpr)/dpr,w,h)}
function wrap(name,maxW,measure){if(measure(name)<=maxW)return[name];const w=name.split(' ');if(w.length<2)return[name];let best=null,bd=1e9;for(let i=1;i<w.length;i++){const a=w.slice(0,i).join(' '),b=w.slice(i).join(' '),d=Math.abs(measure(a)-measure(b));if(d<bd){bd=d;best=[a,b]}}return best}
const boxes=[];
function free(x,y,w,h,pad=2){const a=x-w/2-pad,b=y-h/2-pad,c=x+w/2+pad,d=y+h/2+pad;for(const q of boxes)if(a<q[2]&&c>q[0]&&b<q[3]&&d>q[1])return false;boxes.push([a,b,c,d]);return true}
const fade=(v,a,b)=>Math.max(0,Math.min(1,(v-a)/(b-a)));
const HALO='rgba(240,228,194,.82)',INK='#2d1d0e';
const regionsBySize=[...regions].sort((a,b)=>b.c-a.c),townsByRank=[...towns].sort((a,b)=>b.cap-a.cap||b.c-a.c);
function drawLabels(){
  ctx.setTransform(dpr,0,0,dpr,0,0);boxes.length=0;
  const s=view.s,sx=x=>(x-view.x)*s,sy=y=>(y-view.y)*s;
  const vis=(x,y,m=120)=>x>-m&&x<vw+m&&y>-m&&y<vh+m;
  const ra=1-fade(s/fitS,1.45,2.0);
  if(ra>0)for(const r of W.realms){if(r.n==='The Outer Isles')continue;const x=sx(r.x),y=sy(r.y),px=Math.max(26,Math.min(58,120*s));if(!vis(x,y,300))continue;
    ctx.globalAlpha=ra*.2;blit(curvedSprite(r.n.toUpperCase(),'',FD,px,.18,0,0,'#4a2e16','',0),x,y-px*.9,px);ctx.globalAlpha=1}
  for(const r of W.seas){const x=sx(r.x),y=sy(r.y);if(!vis(x,y,250))continue;const px=Math.max(11,Math.min(34,44*r.z*s)),rot=r.a*Math.PI/180;
    const sp=curvedSprite(r.n,'italic ',FF,px,.16,.35,rot,'rgba(44,66,74,.92)','rgba(216,214,180,.55)',3);blit(sp,x,y,px);
    const w=sp.tot*px/sp.q;boxes.push(Math.abs(r.a)>45?[x-px,y-w/2,x+px,y+w/2]:[x-w/2,y-px,x+w/2,y+px])}
  const rFade=1-fade(s,2.4,3.6);
  if(rFade>0)for(const r of regionsBySize){const x=sx(r.x),y=sy(r.y);if(!vis(x,y,300))continue;
    const fs=Math.min(58,Math.max(21,Math.sqrt(r.area)*.052));const px=Math.max(12,Math.min(fs*s,34)),sp=px*.12;
    const m=t=>mt(t,'',FS,px)+sp*t.length;
    const ls=r.__l&&r.__lk===Math.round(Math.max(70,r.w*s*1.6)/12)?r.__l:(r.__lk=Math.round(Math.max(70,r.w*s*1.6)/12),r.__l=wrap(r.n,Math.max(70,r.w*s*1.6),m));
    const lh=px*1.08,rot=Math.round(r.ang*.7)*Math.PI/180,wMax=Math.max(...ls.map(m));
    if(!free(x,y,wMax,lh*ls.length+px*.2))continue;
    ctx.globalAlpha=rFade;
    ls.forEach((t,i)=>{const o=(i-(ls.length-1)/2)*lh;blit(curvedSprite(t,'',FS,px,.12,.3,rot,INK,HALO,Math.max(2.5,Math.round(px)*.2)),x+Math.sin(rot)*o,y+Math.cos(rot)*o,px)});
    ctx.globalAlpha=1}
  const t3=fade(s,.33,.42);
  for(const t of townsByRank){const need=t.cap?.36:(t.c>=5?.5:.82);if(s<need)continue;
    const x=sx(t.x),y=sy(t.y);if(!vis(x,y,80))continue;
    const big=t.cap?1:0,sz=big?Math.min(26,10+s*6):Math.min(17,6+s*4.5),px=big?Math.min(21,13+s*2.5):Math.min(17,11.5+s*1.6);
    if(!free(x,y-sz*.5,sz*1.1,sz*1.1,0))continue;
    ctx.globalAlpha=t.cap?t3:fade(s,need,need*1.15);drawTown(x,y,sz,big);
    const w=mt(t.n,'',FS,px),ly=y+sz*.2+px*.75;
    if(free(x,ly,w,px,1))blit(curvedSprite(t.n,'',FS,px,0,0,0,INK,HALO,3),x,ly,px);
    ctx.globalAlpha=1}
  for(const f of W.features){const min=f.c>=30?.42:f.c>=12?.62:f.c>=4?.95:1.3;if(s<min)continue;const fa=fade(s,min,min*1.15);if(fa<=0)continue;
    const x=sx(f.x),y=sy(f.y);if(!vis(x,y,120))continue;
    const sea=f.k==='sea',px=Math.min(sea?19:17,(sea?13:12)+s*1.6),w=mt(f.n,'italic ',FF,px);
    if(!free(x,y,w,px))continue;
    ctx.globalAlpha=fa*(s>2.6?.72:1);blit(curvedSprite(f.n,'italic ',FF,px,0,0,0,sea?'#2c4a55':'#4e3216',sea?'rgba(216,214,180,.75)':HALO,2.6),x,y,px);ctx.globalAlpha=1}
}
const townSprites=new Map();
function drawTown(x,y,sz,big){const q=Math.round(sz),key=big+'|'+q;let sp=townSprites.get(key);
  if(!sp){const S=q*3,cv=document.createElement('canvas');cv.width=cv.height=Math.ceil(S*devDpr);const c=cv.getContext('2d');c.scale(devDpr,devDpr);c.translate(S/2,S*.8);paintTown(c,q,big);sp={cv,S};townSprites.set(key,sp)}
  const k=sz/q,S=sp.S*k;ctx.drawImage(sp.cv,x-S/2,y-S*.8,S,S)}
function paintTown(c,sz,big){
  if(big){const w=sz*.9,h=sz*.72;c.fillStyle='#e8d9b2';c.strokeStyle='#2d1d0e';c.lineWidth=1.3;c.beginPath();const t=w/7;
    c.moveTo(-w/2,0);c.lineTo(-w/2,-h);c.lineTo(-w/2+t,-h);c.lineTo(-w/2+t,-h+t*.9);c.lineTo(-w/2+2*t,-h+t*.9);c.lineTo(-w/2+2*t,-h);c.lineTo(-w/2+3*t,-h);c.lineTo(-w/2+3*t,-h+t*.9);c.lineTo(-w/2+4*t,-h+t*.9);c.lineTo(-w/2+4*t,-h);c.lineTo(-w/2+5*t,-h);c.lineTo(-w/2+5*t,-h+t*.9);c.lineTo(-w/2+6*t,-h+t*.9);c.lineTo(-w/2+6*t,-h);c.lineTo(w/2,-h);c.lineTo(w/2,0);c.closePath();c.fill();c.stroke();
    c.fillStyle='#2d1d0e';c.beginPath();c.moveTo(-t*.8,0);c.lineTo(-t*.8,-h*.35);c.quadraticCurveTo(0,-h*.62,t*.8,-h*.35);c.lineTo(t*.8,0);c.fill();
    c.beginPath();c.moveTo(0,-h);c.lineTo(0,-h-sz*.42);c.stroke();c.fillStyle='#8c2f1c';c.beginPath();c.moveTo(0,-h-sz*.42);c.lineTo(sz*.32,-h-sz*.32);c.lineTo(0,-h-sz*.2);c.fill();
  }else{const r=sz*.42;c.fillStyle='#e8d9b2';c.strokeStyle='#2d1d0e';c.lineWidth=1.2;c.beginPath();c.moveTo(-r,0);c.lineTo(-r,-r*1.05);c.lineTo(0,-r*1.95);c.lineTo(r,-r*1.05);c.lineTo(r,0);c.closePath();c.fill();c.stroke();c.fillStyle='#2d1d0e';c.fillRect(-r*.28,-r*.72,r*.56,r*.72)}}

/* ---------- songs ---------- */
const COVERON=1.25;
let dotSprite=null;
function getDot(){if(dotSprite)return dotSprite;const S=12,cv=document.createElement('canvas');cv.width=cv.height=S*2;const c=cv.getContext('2d');c.scale(2,2);c.fillStyle='#efe3bf';c.beginPath();c.arc(S/2,S/2,5.4,0,6.3);c.fill();c.fillStyle='#3a2410';c.beginPath();c.arc(S/2,S/2,3.6,0,6.3);c.fill();return dotSprite=cv}
const fitCache=new Map();
function fitText(a,field,t,mw,fs,style){const k=a.i+field+Math.round(mw/6);let v=fitCache.get(k);if(v===undefined){v=t;if(mt(v,style,FF,fs)>mw){while(v.length>1&&mt(v+'…',style,FF,fs)>mw)v=v.slice(0,-1);v+='…'}fitCache.set(k,v)}return v}
function drawSongs(){
  const s=view.s;if(s<.62)return;
  ctx.setTransform(dpr,0,0,dpr,0,0);
  const x0=view.x-40/s,y0=view.y-40/s,x1=view.x+vw/s+40/s,y1=view.y+vh/s+40/s;
  const coverA=fade(s,COVERON,COVERON*1.28),dotA=fade(s,.62,.74)*(1-coverA);
  const vis=[];for(const a of songs)if(a.x>x0&&a.x<x1&&a.wy>y0&&a.wy<y1)vis.push(a);
  if(dotA>0){const d=getDot(),D=Math.min(8.4,4.4+s*2.2)*1.11;ctx.globalAlpha=dotA;for(const a of vis)ctx.drawImage(d,(a.x-view.x)*s-D/2,(a.wy-view.y)*s-D/2,D,D);ctx.globalAlpha=1}
  if(coverA>0&&atlasC){
    const px=COVER*s,useHi=px*devDpr>120&&vis.length<70;
    vis.sort((p,q)=>p.wy-q.wy);
    for(const a of vis){if(a===hover||a===sel)continue;drawCover(a,px,coverA,useHi)}
    if(sel&&vis.includes(sel))drawCover(sel,px*1.25,1,true,true);
    if(hover&&hover!==sel&&vis.includes(hover))drawCover(hover,px*1.25,1,true,true);
    if(px>=70){const fs=Math.min(15,10+px*.032),mw=px*1.28;
      for(const a of vis){const x=(a.x-view.x)*s,y=(a.wy-view.y)*s+px/2+3+fs*.6;
        blit(curvedSprite(fitText(a,'t',a.t,mw,fs,''),'',FF,fs,0,0,0,INK,'rgba(240,228,194,.9)',3),x,y,fs);
        if(px>=110)blit(curvedSprite(fitText(a,'a',a.a[0],mw,fs-1,'italic '),'italic ',FF,fs-1,0,0,0,'#5a3e20','rgba(240,228,194,.9)',3),x,y+fs+1,fs-1)}}
  }
  if(sel&&coverA===0&&vis.includes(sel))drawRing(sel);
}
function drawRing(a){const x=(a.x-view.x)*view.s,y=(a.wy-view.y)*view.s;ctx.strokeStyle='#8c2f1c';ctx.lineWidth=2.5;ctx.beginPath();ctx.arc(x,y,9,0,6.3);ctx.stroke()}
let shadowCv=null;
function shadowSprite(){if(shadowCv)return shadowCv;const N=96,cv=document.createElement('canvas');cv.width=cv.height=N;const c=cv.getContext('2d');const p=N/1.5;c.shadowColor='rgba(40,20,5,.55)';c.shadowBlur=p*.14;c.fillStyle='rgba(40,20,5,1)';c.fillRect((N-p)/2,(N-p)/2,p,p);return shadowCv=cv}
function drawCover(a,px,alpha,useHi,focus){
  const x=(a.x-view.x)*view.s,y=(a.wy-view.y)*view.s,b=Math.max(1.5,px*.045);ctx.globalAlpha=alpha;
  // a plain offset rectangle reads as a shadow and costs almost nothing, unlike a blur
  // same soft shadow as a live blur, but painted once and reused for every cover
  const S=px*1.5;ctx.drawImage(shadowSprite(),x-S/2,y-S/2+px*.04,S,S);
  ctx.fillStyle='#f3e8c9';ctx.fillRect(x-px/2-b,y-px/2-b,px+2*b,px+2*b);
  const im=useHi&&a.iid?hi(a.iid):null;
  if(im)ctx.drawImage(im,x-px/2,y-px/2,px,px);
  else if(a.c>=0){const n=W.atlasCols,C=W.atlasCell;ctx.drawImage(atlasC,(a.c%n)*C,((a.c/n)|0)*C,C,C,x-px/2,y-px/2,px,px)}
  else{ctx.fillStyle='#c9b58a';ctx.fillRect(x-px/2,y-px/2,px,px)}
  if(focus){ctx.lineWidth=2.4;ctx.strokeStyle=a===sel?'#8c2f1c':'#3a2410';ctx.strokeRect(x-px/2-b,y-px/2-b,px+2*b,px+2*b)}
  ctx.globalAlpha=1;
}

/* ---------- frame ---------- */
let lastT=0,lastInput=0;
function frame(now){
  requestAnimationFrame(frame);
  const dt=Math.min(120,now-lastT||16);lastT=now;
  // speed follows the clock, so a slow frame moves further instead of feeling laggy
  const k=1-Math.exp(-dt/75);let lerp=false;
  for(const p of['x','y']){const d=goal[p]-view[p];if(Math.abs(d)*view.s>.25){view[p]+=d*k;lerp=true}else view[p]=goal[p]}
  const ds=goal.s/view.s;if(Math.abs(ds-1)>.002){view.s*=Math.pow(ds,k);lerp=true}else view.s=goal.s;
  const was=moving;moving=lerp||now-lastInput<130;
  if(moving||was)dirty=true;
  if(!dirty&&!queue.length)return;
  dirty=false;tick++;
  drawWorld();
  if(!moving)pump();else{const t0=performance.now();while(queue.length&&performance.now()-t0<6){const q=queue.shift();if(!tiles.has(q.k))renderTile(q.k,q.l,q.i,q.j)}}
  drawLabels();drawSongs();
  if(queue.length)dirty=true;
  const small=view.s>fitS*1.4;if(small!==cartSmall){cartSmall=small;$('cartouche').classList.toggle('small',small)}
}
let cartSmall=false;

/* ---------- view control ---------- */
function clampView(v){const m=50/v.s,w=vw/v.s,h=vh/v.s;
  v.x=w>W.W?(W.W-w)/2:Math.max(-m,Math.min(W.W-w+m,v.x));
  v.y=h>W.H?(W.H-h)/2:Math.max(-m,Math.min(W.H-h+m,v.y));return v}
function resize(){if(!softwareGL)devDpr=Math.min(window.devicePixelRatio||1,2);dpr=devDpr;vw=innerWidth;vh=innerHeight;canvas.width=Math.round(vw*dpr);canvas.height=Math.round(vh*dpr);
  fitS=Math.min(vw/W.W,vh/W.H)*1.0;dirty=true;
  if(!touched){goal.s=view.s=fitS;goal.x=view.x=(W.W-vw/fitS)/2;goal.y=view.y=(W.H-vh/fitS)/2}
  goal=clampView(goal)}
function zoomAt(f,mx,my){touched=true;const s=Math.max(fitS*.9,Math.min(MAXS,goal.s*f));const wx=goal.x+mx/goal.s,wy=goal.y+my/goal.s;goal.s=s;goal.x=wx-mx/s;goal.y=wy-my/s;clampView(goal);hint();dirty=true}
function fly(x,y,s){touched=true;const pw=$('panel').hidden?0:$('panel').offsetWidth;goal.s=Math.max(fitS*.9,Math.min(MAXS,s));goal.x=x-(vw-pw)/2/goal.s;goal.y=y-vh/2/goal.s;clampView(goal);dirty=true;hint()}
let hinted=false;function hint(){if(!hinted){hinted=true;setTimeout(()=>$('hint').classList.add('gone'),2500)}}
function fitAll(){touched=false;goal.s=fitS;goal.x=(W.W-vw/fitS)/2;goal.y=(W.H-vh/fitS)/2;dirty=true}

/* ---------- picking ---------- */
const BIN=80,bins=new Map();songs.forEach(a=>{const k=((a.x/BIN)|0)+','+((a.wy/BIN)|0);(bins.get(k)||bins.set(k,[]).get(k)).push(a)});
function songAt(mx,my){const s=view.s;if(s<.62)return null;const wx=view.x+mx/s,wy=view.y+my/s;let best=null,bd=1e9;const gx=(wx/BIN)|0,gy=(wy/BIN)|0;
  for(let i=-1;i<=1;i++)for(let j=-1;j<=1;j++){const b=bins.get((gx+i)+','+(gy+j));if(!b)continue;for(const a of b){const d=Math.hypot(a.x-wx,a.wy-wy);if(d<bd){bd=d;best=a}}}
  const R=s>=COVERON?COVER*.6:7/s;return bd<=R?best:null}
function townAt(mx,my){const s=view.s;if(s<.36)return null;for(const t of towns){const need=t.cap?.36:(t.c>=5?.5:.82);if(s<need)continue;const x=(t.x-view.x)*s,y=(t.y-view.y)*s;if(Math.abs(mx-x)<13&&my>y-20&&my<y+6)return t}return null}
function regionAt(mx,my){const wx=view.x+mx/view.s,wy=view.y+my/view.s;if(wx<0||wy<0||wx>=W.W||wy>=W.H)return null;const row=W.regionGrid[(wy/W.H*W.grh)|0];const ch=row[(wx/W.W*W.grw)|0];return ch==='~'?null:regions[ch.charCodeAt(0)-48]}

/* ---------- pointer ---------- */
const ptrs=new Map();let drag=null,moved=0,pinch=null;
canvas.addEventListener('pointerdown',e=>{canvas.setPointerCapture(e.pointerId);ptrs.set(e.pointerId,[e.clientX,e.clientY]);moved=0;
  if(ptrs.size===1){drag={x:e.clientX,y:e.clientY,gx:goal.x,gy:goal.y};canvas.classList.add('drag')}
  if(ptrs.size===2){const[p,q]=[...ptrs.values()];pinch={d:Math.hypot(p[0]-q[0],p[1]-q[1]),s:goal.s};drag=null}});
canvas.addEventListener('pointermove',e=>{
  if(ptrs.has(e.pointerId))ptrs.set(e.pointerId,[e.clientX,e.clientY]);
  if(pinch&&ptrs.size===2){const[p,q]=[...ptrs.values()],d=Math.hypot(p[0]-q[0],p[1]-q[1]);zoomAt(pinch.s*d/pinch.d/goal.s,(p[0]+q[0])/2,(p[1]+q[1])/2);moved+=10;return}
  if(drag){const dx=e.clientX-drag.x,dy=e.clientY-drag.y;moved=Math.max(moved,Math.hypot(dx,dy));touched=true;lastInput=performance.now();goal.x=drag.gx-dx/goal.s;goal.y=drag.gy-dy/goal.s;clampView(goal);view.x=goal.x;view.y=goal.y;dirty=true;$('tip').hidden=true;hint();return}
  doHover(e.clientX,e.clientY)});
function doHover(mx,my){const a=songAt(mx,my),tip=$('tip');
  if(a!==hover){hover=a;dirty=true}
  const t=a?null:townAt(mx,my),r=(a||t)?null:regionAt(mx,my);
  canvas.classList.toggle('hit',!!(a||t||r));
  if(a){tip.innerHTML=`${esc(a.t)}<small>${esc(a.a.join(', '))}</small>`}
  else if(t){tip.innerHTML=`${esc(t.n)}<small>${t.c} songs in ${esc(regions[t.r].n)}</small>`}
  else if(r&&view.s<1.25){tip.innerHTML=`${esc(r.n)}<small>${r.c} songs</small>`}
  else{tip.hidden=true;return}
  tip.hidden=false;const w=tip.offsetWidth;tip.style.left=Math.min(vw-w-8,mx+14)+'px';tip.style.top=Math.min(vh-60,my+14)+'px'}
function endPtr(e){ptrs.delete(e.pointerId);if(ptrs.size<2)pinch=null;
  if(drag&&moved<5){const a=songAt(e.clientX,e.clientY);
    if(a)openSong(a);else{const t=townAt(e.clientX,e.clientY);if(t)openArtist(t.n);else{const r=regionAt(e.clientX,e.clientY);if(r)openRegion(r);else closePanel()}}}
  drag=null;canvas.classList.remove('drag')}
canvas.addEventListener('pointerup',endPtr);canvas.addEventListener('pointercancel',endPtr);
canvas.addEventListener('pointerleave',()=>{$('tip').hidden=true;if(hover){hover=null;dirty=true}});
canvas.addEventListener('wheel',e=>{e.preventDefault();const f=Math.exp(-e.deltaY*(e.ctrlKey?.01:.0016));zoomAt(f,e.clientX,e.clientY)},{passive:false});
canvas.addEventListener('dblclick',e=>{zoomAt(2,e.clientX,e.clientY)});
addEventListener('resize',resize);
addEventListener('keydown',e=>{if(e.target===$('q'))return;const st=140/goal.s;
  if(e.key==='+'||e.key==='=')zoomAt(1.5,vw/2,vh/2);else if(e.key==='-')zoomAt(1/1.5,vw/2,vh/2);
  else if(e.key==='ArrowLeft')goal.x-=st;else if(e.key==='ArrowRight')goal.x+=st;else if(e.key==='ArrowUp')goal.y-=st;else if(e.key==='ArrowDown')goal.y+=st;
  else if(e.key==='Escape')closePanel();else if(e.key==='/'){e.preventDefault();$('q').focus();return}else return;clampView(goal);dirty=true});
$('zin').onclick=()=>zoomAt(1.6,vw/2,vh/2);$('zout').onclick=()=>zoomAt(1/1.6,vw/2,vh/2);$('zfit').onclick=()=>{closePanel();fitAll()};
$('wander').onclick=()=>{const a=songs[Math.floor(Math.random()*songs.length)];flySong(a)};
$('pclose').onclick=closePanel;

/* ---------- panels ---------- */
function esc(t){return String(t).replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]))}
function bar(n,v){return `<div class="bar"><span>${n}</span><i><span style="width:${Math.round(v*100)}%"></span></i></div>`}
function coverURL(a){return a.iid?`assets/covers/${a.iid}.jpg`:''}
function grid(list){return `<div class="grid">${list.map(a=>`<button data-song="${a.i}" title="${esc(a.t)} — ${esc(a.a[0])}" aria-label="${esc(a.t)}">${a.iid?`<img loading="lazy" src="${coverURL(a)}" alt="">`:''}</button>`).join('')}</div>`}
function show(html){const p=$('panel');p.hidden=false;$('pbody').innerHTML=html;p.scrollTop=0}
function closePanel(){$('panel').hidden=true;$('pbody').innerHTML='';sel=null;dirty=true}
function mmss(ms){const m=Math.floor(ms/60000),s=Math.floor(ms/1000)%60;return m+':'+String(s).padStart(2,'0')}
function featureFor(g){return W.features.find(f=>f.g===g)}
function openSong(a,keep){sel=a;dirty=true;const r=regions[a.r];
  const near=songs.filter(b=>b!==a).map(b=>[Math.hypot(b.x-a.x,b.wy-a.wy),b]).sort((p,q)=>p[0]-q[0]).slice(0,8).map(p=>p[1]);
  const artistSongs=songs.filter(b=>b.a[0]===a.a[0]).length;
  show(`${a.iid?`<img class="cover" src="${coverURL(a)}" alt="Album cover of ${esc(a.al)}">`:''}
   <h2>${esc(a.t)}</h2>
   <p>${a.a.map((n,i)=>i===0?`<button class="link" data-artist="${esc(n)}">${esc(n)}</button>`:esc(n)).join(', ')}</p>
   <p class="sub">${esc(a.al)}${a.y?' · '+a.y:''} · ${mmss(a.ms)}</p>
   <p>Found in <button class="link" data-region="${a.r}">${esc(r.n)}</button></p>
   <iframe class="embed" title="Spotify player" loading="lazy" allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture" src="https://open.spotify.com/embed/track/${a.uri}?utm_source=oembed"></iframe>
   <a class="link spot" href="https://open.spotify.com/track/${a.uri}" target="_blank" rel="noopener">Open in Spotify</a>
   <h3>The Lay of This Land</h3>
   ${bar('Energy',a.e)}${bar('Mood',a.v)}${bar('Acoustic',a.ac)}${bar('Danceable',a.d)}
   <p class="sub">${a.tempo} BPM</p>
   ${a.g.length?`<h3>Genres</h3><div class="chips">${a.g.map(g=>`<span class="chip" data-genre="${esc(g)}">${esc(g)}</span>`).join('')}</div>`:''}
   <h3>Neighbours</h3>${grid(near)}`);
  if(!keep)flySong(a,true)}
function flySong(a,already){if(!already){openSong(a,true)}fly(a.x,a.wy,Math.max(view.s,goal.s,3.2))}
function openArtist(n){const l=songs.filter(b=>b.a[0]===n).sort((p,q)=>q.pop-p.pop);if(!l.length)return;const r=regions[l[0].r];sel=null;
  show(`<h2>${esc(n)}</h2><p class="sub">${l.length} song${l.length>1?'s':''}, in <button class="link" data-region="${l[0].r}">${esc(r.n)}</button></p><h3>Songs</h3><div class="list">${l.map(a=>`<button data-song="${a.i}">${esc(a.t)}</button>`).join('')}</div>`);
  const t=towns.find(t=>t.n===n);if(t)fly(t.x,t.y,Math.max(goal.s,2));else fly(l[0].x,l[0].wy,Math.max(goal.s,2))}
function openRegion(r){sel=null;const l=songs.filter(a=>a.r===regions.indexOf(r));const pick=[...l].sort((p,q)=>q.pop-p.pop).slice(0,12);
  show(`<h2>${esc(r.n)}</h2><p class="sub">${esc(r.realm)} · ${r.c} songs · ${r.y0}–${r.y1}</p><p>${esc(r.desc)}</p>
   <h3>The Lay of This Land</h3>${bar('Energy',r.e)}${bar('Mood',r.v)}${bar('Acoustic',r.ac)}${bar('Danceable',r.d)}<p class="sub">${r.tempo} BPM on average</p>
   <h3>Leading Artists</h3><div class="list">${r.topA.map(n=>`<button data-artist="${esc(n)}">${esc(n)}</button>`).join('')}</div>
   ${r.topG.length?`<h3>Local Genres</h3><div class="chips">${r.topG.map(g=>`<span class="chip" data-genre="${esc(g)}">${esc(g)}</span>`).join('')}</div>`:''}
   <h3>Best Known Here</h3>${grid(pick)}`);
  fly(r.x,r.y,Math.max(goal.s,Math.min(1.2,Math.sqrt(vw*vh*.35/r.area))))}
function openGenre(g){const f=featureFor(g),l=songs.filter(a=>a.g.includes(g)).sort((p,q)=>q.pop-p.pop);sel=null;
  show(`<h2>${esc(f?f.n:g)}</h2><p class="sub">${esc(g)} · ${l.length} song${l.length>1?'s':''}</p><h3>Songs</h3>${grid(l.slice(0,24))}<div class="list" style="margin-top:8px">${l.slice(0,40).map(a=>`<button data-song="${a.i}">${esc(a.t)} <span class="sub">${esc(a.a[0])}</span></button>`).join('')}</div>`);
  if(f)fly(f.x,f.y,Math.max(goal.s,1.3))}
$('pbody').addEventListener('click',e=>{const t=e.target.closest('[data-song],[data-artist],[data-region],[data-genre]');if(!t)return;
  if(t.dataset.song)openSong(songs[+t.dataset.song]);else if(t.dataset.artist)openArtist(t.dataset.artist);else if(t.dataset.region)openRegion(regions[+t.dataset.region]);else if(t.dataset.genre)openGenre(t.dataset.genre)});

/* ---------- search ---------- */
const norm=t=>String(t).normalize('NFD').replace(/[̀-ͯ]/g,'').toLowerCase();
const index=[];
songs.forEach(a=>index.push({k:'s',a,n:norm(a.t+' '+a.a.join(' ')+' '+a.al),t:norm(a.t)}));
[...new Set(songs.map(a=>a.a[0]))].forEach(n=>index.push({k:'a',v:n,n:norm(n),t:norm(n)}));
regions.forEach(r=>index.push({k:'r',r,n:norm(r.n),t:norm(r.n)}));
W.features.forEach(f=>index.push({k:'g',f,n:norm(f.g+' '+f.n),t:norm(f.g||f.n)}));
const q=$('q'),res=$('results');let on=0,found=[];
q.addEventListener('input',()=>{const v=norm(q.value.trim());if(v.length<2){res.hidden=true;return}
  const terms=v.split(/\s+/),score=o=>{if(!terms.every(t=>o.n.includes(t)))return-1;return(o.t===v?100:o.t.startsWith(v)?50:0)+({r:30,a:25,g:20,s:0}[o.k])};
  found=index.map(o=>[score(o),o]).filter(p=>p[0]>=0).sort((p,q)=>q[0]-p[0]).slice(0,14).map(p=>p[1]);on=0;renderRes()});
function renderRes(){if(!found.length){res.innerHTML='<div class="res"><span class="sub">Nothing charted by that name.</span></div>';res.hidden=false;return}
  res.innerHTML=found.map((o,i)=>{let img='<span class="noimg">✦</span>',a='',b='';
    if(o.k==='s'){img=o.a.iid?`<img src="${coverURL(o.a)}" alt="">`:img;a=o.a.t;b=o.a.a[0]+' · '+regions[o.a.r].n}
    if(o.k==='a'){a=o.v;b='Artist'}if(o.k==='r'){img='<span class="noimg">⚑</span>';a=o.r.n;b='Land · '+o.r.c+' songs'}if(o.k==='g'){img='<span class="noimg">≈</span>';a=o.f.n;b=o.f.g?'Genre: '+o.f.g:'A place on the map'}
    return `<button class="res ${i===on?'on':''}" data-i="${i}">${img}<span><b>${esc(a)}</b><small>${esc(b)}</small></span></button>`}).join('');res.hidden=false}
function choose(o){res.hidden=true;q.value='';q.blur();if(o.k==='s')openSong(o.a);if(o.k==='a')openArtist(o.v);if(o.k==='r')openRegion(o.r);if(o.k==='g'){if(o.f.g)openGenre(o.f.g);else fly(o.f.x,o.f.y,Math.max(goal.s,1.1))}}
res.addEventListener('click',e=>{const t=e.target.closest('[data-i]');if(t)choose(found[+t.dataset.i])});
q.addEventListener('keydown',e=>{if(res.hidden)return;if(e.key==='ArrowDown'){on=Math.min(found.length-1,on+1);renderRes();e.preventDefault()}else if(e.key==='ArrowUp'){on=Math.max(0,on-1);renderRes();e.preventDefault()}else if(e.key==='Enter'&&found[on])choose(found[on]);else if(e.key==='Escape'){res.hidden=true;q.blur()}});
document.addEventListener('pointerdown',e=>{if(!e.target.closest('.search'))res.hidden=true});

/* ---------- decorations from open water ---------- */
function setupDecor(){
  const spots=W.openSea||[];if(!spots.length)return;
  const c0=spots[0];decor.push({x:c0.x,y:c0.y,r:c0.r,draw:c=>{const R=Math.min(150,c0.r*.74);SEA.compass(c,c0.x,c0.y,R)}});
  // faint rhumb lines radiating from the compass across the sea
  const art=['serpent','ship','whale','kraken','ship','serpent','whale','kraken'];
  const caption={serpent:'Here be Bass Drops',kraken:'Here be Breakdowns',whale:'Here be Long Fades',ship:''};
  spots.slice(1).forEach((p,i)=>{const kind=art[i%art.length],k=Math.min(1.55,p.r/95);
    decor.push({x:p.x,y:p.y,r:p.r+80,draw:c=>{SEA[kind](c,p.x,p.y,k);const cap=caption[kind];if(cap){c.fillStyle='rgba(44,60,62,.85)';c.font=`italic ${15*k}px ${FF}`;c.textAlign='center';c.textBaseline='middle';c.fillText(cap,p.x,p.y+44*k)}}})});
}

/* ---------- start ---------- */
function start(){
  $('c-count').textContent=`being all ${songs.length.toLocaleString()} songs, in ${regions.length} lands`;
  setupDecor();
  overview=document.createElement('canvas');overview.width=W.W*OVS;overview.height=W.H*OVS;
  const c=overview.getContext('2d',{alpha:false});paint(c,0,0,W.W,W.H,OVS);
  resize();{const h=location.hash.slice(1).split(',').map(Number);if(h.length===3&&h.every(isFinite)){touched=true;goal.s=view.s=h[2];goal.x=view.x=h[0]-vw/2/h[2];goal.y=view.y=h[1]-vh/2/h[2];clampView(goal);view.x=goal.x;view.y=goal.y}}requestAnimationFrame(frame);
  if(location.search==='?bench')setTimeout(async()=>{let minW=1e9;const r=__map.bench();
    // live test: real animation frames while zooming and panning, as a person would
    const run=(name,sc,ms)=>new Promise(res=>{const c=regions.find(x=>x.k==='classic');goal.s=view.s=sc;goal.x=view.x=c.x-vw/2/sc;goal.y=view.y=c.y-vh/2/sc;dirty=true;
      minW=Math.min(minW,canvas.width);let n=0,worst=0,prev=performance.now();const t0=prev;(function st(){const t=performance.now();minW=Math.min(minW,canvas.width);worst=Math.max(worst,t-prev);prev=t;n++;
        const u=(t-t0)/ms;goal.s=sc*(1+.5*Math.sin(u*6.283));goal.x=c.x+Math.sin(u*12)*200-vw/2/goal.s;goal.y=c.y-vh/2/goal.s;dirty=true;
        if(t-t0<ms)requestAnimationFrame(st);else res([Math.round(n/(ms/1000)),Math.round(worst)])})()});
    for(const[n,sc]of[['liveFit',fitS*1.1],['liveMid',.8],['liveCovers',2.4]])r[n]=await run(n,sc,4000);
    r.dpr=devDpr;r.soft=softwareGL;r.minCanvasRatio=+(minW/(vw*devDpr)).toFixed(3);
    fetch('/__bench?'+encodeURIComponent(JSON.stringify(r))).catch(()=>{})},1500);
  const l=$('loading');l.style.opacity=0;setTimeout(()=>l.remove(),450);
  window.__map={view,goal:()=>goal,songs,regions,fly,openSong,openRegion,zoomAt,tiles,bench(){
    // real per-frame cost: getImageData forces the queued canvas work to finish
    const out={};const flush=()=>ctx.getImageData(0,0,1,1);
    for(const[name,sc]of[['fit',fitS],['mid',.8],['dots',1.1],['covers',2.6]]){
      const r=regions.find(r=>r.k==='classic');
      goal.s=view.s=sc;goal.x=view.x=r.x-vw/2/sc;goal.y=view.y=r.y-vh/2/sc;clampView(goal);view.x=goal.x;view.y=goal.y;
      moving=false;drawWorld();pump();while(queue.length){drawWorld();pump()}flush();
      let t=performance.now();const N=12;
      for(let i=0;i<N;i++){view.s=sc*(1+i*.01);view.x+=3/sc;moving=true;drawWorld();drawLabels();drawSongs();flush()}
      moving=false;out[name]=+((performance.now()-t)/N).toFixed(1);
    }
    return out}};
}
base.onload=()=>{const go=()=>start();(document.fonts&&document.fonts.load?Promise.all([document.fonts.load(`20px "IM Fell English"`),document.fonts.load(`italic 20px "IM Fell English"`),document.fonts.load(`20px "IM Fell English SC"`),document.fonts.load(`700 20px "Cinzel Decorative"`)]):Promise.resolve()).then(go,go)};
base.src='assets/base.jpg';atlas.src='assets/atlas.jpg';
})();
