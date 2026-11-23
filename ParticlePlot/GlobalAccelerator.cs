using System.Numerics;

namespace ParticlePlot {
	public class GlobalAccelerator(Vector2 acceleration) : Thing {
		public Vector2 Acceleration = acceleration;

		public override void Update(ParticleSystem particleSystem) {
			var vel = Acceleration * particleSystem.TimeDelta;
			foreach(var particle in particleSystem.Particles)
				particle.Velocity += vel;
		}
	}
}
