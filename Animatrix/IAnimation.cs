using System.Collections.Generic;
using System.Numerics;

namespace Animatrix {
	public interface IAnimation {
		Vector2 Dimensions { get; }

		List<List<(string, List<Vector2>)>> GenerateFrames();
	}
}