from PIL import Image
import os, sys

path = os.getcwd()+"\\"
basefile = "dollymix"
ext = ".png"
size = int(input("size: "))
offset = float(input("offset: "))

base = Image.open(path+basefile+ext)

reverse = size > 0
size = abs(size)

amount = base.height // size


if not os.path.exists(path+"test"):
    os.makedirs(path+"test")

copies = 24
scaleFactor = 4
for rot in range(copies):
    angle = rot*(360/copies)
    print(path+f"{angle:03}.png")
    out = Image.new("RGBA", (size*scaleFactor, size*scaleFactor), (0,0,0,0))
    for i in range(amount):
        if(reverse):
            out.alpha_composite(base.crop((0,size*(amount-1-i),base.width,size*(amount-i))).resize((size*scaleFactor, size*scaleFactor), 0).rotate(angle), (0,round(-i*scaleFactor*offset)))
        else:
            out.alpha_composite(base.crop((0,size*i,base.width,size*(i+1))).resize((size*scaleFactor, size*scaleFactor), 0).rotate(angle), (0,round(-i*scaleFactor*offset)))

    out.save(path+"test\\"+f"{angle:03}.png")
