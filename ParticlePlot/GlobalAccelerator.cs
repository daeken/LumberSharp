using System.Collections.Generic;
using System.Numerics;

namespace ParticlePlot {
	public class GlobalAccelerator : Thing {
		public Vector2 Acceleration;
		
		public GlobalAccelerator(Vector2 acceleration) => Acceleration = acceleration;

		public override void Update(ParticleSystem particleSystem) {
			var vel = Acceleration * particleSystem.TimeDelta;
			foreach(var particle in particleSystem.Particles)
				particle.Velocity += vel;
		}
	}
}