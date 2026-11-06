/* Hand-drawn map symbols. Each draws around (x,y) = base centre, in world units, with an ink stroke. */
(function(){
const INK='rgba(58,40,22,.92)',INKL='rgba(58,40,22,.55)',LIGHT='#efe2bf',SHADE='rgba(92,64,36,.38)',TREE='#a5a776',TREE2='#88935f',PALM='#9da66a';
function rnd(seed){let s=seed*9301+49297;return()=>{s=(s*9301+49297)%233280;return s/233280}}
const G={};
G.mountain=(c,x,y,k,sd,lw)=>{const r=rnd(sd),w=24*k*(.9+r()*.3),h=25*k*(.85+r()*.4),px=x+(r()-.5)*w*.18,py=y-h;
  const mx=x+w*(.08+r()*.1),my=y-h*.45;
  c.beginPath();c.moveTo(x-w/2,y);c.lineTo(x-w*.26,y-h*.42+(r()-.5)*2);c.lineTo(x-w*.14,y-h*.56);c.lineTo(px,py);c.lineTo(x+w*.2,y-h*.5);c.lineTo(x+w/2,y);c.closePath();
  c.fillStyle=LIGHT;c.fill();
  c.beginPath();c.moveTo(px,py);c.lineTo(mx-w*.05,my+h*.12);c.lineTo(mx+w*.05,my+h*.22);c.lineTo(mx-w*.02,y);c.lineTo(x+w/2,y);c.lineTo(x+w*.2,y-h*.5);c.closePath();
  c.fillStyle=SHADE;c.fill();
  c.lineWidth=lw;c.strokeStyle=INK;c.beginPath();c.moveTo(x-w/2,y);c.lineTo(x-w*.26,y-h*.42);c.lineTo(x-w*.14,y-h*.56);c.lineTo(px,py);c.lineTo(x+w*.2,y-h*.5);c.lineTo(x+w/2,y);c.stroke();
  c.lineWidth=lw*.7;c.strokeStyle=INKL;c.beginPath();c.moveTo(px,py);c.lineTo(mx-w*.05,my+h*.12);c.lineTo(mx+w*.05,my+h*.22);c.lineTo(mx-w*.02,y-h*.06);c.stroke();
};
G.peak=(c,x,y,k,sd,lw)=>{const r=rnd(sd),w=26*k,h=36*k*(.9+r()*.35),a=x-w*.14,b=x+w*.17,h2=h*(.58+r()*.14);
  c.beginPath();c.moveTo(x-w/2,y);c.lineTo(a-w*.09,y-h*.5);c.lineTo(a,y-h);c.lineTo(x+w*.03,y-h*.62);c.lineTo(b,y-h2);c.lineTo(x+w/2,y);c.closePath();
  c.fillStyle=LIGHT;c.fill();
  c.beginPath();c.moveTo(a,y-h);c.lineTo(a+w*.1,y-h*.62);c.lineTo(a+w*.02,y-h*.45);c.lineTo(a+w*.16,y);c.lineTo(x+w/2,y);c.lineTo(b,y-h2);c.lineTo(x+w*.03,y-h*.62);c.closePath();
  c.fillStyle='rgba(72,50,30,.5)';c.fill();
  c.lineWidth=lw;c.strokeStyle=INK;c.beginPath();c.moveTo(x-w/2,y);c.lineTo(a-w*.09,y-h*.5);c.lineTo(a,y-h);c.lineTo(x+w*.03,y-h*.62);c.lineTo(b,y-h2);c.lineTo(x+w/2,y);c.stroke();
  c.fillStyle='#faf3dc';c.beginPath();c.moveTo(a,y-h);c.lineTo(a-w*.075,y-h*.78);c.lineTo(a-w*.02,y-h*.82);c.lineTo(a+w*.03,y-h*.74);c.lineTo(a+w*.07,y-h*.8);c.closePath();c.fill();c.lineWidth=lw*.6;c.stroke();
};
G.volcano=(c,x,y,k,sd,lw)=>{const r=rnd(sd),w=30*k,h=27*k,t=w*.13;
  c.beginPath();c.moveTo(x-w/2,y);c.quadraticCurveTo(x-w*.2,y-h*.5,x-t,y-h);c.lineTo(x+t,y-h);c.quadraticCurveTo(x+w*.2,y-h*.5,x+w/2,y);c.closePath();
  c.fillStyle=LIGHT;c.fill();
  c.beginPath();c.moveTo(x+t*.3,y-h);c.lineTo(x+t,y-h);c.quadraticCurveTo(x+w*.2,y-h*.5,x+w/2,y);c.lineTo(x+w*.05,y);c.closePath();c.fillStyle='rgba(60,38,24,.55)';c.fill();
  c.lineWidth=lw;c.strokeStyle=INK;c.beginPath();c.moveTo(x-w/2,y);c.quadraticCurveTo(x-w*.2,y-h*.5,x-t,y-h);c.quadraticCurveTo(x,y-h*.92,x+t,y-h);c.quadraticCurveTo(x+w*.2,y-h*.5,x+w/2,y);c.stroke();
  c.strokeStyle='rgba(140,48,24,.9)';c.lineWidth=lw*1.1;c.beginPath();c.moveTo(x-t*.2,y-h*.98);c.quadraticCurveTo(x-w*.12,y-h*.6,x-w*.05,y-h*.35);c.stroke();
  c.strokeStyle='rgba(58,40,22,.5)';c.lineWidth=lw*.8;c.beginPath();c.moveTo(x,y-h*1.02);const q=r()*5;c.bezierCurveTo(x-6*k,y-h*1.2,x+7*k,y-h*1.35,x+q*k-2*k,y-h*1.62);c.stroke();
};
G.mesa=(c,x,y,k,sd,lw)=>{const r=rnd(sd),w=21*k*(.8+r()*.5),h=9.5*k*(.75+r()*.6),t=w*.3+r()*w*.1;
  c.beginPath();c.moveTo(x-w/2,y);c.lineTo(x-t,y-h);c.lineTo(x+t,y-h);c.lineTo(x+w/2,y);c.closePath();c.fillStyle=LIGHT;c.fill();
  c.beginPath();c.moveTo(x+t*.4,y-h);c.lineTo(x+t,y-h);c.lineTo(x+w/2,y);c.lineTo(x+w*.08,y);c.closePath();c.fillStyle=SHADE;c.fill();
  c.lineWidth=lw;c.strokeStyle=INK;c.beginPath();c.moveTo(x-w/2,y);c.lineTo(x-t,y-h);c.lineTo(x+t,y-h);c.lineTo(x+w/2,y);c.stroke();
  c.lineWidth=lw*.55;c.strokeStyle=INKL;c.beginPath();for(const f of[.35,.65]){c.moveTo(x-t-(w/2-t)*f+w*.05,y-h+h*f);c.lineTo(x+t+(w/2-t)*f-w*.05,y-h+h*f+(r()-.5)*1.5)}c.stroke();
};
G.crystal=(c,x,y,k,sd,lw)=>{const r=rnd(sd);const shard=(dx,w,h,l)=>{const X=x+dx,tip=X+l;
    c.beginPath();c.moveTo(X-w/2,y);c.lineTo(X-w/2+l*.6,y-h*.74);c.lineTo(tip,y-h);c.lineTo(X+w/2+l*.6,y-h*.7);c.lineTo(X+w/2,y);c.closePath();c.fillStyle='#ece6cd';c.fill();
    c.beginPath();c.moveTo(tip,y-h);c.lineTo(X+w/2+l*.6,y-h*.7);c.lineTo(X+w/2,y);c.lineTo(X+w*.05,y);c.lineTo(X+w*.05+l*.6,y-h*.68);c.closePath();c.fillStyle='rgba(70,62,80,.48)';c.fill();
    c.lineWidth=lw;c.strokeStyle=INK;c.beginPath();c.moveTo(X-w/2,y);c.lineTo(X-w/2+l*.6,y-h*.74);c.lineTo(tip,y-h);c.lineTo(X+w/2+l*.6,y-h*.7);c.lineTo(X+w/2,y);c.stroke();
    c.lineWidth=lw*.6;c.beginPath();c.moveTo(tip,y-h);c.lineTo(X+w*.05+l*.6,y-h*.68);c.lineTo(X+w*.05,y);c.stroke()};
  shard(-8*k,7*k,20*k*(.8+r()*.4),-3*k);shard(8*k,7*k,17*k*(.8+r()*.4),3*k);shard(0,10*k,30*k*(.85+r()*.35),(r()-.5)*4*k);
};
G.hill=(c,x,y,k,sd,lw)=>{const r=rnd(sd),w=20*k*(.85+r()*.4),h=8*k*(.8+r()*.5);
  c.beginPath();c.moveTo(x-w/2,y);c.bezierCurveTo(x-w*.3,y-h*1.25,x+w*.2,y-h*1.3,x+w/2,y);c.fillStyle=LIGHT;c.fill();
  c.lineWidth=lw;c.strokeStyle=INKL;c.beginPath();c.moveTo(x-w/2,y);c.bezierCurveTo(x-w*.3,y-h*1.25,x+w*.2,y-h*1.3,x+w/2,y);c.stroke();
  c.lineWidth=lw*.6;c.beginPath();c.moveTo(x+w*.12,y-h*.72);c.lineTo(x+w*.17,y-h*.2);c.moveTo(x+w*.25,y-h*.55);c.lineTo(x+w*.3,y-h*.15);c.stroke();
};
G.conifer=(c,x,y,k,sd,lw)=>{const r=rnd(sd),h=15*k*(.8+r()*.45),w=7*k;
  c.lineWidth=lw*.8;c.strokeStyle=INK;c.beginPath();c.moveTo(x,y);c.lineTo(x,y-h*.2);c.stroke();
  c.beginPath();c.moveTo(x,y-h);c.lineTo(x+w*.33,y-h*.68);c.lineTo(x+w*.2,y-h*.68);c.lineTo(x+w*.5,y-h*.42);c.lineTo(x+w*.3,y-h*.42);c.lineTo(x+w*.65,y-h*.15);c.lineTo(x-w*.65,y-h*.15);c.lineTo(x-w*.3,y-h*.42);c.lineTo(x-w*.5,y-h*.42);c.lineTo(x-w*.2,y-h*.68);c.lineTo(x-w*.33,y-h*.68);c.closePath();
  c.fillStyle=TREE2;c.fill();c.lineWidth=lw*.75;c.stroke();
};
G.deciduous=(c,x,y,k,sd,lw)=>{const r=rnd(sd),s=(5.6+r()*2)*k,cy=y-s*1.3;
  c.lineWidth=lw*.8;c.strokeStyle=INK;c.beginPath();c.moveTo(x,y);c.lineTo(x,cy+s*.4);c.stroke();
  c.beginPath();const n=7;for(let i=0;i<=n;i++){const a=i/n*6.283,b=(i+.5)/n*6.283,rr=s*(.85+r()*.28);const px=x+Math.cos(a)*rr,py=cy+Math.sin(a)*rr*.85;
    if(i===0)c.moveTo(px,py);else c.quadraticCurveTo(x+Math.cos(a-.45)*rr*1.3,cy+Math.sin(a-.45)*rr*1.1,px,py)}
  c.closePath();c.fillStyle=TREE;c.fill();c.lineWidth=lw*.75;c.stroke();
  c.lineWidth=lw*.5;c.strokeStyle=INKL;c.beginPath();c.arc(x+s*.1,cy+s*.1,s*.5,.2,1.4);c.stroke();
};
G.palm=(c,x,y,k,sd,lw)=>{const r=rnd(sd),h=15*k*(.85+r()*.4),l=(r()-.5)*7*k,tx=x+l,ty=y-h;
  c.lineWidth=lw*1.2;c.strokeStyle=INK;c.beginPath();c.moveTo(x,y);c.quadraticCurveTo(x-l*.2,y-h*.6,tx,ty);c.stroke();
  c.strokeStyle='#5f6b35';c.lineWidth=lw*1.1;c.beginPath();
  for(const[a,b]of[[-8,-1],[-6.5,-4.5],[-3,-6],[3,-6],[6.5,-4.5],[8,-1]]){const ex=tx+a*k,ey=ty+(b+3.5)*k;c.moveTo(tx,ty);c.quadraticCurveTo(tx+a*.5*k,ty+b*k,ex,ey)}c.stroke();
};
G.willow=(c,x,y,k,sd,lw)=>{const r=rnd(sd),s=7.5*k;
  c.lineWidth=lw;c.strokeStyle=INK;c.beginPath();c.moveTo(x,y);c.lineTo(x,y-s*1.2);c.stroke();
  c.fillStyle=TREE2;c.beginPath();c.ellipse(x,y-s*1.5,s*1.05,s*.62,0,3.14,0);c.quadraticCurveTo(x,y-s*1.2,x-s*1.05,y-s*1.5);c.fill();
  c.lineWidth=lw*.7;c.beginPath();for(let i=-3;i<=3;i++){const X=x+i*s*.33;c.moveTo(X,y-s*1.55-(3-Math.abs(i))*s*.1);c.quadraticCurveTo(X+i*.5,y-s*.9,X+i*.4,y-s*(.2+r()*.4))}c.stroke();
};
G.mushroom=(c,x,y,k,sd,lw)=>{const r=rnd(sd),h=11*k*(.7+r()*.8),w=7.5*k*(.8+r()*.5);
  c.fillStyle='#e8dcb8';c.beginPath();c.moveTo(x-w*.18,y);c.quadraticCurveTo(x-w*.12,y-h*.5,x-w*.14,y-h);c.lineTo(x+w*.14,y-h);c.quadraticCurveTo(x+w*.12,y-h*.5,x+w*.18,y);c.closePath();c.fill();c.lineWidth=lw*.7;c.strokeStyle=INK;c.stroke();
  c.fillStyle='#b1623f';c.beginPath();c.moveTo(x-w*.75,y-h*.95);c.bezierCurveTo(x-w*.7,y-h*1.65,x+w*.7,y-h*1.65,x+w*.75,y-h*.95);c.quadraticCurveTo(x,y-h*.8,x-w*.75,y-h*.95);c.fill();c.stroke();
  c.fillStyle='#f3e8c8';for(const[a,b]of[[-.32,1.2],[.08,1.32],[.4,1.12]]){c.beginPath();c.arc(x+a*w,y-h*b,w*.09,0,6.3);c.fill()}
};
G.marsh=(c,x,y,k,sd,lw)=>{const r=rnd(sd);
  c.lineWidth=lw*.65;c.strokeStyle='rgba(52,70,62,.75)';c.beginPath();
  for(let i=0;i<3;i++){const X=x+(i-1)*3.4*k;c.moveTo(X,y);c.lineTo(X+(i-1)*1.8*k,y-(5+r()*3)*k)}
  c.moveTo(x-7*k,y+1.5*k);c.quadraticCurveTo(x-3.5*k,y+.3*k,x,y+1.5*k);c.quadraticCurveTo(x+3.5*k,y+2.7*k,x+7*k,y+1.5*k);c.stroke();
};
G.dune=(c,x,y,k,sd,lw)=>{const r=rnd(sd),w=20*k*(.8+r()*.5),h=6*k;
  c.lineWidth=lw*.8;c.strokeStyle=INKL;c.beginPath();c.moveTo(x-w/2,y);c.quadraticCurveTo(x-w*.1,y-h*1.6,x+w*.2,y-h*.65);c.quadraticCurveTo(x+w*.35,y-h*.3,x+w/2,y);c.stroke();
  c.lineWidth=lw*.5;c.beginPath();c.moveTo(x+w*.2,y-h*.65);c.quadraticCurveTo(x+w*.12,y-h*.2,x+w*.18,y);c.stroke();
};
G.wheat=(c,x,y,k,sd,lw)=>{const r=rnd(sd);c.lineWidth=lw*.55;c.strokeStyle='rgba(120,86,28,.8)';c.beginPath();
  for(let i=0;i<4;i++){const X=x+(i-1.5)*2.7*k+(r()-.5)*k,h=(5+r()*2.8)*k;c.moveTo(X,y);c.lineTo(X+.5*k,y-h);c.moveTo(X+.5*k,y-h);c.lineTo(X+1.4*k,y-h+1.3*k);c.moveTo(X+.5*k,y-h);c.lineTo(X-.5*k,y-h+1.3*k)}c.stroke();
};
G.flower=(c,x,y,k,sd,lw)=>{const r=rnd(sd),cols=['#a5503c','#b99232','#7a6a9a','#c47c52'];
  for(let i=0;i<3;i++){const X=x+(i-1)*4*k+(r()-.5)*2*k,h=(3.4+r()*3)*k;c.lineWidth=lw*.45;c.strokeStyle='rgba(70,84,44,.8)';c.beginPath();c.moveTo(X,y);c.lineTo(X,y-h);c.stroke();c.fillStyle=cols[Math.floor(r()*4)];c.beginPath();c.arc(X,y-h,1.15*k,0,6.3);c.fill()}
};
G.scrub=(c,x,y,k,sd,lw)=>{const r=rnd(sd);c.lineWidth=lw*.55;c.strokeStyle=INKL;c.beginPath();
  for(let i=0;i<3;i++){const X=x+(r()-.5)*11*k,Y=y+(r()-.5)*4*k;c.moveTo(X-1.7*k,Y);c.lineTo(X,Y-3*k);c.lineTo(X+1.7*k,Y);c.moveTo(X,Y);c.lineTo(X,Y-3*k)}c.stroke();
};

/* Sea creatures, ships and the compass. (x,y) = centre, k = scale */
const S={};
const inkS='rgba(50,58,56,.8)';
S.serpent=(c,x,y,k)=>{c.save();c.translate(x,y);c.scale(k,k);c.strokeStyle=inkS;c.fillStyle='rgba(120,140,120,.5)';c.lineWidth=2;c.lineJoin='round';
  for(const[a,w,h]of[[-44,26,22],[-12,30,30],[22,24,20]]){c.beginPath();c.moveTo(a-w/2,8);c.bezierCurveTo(a-w/2,8-h*1.3,a+w/2,8-h*1.3,a+w/2,8);c.bezierCurveTo(a+w/3,8-h*.55,a-w/3,8-h*.55,a-w/2,8);c.fill();c.stroke()}
  c.beginPath();c.moveTo(50,8);c.bezierCurveTo(50,-12,60,-34,72,-34);c.quadraticCurveTo(88,-36,92,-26);c.lineTo(78,-24);c.quadraticCurveTo(70,-20,68,-8);c.quadraticCurveTo(65,0,64,8);c.closePath();c.fill();c.stroke();
  c.beginPath();c.moveTo(70,-34);c.lineTo(64,-44);c.lineTo(74,-38);c.lineTo(70,-48);c.lineTo(80,-37);c.stroke();
  c.fillStyle=inkS;c.beginPath();c.arc(78,-30,1.6,0,6.3);c.fill();
  c.beginPath();c.moveTo(-58,8);c.quadraticCurveTo(-74,2,-82,-8);c.quadraticCurveTo(-78,6,-70,10);c.stroke();
  c.lineWidth=1.2;c.beginPath();for(let i=-5;i<6;i++){c.moveTo(i*18-7,14+(i%2)*2);c.quadraticCurveTo(i*18,11+(i%2)*2,i*18+7,14+(i%2)*2)}c.stroke();c.restore()};
S.whale=(c,x,y,k)=>{c.save();c.translate(x,y);c.scale(k,k);c.strokeStyle=inkS;c.fillStyle='rgba(120,140,130,.5)';c.lineWidth=2;
  c.beginPath();c.moveTo(-60,4);c.bezierCurveTo(-50,-30,20,-34,48,-8);c.quadraticCurveTo(56,-2,70,-16);c.quadraticCurveTo(66,-2,80,-2);c.quadraticCurveTo(66,4,60,6);c.quadraticCurveTo(10,26,-44,16);c.quadraticCurveTo(-60,14,-60,4);c.closePath();c.fill();c.stroke();
  c.lineWidth=1.2;c.beginPath();c.moveTo(-58,6);c.quadraticCurveTo(-30,14,-8,10);for(let i=0;i<5;i++){c.moveTo(-44+i*8,11);c.lineTo(-42+i*8,17)}c.stroke();
  c.fillStyle=inkS;c.beginPath();c.arc(-42,-4,1.8,0,6.3);c.fill();
  c.lineWidth=1.6;c.beginPath();c.moveTo(-26,-26);c.quadraticCurveTo(-30,-42,-40,-44);c.moveTo(-26,-26);c.quadraticCurveTo(-22,-42,-12,-46);c.moveTo(-26,-26);c.lineTo(-26,-46);c.stroke();
  c.lineWidth=1.2;c.beginPath();for(let i=-4;i<5;i++){c.moveTo(i*20-8,28);c.quadraticCurveTo(i*20,24,i*20+8,28)}c.stroke();c.restore()};
S.kraken=(c,x,y,k)=>{c.save();c.translate(x,y);c.scale(k,k);c.strokeStyle=inkS;c.lineWidth=2;c.fillStyle='rgba(135,110,100,.5)';
  for(const[a,b,e,f]of[[-22,0,-70,-46],[-14,4,-44,-62],[14,4,44,-62],[22,0,70,-46],[-8,4,-22,-44],[8,4,24,-46]]){c.lineWidth=5;c.strokeStyle='rgba(50,58,56,.7)';c.beginPath();c.moveTo(a,b+6);c.bezierCurveTo(a*2,b+22,e+(a>0?-22:22),f+24,e,f);c.stroke();}
  c.lineWidth=2;c.strokeStyle=inkS;c.beginPath();c.moveTo(-26,12);c.bezierCurveTo(-30,-34,30,-34,26,12);c.quadraticCurveTo(0,20,-26,12);c.fill();c.stroke();
  c.fillStyle=inkS;c.beginPath();c.arc(-9,-6,2.2,0,6.3);c.arc(9,-6,2.2,0,6.3);c.fill();
  c.lineWidth=1.2;c.beginPath();for(let i=-4;i<5;i++){c.moveTo(i*20-8,20);c.quadraticCurveTo(i*20,16,i*20+8,20)}c.stroke();c.restore()};
S.ship=(c,x,y,k)=>{c.save();c.translate(x,y);c.scale(k,k);c.strokeStyle=inkS;c.lineWidth=1.8;c.lineJoin='round';
  c.fillStyle='rgba(120,88,50,.65)';c.beginPath();c.moveTo(-44,-4);c.lineTo(-38,-14);c.lineTo(-24,-12);c.lineTo(-24,-4);c.lineTo(36,-6);c.lineTo(50,-14);c.quadraticCurveTo(44,14,28,14);c.lineTo(-26,14);c.quadraticCurveTo(-40,12,-44,-4);c.fill();c.stroke();
  c.beginPath();for(const X of[-16,8,28]){c.moveTo(X,-6);c.lineTo(X,-58+Math.abs(X-8)*.4)}c.stroke();
  c.fillStyle='#efe5c6';for(const[X,h]of[[-16,34],[8,42],[28,28]]){c.beginPath();c.moveTo(X-13,-48+(42-h));c.quadraticCurveTo(X+9,-34+(42-h)/1.6,X-13,-16);c.lineTo(X+13,-16);c.quadraticCurveTo(X+19,-34+(42-h)/1.6,X+13,-48+(42-h));c.closePath();c.fill();c.stroke()}
  c.lineWidth=1.2;c.beginPath();for(let i=-4;i<5;i++){c.moveTo(i*20-8,20);c.quadraticCurveTo(i*20,16,i*20+8,20)}c.stroke();c.restore()};
S.compass=(c,x,y,R)=>{c.save();c.translate(x,y);const ink='rgba(58,40,22,.9)';c.strokeStyle=ink;
  c.lineWidth=R*.012;for(const r of[1,.93,.42]){c.beginPath();c.arc(0,0,R*r,0,6.3);c.stroke()}
  c.lineWidth=R*.006;for(let i=0;i<64;i++){const a=i/64*6.283,l=i%8===0?.86:(i%4===0?.89:.91);c.beginPath();c.moveTo(Math.cos(a)*R*l,Math.sin(a)*R*l);c.lineTo(Math.cos(a)*R*.93,Math.sin(a)*R*.93);c.stroke()}
  const star=(n,rot,ro,ri,dark,light)=>{for(let i=0;i<n;i++){const a=rot+i/n*6.283,d=3.1416/n;
      c.beginPath();c.moveTo(0,0);c.lineTo(Math.cos(a)*ro*R,Math.sin(a)*ro*R);c.lineTo(Math.cos(a+d)*ri*R,Math.sin(a+d)*ri*R);c.closePath();c.fillStyle=dark;c.fill();
      c.beginPath();c.moveTo(0,0);c.lineTo(Math.cos(a)*ro*R,Math.sin(a)*ro*R);c.lineTo(Math.cos(a-d)*ri*R,Math.sin(a-d)*ri*R);c.closePath();c.fillStyle=light;c.fill();c.lineWidth=R*.007;c.stroke()}};
  star(8,.3927,.62,.13,'#a98c5c','#e9dcb6');star(4,.7854,.74,.15,'#6e5230','#e4d5ac');star(4,-1.5708,.9,.17,'#3f2c18','#efe4c2');
  c.beginPath();c.arc(0,0,R*.05,0,6.3);c.fillStyle='#8c2f1c';c.fill();c.stroke();
  c.fillStyle=ink;c.textAlign='center';c.textBaseline='middle';c.font=`${R*.2}px "IM Fell English SC",Georgia,serif`;
  c.fillText('N',0,-R*1.14);c.fillText('S',0,R*1.14);c.fillText('E',R*1.14,0);c.fillText('W',-R*1.14,0);c.restore()};
window.GLYPH=G;window.SEAART=S;
})();
