from PIL import Image
import os, sys

path = os.getcwd()
basefile = "dollymix"
ext = ".png"
size = int(input("size: "))
offset = float(input("offset: "))
repeat = int(input("repeat: "))

base = Image.open(os.path.join(path,basefile+ext)))

reverse = size > 0
size = abs(size)

amount = base.height // size

testfolder = os.path.join(path, "test")
if not os.path.exists(testfolder):
    os.makedirs(testfolder)

copies = 24
scaleFactor = 4
for rot in range(copies):
    angle = rot*(360/copies)
    print(f"{angle:03}.png")
    out = Image.new("RGBA", (size*scaleFactor, size*scaleFactor), (0,0,0,0))
    for i in range(amount):
        for r in range(repeat):
            vertOffset = round(-(i-1+r/repeat)*scaleFactor*offset)
            if(reverse):
                out.alpha_composite(base.crop((0,size*(amount-1-i),base.width,size*(amount-i))).resize((size*scaleFactor, size*scaleFactor), 0).rotate(angle), (0,vertOffset))
            else:
                out.alpha_composite(base.crop((0,size*i,base.width,size*(i+1))).resize((size*scaleFactor, size*scaleFactor), 0).rotate(angle), (0,vertOffset))

    out.save(os.path.join(path, "test", f"{angle:03}.png"))
