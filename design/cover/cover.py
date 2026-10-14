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
FIELD_TEXTURE=None         # None, "sky" (the plate's own sky hatching tiled over the field), "hatch" (synthetic engraver's hatch)
FIELD_TEX_STRENGTH=0.5     # how far the texture lines lift from the field colour toward the mid tone
FIELD_TEX_IN_LETTERS=True  # texture shows through the hollow letters too
CATALOGUE=None             # catalogue line on the left edge, e.g. "TF {version}"; {version} is read from TerraFirma/manifest.json at render time
FIELD_TEX_WEAR=0.0         # plate wear: the field hatch thins and breaks toward the frame edge (0 = off, 1 = heavy)
SHADOW_MID=None            # shadow pass on the halftone: None, "all" (every hatched midtone goes to SHADOW_MID_COLOUR), "split" (only on the shadow side of the light), "blend" (fades in toward the shadow side)
SHADOW_MID_COLOUR="c"
SHADOW_MID_SPLIT=0.52       # where the split happens along the light-to-shadow parameter
SHADOW_BLEND=(0.25,0.85)    # where the blended shadow pass starts and reaches full, along the light-to-shadow parameter (0 = lit corner, 1 = shadow corner)
SHADOW_LINES_BLEND=None    # separate (start,end) for the seal's heavy lines; None = same as SHADOW_BLEND
SHADOW_RAMP=(0.60,1.0)      # in the shadow pass: ink density where the halftone colour peaks, and where the line tone is reached (lower = more of the plate counts as line)
SHADOW_ON_LETTERS=0.0      # how far the title outlines follow the shadow pass toward SHADOW_MID_COLOUR (0..1)
SHADOW_ON_LINES=0.0        # how far the seal's heavy lines (ring letters, contours) follow it too (0..1)
TINT_GEOM="diagonal"       # band geometry: "diagonal", or ("shadow", cx, cy) = arcs concentric about a point (0..1 canvas coords) with white farthest from it, or ("light", cx, cy) = arcs concentric about the light
SEAM_SOFT=0.0              # width (in t, 0..1 along the diagonal) of the transition at each band seam; 0 = hard
SEAM_DITHER=True           # stipple the transition with an ordered pattern at final-pixel scale instead of blending
def tparam(xc, yc):
    """band parameter 0..1 from canvas-normalised coords: 0 = lit (white), 1 = shadow"""
    if TINT_GEOM=="diagonal": return (xc+yc)/2
    kind,cx,cy=TINT_GEOM; d=np.hypot(xc-cx,yc-cy)
    far=max(np.hypot(cx-a,cy-b) for a in (0,1) for b in (0,1))
    return d/far if kind=="light" else 1-d/far
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
    if SHADOW_MID:
        sc_=hx(Q[SHADOW_MID_COLOUR]); m0,m1=SHADOW_RAMP; gg=np.clip(g/m1,0,1) if m1<1 else g
        grey2=ramp(gg,[(0,b),(m0,sc_),(1,hx(Q["y"]))]); hot2=ramp(gg,[(0,b),(m0,sc_),(0.88,hx(Q["y"])),(1,hx(Q["Y"]))])
        rgb2=grey2*(1-w[:,:,None])+hot2*w[:,:,None]
        xc=(xx/size*D+(512-D)/2)/512; yc=(yy/size*D+(512-D)/2)/512; t=tparam(xc,yc)
        wm={"all":np.ones_like(t),"split":(t>SHADOW_MID_SPLIT).astype(float),"blend":np.clip((t-SHADOW_BLEND[0])/(SHADOW_BLEND[1]-SHADOW_BLEND[0]),0,1)}[SHADOW_MID]
        rgb=rgb*(1-wm[:,:,None])+rgb2*wm[:,:,None]
        if SHADOW_ON_LINES>0:
            lb=SHADOW_LINES_BLEND or SHADOW_BLEND; wl_=np.clip((t-lb[0])/(lb[1]-lb[0]),0,1) if SHADOW_MID=="blend" else wm
            lines=np.clip((g-0.60)/0.40,0,1)*wl_*SHADOW_ON_LINES
            rgb=rgb*(1-lines[:,:,None])+(rgb*0.5+sc_*0.5)*lines[:,:,None]
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
        xc=(xx/size*D+(512-D)/2)/512; yc=(yy/size*D+(512-D)/2)/512; t=tparam(xc,yc); top=tint_top(t)                      # per-pixel colour the ink drifts toward
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
    tex=None
    if FIELD_TEXTURE=="sky":
        sky=np.array(prepared_src(),dtype=float)/255; h0=sky.shape[0]
        patch=1-sky[int(h0*0.12):int(h0*0.30), int(h0*0.30):int(h0*0.70)]     # sky hatching above the trees, inside the ring
        patch=np.clip((patch-0.15)/0.5,0,1)
        ph,pw=patch.shape; tile=np.block([[patch,patch[:,::-1]],[patch[::-1,:],patch[::-1,::-1]]])
        reps=(N//tile.shape[0]+2, N//tile.shape[1]+2); big=np.tile(tile,reps)[:N,:N]
        tex=np.array(Image.fromarray((big*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6*SS)),dtype=float)/255
    elif FIELD_TEXTURE in ("hatch","hatch_jitter"):
        rng=np.random.default_rng(3); yy_=np.arange(N)[:,None]; xx_=np.arange(N)[None,:]
        if FIELD_TEXTURE=="hatch":
            wob=np.sin(xx_/(37.0*SS))*1.2*SS+np.sin(xx_/(9.0*SS)+yy_/(53.0*SS))*0.6*SS
            period=3.0*SS; line=((yy_+wob)%period)<(1.1*SS)
        else:
            # hand-ruled: each line has its own spacing, weight and wobble; lines are laid down one at a time
            line=np.zeros((N,N),bool); y=rng.uniform(0,3*SS)
            while y<N:
                gap=rng.normal(3.0,0.55)*SS; wgt=rng.uniform(0.7,1.5)*SS
                wob=(np.sin(xx_/(rng.uniform(25,60)*SS)+rng.uniform(0,6.28))*rng.uniform(0.6,1.6)*SS
                     +np.sin(xx_/(rng.uniform(6,14)*SS)+rng.uniform(0,6.28))*rng.uniform(0.2,0.6)*SS)
                line|=np.abs(yy_-(y+wob))<wgt/2
                y+=max(gap,1.6*SS)
        gaps=np.array(Image.fromarray((rng.random((N//6,N//6))*255).astype(np.uint8)).resize((N,N),Image.BILINEAR),dtype=float)/255
        tex=line.astype(float)*(gaps>0.22)
        if FIELD_TEX_WEAR>0:
            ey=np.minimum(np.arange(N),N-1-np.arange(N))[:,None]/(N/2); ex=np.minimum(np.arange(N),N-1-np.arange(N))[None,:]/(N/2)
            edge=1-np.clip(np.minimum(ey,ex)/0.45,0,1)                      # 1 at the frame, 0 from ~45% in
            worn=np.array(Image.fromarray((rng.random((N//3,N//3))*255).astype(np.uint8)).resize((N,N),Image.BILINEAR),dtype=float)/255
            tex=tex*(worn>edge*FIELD_TEX_WEAR*0.9)
    if tex is not None:
        tex_col=hx(Q[FIELD])+(MID-hx(Q[FIELD]))*FIELD_TEX_STRENGTH
        cv=cv*(1-tex[:,:,None])+tex_col*tex[:,:,None]
    if LIGHT>0 and LIGHT_FIELD:
        t=(XX+YY)/(2*N); cv=cv*(1+0.6*LIGHT*(1-2*t))[:,:,None]
    sim,smask=seal(D*SS); sa=np.array(smask,dtype=float)[:,:,None]/255; srgb=np.array(sim,dtype=float)
    o=(N-D*SS)//2; reg=cv[o:o+D*SS,o:o+D*SS]; cv[o:o+D*SS,o:o+D*SS]=reg*(1-sa)+srgb*sa
    A=np.zeros((N,N))
    for w,deg,bi in WORDS: A=np.maximum(A,warp(w,np.radians(deg),np.radians(spans[w]),D*SS/2,bi))
    global LAST_LETTERS; LAST_LETTERS=Image.fromarray((A*255).astype(np.uint8)).resize((512,512),Image.LANCZOS)
    kk=2*int(round(EDGE_PX*SS))+1
    dil=np.array(Image.fromarray((A*255).astype(np.uint8)).filter(ImageFilter.MaxFilter(kk)),dtype=float)/255; ea=np.clip(dil-A,0,1)
    if EDGE_BREAKS>0:
        rng=np.random.default_rng(BREAK_SEED); nz=np.array(Image.fromarray((rng.random((N//8,N//8))*255).astype(np.uint8)).resize((N,N),Image.BILINEAR),dtype=float)/255
        ea=ea*((nz>EDGE_BREAKS).astype(float)*(1-BREAK_FLOOR)+BREAK_FLOOR)
    edge_col=hx(Q[EDGE])[None,None,:]
    if TINT>0 and TINT_LETTERS:
        tt=tparam(XX/N,YY/N); top=stepped(tt[...,None],LETTER_STEPS) if LETTER_STEPS else tint_top(tt)
        edge_col=hx(Q[EDGE])+(top-hx(Q["y"]))*TINT
        if SHADOW_MID and SHADOW_ON_LETTERS>0:
            wl={"all":np.ones_like(tt),"split":(tt>SHADOW_MID_SPLIT).astype(float),"blend":np.clip((tt-SHADOW_BLEND[0])/(SHADOW_BLEND[1]-SHADOW_BLEND[0]),0,1)}[SHADOW_MID]*SHADOW_ON_LETTERS
            edge_col=edge_col*(1-wl[:,:,None])+hx(Q[SHADOW_MID_COLOUR])*wl[:,:,None]
    cv=cv*(1-ea[:,:,None])+edge_col*ea[:,:,None]
    if not (FIELD_TEXTURE and FIELD_TEX_IN_LETTERS and LETTER_FILL==FIELD): cv=cv*(1-A[:,:,None])+hx(Q[LETTER_FILL])*A[:,:,None]
    return Image.fromarray(cv.clip(0,255).astype(np.uint8))
pal=Image.new("P",(1,1)); cols=[hx(Q["k"]),hx(Q["K"]),MID,hx(Q["y"]),hx(Q["Y"]),hx(Q["c"]),hx(Q["C"]),(hx(Q["y"])+hx(Q["Y"]))/2,(hx(Q["y"])+hx(Q["c"]))/2]; pal.putpalette([int(c) for col in cols for c in col]+[0]*(768-27))
def pix(im,n,dither,palette=None):
    pl=pal
    if palette:
        pl=Image.new("P",(1,1)); cc=[hx(Q[c]) if isinstance(c,str) else c for c in palette]; pl.putpalette([int(v) for col in cc for v in col]+[0]*(768-3*len(cc)))
    return im.resize((n,n),Image.LANCZOS).quantize(palette=pl,dither=dither).convert("RGB").resize((512,512),Image.NEAREST)

# ---------- presets ----------
# every knob above has a default; a preset is the set of values that produce one agreed cover.
_COMMON=dict(FIELD="k",SEAL_BASE="k",LETTER_FILL="k",EDGE_PX=1.25,EDGE_BREAKS=0.0,LIGHT=0.0,FLATTEN=True,D=430,
             TINT=1.0,TINT_LETTERS=True,LETTER_STEPS=None,SHADOW=None,SHADOW_MID=None,SHADOW_ON_LETTERS=0.0,SHADOW_ON_LINES=0.0,
             FIELD_TEXTURE=None,FIELD_TEX_STRENGTH=0.4,FIELD_TEX_IN_LETTERS=False,TINT_GEOM="diagonal",SEAM_SOFT=0.0,SEAM_DITHER=True)
PRESETS={
 # FINAL 2026-10-14 (user: "that's the one i want"): continuous ink drift from white at the upper left through grey to
 # shadow cyan at the lower right, running through the seal, the title and the field alike; hand-ruled hatch over the
 # black field; solid black letters with a grey outline; posterised to the base tones plus eight samples of the drift.
 "final":dict(_COMMON,TINT_STEPS=None,FIELD_TEXTURE="hatch_jitter",FIELD_TEX_WEAR=0.5,CATALOGUE="CL - {version}"),
 # earlier bookmark: four stepped inks (Y y C c at half strength, shadow ink full), seams stippled, flat black field, hollow letters.
 "bookmark_A1_stipple":dict(_COMMON,CATALOGUE=None,TINT_STEPS=[(0.30,"Y",0.5),(0.52,"y",0.5),(0.80,"C",0.5),(1.0,"c",1.0)],SEAM_SOFT=0.12,LETTER_FILL="k",FIELD_TEX_IN_LETTERS=True),
}
def apply(preset):
    for k,v in PRESETS[preset].items(): globals()[k]=v
def palette_for(preset):
    y_=hx(Q["y"])
    if TINT_STEPS: return ["k","K",MID]+[y_+(hx(Q[c])-y_)*k for _,c,k in TINT_STEPS]
    return ["k","K",MID,"Y"]+[tint_top(np.array([t]))[0] for t in np.linspace(0,1,8)]
def mod_version():
    import json
    try: return json.load(open(os.path.join(HERE,"..","..","TerraFirma","manifest.json")))["version"]
    except Exception: return "?"
def stamp_catalogue(im):
    """vertical line in the left-edge gap between TERRA and DIASPORA, reading upward, centred in the spandrel between ring and frame"""
    if not CATALOGUE: return im
    text=CATALOGUE.format(version=mod_version()); f=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",11)
    d=ImageDraw.Draw(Image.new("L",(1,1))); bb=d.textbbox((0,0),text,font=f); w,h=bb[2]-bb[0],bb[3]-bb[1]
    col=tuple(int(v) for v in tint_top(np.array([0.5]))[0])            # the drift's grey, same as the letters' outline at mid-plate
    t=Image.new("RGBA",(w+2,h+2),(0,0,0,0)); ImageDraw.Draw(t).text((1-bb[0],1-bb[1]),text,font=f,fill=col+(255,)); t=t.rotate(90,expand=True)
    gap_x=(512-D)//2; x=max(2,gap_x//2-t.size[0]//2)
    # find the clear run of rows along that strip (no title letters, with 3px of breathing room) nearest the horizontal, and centre in it
    L=np.array(LAST_LETTERS)[:,max(0,x-3):x+t.size[0]+3]>40; busy=L.any(1); free=~busy
    runs=[]; start=None
    for yy_,fr in enumerate(list(free)+[False]):
        if fr and start is None: start=yy_
        if not fr and start is not None: runs.append((start,yy_-1)); start=None
    runs=[r for r in runs if r[1]-r[0]>=t.size[1]] or runs
    a,b=min(runs,key=lambda r:abs((r[0]+r[1])/2-256)); y=(a+b)//2-t.size[1]//2
    im=im.copy(); im.paste(t,(x,y),t); return im
def render_preset(preset, pixel=True):
    apply(preset); m=build(); sm=m.resize((512,512),Image.LANCZOS)
    out=pix(sm,512,Image.NONE,palette_for(preset)) if pixel else sm
    return stamp_catalogue(out), m
if __name__=="__main__":
    import sys
    for n in (sys.argv[1:] or list(PRESETS)):
        out,master=render_preset(n)
        out.save(os.path.join(OUT,f"{n}_512.png")); render_preset(n,pixel=False)[0].save(os.path.join(OUT,f"{n}_smooth_512.png"))
        master.resize((1024,1024),Image.LANCZOS).save(os.path.join(OUT,f"{n}_smooth_1024.png")); print("rendered",n)
