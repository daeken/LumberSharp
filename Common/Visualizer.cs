#pragma warning disable CA1806
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using DoubleSharp.MathPlus;
using static SDL2.SDL;
using static Common.Helpers;

namespace Common; 

public class Visualizer : IDisposable {
	public static Visualizer Instance;
	readonly IntPtr Window;
	readonly IntPtr Renderer;
	readonly int Width, Height;
	bool Disposed;
	readonly ConcurrentQueue<Action> ActionQueue = new();
	readonly ConcurrentBag<AutoResetEvent> Waiters = new();

	bool Changed;
	readonly List<((byte R, byte G, byte B) Color, List<Vector2> Path)> Paths = new();
	readonly List<((byte R, byte G, byte B) Color, Vector2 Start, Vector2 End)> Arrows = new();

	public Visualizer(Action start, int width = 800, int height = 600, string title = "LumberSharp") {
		Instance = this;
		Width = width;
		Height = height;
		if(SDL_Init(SDL_INIT_VIDEO) < 0)
			throw new Exception("SDL failed to initialize");
		Window = SDL_CreateWindow(title, SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, width, height, SDL_WindowFlags.SDL_WINDOW_SHOWN);
		if(Window == IntPtr.Zero)
			throw new Exception("SDL failed to create window");
		SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "2");
		Renderer = SDL_CreateRenderer(Window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
		if(Renderer == IntPtr.Zero)
			throw new Exception("SDL failed to create renderer");

		new Thread(() => {
			start();
			Dispose();
		}).Start();

		while(!Disposed) {
			while(SDL_PollEvent(out var e) != 0) {
				switch(e.type) {
					case SDL_EventType.SDL_KEYUP:
					case SDL_EventType.SDL_MOUSEBUTTONUP:
						lock(Waiters) {
							foreach(var waiter in Waiters)
								waiter.Set();
							Waiters.Clear();
						}
						break;
				}
			}
			while(ActionQueue.TryDequeue(out var func)) {
				func();
			}
			Render();
			Thread.Sleep(10);
		}

		Instance = null;
		SDL_DestroyRenderer(Renderer);
		SDL_DestroyWindow(Window);
	}

	public static void Run(Action func) => Run(800, 600, func);
	public static void Run(int width, int height, Action func) =>
		Run(width, height, "LumberSharp", func);
	public static void Run(int width, int height, string title, Action func) =>
		_ = new Visualizer(func, width, height, title);

	public void Dispose() {
		lock(this) {
			if(Disposed) return;
			Disposed = true;
		}
	}

	void Enqueue(Action func) => ActionQueue.Enqueue(func);

	public static void WaitForInput() {
		if(Instance == null) return;
		var are = new AutoResetEvent(false);
		lock(Instance.Waiters)
			Instance.Waiters.Add(are);
		are.WaitOne();
	}

	void Render() {
		lock(this) {
			if(!Changed || (Paths.Count == 0 && Arrows.Count == 0)) return;
			SDL_SetRenderDrawColor(Renderer, 255, 255, 255, 255);
			SDL_RenderClear(Renderer);

			var bf = Paths.Select(x => x.Path).SelectMany(x => x)
				.Concat(Arrows.Select(a => new[] { a.Start, a.End }).SelectMany(x => x)).ToList();
			var min = bf.Aggregate(Vector2.Min);
			var max = bf.Aggregate(Vector2.Max);
			var size = max - min;
			if(size.Y < 0.01f)
				size = size with { Y = 1 };

			var padding = 0.9f;
			var width = Width * padding;
			var height = Height * padding;
			var offset = new Vector2(Width * (1 - padding), Height * (1 - padding)) / 2;

			var par = size.X / size.Y;
			var war = width / height;
			var scale = war > par ? height / size.Y : width / size.X;
			var centering = (new Vector2(width, height) - size * scale) / 2 + offset;

			SDL_Point ConvPoint(Vector2 p) => ((p - min) * scale + centering).ToPoint();

			foreach(var (c, p) in Paths) {
				SDL_SetRenderDrawColor(Renderer, c.R, c.G, c.B, 255);
				SDL_RenderDrawLines(Renderer, p.Select(ConvPoint).ToArray(), p.Count);
			}

			var headWidth = 20f;
			foreach(var (c, _a, _b) in Arrows) {
				SDL_SetRenderDrawColor(Renderer, c.R, c.G, c.B, 255);
				var a = ConvPoint(_a).ToVector();
				var b = ConvPoint(_b).ToVector();
				var dir = (b - a).Normalize();
				Renderer.DrawLine(a, b);
				var tip = dir * headWidth + b;
				var left = dir.Rotate(-MathF.PI / 2) * (headWidth / 2) + b;
				var right = dir.Rotate(MathF.PI / 2) * (headWidth / 2) + b;
				Renderer.DrawLine(left, right);
				Renderer.DrawLine(right, tip);
				Renderer.DrawLine(tip, left);
			}

			SDL_RenderPresent(Renderer);
			Changed = false;
		}
	}

	static (byte R, byte G, byte B) ParseColor(string color) => 
		color.Split("##", 2)[0].ToLower() switch {
			"black" => (0, 0, 0),
			"white" => (255, 255, 255),
			"red" => (255, 0, 0),
			"green" => (0, 255, 0),
			"blue" => (0, 0, 255),
			"yellow" => (255, 255, 0),
			"cyan" => (0, 255, 255),
			"purple" => (255, 0, 255),
			{} x when x[0] == '#' && x.Length == 4 => (
				byte.Parse(x[1] + x[1].ToString(), NumberStyles.HexNumber),
				byte.Parse(x[2] + x[2].ToString(), NumberStyles.HexNumber),
				byte.Parse(x[3] + x[3].ToString(), NumberStyles.HexNumber)
			),
			{} x when x[0] == '#' && x.Length == 7 => (
				byte.Parse(x[1..3], NumberStyles.HexNumber),
				byte.Parse(x[3..5], NumberStyles.HexNumber),
				byte.Parse(x[5..], NumberStyles.HexNumber)
			),
			{} x => throw new Exception($"Unsupported color: {x}")
		};

	public static void DrawLine(Vector2 a, Vector2 b, string color = "black") => 
		Instance?.Enqueue(() => {
			lock(Instance) {
				Instance.Changed = true;
				Instance.Paths.Add((ParseColor(color), new() { a, b }));
			}
		});

	public static void DrawPath(List<Vector2> path, string color = "black") =>
		Instance?.Enqueue(() => {
			lock(Instance) {
				Instance.Changed = true;
				Instance.Paths.Add((ParseColor(color), path.ToList()));
			}
		});

	public static void DrawPaths(List<List<Vector2>> paths, string color = "black") =>
		Instance?.Enqueue(() => {
			var c = ParseColor(color);
			lock(Instance) {
				Instance.Changed = true;
				Instance.Paths.AddRange(paths.Select(x => (c, x.ToList())));
			}
		});

	public static void DrawPaths(List<(string Color, List<Vector2> Path)> paths) =>
		Instance?.Enqueue(() => {
			lock(Instance) {
				Instance.Changed = true;
				Instance.Paths.AddRange(paths.Select(x => (ParseColor(x.Color), x.Path.ToList())));
			}
		});

	public static void DrawArrow(Vector2 a, Vector2 b, string color = "black") => 
		Instance?.Enqueue(() => {
			lock(Instance) {
				Instance.Changed = true;
				Instance.Arrows.Add((ParseColor(color), a, b));
			}
		});

	public static void DrawRect(Vector2 min, Vector2 max, string color = "black") =>
		DrawPath(new() {
			min, 
			new(max.X, min.Y),
			max,
			new(min.X, max.Y),
			min
		}, color);

	public static void Clear() =>
		Instance?.Enqueue(() => {
			lock(Instance) {
				Instance.Changed = true;
				Instance.Arrows.Clear();
				Instance.Paths.Clear();
			}
		});
}