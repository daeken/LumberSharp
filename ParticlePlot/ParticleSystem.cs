using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ParticlePlot {
	public class ParticleSystem {
		public readonly List<Particle> Particles = new List<Particle>();
		public readonly List<Particle> DeadParticles = new List<Particle>();
		public readonly List<Thing> Things = new List<Thing>();
		public readonly Random Random = new Random();

		public IEnumerable<Particle> AllParticles => ShowDead ? Particles.Concat(DeadParticles) : Particles;
		
		public float Time, TimeDelta;
		public Vector2 LowerBound, UpperBound;

		public bool ShowDead = true;
		
		public void Run(float seconds, float step) {
			TimeDelta = step;
			while(Time < seconds) {
				Things.ForEach(x => x.Update(this));
				Particles.ForEach(x => x.Update(this));
				Time += step;

				if(UpperBound != LowerBound) {
					for(var i = Particles.Count - 1; i >= 0; --i) {
						var particle = Particles[i];
						if(particle.Position.X < LowerBound.X || particle.Position.Y < LowerBound.Y ||
						   particle.Position.X > UpperBound.X || particle.Position.Y > UpperBound.Y) {
							particle.PositionHistory.RemoveAt(particle.PositionHistory.Count - 1);
							DeadParticles.Add(particle);
							Particles.RemoveAt(i);
						}
					}
				}
			}
		}

		public void Add(Particle particle) {
			particle.Record(this);
			Particles.Add(particle);
		}

		public void Add(Thing thing) => Things.Add(thing);
	}
}