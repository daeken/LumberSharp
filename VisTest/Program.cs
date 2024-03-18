using System.Numerics;
using Common;

Visualizer.Run(() => {
	Visualizer.DrawLine(Vector2.Zero, Vector2.One);
	Visualizer.WaitForInput();
	Visualizer.DrawArrow(Vector2.Zero, new(-1, 1), "green");
	Visualizer.WaitForInput();
});