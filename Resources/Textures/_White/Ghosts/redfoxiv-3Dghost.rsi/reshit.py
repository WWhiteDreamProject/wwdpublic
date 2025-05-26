from PIL import Image
import os, sys

path = "C:\\Users\\Limon\\repos\\csharp\\wwdpublic\\Resources\\Textures\\_White\\Ghosts\\redfoxiv-3Dghost.rsi\\"
ext = ".png"
size = 128

files = [f"hqyaitca{x}.png" for x in range(1,98)]

files.reverse()

for i in range(len(files)):
    if not os.path.isfile(path+files[i]) or not os.path.isfile(path+files[i-1]):
        continue
    print(f"{files[i]}+{files[i-1]}")
    
    prev = Image.open(path+files[i-1])
    current = Image.open(path+files[i])
    res = Image.alpha_composite(current, prev)
    res.save(path+files[i])
a