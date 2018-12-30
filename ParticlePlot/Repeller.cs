using System.Collections.Generic;
using System.Numerics;

namespace ParticlePlot {
	public class Repeller : Thing {
		public Vector2 Position;
		public float Mass;

		public Repeller(Vector2 position, float mass) {
			Position = position;
			Mass = mass;
		}
		
		public override void Update(ParticleSystem particleSystem) {
			foreach(var particle in particleSystem.Particles) {
				var dir = particle.Position - Position;
				var strength = (Mass * particle.Mass) / dir.LengthSquared() * particleSystem.TimeDelta;
				particle.Velocity += dir.Normalized() * strength;
			}
		}
	}
}