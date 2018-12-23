using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Renderer {
	public class Scene : IEnumerable<Octree>, IEnumerable<Light> {
		public Vector3 AmbientColor;
		
		public readonly List<Octree> Octrees = new List<Octree>();
		public readonly List<Light> Lights = new List<Light>();

		public void Add(Mesh mesh) {
			Octrees.Add(new Octree(mesh, 1500));
		}
		
		public void Add(Light light) => Lights.Add(light);
		
		IEnumerator<Octree> IEnumerable<Octree>.GetEnumerator() => Octrees.GetEnumerator();
		IEnumerator<Light> IEnumerable<Light>.GetEnumerator() => Lights.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => Octrees.GetEnumerator();
	}
}