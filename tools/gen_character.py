"""Generates El Kid's layered combat sprite and the Godot scene that rigs/animates it.

Run from the project root:  python tools/gen_character.py   (then Godot --import / dotnet publish)
Outputs:
  ElKid/images/character/*.png   one PNG per body part (cropped), drawn at K x resolution
  ElKid/scenes/el_kid.tscn       Node2D rig + AnimationPlayer (idle/attack/cast/hurt/die)
  tools/preview.png              composite preview (not shipped; tools/ is .gdignore'd)

Look: hooded blade-dancer (after the Frosthaven Blinkblade mini) -- navy cloth, gold trim,
goggles on the hood, crystal blades. Painted style: thin outlines, gradient shading lit from
the upper right, cool rim light on the back edges.

Coordinates are "canvas" units of a 360x440 frame with the feet at FEET. Parts are rendered at
K*SS scale and downsampled; the scene's Rig node scales them back by 1/K (mipmapped).
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IMG_DIR = os.path.join(ROOT, "ElKid", "images", "character")
SCENE_PATH = os.path.join(ROOT, "ElKid", "scenes", "el_kid.tscn")
RES_IMG = "res://ElKid/images/character"

W, H = 360, 440
K = 2    # texture resolution multiplier
SS = 4   # supersampling for anti-aliasing
FEET = (180, 420)
HIPS = (180, 300)  # pivot of the upper-body group

OUT = (10, 12, 20, 255)
OW = 0.8  # outline width (canvas units)
LDIR = (-0.55, 0.85)  # light (upper right) -> shadow (lower left)

CLOTH = ((66, 82, 112), (18, 22, 34))
CLOTH_DK = ((48, 58, 82), (13, 16, 26))
LEATHER = ((70, 72, 84), (22, 22, 30))
GOLD = ((240, 218, 124), (140, 108, 36))
SKIN = ((214, 192, 176), (110, 90, 88))
LENS = ((170, 230, 246), (36, 100, 140))
CRYSTAL = ((210, 246, 255), (120, 200, 228), (52, 138, 180))
RIM = (120, 160, 200, 255)
FOLD = (8, 10, 18, 150)
HOLLOW = ((20, 22, 32), (4, 5, 9))


def rgba(c, a=255):
    return (c[0], c[1], c[2], a)


def lerp(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


def unit(a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]
    n = math.hypot(dx, dy)
    return dx / n, dy / n


def smooth(pts, n=2):
    """Chaikin corner cutting for a closed outline -> organic curves instead of facets."""
    for _ in range(n):
        out = []
        for i in range(len(pts)):
            a, b = pts[i], pts[(i + 1) % len(pts)]
            out += [lerp(a, b, 0.25), lerp(a, b, 0.75)]
        pts = out
    return pts


def seg_quad(a, b, wa, wb):
    ux, uy = unit(a, b)
    nx, ny = -uy, ux
    return [(a[0] + nx * wa / 2, a[1] + ny * wa / 2), (b[0] + nx * wb / 2, b[1] + ny * wb / 2),
            (b[0] - nx * wb / 2, b[1] - ny * wb / 2), (a[0] - nx * wa / 2, a[1] - ny * wa / 2)]


def band(a, b, wa, wb, t1, t2, grow=1.2):
    p1, p2 = lerp(a, b, t1), lerp(a, b, t2)
    return seg_quad(p1, p2, wa + (wb - wa) * t1 + grow, wa + (wb - wa) * t2 + grow)


class Layer:
    def __init__(self, name, pivot, k=K):
        self.name, self.pivot, self.k = name, pivot, k
        self.r = k * SS
        self.img = Image.new("RGBA", (W * self.r, H * self.r), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)

    def s(self, pts):
        return [(x * self.r, y * self.r) for x, y in pts]

    def w(self, width):
        return max(1, round(width * self.r))

    def shade(self, pts, pal, outline=True, rim=None, lo=0.1, hi=0.95, ldir=LDIR):
        """Polygon with a light->dark gradient along LDIR; rim = indices of edges to rim-light."""
        hp = self.s(pts)
        x0, y0 = int(min(p[0] for p in hp)), int(min(p[1] for p in hp))
        x1, y1 = int(math.ceil(max(p[0] for p in hp))) + 1, int(math.ceil(max(p[1] for p in hp))) + 1
        mask = Image.new("L", (x1 - x0, y1 - y0), 0)
        ImageDraw.Draw(mask).polygon([(x - x0, y - y0) for x, y in hp], fill=255)
        proj = [x * ldir[0] + y * ldir[1] for x, y in hp]
        pmin, pmax = min(proj), max(proj)
        yy, xx = np.mgrid[y0:y1, x0:x1].astype(np.float32)
        t = ((xx * ldir[0] + yy * ldir[1]) - pmin) / max(pmax - pmin, 1e-3)
        t = lo + np.clip(t, 0, 1) * (hi - lo)
        light, dark = np.array(pal[0], np.float32), np.array(pal[1], np.float32)
        rgb = light[None, None, :] * (1 - t[..., None]) + dark[None, None, :] * t[..., None]
        arr = np.dstack([rgb, np.full(t.shape, 255, np.float32)]).astype(np.uint8)
        self.img.paste(Image.fromarray(arr, "RGBA"), (x0, y0), mask)
        if rim:
            for i in rim:
                self.line([pts[i], pts[(i + 1) % len(pts)]], RIM, 1.0)
        if outline:
            self.d.polygon(hp, outline=OUT, width=self.w(OW))

    def flat(self, pts, color, outline=None, width=OW):
        self.d.polygon(self.s(pts), fill=color, outline=outline, width=self.w(width) if outline else 0)

    def ellipse(self, cx, cy, rx, ry, fill, outline=OUT, width=OW):
        r = self.r
        self.d.ellipse([(cx - rx) * r, (cy - ry) * r, (cx + rx) * r, (cy + ry) * r], fill=fill,
                       outline=outline, width=self.w(width) if outline else 0)

    def line(self, pts, fill, width):
        self.d.line(self.s(pts), fill=fill, width=self.w(width), joint="curve")

    def glow(self, draw, blur):
        g = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        draw(ImageDraw.Draw(g), self)
        self.img.alpha_composite(g.filter(ImageFilter.GaussianBlur(blur * self.r)))
        self.d = ImageDraw.Draw(self.img)

    def finish(self):
        img = self.img.resize((W * self.k, H * self.k), Image.LANCZOS)
        bbox = img.getbbox()
        return img.crop(bbox), bbox


# ---------------------------------------------------------------- shared details

def limb(L, pts, widths, pal, rim_side=True):
    """Chain of shaded tapered segments; later segments overlap earlier ones at the joints."""
    for i in range(len(pts) - 1):
        q = seg_quad(pts[i], pts[i + 1], widths[i], widths[i + 1])
        L.shade(q, pal, rim=[2] if rim_side else None)
    return pts


def bracer(L, a, b, wa, wb):
    """Leather bracer with two gold bands between a (elbow side) and b (wrist)."""
    L.shade(band(a, b, wa, wb, 0.25, 0.95, 1.6), LEATHER)
    for t1 in (0.32, 0.72):
        L.shade(band(a, b, wa, wb, t1, t1 + 0.1, 2.2), GOLD)
    L.line([lerp(a, b, 0.45), lerp(a, b, 0.62)], (255, 240, 180, 110), 0.6)


def crystal_blade(L, base, ang_deg, length, width):
    ux, uy = math.cos(math.radians(ang_deg)), math.sin(math.radians(ang_deg))
    nx, ny = -uy, ux
    tip = (base[0] + ux * length, base[1] + uy * length)
    left, right, center = [], [], []
    for i in range(13):
        t = i / 12
        w = width * (0.75 + 0.6 * t / 0.45) if t < 0.45 else width * 1.35 * (1 - (t - 0.45) / 0.55) ** 0.8
        c = (base[0] + ux * length * t, base[1] + uy * length * t)
        center.append(c)
        left.append((c[0] + nx * w, c[1] + ny * w))
        right.append((c[0] - nx * w, c[1] - ny * w))
    L.glow(lambda d, l: d.line(l.s([base, tip]), fill=(120, 210, 240, 120), width=l.w(width * 5)), width * 1.6)
    edge_col = (30, 90, 124, 255)
    L.flat(left + [tip] + center[::-1], rgba(CRYSTAL[0]))
    L.flat(right + [tip] + center[::-1], rgba(CRYSTAL[2]))
    # inner facets
    L.flat([center[1], left[4], left[7], center[8]], rgba(CRYSTAL[1]))
    L.flat([center[5], right[7], right[10], center[11]], rgba(CRYSTAL[1]))
    L.line([center[0], center[-2]], (255, 255, 255, 200), 0.6)
    for i in (3, 6, 9):
        L.line([left[i], center[i + 1]], (255, 255, 255, 120), 0.5)
    L.d.polygon(L.s(left + [tip] + right[::-1]), outline=edge_col, width=L.w(0.7))
    return tip


def hilt(L, hand, ang_deg, back=9, front=8, guard=5):
    ux, uy = math.cos(math.radians(ang_deg)), math.sin(math.radians(ang_deg))
    nx, ny = -uy, ux
    pommel = (hand[0] - ux * back, hand[1] - uy * back)
    g = (hand[0] + ux * front, hand[1] + uy * front)
    L.shade(seg_quad(pommel, g, 3.4, 3.4), LEATHER)
    for k in (-5, -1.5, 2):
        p = (hand[0] + ux * k, hand[1] + uy * k)
        L.line([(p[0] + nx * 1.7, p[1] + ny * 1.7), (p[0] - nx * 1.7, p[1] - ny * 1.7)], (8, 8, 12, 200), 0.5)
    L.ellipse(pommel[0], pommel[1], 2.2, 2.2, rgba(GOLD[0]), width=0.6)
    L.shade(seg_quad((g[0] + nx * guard, g[1] + ny * guard), (g[0] - nx * guard, g[1] - ny * guard), 2.4, 2.4), GOLD)
    return (g[0] + ux * 1.5, g[1] + uy * 1.5)


def fist(L, c, r=5.5):
    L.ellipse(c[0], c[1], r, r * 0.9, rgba(LEATHER[0]), width=OW)
    L.ellipse(c[0] - r * 0.25, c[1] + r * 0.2, r * 0.7, r * 0.6, rgba(LEATHER[1]), outline=None)
    L.line([(c[0] - r * 0.5, c[1] - r * 0.5), (c[0] + r * 0.4, c[1] - r * 0.6)], (110, 112, 126, 200), 0.6)


# ---------------------------------------------------------------- parts

def draw_shadow():
    L = Layer("shadow", (180, 418), k=1)
    L.ellipse(178, 418, 92, 8, (0, 0, 0, 110), outline=None)
    L.glow(lambda d, l: d.ellipse(l.s([(110, 414), (250, 422)]), fill=(95, 190, 230, 45)), 5)
    return L


def draw_leg(name, hip, knee, ankle, boot, widths, band_t):
    L = Layer(name, hip)
    pal = CLOTH_DK if name == "back_leg" else CLOTH
    L.shade(smooth(seg_quad(hip, knee, widths[0], widths[1]), 1), pal)
    L.shade(smooth(boot, 2), LEATHER)
    L.shade(band(knee, ankle, widths[1], widths[2], 0.02, 0.14, 1.5), LEATHER)  # boot cuff
    L.shade(band(knee, ankle, widths[1], widths[2], band_t, band_t + 0.09, 0.8), GOLD)
    L.line([(p[0], 418.6) for p in (min(boot), max(boot))], (6, 6, 10, 255), 1.0)  # sole
    L.line([lerp(knee, ankle, 0.2), lerp(knee, ankle, 0.6)], (110, 112, 126, 120), 0.6)  # shin sheen
    # cloth wrinkles on the thigh
    for t in (0.35, 0.6):
        a = lerp(hip, knee, t)
        ux, uy = unit(hip, knee)
        L.line([(a[0] - uy * 5, a[1] + ux * 5), (a[0] + uy * 2 + ux * 4, a[1] - ux * 2 + uy * 4)], FOLD, 0.6)
    # knee wrap
    L.shade(band(hip, knee, widths[0], widths[1], 0.86, 1.0, 1.5), LEATHER)
    return L


def draw_back_arm():
    shoulder, elbow, wrist = (174, 236), (140, 212), (150, 180)
    L = Layer("back_arm", shoulder)
    limb(L, [shoulder, elbow], [16, 12], CLOTH_DK)
    L.shade(seg_quad(elbow, wrist, 12, 10), CLOTH_DK)
    bracer(L, elbow, wrist, 12, 10)
    fist(L, (151, 174), 5.8)
    # shoulder plate with gold stripe
    plate = [(162, 230), (180, 225), (188, 238), (182, 248), (166, 246)]
    L.shade(smooth(plate), CLOTH)
    L.shade([(166, 233), (182, 229), (184, 233), (168, 238)], GOLD, outline=False)
    return L


def draw_back_blade():
    L = Layer("blade_back", (174, 238))
    base = hilt(L, (151, 174), -140)
    crystal_blade(L, base, -140, 92, 3.4)
    return L


def draw_cloth_strips():
    L = Layer("cloth_strips", (182, 296))
    strips = [
        ([(160, 292), (171, 296), (152, 340), (146, 346), (143, 339), (136, 342)], CLOTH_DK),
        ([(169, 296), (184, 297), (178, 346), (170, 353), (167, 345), (159, 348)], CLOTH),
        ([(183, 297), (197, 296), (202, 350), (194, 357), (190, 349), (183, 354)], CLOTH_DK),
        ([(195, 294), (207, 292), (224, 342), (217, 349), (212, 342), (207, 348)], CLOTH),
    ]
    for pts, pal in strips:
        L.shade(pts, pal, rim=[1] if pal is CLOTH else None)
        top = lerp(pts[0], pts[1], 0.5)
        bot = lerp(pts[2], pts[5], 0.5)
        L.line([lerp(top, bot, 0.2), lerp(top, bot, 0.85)], FOLD, 0.7)
        L.line([lerp(pts[0], pts[5], 0.12), lerp(pts[1], pts[2], 0.12)], rgba(GOLD[1], 200), 0.8)
    return L


def draw_torso():
    L = Layer("torso", HIPS)
    # backpack / gear pouch behind the shoulders
    pack = [(150, 242), (168, 236), (172, 276), (156, 284), (145, 264)]
    L.shade(smooth(pack, 1), LEATHER)
    L.shade([(152, 256), (166, 252), (167, 258), (153, 262)], GOLD)
    L.line([(158, 240), (160, 282)], FOLD, 0.7)
    # body
    body = [(168, 234), (196, 224), (214, 238), (217, 262), (207, 290), (199, 304), (165, 304), (159, 280), (157, 256)]
    L.shade(smooth(body, 1), CLOTH)
    for a, b in [((176, 250), (186, 284)), ((202, 250), (196, 280)), ((168, 270), (172, 296))]:
        L.line([a, b], FOLD, 0.7)
    # chest harness strap with buckle
    L.shade(seg_quad((170, 238), (208, 294), 5, 5), LEATHER)
    L.shade([(185, 259), (193, 256), (196, 264), (188, 267)], GOLD)
    L.flat([(188, 260), (192, 259), (193, 263), (189, 264)], rgba(LEATHER[1]))
    # belt / sash with buckle and pouch
    L.shade([(161, 289), (205, 284), (207, 298), (163, 304)], LEATHER)
    L.line([(163, 292), (205, 287)], rgba(GOLD[1], 220), 0.7)
    L.shade([(186, 287), (196, 286), (197, 297), (187, 298)], GOLD)
    L.shade([(166, 292), (176, 291), (177, 303), (167, 304)], LEATHER)
    # cowl (hood drape) round the neck
    cowl = [(172, 232), (196, 218), (214, 230), (208, 246), (186, 250), (170, 244)]
    L.shade(smooth(cowl), CLOTH)
    L.line([(178, 238), (200, 232)], FOLD, 0.7)
    L.line([(184, 245), (206, 238)], FOLD, 0.6)
    return L


def draw_head():
    L = Layer("head", (196, 228))
    hood = [(186, 224), (176, 210), (174, 193), (182, 178), (196, 170), (212, 171), (225, 181),
            (231, 197), (229, 214), (221, 227), (204, 231)]
    L.shade(smooth(hood), CLOTH)
    L.line(smooth(hood)[6:22], RIM, 0.9)
    # hood opening + face
    L.shade(smooth([(212, 188), (226, 193), (231, 211), (224, 227), (207, 228), (205, 206)]), HOLLOW, outline=False)
    face = [(211, 197), (225, 201), (229, 209), (227, 218), (221, 227), (208, 226), (206, 210)]
    # lit from below-right, falling into the hood's shadow at the top-left
    L.shade(smooth(face), ((226, 204, 188), (40, 34, 42)), outline=False, lo=0.0, hi=1.0, ldir=(-0.45, -1))
    L.flat(smooth([(216, 206.5), (220, 205.5), (224, 207), (220, 209)], 1), (24, 18, 22, 255))  # eye
    L.ellipse(221.5, 206.8, 0.7, 0.6, (190, 230, 240, 255), outline=None)  # eye glint
    L.line([(214, 203.5), (224, 203)], (30, 22, 24, 200), 0.7)  # brow
    L.line([(227, 209), (229, 213), (226.5, 214)], (120, 92, 88, 255), 0.6)  # nose
    L.line([(215, 219), (219, 220.5), (223, 219)], (70, 40, 40, 255), 0.7)  # smirk
    L.line([(210, 224), (220, 225.5)], (80, 64, 64, 180), 0.6)  # jaw
    # hood folds and seam
    L.line([(186, 182), (200, 200)], FOLD, 0.7)
    L.line([(180, 200), (196, 216)], FOLD, 0.7)
    L.line([(198, 172), (208, 186)], FOLD, 0.6)
    # goggles strapped on the brow
    L.shade(seg_quad((178, 190), (226, 186), 4, 4), LEATHER)
    for (cx, cy, r) in [(212, 184, 5.5), (223, 188, 4.6)]:
        L.ellipse(cx, cy, r + 1.4, r + 1.4, rgba(GOLD[1]), width=0.7)
        L.ellipse(cx, cy, r + 0.6, r + 0.6, rgba(GOLD[0]), outline=None)
        L.shade([(cx + math.cos(a) * r, cy + math.sin(a) * r) for a in np.linspace(0, math.tau, 20)], LENS)
        L.ellipse(cx + r * 0.3, cy - r * 0.35, r * 0.35, r * 0.22, (255, 255, 255, 220), outline=None)
    L.flat([(216.5, 185), (219, 186), (219, 188), (216.5, 187)], rgba(GOLD[1]))  # bridge
    return L


def draw_front_arm():
    shoulder, elbow, wrist = (208, 244), (221, 272), (240, 283)
    L = Layer("front_arm", shoulder)
    limb(L, [shoulder, elbow], [15, 12], CLOTH)
    L.shade(seg_quad(elbow, wrist, 12, 10), CLOTH)
    bracer(L, elbow, wrist, 12, 10)
    # layered shoulder plate with two gold stripes
    plate = [(196, 232), (214, 232), (225, 246), (218, 258), (200, 252)]
    L.shade(smooth(plate), CLOTH)
    L.shade([(202, 238), (216, 237), (219, 241), (204, 243)], GOLD, outline=False)
    L.shade([(203, 245), (219, 245), (220, 249), (205, 250)], GOLD, outline=False)
    L.line([(199, 250), (218, 256)], FOLD, 0.6)
    return L


def draw_front_blade():
    L = Layer("blade_front", (208, 244))
    hand = (245, 286)
    base = hilt(L, hand, 70, back=8, front=7, guard=4)
    crystal_blade(L, base, 70, 62, 2.8)
    fist(L, hand, 6.4)  # hand wraps the reverse-grip hilt
    return L


# ---------------------------------------------------------------- rig / scene

RIG = "Visuals/Rig"
UP = f"{RIG}/Upper"
HEAD = f"{UP}/head"
ARM_F = f"{UP}/front_arm"
ARM_B = f"{UP}/back_arm"
BLADE_F = f"{ARM_F}/blade_front"
BLADE_B = f"{ARM_B}/blade_back"
CLOTH_S = f"{UP}/cloth_strips"


def build_rig():
    back_arm = draw_back_arm()
    front_arm = draw_front_arm()
    head = draw_head()
    rig = [
        (draw_shadow(), "."),
        (draw_leg("back_leg", (172, 300), (144, 350), (116, 394),
                  [(137, 345), (151, 351), (140, 372), (127, 394), (135, 410), (137, 419), (113, 419),
                   (105, 405), (110, 389), (124, 365)], [22, 15, 11], 0.7), RIG),
        (draw_leg("front_leg", (190, 300), (228, 334), (240, 398),
                  [(221, 331), (235, 332), (238, 352), (242, 392), (257, 404), (265, 411), (265, 419),
                   (232, 419), (231, 402), (228, 378), (222, 354)], [23, 16, 12], 0.8), RIG),
        (back_arm, UP),
        (draw_back_blade(), ARM_B),
        (draw_cloth_strips(), UP),
        (draw_torso(), UP),
        (head, UP),
        (front_arm, UP),
        (draw_front_blade(), ARM_F),
    ]
    origins = {".": FEET, RIG: FEET, UP: HIPS, ARM_B: back_arm.pivot, ARM_F: front_arm.pivot, HEAD: head.pivot}
    return rig, origins


def main():
    os.makedirs(IMG_DIR, exist_ok=True)
    os.makedirs(os.path.dirname(SCENE_PATH), exist_ok=True)
    rig, origins = build_rig()

    preview = Image.new("RGBA", (W * K, H * K), (22, 26, 36, 255))
    nodes = []
    for layer, parent in rig:
        img, (x0, y0, _, _) = layer.finish()
        path = os.path.join(IMG_DIR, layer.name + ".png")
        img.save(path)
        ensure_mipmaps(path)
        big = img if layer.k == K else img.resize((img.width * K // layer.k, img.height * K // layer.k), Image.LANCZOS)
        preview.alpha_composite(big, (x0 * K // layer.k, y0 * K // layer.k))
        ox, oy = origins[parent]
        px, py = layer.pivot
        scale = 1 if parent == "." else K  # everything under Rig lives in K-scaled space
        nodes.append((layer.name, parent, ((px - ox) * scale, (py - oy) * scale), (x0 - px * layer.k, y0 - py * layer.k)))
    preview.save(os.path.join(ROOT, "tools", "preview.png"))

    keep = {layer.name + ".png" for layer, _ in rig}
    for f in os.listdir(IMG_DIR):  # drop parts from older designs
        png = f[:-len(".import")] if f.endswith(".import") else f
        if png.endswith(".png") and png not in keep:
            os.remove(os.path.join(IMG_DIR, f))

    write_scene(nodes)
    print("wrote", len(nodes), "parts and", SCENE_PATH)


def ensure_mipmaps(png_path):
    """Parts are drawn at K x and shown at 1/K, so they need mipmaps to stay crisp."""
    imp = png_path + ".import"
    if os.path.exists(imp):
        s = open(imp, encoding="utf-8").read()
        if "mipmaps/generate=false" in s:
            open(imp, "w", encoding="utf-8", newline="\n").write(s.replace("mipmaps/generate=false", "mipmaps/generate=true"))
    else:
        with open(imp, "w", encoding="utf-8", newline="\n") as f:
            f.write('[remap]\n\nimporter="texture"\ntype="CompressedTexture2D"\n\n[params]\n\n'
                    "compress/mode=0\nmipmaps/generate=true\nprocess/fix_alpha_border=true\n")


def fmt(v):
    return f"{v:g}"


def track(path, times, values):
    return dict(path=path, times=times, values=values)


def v2(x, y):
    return f"Vector2({fmt(round(x, 3))}, {fmt(round(y, 3))})"


def col(r, g, b, a=1.0):
    return f"Color({fmt(r)}, {fmt(g)}, {fmt(b)}, {fmt(a)})"


def rad(deg):
    return round(math.radians(deg), 5)


def animations(base):
    """base: node path -> rest position (in that node's parent space)."""
    white = col(1, 1, 1)
    flare = col(1.8, 1.8, 1.8)

    def pos(node, *keys):
        s = K if node.startswith(RIG + "/") else 1
        bx, by = base[node]
        return [v2(bx + dx * s, by + dy * s) for dx, dy in keys]

    anims = {}

    # idle: coiled, breathing stance; raised blade sways, cloth strips drift, crystals shimmer
    t = [0.0, 0.8, 1.6]
    anims["idle"] = dict(length=1.6, loop=True, tracks=[
        track(f"{UP}:position", t, pos(UP, (0, 0), (0, 2.5), (0, 0))),
        track(f"{UP}:scale", t, [v2(1, 1), v2(1.008, 0.99), v2(1, 1)]),
        track(f"{HEAD}:rotation", t, [0, rad(-1.5), 0]),
        track(f"{ARM_B}:rotation", t, [0, rad(-4), 0]),
        track(f"{ARM_F}:rotation", t, [0, rad(3), 0]),
        track(f"{CLOTH_S}:rotation", [0.0, 0.4, 0.8, 1.2, 1.6], [0, rad(2.5), rad(-1.5), rad(2), 0]),
        track(f"{BLADE_B}:modulate", [0.0, 0.5, 1.1, 1.6], [white, col(1.15, 1.15, 1.15), col(0.95, 0.95, 0.95), white]),
        track(f"{BLADE_F}:modulate", [0.0, 0.7, 1.3, 1.6], [white, col(0.95, 0.95, 0.95), col(1.15, 1.15, 1.15), white]),
        track("shadow:scale", t, [v2(1, 1), v2(1.02, 1), v2(1, 1)]),
    ])

    # attack: blink forward (flash), overhead crystal slash with the raised blade, snap back
    t = [0.0, 0.08, 0.17, 0.32, 0.5]
    anims["attack"] = dict(length=0.5, loop=False, next="idle", tracks=[
        track("Visuals:position", t, [v2(0, 0), v2(-8, 0), v2(62, 0), v2(54, 0), v2(0, 0)]),
        track("Visuals:modulate", [0.0, 0.08, 0.11, 0.18, 0.26], [white, white, col(1.6, 2.2, 2.6, 0.3), col(1.3, 1.6, 1.8), white]),
        track(f"{UP}:rotation", t, [0, rad(-5), rad(8), rad(5), 0]),
        track(f"{UP}:position", t, pos(UP, (0, 0), (0, 1), (0, 4), (0, 2), (0, 0))),
        track(f"{UP}:scale", t, [v2(1, 1)] * 5),
        track(f"{ARM_B}:rotation", t, [0, rad(-18), rad(125), rad(105), 0]),
        track(f"{ARM_F}:rotation", t, [0, rad(8), rad(-25), rad(-15), 0]),
        track(f"{BLADE_B}:modulate", t, [white, flare, col(2.2, 2.2, 2.2), flare, white]),
        track(f"{CLOTH_S}:rotation", t, [0, rad(-3), rad(12), rad(8), 0]),
        track(f"{HEAD}:rotation", t, [0, rad(-3), rad(4), rad(2), 0]),
    ])

    # cast: lift both blades, crystals flare
    t = [0.0, 0.15, 0.4, 0.6]
    anims["cast"] = dict(length=0.6, loop=False, next="idle", tracks=[
        track(f"{ARM_B}:rotation", t, [0, rad(-22), rad(-18), 0]),
        track(f"{ARM_F}:rotation", t, [0, rad(-30), rad(-24), 0]),
        track(f"{HEAD}:rotation", t, [0, rad(-4), rad(-3), 0]),
        track(f"{BLADE_B}:modulate", t, [white, col(2.4, 2.4, 2.4), flare, white]),
        track(f"{BLADE_F}:modulate", t, [white, col(2.4, 2.4, 2.4), flare, white]),
        track(f"{UP}:position", t, pos(UP, (0, 0), (0, -3), (0, -2), (0, 0))),
        track(f"{UP}:scale", t, [v2(1, 1)] * 4),
        track(f"{CLOTH_S}:rotation", t, [0, rad(6), rad(4), 0]),
    ])

    # hurt: recoil with a red flash
    t = [0.0, 0.06, 0.2, 0.4]
    anims["hurt"] = dict(length=0.4, loop=False, next="idle", tracks=[
        track("Visuals:position", t, [v2(0, 0), v2(-18, 0), v2(-9, 0), v2(0, 0)]),
        track("Visuals:modulate", t, [white, col(1.8, 0.55, 0.6), col(1.3, 0.8, 0.85), white]),
        track(f"{UP}:rotation", t, [0, rad(-9), rad(-4), 0]),
        track(f"{HEAD}:rotation", t, [0, rad(-10), rad(-5), 0]),
        track(f"{ARM_B}:rotation", t, [0, rad(-12), rad(-6), 0]),
        track(f"{ARM_F}:rotation", t, [0, rad(15), rad(7), 0]),
        track(f"{CLOTH_S}:rotation", t, [0, rad(10), rad(5), 0]),
    ])

    # die: stagger and drop the guard, then blink out of existence in a cyan flash
    t = [0.0, 0.3, 0.6]
    anims["die"] = dict(length=1.3, loop=False, tracks=[
        track(f"{UP}:rotation", t, [0, rad(-10), rad(-14)]),
        track(f"{UP}:position", t, pos(UP, (0, 0), (-4, 10), (-6, 16))),
        track(f"{HEAD}:rotation", t, [0, rad(12), rad(20)]),
        track(f"{ARM_B}:rotation", t, [0, rad(60), rad(95)]),
        track(f"{ARM_F}:rotation", t, [0, rad(30), rad(45)]),
        track("Visuals:modulate", [0.0, 0.8, 0.95, 1.05, 1.3],
              [white, white, col(1.8, 2.4, 2.8, 0.9), col(2, 2.6, 3, 0.6), col(2, 2.6, 3, 0)]),
        track("Visuals:scale", [0.0, 0.85, 1.05, 1.3], [v2(1, 1), v2(1, 1), v2(0.35, 1.15), v2(0.05, 1.3)]),
        track("shadow:modulate", [0.0, 0.9, 1.3], [white, white, col(1, 1, 1, 0)]),
    ])

    rest = {
        "Visuals:position": v2(0, 0), "Visuals:modulate": white, "Visuals:scale": v2(1, 1),
        f"{UP}:position": v2(*base[UP]), f"{UP}:rotation": 0, f"{UP}:scale": v2(1, 1),
        f"{HEAD}:rotation": 0, f"{ARM_F}:rotation": 0, f"{ARM_B}:rotation": 0, f"{CLOTH_S}:rotation": 0,
        f"{BLADE_F}:modulate": white, f"{BLADE_B}:modulate": white,
        "shadow:scale": v2(1, 1), "shadow:modulate": white,
    }
    anims["RESET"] = dict(length=0.001, loop=False, tracks=[track(p, [0.0], [v]) for p, v in rest.items()])
    return anims


def write_scene(nodes):
    base = {"Visuals": (0, 0), RIG: (0, 0), UP: ((HIPS[0] - FEET[0]) * K, (HIPS[1] - FEET[1]) * K)}
    for name, parent, p, _ in nodes:
        base[name if parent == "." else f"{parent}/{name}"] = p
    anims = animations(base)

    ext = [f'[ext_resource type="Texture2D" path="{RES_IMG}/{name}.png" id="{i}_{name}"]'
           for i, (name, _, _, _) in enumerate(nodes, start=1)]

    subs, anim_ids = [], []
    for aname, a in anims.items():
        sid = f"Animation_{aname.lower()}"
        anim_ids.append((aname, sid))
        lines = [f'[sub_resource type="Animation" id="{sid}"]', f'resource_name = "{aname}"',
                 f"length = {fmt(a['length'])}"]
        if a["loop"]:
            lines.append("loop_mode = 1")
        for ti, tr in enumerate(a["tracks"]):
            lines += [
                f'tracks/{ti}/type = "value"',
                f"tracks/{ti}/imported = false",
                f"tracks/{ti}/enabled = true",
                f'tracks/{ti}/path = NodePath("{tr["path"]}")',
                f"tracks/{ti}/interp = 2",
                f"tracks/{ti}/loop_wrap = true",
                f"tracks/{ti}/keys = {{",
                f'"times": PackedFloat32Array({", ".join(fmt(x) for x in tr["times"])}),',
                f'"transitions": PackedFloat32Array({", ".join("1" for _ in tr["times"])}),',
                '"update": 0,',
                f'"values": [{", ".join(str(v) for v in tr["values"])}]',
                "}",
            ]
        subs.append("\n".join(lines))

    subs.append("\n".join(['[sub_resource type="AnimationLibrary" id="AnimationLibrary_elkid"]', "_data = {",
                           ",\n".join(f'&"{n}": SubResource("{sid}")' for n, sid in anim_ids), "}"]))

    body = ['[node name="ElKidVisuals" type="Node2D"]',
            '[node name="Bounds" type="Control" parent="."]\nlayout_mode = 3\nanchors_preset = 0\n'
            "offset_left = -90.0\noffset_top = -250.0\noffset_right = 90.0\noffset_bottom = 0.0\nmouse_filter = 2"]
    for i, (name, parent, p, off) in enumerate(nodes, start=1):
        body.append(f'[node name="{name}" type="Sprite2D" parent="{parent}"]\n'
                    f"position = {v2(*p)}\n"
                    f'texture = ExtResource("{i}_{name}")\n'
                    "centered = false\n"
                    f"offset = {v2(*off)}")
        if name == "shadow":
            body.append('[node name="Visuals" type="Node2D" parent="."]\nunique_name_in_owner = true')
            body.append(f'[node name="Rig" type="Node2D" parent="Visuals"]\ntexture_filter = 4\nscale = {v2(1 / K, 1 / K)}')
        if name == "front_leg":
            body.append(f'[node name="Upper" type="Node2D" parent="{RIG}"]\nposition = {v2(*base[UP])}')
    body.append('[node name="AnimationPlayer" type="AnimationPlayer" parent="."]\n'
                'libraries = {\n&"": SubResource("AnimationLibrary_elkid")\n}\n'
                'autoplay = "idle"\n'
                + "\n".join(f'next/{n} = &"{a["next"]}"' for n, a in anims.items() if a.get("next")))

    out = [f"[gd_scene load_steps={len(ext) + len(subs) + 1} format=3]", ""] + ext + [""]
    for chunk in subs + body:
        out += [chunk, ""]
    with open(SCENE_PATH, "w", newline="\n") as f:
        f.write("\n".join(out))


if __name__ == "__main__":
    main()
