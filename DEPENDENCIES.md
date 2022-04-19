# Dependencies
To run this bot, you'll need 
- ffmpeg
- libsodium
- (lib)opus

## Windows
Luckily the Discord.NET-Maintainers have added `.dll`-files that are required to their repo. 
You can find them [here](https://github.com/discord-net/Discord.Net/tree/dev/voice-natives). 
Just drop them into the folder of `DAB.exe`.
(Consider to rename `libopus.dll` to `opus.dll` when the bot fails to play audio.)

Since this bot uses `ffmpeg` to encode audo-files into pcm, you'll need to install it onto your machine and add it's executable to your `PATH`. 
I recommend using some kind of package-manager, e.g. `scoop`.

## Linux-Arm - Raspberry Pi
You can also run the bot on a raspberry, I do so myself using a RPI 4B+.
It seems like D.NET-maintainers automatically asssume to know what we're doing in this case,
since no dependencies are provided here. You have to get them with your package-manager or build them from scratch:

### libsodium
Download the latest `-stable` tarball of libsodium [here](https://download.libsodium.org/libsodium/releases/).

Run
```bash
$ tar -xvf <downloaded libsodium-tarball>
$ cd <folder with extracted source>
$ ./configure
$ make && make check
$ sudo make install
```
Consider adding `-j 4` to each `make` to speed up the process.

### libopus
In this case, download the latest `opus-x.x.tar.gz` from [here](https://ftp.osuosl.org/pub/xiph/releases/opus/),
compiling it just as you did with libsodium.
This will place a `libopus.so` in `/usr/local/lib/libopus.so`.
It has to be linked to `opus.so`, like
```bash
$ ln -s /usr/local/lib/libopus.so /path/to/DAB/opus.so
```

After that, you should be ready to run!
