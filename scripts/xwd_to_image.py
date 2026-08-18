"""Turns an XWD dump from xwd into a PIL image."""
import struct, sys
from PIL import Image

def read_xwd(path):
    d = open(path, 'rb').read()
    f = struct.unpack('>25I', d[:100])
    (hdr, ver, fmt, depth, width, height, xoff, border, bunit, bbo,
     bpad, bpp, bpl, vclass, rmask, gmask, bmask, bprgb, cmap, ncol,
     wwidth, wheight, wx, wy, wbw) = f
    ncolors = ncol
    off = hdr + ncolors * 12          # colormap entries are 12 bytes each
    px = d[off:off + bpl * height]
    if bpp != 32:
        raise SystemExit(f'only 32 bits per pixel handled, got {bpp}')
    img = Image.new('RGB', (width, height))
    out = []
    for y in range(height):
        row = px[y * bpl:y * bpl + width * 4]
        for x in range(width):
            b, g, r, _ = row[x*4:x*4+4]      # X stores BGRX on little endian
            out.append((r, g, b))
    img.putdata(out)
    return img

if __name__ == '__main__':
    read_xwd(sys.argv[1]).save(sys.argv[2])
    print('wrote', sys.argv[2])
