using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MarchingBand {
	public static class GlslExtensions {
		public static string ToGlslString(this Type type) {
			if(!type.FullName.StartsWith("System.") && type.IsValueType)
				return type.Name;
			return type.FullName switch {
				"System.Int32" => "int", 
				"System.Single" => "float",
				"System.Numerics.Vector2" => "vec2",
				"System.Numerics.Vector3" => "vec3",
				"System.Numerics.Vector4" => "vec4",
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

			public override string ToString() => $"{Type.ToGlslString()} {Name};";
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

		public class FloatExpression : Expression {
			public float Value;

			public override string ToString() {
				var str = Value.ToString(CultureInfo.InvariantCulture);
				return str.Contains(".") ? str + "f" : str + ".0f";
			}
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
	}
}