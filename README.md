# Tales of Berseria Tools
A collection of tools and resources for Tales of Berseria.

# ImHex Patterns
The `hexpat` directory contains pattern files for [ImHex](https://github.com/WerWolv/ImHex):
* `scpackr.hexpat`: `.SCPACKR` script package files.

# ScpkTool
A tool for unpacking and repacking `.SCPACKR` script package files. These contain Lua scripts that implement UI and gameplay elements. Usage:

`scpktool unpack <in.scpackr> <out directory>`

`scpktool pack <in directory> <out.scpackr>`

`scpktool pack <in directory> <out.scpackr> big-endian` (for PS3)
