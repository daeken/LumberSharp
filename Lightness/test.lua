scene.width = 4000;
scene.height = 4000;
scene.preview = false;

scene.camera = PerspectiveCamera.create();
scene.camera.position = vec3(0, -100, 0);
scene.camera.lookAt = vec3(0, 0, 0);

knot = StlLoader.load("knot.stl");
scene.add(knot.rotate(vec3(0, 0, 1), PI / 3).translate(vec3(5, 0, -10)));

--[[
    var egg2Mesh = StlLoader.Load("egg-2.stl");
    scene.Add(new Model(egg2Mesh).Rotate(Vector3.UnitZ, MathF.PI / 5).Translate(new Vector3(0, 30, -25)));
    var radioTowerMesh = StlLoader.Load("radiotower.stl");
    scene.Add(new Model(radioTowerMesh).Rotate(Vector3.UnitZ, MathF.PI / 4).Translate(new Vector3(0, 250, -100)));
    var statueMesh = StlLoader.Load("statue.stl");
    scene.Add(new Model(statueMesh).Translate(new Vector3(-225, -225, -60)));
    var vaseMesh = StlLoader.Load("vase.stl");
    scene.Add(new Model(vaseMesh).Translate(new Vector3(0, 1000, 0)));
    var csphereMesh = StlLoader.Load("csphere4.stl");
    scene.Add(new Model(csphereMesh).Rotate(Vector3.UnitX, -MathF.PI / 3).Rotate(Vector3.UnitZ, -MathF.PI / 6).Translate(new Vector3(0, -50, 0)));
    var knotMesh = StlLoader.Load("knot.stl");
    scene.Add(knotMesh.Rotate(Vector3.UnitZ, MathF.PI / 3).Translate(new Vector3(5, 0, -10)));
]]