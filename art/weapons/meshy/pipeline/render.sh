#!/bin/bash
# render.sh <id> <lunghezza_m>
G=/home/claude/tools/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64
cd /home/claude/weapons/review_proj
timeout 300 xvfb-run -a -s "-screen 0 1600x1000x24" $G --path . --rendering-driver opengl3 -- /home/claude/weapons/out/$1/$1.glb /home/claude/weapons/review/$1 $2 2>/dev/null | grep ORTHO | awk '{print $2}' > /home/claude/weapons/review/$1_ortho.txt
cd /home/claude/weapons && python3 sheet.py $1 $2
