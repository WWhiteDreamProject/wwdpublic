from PIL import Image
import os, sys

path = os.getcwd()
basefile = "dollymix"
ext = ".png"
size = int(input("size: "))
offset = int(input("downward offset (in pixels): "))

base = Image.open(os.path.join(path,basefile+ext))

reverse = size > 0
size = abs(size)

amount = base.height // size

out = Image.new("RGBA", (size, size), (0,0,0,0))

for i in range(amount):
    if(reverse):
        out.alpha_composite(base.crop((0,size*(amount-1-i),base.width,size*(amount-i))).rotate(90), (0,-i-offset))
    else:
        out.alpha_composite(base.crop((0,size*i,base.width,size*(i+1))).rotate(90), (0,-i-offset))

out.save(os.path.join(path,"icon.png"))
