"""Removes a baked-in checkerboard "transparency" background from opaque icons in Content/Images and writes the result, resized,
to Assets/Resources/ContentImages. Only images without any transparent pixel are touched. Usage: python Tools/strip_checker.py"""
import glob, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage

SIZES = {'Items': 256, 'Skills': 512, 'Artifacts':512, 'Realms':512, 'Elements':256, 'Badges':128}

def strip(im):
    rgba = np.array(im.convert('RGBA')); rgb = rgba[..., :3].astype(int)
    h, w = rgb.shape[:2]
    ring = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
    keys, counts = np.unique(ring // 6, axis=0, return_counts=True)
    order = np.argsort(-counts)[:2]
    tones = [keys[i] * 6 + 3 for i in order]
    mask = np.zeros((h, w), bool)
    for t in tones:
        mask |= (np.abs(rgb - t).max(axis=2) <= 30)
    # checker tones are neutral greys/whites
    mask &= (rgb.max(axis=2) - rgb.min(axis=2)) <= 26
    labels, n = ndimage.label(mask)
    border = set(np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))) - {0}
    bg = np.isin(labels, list(border))
    bg = ndimage.binary_dilation(bg, iterations=1) & (mask | bg)   # take the anti-aliased rim
    alpha = ndimage.gaussian_filter((~bg).astype(float), 1.2)
    alpha = np.clip((alpha - .35) / .5, 0, 1)
    rgba[..., 3] = (alpha * 255).astype(np.uint8)
    return Image.fromarray(rgba, 'RGBA'), bg.mean()

REGENERATED = {'tu-linh-dan.png', 'kiem-tam-dan.png', 'cuong-luc-dan.png', 'bang-tam-phu.png', 'ti-hoa-chau.png', 'kim-cuong-phu.png', 'hoi-xuan-dan.png', 'hoi-khi-dan.png'}

count = 0
for group, size in SIZES.items():
    for f in sorted(glob.glob('Content/Images/%s/*.png' % group)):
        if group == 'Items' and os.path.basename(f) in REGENERATED: continue
        im = Image.open(f).convert('RGBA')
        if im.getchannel('A').getextrema()[0] < 255: continue
        small = im.resize((size, size), Image.LANCZOS)
        out, ratio = strip(small)
        if ratio < .12: continue
        dst = 'Assets/Resources/ContentImages/%s/%s' % (group, os.path.basename(f))
        out.save(dst, optimize=True); count += 1
        print('%-40s background %.0f%%' % (dst, ratio * 100))
print(count, 'images cleaned')
