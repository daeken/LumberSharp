LumberSharp
===========

Stylized 3d -> SVG converter for plotters, inspired by Escher's woodcuts.

Dependencies
------------

- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0)

Installation
------------

	git clone https://github.com/daeken/LumberSharp
	cd LumberSharp
	dotnet build LumberSharp.sln

Running
-------

From the Lightness directory under LumberSharp:

	dotnet run -- tests/test.py test.svg

Scene Scripts
-------------

LumberSharp uses Python to define scenes and rendering parameters:

- Set page parameters (defaults to A4 in portrait mode with 10mm margins on all sides)
- Define a camera
- Load some number of meshes
- Add them to the scene
- Specify the rendering size (default 1000x1000px -- you'll want to go higher, e.g. 4000-8000)

Set `scene.Preview = True` to render the scene normals to `preview.png`.
