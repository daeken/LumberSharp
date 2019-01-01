using System.Collections.Generic;

namespace Lightness.Renderer {
	public abstract class Scene {
		public int Width = 4000, Height = 4000;
		public bool Preview, EdgePreview;
		public Camera Camera;
	}

	public class RasterScene : Scene {
		public readonly List<Model> Models = new List<Model>();

		public void Add(Model model) => Models.Add(model);
	}

	public class RaymarchedScene : Scene {
		public string FragmentShader;

		public RaymarchedScene(string fragmentShader) => FragmentShader = fragmentShader;
	}
}
