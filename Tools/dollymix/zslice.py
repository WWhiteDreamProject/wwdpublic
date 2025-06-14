from PIL import Image
import os, sys

path = os.getcwd()+"\\"
basefile = "dollymix"
ext = ".png"
size = int(input("size: "))
reverse = size > 0
size = abs(size)


base = Image.open(path+basefile+ext)
amount = base.height // size


for i in range(amount):
    filename = basefile+f"{i+1}"+ext
    print(filename)
    if(reverse):
        base.crop((0,size*(amount-1-i),base.width,size*(amount-i))).save(path+filename)
    else:
        base.crop((0,size*i,base.width,size*(i+1))).save(path+filename)

# yes, this will throw if the file does not exist.
# no, i do not care. it does what it was designed to do and that's good enough.
base = Image.open(path+basefile+"-unshaded"+ext)
amount = base.height // size


for i in range(amount):
    filename = basefile+f"{i+1}"+"-unshaded"+ext
    print(filename)
    if(reverse):
        base.crop((0,size*(amount-1-i),base.width,size*(amount-i))).save(path+filename)
    else:
        base.crop((0,size*i,base.width,size*(i+1))).save(path+filename)
