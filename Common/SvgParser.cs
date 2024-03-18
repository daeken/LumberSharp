using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml;
using DoubleSharp.Linq;
using static Common.Helpers;
using static Common.SvgHelper;

namespace Common; 

public class SvgParser {
	class CssStack {
		readonly Stack<Dictionary<string, string>> Stack = new();

		public CssStack() => Stack.Push(new());

		public void Push() => Stack.Push(Stack.Peek().ToDictionary(x => x.Key, x => x.Value));
		public void Pop() => Stack.Pop();

		public bool TryGetValue(string key, out string value) => Stack.Peek().TryGetValue(key, out value);
		public string this[string key] {
			get => Stack.Peek().TryGetValue(key, out var value) ? value : "";
			set => Stack.Peek()[key] = value;
		}

		public override string ToString() =>
			string.Join('\n', Stack.Peek().Select(x => $"{x.Key}: {x.Value};"));
	}
	
	public readonly List<(string Color, List<Vector2> Path)> Paths = new();
	readonly int BezierStepsPerUnit;
	readonly Dictionary<string, XmlElement> Symbols = new();
	readonly bool IgnoreZ;

	SvgParser(XmlDocument doc, int bezierStepsPerUnit, bool ignoreZ) {
		BezierStepsPerUnit = bezierStepsPerUnit;
		IgnoreZ = ignoreZ;
		Parse(doc, Matrix4x4.Identity, new CssStack());
	}

	public static List<(string Color, List<Vector2> Path)> Load(string fn, int bezierStepsPerUnit = 100, bool ignoreZ = false) =>
		Parse(File.ReadAllText(fn), bezierStepsPerUnit, ignoreZ);

	public static List<(string Color, List<Vector2> Path)> Parse(string xml, int bezierStepsPerUnit = 100, bool ignoreZ = false) {
		var doc = new XmlDocument();
		doc.LoadXml(xml);
		var paths = new SvgParser(doc, bezierStepsPerUnit, ignoreZ).Paths;
		return paths.GroupBy(x => x.Color)
			.Select(x => 
				x.Select(y => y.Path).ToList()
					.ReorderPaths()
					.TriviallyJoinPaths()
					.ReorderPaths()
					.SimplifyPaths(0.01f)
					.Select(y => (x.Key, y)))
			.SelectMany(x => x).ToList();
	}

	void Parse(XmlNode node, Matrix4x4 transform, CssStack style, bool inUse = false) {
		if(node.NodeType != XmlNodeType.Element) {
			foreach(var child in node.ChildNodes)
				Parse((XmlNode) child, transform, style);
			return;
		}
		style.Push();
		var elem = (XmlElement) node;
		if(elem.Attributes!["transform"] is {} tfa)
			transform = ParseTransform(transform, tfa.Value);
		if(elem.Attributes!["style"] is {} sa) {
			var ss = sa.Value;
			ss.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0 && x.Contains(':'))
				.Select(x => x.Split(':', 2).Select(y => y.Trim()).ToList())
				.ForEach(x => style[x[0]] = x[1]);
			//Console.WriteLine($"Current style: {style}");
		}
		if(elem.Attributes!["x"] is {} xa)
			transform.Translation = transform.Translation with { X = float.Parse(xa.Value) };
		if(elem.Attributes!["y"] is {} ya)
			transform.Translation = transform.Translation with { Y = float.Parse(ya.Value) };

		switch(elem.Name) {
			case "path":
				if(elem.Attributes!["d"] is {} da)
					HandlePath(transform, style, da.Value);
				break;
			case "symbol":
				if(inUse)
					break;
				Symbols[elem.Attributes!["id"]!.Value] = elem;
				style.Pop();
				return;
			case "use":
				Parse(Symbols[elem.Attributes!["xlink:href"]!.Value[1..]], transform, style, inUse: true);
				break;
		}
		
		foreach(var child in elem.ChildNodes)
			Parse((XmlNode) child, transform, style);
		style.Pop();
	}

	void HandlePath(Matrix4x4 tf, CssStack style, string d) {
		var curPoint = Vector2.Zero;
		var paths = new List<List<Vector2>> { new() { curPoint } };
		var cmds = ParsePath(d);
		var seenLine = new List<(Vector2 A, Vector2 B)>();
		var seenCubic = new List<(Vector2 A, Vector2 C1, Vector2 C2, Vector2 B)>();

		bool SawLine(Vector2 a, Vector2 b) {
			if(seenLine.Contains((a, b)) || seenLine.Contains((b, a)))
				return true;
			seenLine.Add((a, b));
			return false;
		}

		bool SawCubic(Vector2 a, Vector2 c1, Vector2 c2, Vector2 b) {
			if(seenCubic.Contains((a, c1, c2, b)) || seenCubic.Contains((b, c2, c1, a)))
				return true;
			seenCubic.Add((a, c1, c2, b));
			return false;
		}

		void DrawCubic(Vector2 start, Vector2 c1, Vector2 c2, Vector2 end) {
			if(SawCubic(start, c1, c2, end)) {
				curPoint = end;
				var np = new List<Vector2> { curPoint };
				paths.Add(np);
				return;
			}
			var tpath = new List<Vector2>();
			for(var i = 0; i < 1000; ++i) {
				var t = i / 999f;
				var a = Mix(start, c1, t);
				var b = Mix(c1, c2, t);
				var c = Mix(c2, end, t);
				
				// ReSharper disable once VariableHidesOuterVariable
				var d = Mix(a, b, t);
				var e = Mix(b, c, t);
				
				tpath.Add(Mix(d, e, t));
			}
			var approxLen = tpath.CalcDrawDistance();
			var steps = (int) MathF.Ceiling(approxLen * BezierStepsPerUnit);
			tpath = paths.Last();
			for(var i = 1; i <= steps; ++i) {
				var t = i / (float) steps;
				var a = Mix(start, c1, t);
				var b = Mix(c1, c2, t);
				var c = Mix(c2, end, t);
				
				// ReSharper disable once VariableHidesOuterVariable
				var d = Mix(a, b, t);
				var e = Mix(b, c, t);
				
				tpath.Add(Mix(d, e, t));
			}
			curPoint = end;
		}

		List<Vector2> AsCoords(List<string> p) {
			var c = new List<Vector2>();
			for(var i = 0; i < p.Count; ++i) {
				var n = p[i];
				if(n.Contains(',')) {
					var coord = n.Split(',', 2);
					c.Add(new(float.Parse(coord[0]), float.Parse(coord[1])));
				} else
					c.Add(new(float.Parse(n), float.Parse(p[++i])));
			}
			return c;
		}
		List<float> AsFloats(List<string> p) => p.Select(float.Parse).ToList();

		foreach(var (cmd, p) in cmds) {
			switch(cmd) {
				case "M":
				case "m": {
					var c = AsCoords(p);
					if(cmd == "m") curPoint += c[0];
					else curPoint = c[0];
					var np = new List<Vector2> { curPoint };
					paths.Add(np);
					foreach(var elem in c.Skip(1)) {
						var ocpoint = curPoint;
						curPoint = cmd == "m" ? curPoint + elem : elem;
						if(!SawLine(ocpoint, curPoint))
							np.Add(curPoint);
						else {
							np = new List<Vector2> { curPoint };
							paths.Add(np);
						}
					} 
					break;
				}
				case "L":
				case "l": {
					var c = AsCoords(p);
					var np = paths.Last();
					foreach(var elem in c) {
						var ocpoint = curPoint;
						curPoint = cmd == "l" ? curPoint + elem : elem;
						if(!SawLine(ocpoint, curPoint))
							np.Add(curPoint);
						else {
							np = new List<Vector2> { curPoint };
							paths.Add(np);
						}
					}
					break;
				}
				case "H":
				case "h": {
					var c = AsFloats(p);
					var np = paths.Last();
					foreach(var elem in c) {
						var ocpoint = curPoint;
						curPoint = cmd == "h" ? curPoint + new Vector2(elem, 0) : new Vector2(elem, curPoint.Y);
						if(!SawLine(ocpoint, curPoint))
							np.Add(curPoint);
						else {
							np = new List<Vector2> { curPoint };
							paths.Add(np);
						}
					}
					break;
				}
				case "V":
				case "v": {
					var c = AsFloats(p);
					var np = paths.Last();
					foreach(var elem in c) {
						var ocpoint = curPoint;
						curPoint = cmd == "v" ? curPoint + new Vector2(0, elem) : new Vector2(curPoint.X, elem);
						if(!SawLine(ocpoint, curPoint))
							np.Add(curPoint);
						else {
							np = new List<Vector2> { curPoint };
							paths.Add(np);
						}
					}
					break;
				}
				case "Z":
				case "z": {
					if(IgnoreZ)
						break;
					var ocpoint = curPoint;
					curPoint = paths.Last()[0];
					if(!SawLine(ocpoint, curPoint))
						paths.Last().Add(curPoint);
					else {
						var np = new List<Vector2> { curPoint };
						paths.Add(np);
					}
					break;
				}
				case "C":
				case "c": {
					foreach(var (c1, c2, end) in AsCoords(p).Batch3()) {
						if(cmd == "c")
							DrawCubic(curPoint, curPoint + c1, curPoint + c2, curPoint + end);
						else
							DrawCubic(curPoint, c1, c2, end);
					}
					break;
				}
				default:
					Console.WriteLine($"Unhandled SVG path command! {cmd}");
					return;
			}
		}
		var stroke = style.TryGetValue("stroke", out var ss) ? ss : "black";
		Paths.AddRange(paths.Where(x => x.Count > 1)
			.Select(x => x.Select(y => Vector2.Transform(y, tf)).ToList())
			.Select(x => (stroke, x))
		);
	}

	List<(string Command, List<string> Parameters)> ParsePath(string path) {
		var ad = path.Split(' ').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
		var cmds = new List<(string, List<string>)>();
		for(var i = 0; i < ad.Count; ) {
			var cmd = ad[i++];
			var p = new List<string>();
			while(i < ad.Count && "-+0123456789.".Contains(ad[i][0]))
				p.Add(ad[i++]);
			cmds.Add((cmd, p));
		}
		return cmds;
	}

	Matrix4x4 ParseTransform(Matrix4x4 cur, string tf) {
		do {
			tf = tf.Trim();
			var s = tf.Split('(', 2);
			if(s.Length == 1) break;
			var (cmd, p) = (s[0], s[1].Split(')', 2));
			(var ps, tf) = (p[0], p[1]);
			p = ps.Split(new[] { ' ', ',' }).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
			var ap = p.Select(float.Parse).ToList();
			switch(cmd) {
				case "translate":
					cur *= ap.Count switch {
						1 => Matrix4x4.CreateTranslation(new Vector3(ap[0], 0, 0)),
						2 => Matrix4x4.CreateTranslation(new Vector3(ap[0], ap[1], 0)),
						_ => Matrix4x4.CreateTranslation(new Vector3(ap[0], ap[1], ap[2]))
					};
					break;
				default:
					Console.WriteLine($"UNSUPPORTED TRANSFORM: {cmd}");
					break;
			}
		} while(tf.Length > 0);
		return cur;
	}
}