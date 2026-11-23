using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MarchingBand {
	public static class GlslExtensions {
		public static string ToGlslString(this Type type) {
			if(type == typeof(void)) return "void";
			if(type == typeof(bool)) return "bool";
			if(type == typeof(int)) return "int";
			if(type == typeof(float)) return "float";
			if(type == typeof(Vec2)) return "vec2";
			if(type == typeof(Vec3)) return "vec3";
			if(type == typeof(Vec4)) return "vec4";
			if(type == typeof(Matrix4x4)) return "mat4";
			if(type.FullName == "System.Numerics.Matrix3x3") return "mat3";
			if(type.IsValueType && type.FullName?.StartsWith("System.", StringComparison.Ordinal) == false)
				return type.Name;
			throw new NotSupportedException($"Unknown shader type {type.FullName}");
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
				string.Join(' ', [.. Modifiers ?? [],
					Value != null
						? $"{Type.ToGlslString()} {Name} = {Value};"
						: $"{Type.ToGlslString()} {Name};"
				]);
		}

		public class ExpressionStatement : GlslAst {
			public new Expression Expression;

			public override string ToString() => $"{Expression};";
		}

		public class AssignExpression : Expression {
			public GlslAst Target, Value;
			public string Operator = "=";

			public override string ToString() => $"{Target} {Operator} {Value}";
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
				return str.Contains('.') || str.Contains('E') ? str + "f" : str + ".0f";
			}
		}

		public class IntExpression : Expression {
			public int Value;

			public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
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
