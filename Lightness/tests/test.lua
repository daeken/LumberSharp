scene.width = 1000;
scene.height = 1000;
scene.preview = true;

scene.camera = PerspectiveCamera.create();
scene.camera.position = vec3(0, -100, 0);
scene.camera.lookAt = vec3(0, 0, 0);

--knot = StlLoader.load("knot.stl");
--scene.add(knot.rotate(vec3(0, 0, 1), PI / 3).translate(vec3(5, 0, -10)));

--egg2 = StlLoader.load("egg-2.stl");
--scene.add(egg2.rotate(vec3(0, 0, 1), PI / 5).translate(vec3(0, 30, -25)));

--radioTower = StlLoader.load("radiotower.stl");
--scene.add(radioTower.rotate(vec3(0, 0, 1), PI / 4).translate(vec3(0, 250, -100)));

--statue = StlLoader.load("statue.stl");
--scene.add(statue.translate(vec3(-225, -225, -60)));

csphere = StlLoader.load("csphere4.stl");
scene.add(csphere.rotate(vec3(1, 0, 0), -PI / 3).rotate(vec3(0, 0, 1), -PI / 6).translate(vec3(0, -50, 0)));
