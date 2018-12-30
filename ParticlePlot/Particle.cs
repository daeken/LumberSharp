using System.Collections.Generic;
using System.Numerics;

namespace ParticlePlot {
	public class Particle {
		public Vector2 Position, Velocity;
		public float Mass;
		
		public readonly List<(float, Vector2)> PositionHistory = new List<(float, Vector2)>();

		public void Record(ParticleSystem particleSystem) => PositionHistory.Add((particleSystem.Time, Position));

		public void Update(ParticleSystem particleSystem) {
			if(Velocity.LengthSquared() == 0) return;
			Position += Velocity * particleSystem.TimeDelta;
			PositionHistory.Add((particleSystem.Time, Position));
		}
	}
}