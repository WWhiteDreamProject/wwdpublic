Copy these into the .rsi folder you're working on.

These require the pillow module ("pip install pillow")

zslice - splits a vertical slice strip produced by MagicaVoxel into separate files.
	 Expects the source file to be named "dollymix.png".
	 Asks for vertical size and then will split the image into that many rows.
	 If "dollymix-unshaded.png" is present, splits that aswell.
	 Does not automatically remove empty images.

zicon - uses the same "dollymix.png" file to produce a default sprite for the model.

ztest - does the same thing as zicon at a higher resolution and different angles.
	For checking how the "model" looks without starting the game.

All file names start with a "z" so that they appear last in the folder when sorted by name.
Also for the purposes of goida.

Recommended toolchain - MagicaVoxel for initial modeling -> Aseprite for layer edits and very basic animation -> the shit in this folder.