"""Generates the Campus Rift 'celestial' UI sprite set (authored at 2x, AA via analytic SDF coverage)."""
import os, sys
import numpy as np
from PIL import Image, ImageFilter

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)

GOLD_DARK = np.array([0.42, 0.29, 0.13])
GOLD = np.array([0.93, 0.76, 0.45])
GOLD_LIGHT = np.array([1.0, 0.95, 0.80])
VIOLET = np.array([0.55, 0.40, 1.0])
INK = np.array([0.055, 0.043, 0.105])


def grid(w, h):
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    return x + 0.5, y + 0.5


def cov(d, soft=0.75):
    """Coverage of the region d<0 with ~1px anti-aliasing."""
    return np.clip(0.5 - d / (2 * soft), 0, 1)


def stroke(d, width, offset=0.0):
    return cov(np.abs(d - offset) - width / 2)


def chamfer_rect(x, y, cx, cy, hx, hy, c):
    px, py = np.abs(x - cx), np.abs(y - cy)
    box = np.maximum(px - hx, py - hy)
    ch = (px + py - (hx + hy - c)) / np.sqrt(2)
    return np.maximum(box, ch)


def star(x, y, cx, cy, a, b, p=0.55):
    """Concave four-point star (superellipse with p<1); returns approx signed distance."""
    u, v = np.abs(x - cx) / a, np.abs(y - cy) / b
    f = (u ** p + v ** p) ** (1 / p) - 1
    gy, gx = np.gradient(f)
    g = np.sqrt(gx * gx + gy * gy) + 1e-6
    return f / g


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.a = np.zeros((h, w), np.float32)

    def over(self, color, alpha):
        """Porter-Duff 'over' with straight alpha; color may be (3,) or (h,w,3)."""
        color = np.broadcast_to(np.asarray(color, np.float32), self.rgb.shape)
        alpha = np.clip(alpha, 0, 1)
        out_a = alpha + self.a * (1 - alpha)
        safe = np.where(out_a > 0, out_a, 1)
        self.rgb = (color * alpha[..., None] + self.rgb * (self.a * (1 - alpha))[..., None]) / safe[..., None]
        self.a = out_a

    def add_glow(self, color, alpha):
        self.over(color, alpha)

    def save(self, name):
        img = np.dstack([np.clip(self.rgb, 0, 1), np.clip(self.a, 0, 1)])
        Image.fromarray((img * 255 + 0.5).astype(np.uint8), 'RGBA').save(os.path.join(OUT, name))


def lerp3(a, b, t):
    t = np.clip(t, 0, 1)[..., None]
    return a * (1 - t) + b * t


def gold_ramp(t):
    """0 dark bronze, .6 gold, 1 pale highlight."""
    t = np.clip(t, 0, 1)
    lo = lerp3(GOLD_DARK, GOLD, t / 0.6)
    hi = lerp3(GOLD, GOLD_LIGHT, (t - 0.6) / 0.4)
    return np.where((t < 0.6)[..., None], lo, hi)


def blur_alpha(a, radius):
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(radius))
    return np.asarray(im, np.float32) / 255


# ---------------------------------------------------------------- gem ornament (echoes the icon frame stars)
def gem(name, w, h, glow=False):
    c = Canvas(w, h)
    x, y = grid(w, h)
    cx, cy = w / 2, h / 2
    a, b = w * 0.40, h * 0.44
    if glow:
        halo = blur_alpha(cov(star(x, y, cx, cy, a * 1.05, b * 1.05)), w * 0.10)
        c.over(np.array([0.75, 0.6, 1.0]), halo * 0.85)
    outer = star(x, y, cx, cy, a, b)
    mid = star(x, y, cx, cy, a * 0.80, b * 0.80)
    inner = star(x, y, cx, cy, a * 0.62, b * 0.62)
    # rim: gold, lit from upper-left
    light = np.clip(0.55 + 0.45 * (-(x - cx) / a * 0.5 - (y - cy) / b * 0.6), 0, 1)
    c.over(np.zeros(3) + 0.02, cov(outer + 1.2))                     # dark outline
    c.over(gold_ramp(light), cov(outer))
    c.over(np.array([0.10, 0.07, 0.17]), cov(mid))                     # metal band
    c.over(gold_ramp(light * 0.8 + 0.1), stroke(mid, 1.2))
    # crystal: deep blue edges, lavender core
    r = np.sqrt(((x - cx) / (a * 0.62)) ** 2 + ((y - cy) / (b * 0.62)) ** 2)
    crystal = lerp3(np.array([0.93, 0.88, 1.0]), np.array([0.20, 0.16, 0.62]), r ** 0.8)
    c.over(crystal, cov(inner))
    # facet: the upper-left half a little brighter
    facet = ((x - cx) * 0.6 + (y - cy) < 0) & (inner < 0)
    c.over(np.array([1, 1, 1]), facet * 0.12)
    # sparkle cross
    spark = np.maximum(np.exp(-((x - cx) / (w * 0.012)) ** 2) * np.exp(-np.abs(y - cy) / (h * 0.16)),
                       np.exp(-((y - cy) / (h * 0.010)) ** 2) * np.exp(-np.abs(x - cx) / (w * 0.14)))
    c.over(np.array([1, 1, 1]), spark * cov(inner) * 0.95)
    c.over(np.array([1, 1, 1]), np.exp(-(((x - cx) / (w * 0.05)) ** 2 + ((y - cy) / (h * 0.05)) ** 2)) * 0.9)
    c.save(name)


# ---------------------------------------------------------------- 9-slice button frames
def button(name, w=256, h=112, pad=0, primary=False, glow=False):
    W, H = w + pad * 2, h + pad * 2
    c = Canvas(W, H)
    x, y = grid(W, H)
    cx, cy = W / 2, H / 2
    hx, hy, cut = w / 2 - 2, h / 2 - 2, 20
    d = chamfer_rect(x, y, cx, cy, hx, hy, cut)
    t = (y - (cy - hy)) / (2 * hy)  # 0 top -> 1 bottom
    if glow:
        # soft outer bloom + bright double trim + inner violet haze
        bloom = blur_alpha(stroke(d, 6), 9)
        c.over(np.array([0.98, 0.80, 0.48]), bloom * 0.75)
        inner_haze = blur_alpha(stroke(d, 10, -6), 10) * cov(d)
        c.over(np.array([0.62, 0.45, 1.0]), inner_haze * 0.55)
        c.over(np.array([0.70, 0.55, 1.0]), cov(d) * 0.10)
        c.over(gold_ramp(1 - t * 0.5), stroke(d, 3.2, -1.6))
        c.over(np.array([1.0, 0.92, 0.75]), stroke(d, 1.2, -9) * 0.55)
        c.save(name)
        return
    top = np.array([0.20, 0.12, 0.40]) if primary else np.array([0.085, 0.068, 0.16])
    bottom = np.array([0.085, 0.045, 0.19]) if primary else np.array([0.035, 0.028, 0.07])
    body = lerp3(top, bottom, t)
    c.over(body, cov(d) * (0.97 if primary else 0.9))
    # horizon sheen along the upper third
    sheen = np.exp(-((t - 0.18) / 0.10) ** 2) * cov(d + 6)
    c.over(np.array([0.55, 0.42, 0.95]), sheen * (0.20 if primary else 0.10))
    # inner violet hairline and gold trim
    c.over(VIOLET, stroke(d, 1.3, -8) * (0.55 if primary else 0.35))
    c.over(gold_ramp(1 - t * 0.7), stroke(d, 2.6 if primary else 2.0, -1.4) * (1.0 if primary else 0.72))
    # chamfer accents: tiny gold ticks on the cut corners
    for sx, sy in ((-1, -1), (1, 1)):
        px, py = cx + sx * (hx - cut / 2 - 3), cy + sy * (hy - cut / 2 - 3)
        tick = cov(np.abs(x - px) + np.abs(y - py) - 4.5)
        c.over(GOLD_LIGHT, tick * (0.9 if primary else 0.6))
    c.save(name)


# ---------------------------------------------------------------- 9-slice panel
def panel(name, s=320):
    c = Canvas(s, s)
    x, y = grid(s, s)
    cx = cy = s / 2
    h = s / 2 - 3
    cut = 26
    d = chamfer_rect(x, y, cx, cy, h, h, cut)
    t = (y - 3) / (s - 6)
    c.over(lerp3(np.array([0.060, 0.047, 0.115]), np.array([0.030, 0.024, 0.060]), t), cov(d) * 0.95)
    # inner shadow toward the border for depth
    c.over(np.array([0, 0, 0]), blur_alpha(stroke(d, 8, -4), 10) * cov(d) * 0.55)
    c.over(gold_ramp(np.full_like(t, 0.62)), stroke(d, 2.4, -1.2) * 0.85)
    c.over(GOLD, stroke(d, 1.2, -12) * 0.35)
    # corner brackets: thick gold L shapes following the chamfer + a small diamond
    for sx in (-1, 1):
        for sy in (-1, 1):
            arm = 64
            inset = chamfer_rect(x, y, cx, cy, h - 12, h - 12, cut - 6)
            region = (sx * (x - cx) > h - arm) & (sy * (y - cy) > h - arm)
            fade = np.clip((np.maximum(sx * (x - cx), sy * (y - cy)) - (h - arm)) / 24, 0, 1)
            c.over(gold_ramp(np.full_like(t, 0.8)), stroke(inset, 3.0) * region * fade)
            dx, dy = cx + sx * (h - 26), cy + sy * (h - 26)
            dia = (np.abs(x - dx) + np.abs(y - dy)) - 6
            c.over(GOLD_LIGHT, cov(dia) * 0.95)
    c.save(name)


def divider(name, w=1024, h=48):
    c = Canvas(w, h)
    x, y = grid(w, h)
    cx, cy = w / 2, h / 2
    fall = np.clip(1 - np.abs(x - cx) / (w / 2 - 8), 0, 1) ** 0.8
    line = cov(np.abs(y - cy) - 1.1) * fall
    c.over(gold_ramp(0.35 + fall * 0.6), line)
    for side in (-1, 1):
        dx = cx + side * 58
        c.over(GOLD, cov(np.abs(x - dx) + np.abs(y - cy) - 5.5))
        c.over(np.array([0.08, 0.06, 0.14]), cov(np.abs(x - dx) + np.abs(y - cy) - 2.5))
    s = star(x, y, cx, cy, 30, 20, 0.55)
    c.over(np.zeros(3), blur_alpha(cov(s), 6) * 0.8)
    c.over(gold_ramp(np.clip(0.9 - (y - cy) / 40, 0, 1)), cov(s))
    c.over(np.array([0.45, 0.36, 0.95]), cov(star(x, y, cx, cy, 15, 10, 0.55)))
    c.over(np.array([1, 1, 1]), cov(star(x, y, cx, cy, 6, 4, 0.55)))
    c.save(name)


def slider(name_track, name_fill, w=96, h=28):
    x, y = grid(w, h)
    cy = h / 2
    d = chamfer_rect(x, y, w / 2, cy, w / 2 - 2, h / 2 - 7, 6)
    c = Canvas(w, h)
    c.over(np.array([0.02, 0.015, 0.045]), cov(d) * 0.95)
    c.over(GOLD, stroke(d, 1.4, -0.7) * 0.45)
    c.save(name_track)
    c = Canvas(w, h)
    d2 = chamfer_rect(x, y, w / 2, cy, w / 2 - 4, h / 2 - 9, 4)
    t = np.abs(y - cy) / (h / 2 - 9)
    c.over(lerp3(np.array([1.0, 0.90, 0.68]), np.array([0.62, 0.40, 0.98]), t), cov(d2))
    c.save(name_fill)


def toggle(name_box, name_check, s=72):
    x, y = grid(s, s)
    cx = cy = s / 2
    dia = (np.abs(x - cx) + np.abs(y - cy)) / np.sqrt(2) - s * 0.33
    c = Canvas(s, s)
    c.over(np.array([0.03, 0.025, 0.07]), cov(dia))
    c.over(GOLD, stroke(dia, 2.2, -1.1) * 0.9)
    c.over(VIOLET, stroke(dia, 1.0, -6) * 0.45)
    c.save(name_box)
    c = Canvas(s, s)
    inner = (np.abs(x - cx) + np.abs(y - cy)) / np.sqrt(2) - s * 0.17
    c.over(np.array([0.78, 0.62, 1.0]), blur_alpha(cov(inner), 4) * 0.8)
    r = np.sqrt((x - cx) ** 2 + (y - cy) ** 2) / (s * 0.24)
    c.over(lerp3(np.array([1, 0.97, 0.88]), np.array([0.95, 0.72, 0.38]), r), cov(inner))
    c.save(name_check)


def chevron(name, w=48, h=32):
    x, y = grid(w, h)
    c = Canvas(w, h)
    # V shape: distance to two segments
    def seg(ax, ay, bx, by):
        px, py = x - ax, y - ay
        vx, vy = bx - ax, by - ay
        t = np.clip((px * vx + py * vy) / (vx * vx + vy * vy), 0, 1)
        return np.sqrt((px - vx * t) ** 2 + (py - vy * t) ** 2)
    dd = np.minimum(seg(w * 0.2, h * 0.3, w / 2, h * 0.72), seg(w * 0.8, h * 0.3, w / 2, h * 0.72))
    c.over(GOLD, cov(dd - 2.6))
    c.save(name)


def radial(name, s=256, power=1.6):
    x, y = grid(s, s)
    r = np.sqrt((x - s / 2) ** 2 + (y - s / 2) ** 2) / (s / 2)
    c = Canvas(s, s)
    c.over(np.ones(3), np.clip(1 - r, 0, 1) ** power)
    c.save(name)


def circle(name, s=256):
    x, y = grid(s, s)
    r = np.sqrt((x - s / 2) ** 2 + (y - s / 2) ** 2) - (s / 2 - 2)
    c = Canvas(s, s)
    c.over(np.ones(3), cov(r))
    c.save(name)


def vignette(name, w=640, h=360):
    x, y = grid(w, h)
    r = np.sqrt(((x - w / 2) / (w / 2)) ** 2 + ((y - h / 2) / (h / 2)) ** 2)
    c = Canvas(w, h)
    c.over(np.array([0.01, 0.005, 0.03]), np.clip((r - 0.45) / 0.95, 0, 1) ** 1.3)
    c.save(name)


def side_shade(name, w=512, h=8):
    x, y = grid(w, h)
    c = Canvas(w, h)
    t = x / w
    c.over(np.array([0.012, 0.008, 0.03]), np.clip(1 - t, 0, 1) ** 1.4)
    c.save(name)


def mote(name, s=64):
    x, y = grid(s, s)
    r = np.sqrt((x - s / 2) ** 2 + (y - s / 2) ** 2) / (s / 2)
    c = Canvas(s, s)
    c.over(np.ones(3), np.clip(1 - r, 0, 1) ** 3)
    spark = np.maximum(np.exp(-((x - s / 2) / 1.1) ** 2) * np.clip(1 - np.abs(y - s / 2) / (s / 2), 0, 1) ** 2,
                       np.exp(-((y - s / 2) / 1.1) ** 2) * np.clip(1 - np.abs(x - s / 2) / (s / 2), 0, 1) ** 2)
    c.over(np.ones(3), spark * 0.9)
    c.save(name)


def badge(name, w=96, h=44):
    x, y = grid(w, h)
    d = chamfer_rect(x, y, w / 2, h / 2, w / 2 - 2, h / 2 - 2, 10)
    c = Canvas(w, h)
    c.over(np.array([0.035, 0.028, 0.075]), cov(d) * 0.95)
    c.over(GOLD, stroke(d, 1.6, -0.8) * 0.8)
    c.save(name)


gem('Gem.png', 96, 128)
gem('GemGlow.png', 160, 200, glow=True)
button('ButtonFrame.png')
button('ButtonPrimary.png', primary=True)
button('ButtonGlow.png', pad=24, glow=True)
panel('PanelFrame.png')
divider('Divider.png')
slider('SliderTrack.png', 'SliderFill.png')
toggle('ToggleBox.png', 'ToggleCheck.png')
chevron('Chevron.png')
radial('RadialGlow.png')
circle('Circle.png')
vignette('Vignette.png')
side_shade('SideShade.png')
mote('Mote.png')
badge('KeyBadge.png')
print('ok')
