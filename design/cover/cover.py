"""Terra Firma workshop cover (preview.png source). Rollenhagen plate 81 (1613) as a Qud-palette tritone seal,
title warped into the four spandrels. Inputs in src/: spade_src1024.png (the roundel cropped from the 500ppi archive.org scan of plate 81),
spade_ringmask1024.png (per-angle fit of the engraved ring's outer line). Font in fonts/. Renders land in out/.
Run: python cover.py [preset ...]   (no args = every preset)"""
import numpy as np, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import os
HERE=os.path.dirname(os.path.abspath(__file__))
SRC=os.path.join(HERE,"src"); OUT=os.path.join(HERE,"out"); os.makedirs(OUT,exist_ok=True)
FONT=os.path.join(HERE,"fonts","Siegfried.ttf")
Q=dict(k="#0f3b3a",K="#155352",y="#b1c9c3",Y="#ffffff",w="#98875f",W="#cfc041",c="#40a4b9",C="#77bfcf")
def hx(h): return np.array([int(h[i:i+2],16) for i in (1,3,5)],dtype=float)
MID=(hx(Q["K"])+hx(Q["y"]))/2
# ---------- knobs ----------
SS=4; N=512*SS; C=N/2
FIELD="k"; SEAL_BASE="K"; LETTER_FILL="K"; EDGE="y"; EDGE_PX=1.25; D=430; GAP=7
SPOT=(256,200,95,170)      # white-highlight ellipse over the spade, in 512 space
EDGE_BREAKS=0.0            # 0 = solid outline; 0.28 = engraved breaks (fraction of the line that drops out)
BREAK_FLOOR=0.15           # how much of the line survives inside a break (0 = gap, 1 = no break)
BREAK_SEED=7
LIGHT=0.0                  # directional light from the upper left: 0 = off, 0.25 = strong
LIGHT_FIELD=False          # also light the black field
FLATTEN=False              # divide out the scan's uneven paper brightness before mapping
TINT=0.0                   # ink hue drift: 0 = flat grey; 1 = white at upper-left through grey to shadow-cyan at lower-right
TINT_LETTERS=True          # apply the same drift to the letter outlines
SHADOW=None                # cast shadow of the spade and orb: (dx, dy, strength) in 512 px, offset down-right; None = off
SHADOW_COLOUR="C"          # ink colour inside the cast shadow
TINT_STEPS=None            # None = smooth drift; or a list of (upper bound of t, colour code), e.g. [(0.25,'Y'),(0.5,'y'),(0.75,'C'),(1.0,'c')]
# ---------- seal ----------
src=Image.open(os.path.join(SRC,"spade_src1024.png")).convert("L"); mask1024=Image.open(os.path.join(SRC,"spade_ringmask1024.png"))
def flatten(im):
    """paper estimate = local maximum (ink removed) blurred wide; divide it out so the paper is one level everywhere"""
    paper=np.array(im.filter(ImageFilter.MaxFilter(31)).filter(ImageFilter.GaussianBlur(45)),dtype=float)
    a=np.array(im,dtype=float); flat=a/np.maximum(paper,1)*paper.mean()
    return Image.fromarray(flat.clip(0,255).astype(np.uint8))
bb=mask1024.getbbox(); side=max(bb[2]-bb[0],bb[3]-bb[1]); ccx,ccy=(bb[0]+bb[2])//2,(bb[1]+bb[3])//2; R=side//2+2
box=(ccx-R,ccy-R,ccx+R,ccy+R); srcc=src.crop(box); maskc=mask1024.crop(box)
def prepared_src(): return flatten(srcc) if FLATTEN else srcc
def ramp(t,stops):
    out=np.zeros(t.shape+(3,))
    for (p0,c0),(p1,c1) in zip(stops,stops[1:]):
        mm=(t>=p0)&(t<=p1); u=((t-p0)/(p1-p0))[mm][:,None]; out[mm]=c0*(1-u)+c1*u
    return out.clip(0,255)
def _bayer(n=8):
    m=np.array([[0]])
    while m.shape[0]<n: m=np.block([[4*m,4*m+2],[4*m+3,4*m+1]])
    return (m+0.5)/m.size
def stepped(t, steps):
    y=hx(Q["y"]); tt=t[...,0]; cols=[y+(hx(Q[st[1]])-y)*(st[2] if len(st)>2 else 1.0) for st in steps]; his=[st[0] for st in steps]
    idx=np.zeros(tt.shape,int)
    for i,hi in enumerate(his[:-1]): idx[tt>hi]=i+1
    out=np.stack(cols)[idx]
    if SEAM_SOFT>0:
        h,w=tt.shape; cell=max(1,h//512)                       # one dither cell = one final pixel
        B=_bayer(8); thr=B[(np.arange(h)[:,None]//cell)%8,(np.arange(w)[None,:]//cell)%8]
        for i,hi in enumerate(his[:-1]):
            f=np.clip((tt-(hi-SEAM_SOFT/2))/SEAM_SOFT,0,1)      # 0 = fully band i, 1 = fully band i+1
            zone=(f>0)&(f<1)
            if SEAM_DITHER: pick=(f>thr)&zone; out[pick]=cols[i+1]; out[zone&~pick]=cols[i]
            else: out[zone]=cols[i][None,:]*(1-f[zone][:,None])+cols[i+1][None,:]*f[zone][:,None]
    return out
LETTER_STEPS=None          # separate band list for the title outlines; None = same as TINT_STEPS
SEAM_SOFT=0.0              # width (in t, 0..1 along the diagonal) of the transition at each band seam; 0 = hard
SEAM_DITHER=True           # stipple the transition with an ordered pattern at final-pixel scale instead of blending
def tint_top(t):
    """t: 0 at upper-left .. 1 at lower-right. white -> grey -> shadow cyan, centred on grey at t=0.5"""
    Y,y,c=hx(Q["Y"]),hx(Q["y"]),hx(Q["c"]); t=np.asarray(t)[...,None]
    if TINT_STEPS:
        return stepped(t, TINT_STEPS)
    return np.where(t<0.5, Y+(y-Y)*(t/0.5), y+(c-y)*((t-0.5)/0.5))
def seal(size):
    im=prepared_src().resize((size*2,size*2),Image.LANCZOS).filter(ImageFilter.UnsharpMask(1.2,120,2)).resize((size,size),Image.LANCZOS)
    g=1-np.array(im,dtype=float)/255
    yy,xx=np.mgrid[0:size,0:size]; sc=size/512; cx,cy,rx,ry=SPOT
    w=np.clip(1-np.hypot((xx-cx*sc)/(rx*sc),(yy-cy*sc)/(ry*sc))**1.8,0,1)
    b=hx(Q[SEAL_BASE]); grey=ramp(g,[(0,b),(0.60,MID),(1,hx(Q["y"]))]); hot=ramp(g,[(0,b),(0.60,MID),(0.88,hx(Q["y"])),(1,hx(Q["Y"]))])
    rgb=grey*(1-w[:,:,None])+hot*w[:,:,None]
    if SHADOW:
        dx,dy,st=SHADOW; sc=size/512
        sil=((g>0.45)&(w>0.35)).astype(np.uint8)*255                        # spade + orb, from ink inside the spotlight
        sil=Image.fromarray(sil).filter(ImageFilter.MaxFilter(int(6*sc)|1)).filter(ImageFilter.GaussianBlur(2*sc))
        sh=np.zeros((size,size)); ox,oy=int(dx*sc),int(dy*sc); src_=np.array(sil,dtype=float)/255
        sh[oy:,ox:]=src_[:size-oy,:size-ox]
        sh=sh*(1-w)                                                            # the shadow never falls on the thing casting it
        ink=np.clip((g-0.60)/0.40,0,1)
        rgb=rgb+(hx(Q[SHADOW_COLOUR])-hx(Q["y"]))*(ink*sh*st)[:,:,None]
    if TINT>0:
        t=(xx+yy)/(2*size); top=tint_top(t)                      # per-pixel colour the ink drifts toward
        ink=np.clip((g-0.60)/0.40,0,1)[:,:,None]                 # how much of each pixel is 'top ink' rather than base/mid
        rgb=rgb+(top-hx(Q["y"]))*ink*TINT
    if LIGHT>0:
        t=((xx+yy)/(2*size))  # 0 at upper-left, 1 at lower-right
        lf=1+LIGHT*(1-2*t)     # +LIGHT at upper-left, -LIGHT at lower-right
        base_=b[None,None,:]; rgb=base_+(rgb-base_)*lf[:,:,None]
    return Image.fromarray(rgb.clip(0,255).astype(np.uint8)), maskc.resize((size,size),Image.LANCZOS)
# ---------- ring edge per angle (text hugs the real engraving) ----------
mk=np.array(mask1024)>127; ang=np.linspace(-np.pi,np.pi,1440,endpoint=False); edge=[]
for th in ang:
    rs=np.arange(int(R*0.9),int(R*1.05)); xs=(ccx+rs*np.cos(th)).astype(int); ys=(ccy+rs*np.sin(th)).astype(int)
    ok=(xs>=0)&(xs<mk.shape[1])&(ys>=0)&(ys<mk.shape[0]); v=np.zeros(len(rs),bool); v[ok]=mk[ys[ok],xs[ok]]
    edge.append(rs[v].max() if v.any() else R)
edge=np.array(edge,dtype=float)/R
def ring_r(th,Rr): return np.interp(th,ang,edge,period=2*np.pi)*Rr
def frame_r(th): return C/np.maximum(np.abs(np.cos(th)),np.abs(np.sin(th)))
# ---------- letters ----------
def bitmap(word,h=400):
    f=ImageFont.truetype(FONT,h); d=ImageDraw.Draw(Image.new("L",(1,1))); bb=d.textbbox((0,0),word,font=f)
    tw,th_=bb[2]-bb[0]+8,bb[3]-bb[1]+8; im=Image.new("L",(tw,th_),0); ImageDraw.Draw(im).text((4-bb[0],4-bb[1]),word,font=f,fill=255)
    return np.array(im,dtype=float)/255
def bilinear(arr,x,y):
    h,w=arr.shape; x=np.clip(x,0,w-1.001); y=np.clip(y,0,h-1.001); x0=x.astype(int); y0=y.astype(int); fx=x-x0; fy=y-y0
    return arr[y0,x0]*(1-fx)*(1-fy)+arr[y0,x0+1]*fx*(1-fy)+arr[y0+1,x0]*(1-fx)*fy+arr[y0+1,x0+1]*fx*fy
YY,XX=np.mgrid[0:N,0:N]; RR=np.hypot(XX-C,YY-C); TH=np.arctan2(YY-C,XX-C)
def warp(word,th0,span,Rr,baseline_in):
    fill=bitmap(word); th_,tw=fill.shape
    dth=(TH-th0+np.pi)%(2*np.pi)-np.pi; rin=ring_r(TH,Rr)+GAP*SS; rout=frame_r(TH)-GAP*SS
    v=(RR-rin)/(rout-rin); u=dth/span+0.5
    if not baseline_in: u=1-u; v=1-v
    inside=(u>=0)&(u<1)&(v>=0)&(v<1)
    return np.where(inside,bilinear(fill,u*(tw-1),(1-v)*(th_-1)),0)
# words: (text, centre angle in degrees, baseline toward centre?). bottom half is DIASPORA / OF / THE STONE with OF at the bottom centre
WORDS=[("TERRA",225,True),("FIRMA",315,True),("DIASPORA",140,False),("OF",90,False),("THE STONE",40,False)]
WORD_GAP=9                      # degrees of clear arc between neighbouring bottom words
aspect={w:(lambda b:b.shape[1]/b.shape[0])(bitmap(w)) for w,_,_ in WORDS}
spans={w:58/aspect["TERRA"]*aspect[w] for w in aspect}
# bottom three must fit between 5 and 175 degrees with gaps; scale them together if they don't
avail=170-2*WORD_GAP; tot=spans["DIASPORA"]+spans["OF"]+spans["THE STONE"]
if tot>avail:
    k=avail/tot
    for w in ("DIASPORA","OF","THE STONE"): spans[w]*=k
# re-centre DIASPORA and THE STONE so each sits WORD_GAP away from OF
centres={"TERRA":225,"FIRMA":315,"OF":90}
centres["DIASPORA"]=90+spans["OF"]/2+WORD_GAP+spans["DIASPORA"]/2
centres["THE STONE"]=90-spans["OF"]/2-WORD_GAP-spans["THE STONE"]/2
WORDS=[(w,centres[w],bi) for w,_,bi in WORDS]
# ---------- compose ----------
def build():
    cv=np.full((N,N,3),hx(Q[FIELD]))
    if LIGHT>0 and LIGHT_FIELD:
        t=(XX+YY)/(2*N); cv=cv*(1+0.6*LIGHT*(1-2*t))[:,:,None]
    sim,smask=seal(D*SS); sa=np.array(smask,dtype=float)[:,:,None]/255; srgb=np.array(sim,dtype=float)
    o=(N-D*SS)//2; reg=cv[o:o+D*SS,o:o+D*SS]; cv[o:o+D*SS,o:o+D*SS]=reg*(1-sa)+srgb*sa
    A=np.zeros((N,N))
    for w,deg,bi in WORDS: A=np.maximum(A,warp(w,np.radians(deg),np.radians(spans[w]),D*SS/2,bi))
    kk=2*int(round(EDGE_PX*SS))+1
    dil=np.array(Image.fromarray((A*255).astype(np.uint8)).filter(ImageFilter.MaxFilter(kk)),dtype=float)/255; ea=np.clip(dil-A,0,1)
    if EDGE_BREAKS>0:
        rng=np.random.default_rng(BREAK_SEED); nz=np.array(Image.fromarray((rng.random((N//8,N//8))*255).astype(np.uint8)).resize((N,N),Image.BILINEAR),dtype=float)/255
        ea=ea*((nz>EDGE_BREAKS).astype(float)*(1-BREAK_FLOOR)+BREAK_FLOOR)
    edge_col=hx(Q[EDGE])[None,None,:]
    if TINT>0 and TINT_LETTERS:
        tt=(XX+YY)/(2*N); top=stepped(tt[...,None],LETTER_STEPS) if LETTER_STEPS else tint_top(tt)
        edge_col=hx(Q[EDGE])+(top-hx(Q["y"]))*TINT
    cv=cv*(1-ea[:,:,None])+edge_col*ea[:,:,None]
    cv=cv*(1-A[:,:,None])+hx(Q[LETTER_FILL])*A[:,:,None]
    return Image.fromarray(cv.clip(0,255).astype(np.uint8))
pal=Image.new("P",(1,1)); cols=[hx(Q["k"]),hx(Q["K"]),MID,hx(Q["y"]),hx(Q["Y"]),hx(Q["c"]),hx(Q["C"]),(hx(Q["y"])+hx(Q["Y"]))/2,(hx(Q["y"])+hx(Q["c"]))/2]; pal.putpalette([int(c) for col in cols for c in col]+[0]*(768-27))
def pix(im,n,dither,palette=None):
    pl=pal
    if palette:
        pl=Image.new("P",(1,1)); cc=[hx(Q[c]) if isinstance(c,str) else c for c in palette]; pl.putpalette([int(v) for col in cc for v in col]+[0]*(768-3*len(cc)))
    return im.resize((n,n),Image.LANCZOS).quantize(palette=pl,dither=dither).convert("RGB").resize((512,512),Image.NEAREST)
# ---------- presets ----------
PRESETS={
 # bookmarked 2026-10-14: black field, hollow Siegfried, four stepped inks lit from the upper left, shadow ink full strength,
 # all seams stippled wide, posterised hard at 512. open idea: raise the cyan band so it starts at the base of the orb and wings.
 "bookmark_A1_stipple":dict(FIELD="k",SEAL_BASE="k",LETTER_FILL="k",EDGE_PX=1.25,EDGE_BREAKS=0.0,LIGHT=0.0,FLATTEN=True,D=430,
    TINT=1.0,TINT_LETTERS=True,LETTER_STEPS=None,SHADOW=None,SEAM_SOFT=0.12,SEAM_DITHER=True,
    TINT_STEPS=[(0.30,"Y",0.5),(0.52,"y",0.5),(0.80,"C",0.5),(1.0,"c",1.0)]),
 "bookmark_A1_stipple_band_higher":dict(FIELD="k",SEAL_BASE="k",LETTER_FILL="k",EDGE_PX=1.25,EDGE_BREAKS=0.0,LIGHT=0.0,FLATTEN=True,D=430,
    TINT=1.0,TINT_LETTERS=True,LETTER_STEPS=None,SHADOW=None,SEAM_SOFT=0.12,SEAM_DITHER=True,
    TINT_STEPS=[(0.30,"Y",0.5),(0.50,"y",0.5),(0.70,"C",0.5),(1.0,"c",1.0)]),
}
def apply(preset):
    for k,v in PRESETS[preset].items(): globals()[k]=v
def render_preset(preset, pixel=True):
    apply(preset); m=build(); sm=m.resize((512,512),Image.LANCZOS)
    if not pixel: return sm
    y=hx(Q["y"]); p=["k","K",MID]+[y+(hx(Q[c])-y)*k for _,c,k in TINT_STEPS]
    return pix(sm,512,Image.NONE,p)

if __name__=="__main__":
    import sys
    names=sys.argv[1:] or list(PRESETS)
    for n in names:
        render_preset(n).save(os.path.join(OUT,f"{n}_512.png"))
        apply(n); build().resize((1024,1024),Image.LANCZOS).save(os.path.join(OUT,f"{n}_smooth_1024.png"))
        print("rendered",n)
