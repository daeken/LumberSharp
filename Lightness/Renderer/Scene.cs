using System.Collections.Generic;

namespace Lightness.Renderer {
	public class Scene {
		public int Width = 4000, Height = 4000;
		public bool Preview, EdgePreview;
		public Camera Camera;
		public readonly List<Model> Models = new List<Model>();

		public void Add(Model model) => Models.Add(model);
	}
}