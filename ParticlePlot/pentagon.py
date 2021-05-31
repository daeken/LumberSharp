from math import *

particleSystem.LowerBound = vec2(-1000, -1000)
particleSystem.UpperBound = vec2(1000, 1000)

particleSystem.Add(RadialGenerator(
    position=vec2(0, 0), 
    radius=50, 
    particlesPerSecond=100, 
    velocityAverage=30, 
    velocitySpread=10, 
    massAverage=10, 
    massSpread=2
))

for i in xrange(5):
    pos = rotate(vec2(500 + sin(i) * 20, 0), 3.1416 * 2 / 5 * (i + .5))
    particleSystem.Add(Attractor(pos, 10000))
    particleSystem.Add(Repeller(pos * 3, 200000))

particleSystem.Run(30, 0.2)
