# StreamDeckVMix
Simple program to control vMix from a Streamdeck

Uses the OpenStreamDeck C# wrapper (https://github.com/OpenStreamDeck/StreamDeckSharp), as well as some code from vMixScheduler (https://github.com/Tim-R/vScheduler).

I am not a programmer, but did want to share my efforts in case anyone could benefit from them.

Support for 4 inputs, 3 overlays, streaming, recording, quick play, and cut.

The three overlay channels send the item currently in preview to the overlay channel.

Add the icons folder as subdirectory to wherever you put the executable file (from bin/Release).

Expected to be run on the same computer as vMix - change the vMix web controller URL if needed.
