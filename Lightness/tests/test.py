scene.Width = 8000
scene.Height = 8000
scene.EdgePreview = False
page.Width = 297
page.Height = 420

scene.Camera = PerspectiveCamera()
scene.Camera.Position = vec3(0, -100, 0)
scene.Camera.LookAt = vec3(0, 0, 0)

# knot = LoadStl("knot.stl")
# scene.Add(knot.Rotate(vec3(0, 0, 1), PI / 3).Translate(vec3(5, 0, -10)))

# egg2 = LoadStl("egg-2.stl")
# scene.Add(egg2.Rotate(vec3(0, 0, 1), PI / 5).Translate(vec3(0, 30, -25)))

egg = LoadStl("knotwork-egg.stl")
scene.Add(egg.Rotate(vec3(0, 0, 1), PI / 5).Translate(vec3(0, 30, -35)))

#radioTower = LoadStl("radiotower.stl")
#scene.Add(radioTower.Rotate(vec3(0, 0, 1), PI / 4).Translate(vec3(0, 250, -100)))

# statue = LoadStl("statue.stl")
# scene.Add(statue.Translate(vec3(-225, -225, -60)))

# csphere = LoadStl("csphere4.stl")
# scene.Add(csphere.Rotate(vec3(1, 0, 0), -PI / 3).Rotate(vec3(0, 0, 1), -PI / 6).Translate(vec3(0, -50, 0)))

# cbox = LoadStl("cbox.stl")
# scene.Add(cbox.Rotate(vec3(1, 0, 0), -PI / 3).Rotate(vec3(0, 0, 1), -PI / 6).Translate(vec3(0, -50, 0)))

# twist = LoadStl("twist2.stl")
# scene.Add(twist.Translate(vec3(0, -50, 0)))

# column = LoadStl("column.stl")
# scene.Add(column.Translate(vec3(0, -50, -10)))

# suzanne = LoadStl("suzanne.stl")
# scene.Add(suzanne)

# boxes = LoadStlCentered("boxes.stl")
# scene.Add(boxes.Rotate(vec3(0, 1, 1), -PI / 4).Translate(vec3(0, -50, 0)))
