using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Renderer {
	public class Scene : IEnumerable<Octree> {
		public Vector3 AmbientColor;
		
		public readonly List<Octree> Octrees = new List<Octree>();

		public void Add(Mesh mesh) {
			Octrees.Add(new Octree(mesh, 1500));
		}
		
		IEnumerator<Octree> IEnumerable<Octree>.GetEnumerator() => Octrees.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => Octrees.GetEnumerator();
	}
}