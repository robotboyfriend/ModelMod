ModelMod is a system for modifying art in games.  
It works by replacing 3D models (and textures, optionally) at the renderer level.

You start by selecting and snapshotting some model in the target game.
This snapshot can then be edited in a 3D modeling tool.  When the game runs
and draws the original model, ModelMod transparently swaps in your modified
version, and renders that in place of the original.

[![Join the chat at https://gitter.im/jmquigs/ModelMod](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/jmquigs/ModelMod?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Demos
-----

Screenshot of a simple mod from the demo videos:

![skyrim](https://raw.githubusercontent.com/jmquigs/ModelMod-demo/master/screenshots/skyrimebony.jpg)


### Videos

* Short intro video: https://www.youtube.com/watch?v=HDAN63VyJSY
* Longer intro video with commentary: https://www.youtube.com/watch?v=ijmLTTzCGuU
* Testing a game for compatibility: https://www.youtube.com/watch?v=ijmLTTzCGuU
* For developers, guide to making changes to support a new game: https://www.youtube.com/watch?v=KGN7MSjSx_U

Requirements
------------

* Windows.
* Only a few games have been tested.  Programming effort is usually required
to get new games to work.
* The target game must use D3D9 for rendering.  Support for other versions is possible,
but has not been implemented.
* For animated models, the target game
must use GPU based animation.  Support for CPU animation, common in older games, is
known to be possible but is not currently implemented.
* Blender is the only 3D modeling tool supported at this time.  
If you want to write an exporter for something else, See [Contributing](#Contributing).

Installation
------------

Non-programmers should use the binary package: link

Also, [look at the User's Guide](Docs/userguide/README.md).

Development
-----------

ModelMod is written in a combination of C++ and F#.  Most of the core code
is in F#, so hacking on it can be easier than you might think, assuming you
know or are willing to learn F#.

For install & build instructions, [check out the Dev Guide](Docs/devguide/README.md).

### Contributing

Pull Requests are welcome!  In particular, these things are desirable:

* Support for new games, or improved support for existing games

* Improvements to the DLL injection and related initialization code

* Support for recent versions of D3D, especially 11/12.  

* Support for other 3D tools (port MMObj format code, new Snapshot profiles)

License
-------

Unless otherwise noted here, ModelMod code is licensed under the terms of the
GNU GPL version 3.

The included c++ format library (format.h/format.cc) is licensed under its
author's license - see format.h for details.

ModelMod references various third party .NET libraries which have their own
licenses.  

The following components of ModelMod are in the Public Domain.  These programs are distributed WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

* The MMLoader injection utility (files in "MMLoader" subdirectory)
* All unit/integration tests (files in the "Test.MMManaged" subdirectory)
