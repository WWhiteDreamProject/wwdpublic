from PIL import Image
import os, sys

path = r"C:\Users\Limon\repos\csharp\wwdpublic\Resources\Textures\_White\Ghosts\redfoxiv-3Dghost.rsi\\"
basefile = "hqyaitca"
ext = ".png"
size = 128


base = Image.open(path+basefile+ext)

amount = base.height // size


#files = [f"hqyaitca{x}.png" for x in range(1,98)]

for i in range(amount):
    filename = basefile+f"{i+1}"+ext
    print(filename)
    base.crop((0,size*i,size,size*(i+1))).save(path+filename)
