using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MarchingBand {
	public static class GlslExtensions {
		public static string ToGlslString(this Type type) {
			Debug.Assert(type != null && type.FullName != null);
			if(!type.FullName.StartsWith("System.") && type.IsValueType)
				return type.Name;
			return type.FullName switch {
				"System.Int32" => "int", 
				"System.Single" => "float",
				"MarchingBand.Vec2" => "vec2",
				"MarchingBand.Vec3" => "vec3",
				"MarchingBand.Vec4" => "vec4",
				"System.Numerics.Matrix3x3" => "mat3",
				"System.Numerics.Matrix4x4" => "mat4",
				_ => throw new NotImplementedException($"Unknown type {type.FullName}")
			};
		}
	}
	
	public abstract class GlslAst {
		public abstract class Expression : GlslAst {}
		
		public class Struct : GlslAst {
			public string Name;
			public (string Name, Type Type)[] Members;

			public override string ToString() =>
				$"struct {Name} {{\n" + 
					string.Join("\n", Members.Select(x => $"{x.Type.ToGlslString()} {x.Name};")).Indent() +
					"\n};";
		}

		public class Function : GlslAst {
			public string Name;
			public Type ReturnType;
			public (string Name, Type Type)[] Parameters;
			public GlslAst[] Body;

			public override string ToString() {
				var decl = $"{ReturnType.ToGlslString()} {Name}(" +
				           string.Join(", ", Parameters.Select(x => $"{x.Type.ToGlslString()} {x.Name}")) +
				           ")";
				if(Body != null)
					return decl + " {\n" +
					       string.Join('\n', Body.Select(x => x.ToString())).Indent() +
					       "\n}";
				return decl + ";";
			}
		}

		public class ReturnStatement : GlslAst {
			public new GlslAst Expression;

			public override string ToString() => Expression == null ? "return;" : $"return {Expression};";
		}

		public class DeclareStatement : GlslAst {
			public string Name;
			public Type Type;
			public GlslAst Value;
			public string[] Modifiers;

			public override string ToString() =>
				string.Join(' ', (Modifiers ?? new string[0]).Concat(new[] {
					Value != null
						? $"{Type.ToGlslString()} {Name} = {Value};"
						: $"{Type.ToGlslString()} {Name};"
				}));
		}

		public class ExpressionStatement : GlslAst {
			public new Expression Expression;

			public override string ToString() => $"{Expression};";
		}

		public class AssignExpression : Expression {
			public GlslAst Target, Value;

			public override string ToString() => $"{Target} = {Value}";
		}

		public class MemberAccessExpression : Expression {
			public GlslAst Base;
			public string Member;

			public override string ToString() => $"({Base}).{Member}";
		}

		public class BinaryOperatorExpression : Expression {
			public GlslAst Left, Right;
			public string Operator;

			public override string ToString() => $"({Left}) {Operator} ({Right})";
		}

		public class PrefixUnaryExpression : Expression {
			public GlslAst Target;
			public string Operator;

			public override string ToString() => $"{Operator}({Target})";
		}

		public class SuffixUnaryExpression : Expression {
			public GlslAst Target;
			public string Operator;

			public override string ToString() => $"({Target}){Operator}";
		}

		public class FloatExpression : Expression {
			public float Value;

			public override string ToString() {
				var str = Value.ToString(CultureInfo.InvariantCulture);
				return str.Contains(".") ? str + "f" : str + ".0f";
			}
		}

		public class IntExpression : Expression {
			public int Value;

			public override string ToString() => Value.ToString();
		}

		public class CallExpression : Expression {
			public new string Function;
			public GlslAst[] Arguments;

			public override string ToString() =>
				$"{Function}({string.Join(", ", Arguments.Select(x => x.ToString()))})";
		}
		
		public class IdentifierExpression : Expression {
			public string Name;

			public override string ToString() => Name;
		}

		public class InlineExpression : Expression {
			public string Code;

			public override string ToString() => Code;
		}

		public class TernaryExpression : Expression {
			public GlslAst Cond, If, Else;

			public override string ToString() => $"({Cond}) ? ({If}) : ({Else})";
		}

		public class IfElseStatement : GlslAst {
			public GlslAst Cond, If, Else;

			public override string ToString() =>
				Else != null
					? $"if({Cond}) {If} else {Else}"
					: $"if({Cond}) {If}";
		}

		public class ForStatement : GlslAst {
			public GlslAst Init, Cond, Iter, Body;

			public override string ToString() => $"for({Init} {Cond}; {Iter}) {Body}";
		}

		public class BreakStatement : GlslAst {
			public override string ToString() => "break;";
		}

		public class ContinueStatement : GlslAst {
			public override string ToString() => "continue;";
		}

		public class Block : GlslAst {
			public GlslAst[] Body;

			public override string ToString() =>
				$"{{\n{string.Join('\n', Body.Select(x => x.ToString())).Indent()}\n}}";
		}
	}
}