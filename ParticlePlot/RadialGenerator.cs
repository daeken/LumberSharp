using System;
using System.Numerics;

namespace ParticlePlot {
	public class RadialGenerator : Thing {
		public Vector2 Position;
		public float Radius, ParticlesPerSecond, VelocityAverage, VelocitySpread, MassAverage, MassSpread;
		int Emitted;
		
		public RadialGenerator() {}
		public RadialGenerator(Vector2 position, float radius, float particlesPerSecond, 
			float velocityAverage, float velocitySpread, float massAverage, float massSpread) {
			Position = position;
			Radius = radius;
			ParticlesPerSecond = particlesPerSecond;
			VelocityAverage = velocityAverage;
			VelocitySpread = velocitySpread;
			MassAverage = massAverage;
			MassSpread = massSpread;
		}
		
		public override void Update(ParticleSystem particleSystem) {
			var space = 1 / ParticlesPerSecond;
			var totalTime = Emitted * space;
			var count = (int) ((particleSystem.Time - totalTime) / space);
			Emitted += count;
			for(var i = 0; i < count; ++i) {
				var angle = (float) particleSystem.Random.NextDouble() * MathF.PI * 2;
				var dir = new Vector2(Radius, 0).Rotate(angle);
				var vel = VelocityAverage + VelocitySpread * ((float) particleSystem.Random.NextDouble() * 2 - .5f);
				var mass = MassAverage + MassSpread * ((float) particleSystem.Random.NextDouble() * 2 - .5f);
				particleSystem.Add(new Particle { Position = Position + dir, Velocity = dir.Normalized() * vel, Mass = mass });
			}
		}
	}
}