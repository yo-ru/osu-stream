<p align="center">
  <img src="Artwork/osu logo white.jpg">
</p>

# osu!stream

tap, slide, hold and spin to a **beat you can feel!**

## Status

This is basically a “finished” project for peppy, but I plan on putting more life back into it.

#### Plans
- Bring leaderboards back.
- Implement a beatmap submission system.
- Multiplayer (likely a 1v1 system.)
- Clean up README.md a lot more.


## History
For more information on the history of osu!stream, here's some further reading:
- Read peppy's [blog post](https://blog.ppy.sh/osu-stream-2020-release/) about the final release.
- ["osu!stream" on osu! wiki](https://osu.ppy.sh/help/wiki/osu!stream).
- Visit the [App Store](https://apps.apple.com/us/app/osu-stream/id436952197) or [Play Store](https://play.google.com/store/apps/details?id=sh.ppy.osustream) listing page.

## Contributions
While contributions are welcomed I likely won’t have time to review anything too large. There are some exceptions listed below, mostly which fall under the clean-up umbrella – trying to get things into a good final state:

- Bring code standards in line with osu!lazer (using the same DotSettings configuration).
- Doing something about the amount of compile-time `#if`s in the code (especially in `using` blocks).
- Bringing the `arcade` branch up-to-date and potentially merging changes back into master.
- Documentation of any kind.
- Code quality improvements of any kind (as long as they can easily be reviewed and are guaranteed to not change behaviour). Keep individual PRs under 200 lines of change, optimally.

## Running

If you are looking to play osu!stream, the [app store](https://apps.apple.com/us/app/osu-stream/id436952197) or [play store](https://play.google.com/store/apps/details?id=sh.ppy.osustream) release is the best way to consume it.

## Building

The primary target of osu!stream is iOS. It should compile with relatively little effort via `osu!stream.sln`; tested via:
- Visual Studio for Mac and Rider.
- Visual Studio 2022 for Windows utilizing Xarmin "Pair with Mac" for build/deploy.

It will also run on desktop (tested only on windows) via `osu!stream_desktop.sln`. Note that the desktop release needs slightly differently packaged beatmaps (as it doesn't support `m4a` of released beatmaps).

In addition, there is an [arcade branch](https://github.com/ppy/osu-stream/tree/arcade) for the osu!arcade specific release. This branch really needs to be merged up-to-date with the latest master.

## Mapping

The process of mapping for osu!stream is still done via the osu! editor. I highly recommend reading [this document](https://docs.google.com/document/d/1FYmHhRX-onR-osgTS6uHSOZuu_0JEbfRZePVySvvr9g), written by peppy, for more in-depth specifics on osu!stream mapping.

#### osu!stream tester
osu!stream tester (`StreamTester`) can be utilized by mappers to test & generate osz2 files that will load into osu!stream. 

You can get a functional build of osu!stream tester by following these instructions:
- Build using `ReleaseNoEncrpytion` build configuration.
- Navigate to `osu-stream/StreamTester/bin/Release/`.
- Create a new folder called `Beatmaps` (I will fix this in a future commit.)
- Run `StreamTester.exe`.

#### osu!stream mapper build
You can get a mapper-centric build of osu!stream by following these instructions:
- Build using `Debug` build configuration.
- Navigate to `osu-stream/osu!stream/bin/Debug/`.
- Run `osu!stream.exe`.
- Launch the game, as the game launches, click on the Headphones, **ENABLED MAPPER MODE** will appear.
- You can then use in-game mapper features:
    - Pause in Autoplay by clicking anywhere.
    - Enable/Disable Stream changes by pressing the footer in a levels' mode selector.

#### A note from Yoru:
I hope to reach out to peppy soon to request permission to release functional binaries of `osu!stream tester`; following that I will provide tutorials on:
- Mapping for osu!stream.
- Preparing an osu! map for osu!stream.
- Utilizing `osu!stream tester` for testing on osu!stream.
- Generating osz2 (beatmap) files for both desktop & iOS.

I will also, hopefully (with permission), release an osu!stream IPA (iOS app) that supports uploading custom beatmaps from your computer.

Following that I will release a tool that automates the conversion of osu! maps to osu!stream maps.

## Licence

*osu!stream*'s code is released under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

Please note that this *does not cover* the usage of the "osu!" or "ppy" branding in any software, resources, advertising or promotion, as this is protected by trademark law. As in don't go uploading builds of this without permission.

Also a word of caution that there may be exceptions to this license for specific resources included in this repository. The primary purpose of publicising this source code is for educational purposes; if you plan on using it in another way I ask that you contact me [via email](mailto:pe@ppy.sh) or [open an issue](https://github.com/ppy/osu-stream/issues) first!
