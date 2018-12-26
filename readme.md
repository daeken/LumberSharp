LumberSharp
===========

Stylized 3d -> SVG converter for plotters, inspired by Escher's woodcuts.

Dependencies
------------

- .NET Core 2.1+ - https://dotnet.microsoft.com/download

Installation
------------

Ensure dependencies are installed.

	git clone https://github.com/daeken/LumberSharp

Running
-------

From the Lightness directory under LumberSharp:

	dotnet run tests/test.lua test.svg

Lua Files
---------

LumberSharp uses Lua to define scenes and rendering parameters.  The format is largely straightforward:

- Define a camera
- Load some number of meshes
- Add them to the scene
- Specify the rendering size (default 1000x1000px -- you'll want to go higher, e.g. 4000-8000)

Setting `scene.preview = true;` will cause LumberSharp to emit a file called `preview.png` which will be the rendering of the normals of the scene.  This is useful for setting up just the right shot.
