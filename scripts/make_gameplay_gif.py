"""Assembles frames captured from the running game into assets/gameplay.gif.

The frames come from the real SadConsole window, not from a simulation. To
capture a fresh set on a machine with no display:

  1. Start a virtual display that offers GLX:

       Xvfb :99 -screen 0 1200x760x24 +extension GLX +extension RENDER &

  2. Work out the walk, and write the run it starts from:

       dotnet run --project tools/RogueBit.Route -- --seed 2 --explored 45 \
           --save /tmp/rec

     That tool prints the keys. It plays the long approach itself and saves,
     so only the last twenty or so keys have to be replayed. A hundred key
     replay was tried first and the run wandered off the plan: keys go
     missing, and the further the run gets from the save the less any of it
     matches. A short walk from a save is the whole trick.

  3. Start the game against that display with software rendering, pointed at
     the same save, and replay the keys:

       DISPLAY=:99 XDG_DATA_HOME=/tmp/rec LIBGL_ALWAYS_SOFTWARE=1 \
           GALLIUM_DRIVER=llvmpipe ./RogueBit --continue &

  4. Grab each frame with `xwd -id <window>`, one after every key.

  5. Point this script at the directory of .xwd files.

Three things about step 3 that cost an afternoon each.

Hold the key down. `xdotool key h` presses and releases in a few
milliseconds. The window reads the keyboard once a frame, and under llvmpipe
a frame can take longer than that, so most taps are never seen at all. Of
twenty five tapped keys, three arrived. With `keydown`, a wait, then `keyup`,
all twenty five arrived.

There is no window manager on a bare Xvfb, so `xdotool windowactivate` says
so and gives up. Use `xdotool windowfocus --sync <window>` once, then plain
`xdotool key`, which goes to whatever holds the focus. Do not use
`xdotool key --window <window>`: those events reach the window and it ignores
them.

Check the result rather than trusting it. The status row shows the turn
count. It has to have moved by exactly the number of keys sent. Anything less
means keys went missing and the recording is of a run nobody planned.

One warning worth keeping. Do not clean up with `pkill -f "Xvfb :99"`: the
pattern matches the command line of the shell running the script, so the
script kills itself. Use `pkill -x Xvfb`.
"""
import glob
import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from xwd_to_image import read_xwd
from PIL import Image

BG = (10, 12, 14)   # Theme.Background

frame_dir = sys.argv[1] if len(sys.argv) > 1 else '/tmp/frames'
files = sorted(glob.glob(f'{frame_dir}/*.xwd'))
if not files:
    raise SystemExit('no frames')

frames = [read_xwd(f) for f in files]
print(f'{len(frames)} frames at {frames[0].size}')

# Crop to what is actually drawn across the whole run, so the GIF is not
# mostly unexplored blackness. The bounds are the union over every frame.
left, top, right, bottom = frames[0].width, frames[0].height, 0, 0
for im in frames:
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y] != BG:
                left = min(left, x); right = max(right, x)
                top = min(top, y); bottom = max(bottom, y)

pad = 8
box = (max(0, left - pad), max(0, top - pad),
       min(frames[0].width, right + pad + 1), min(frames[0].height, bottom + pad + 1))
print('content box', box, '->', (box[2] - box[0], box[3] - box[1]))

frames = [im.crop(box) for im in frames]
out = pathlib.Path(__file__).resolve().parent.parent / 'assets' / 'gameplay.gif'
frames[0].save(out, save_all=True, append_images=frames[1:],
               duration=140, loop=0, optimize=True)
print(f'wrote {out}')
