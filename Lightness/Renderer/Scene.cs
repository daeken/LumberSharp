using System.Collections.Generic;

namespace Lightness.Renderer {
	public class Scene {
		public readonly List<Model> Models = new List<Model>();

		public void Add(Model model) => Models.Add(model);
	}
}