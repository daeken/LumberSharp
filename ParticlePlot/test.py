particleSystem.LowerBound = vec2(-1000, -1000)
particleSystem.UpperBound = vec2(1000, 2000)

particleSystem.Add(RadialGenerator(
    position=vec2(0, 0), 
    radius=50, 
    particlesPerSecond=10, 
    velocityAverage=30, 
    velocitySpread=10, 
    massAverage=10, 
    massSpread=2
))

#particleSystem.Add(GlobalAccelerator(vec2(0, -5)))

particleSystem.Add(Repeller(vec2(0, 250), 10000))

particleSystem.Add(Attractor(vec2(-250, 250), 1000))
particleSystem.Add(Attractor(vec2(250, 250), 1000))
particleSystem.Add(Attractor(vec2(0, 500), 100000))

particleSystem.Add(RadialGenerator(
    position=vec2(0, 1000), 
    radius=50, 
    particlesPerSecond=10, 
    velocityAverage=30, 
    velocitySpread=10, 
    massAverage=10, 
    massSpread=2
))

particleSystem.Run(30, 0.01)
