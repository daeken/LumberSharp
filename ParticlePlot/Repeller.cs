using System.Numerics;

namespace ParticlePlot {
	public class Repeller(Vector2 position, float mass) : Thing {
		public Vector2 Position = position;
		public float Mass = mass;
		
		public override void Update(ParticleSystem particleSystem) {
			foreach(var particle in particleSystem.Particles) {
				var dir = particle.Position - Position;
				var strength = (Mass * particle.Mass) / dir.LengthSquared() * particleSystem.TimeDelta;
				particle.Velocity += dir.Normalized() * strength;
			}
		}
	}
}
