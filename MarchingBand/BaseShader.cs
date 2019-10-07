using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem;
using PrettyPrinter;
using ConditionalExpression = ICSharpCode.Decompiler.CSharp.Syntax.ConditionalExpression;
using Expression = ICSharpCode.Decompiler.CSharp.Syntax.Expression;
using InvocationExpression = ICSharpCode.Decompiler.CSharp.Syntax.InvocationExpression;

namespace MarchingBand {
	public abstract class BaseShader<OutputT> {
		public abstract OutputT Evaluate(Vec2 position);
		
		public string CompileGlsl() {
			var decompiler = new CSharpDecompiler(GetType().Assembly.Location, new DecompilerSettings());
			var td = decompiler.TypeSystem.MainModule.Compilation.FindType(GetType()).GetDefinition();

			var usings = decompiler.DecompileType(new FullTypeName(GetType().FullName)).Members
				.Where(x => x is UsingDeclaration).Select(x => ((UsingDeclaration) x).Namespace)
				.Concat(new[] { GetType().Namespace }).ToList();

			GlslAst[] TransformBody(SyntaxTree func) =>
				func.Members.First(x => x.NodeType == NodeType.Member).Children
					.First(x => x.NodeType == NodeType.Statement)
					.Children.Select(x => Transform(x)).Where(x => x != null).ToArray();

			Type ToType(AstType astType) {
				var t = astType.ToTypeReference()
					.Resolve(new CSharpTypeResolveContext(decompiler.TypeSystem.MainModule));
				return new[] { t.Namespace }.Concat(usings).Select(ns => AppDomain.CurrentDomain.GetAssemblies()
					.Select(x => x.GetType(ns == null ? t.FullName : $"{ns}.{t.Name}"))
					.FirstOrDefault(x => x != null)).FirstOrDefault(x => x != null);
			}

			string Format(float value) {
				var str = value.ToString(CultureInfo.InvariantCulture);
				return str.Contains(".") ? str + "f" : str + ".0f";
			}

			GlslAst ToLiteral(object value) =>
				new GlslAst.InlineExpression { Code = value switch {
					Vec2 v => $"vec2({Format(v.x)}, {Format(v.y)})", 
					Vec3 v => $"vec3({Format(v.x)}, {Format(v.y)}, {Format(v.z)})", 
					Vec4 v => $"vec4({Format(v.x)}, {Format(v.y)}, {Format(v.z)}, {Format(v.w)})", 
					_ => throw new NotImplementedException($"Unknown type for GLSL literal {value.GetType()}: {value}")
				}};

			GlslAst EnsureStmt(GlslAst node) =>
				node is GlslAst.Expression expr
					? new GlslAst.ExpressionStatement { Expression = expr }
					: node;

			GlslAst MatchIntrinsic(AstNode node) {
				GlslAst VectorStatics() {
					if(!(node is MemberReferenceExpression mre)) return null;
					if(!(mre.Target is TypeReferenceExpression tre)) return null;
					var ctype = ToType(tre.Type);
					var member = ctype.GetMember(mre.MemberName).First();
					object value;
					if(member is FieldInfo fi) {
						if(!fi.IsStatic || !fi.IsInitOnly) return null;
						value = fi.GetValue(null);
					} else if(member is PropertyInfo pi) {
						if(pi.SetMethod != null || !pi.GetMethod.IsStatic) return null;
						value = pi.GetValue(null);
					} else return null;
					return ToLiteral(value);
				}

				GlslAst MatrixTransform() {
					if(!(node is InvocationExpression ie)) return null;
					if(!(ie.Target is MemberReferenceExpression mre)) return null;
					if(mre.MemberName != "Transform" || !mre.Target.ToString().StartsWith("Vector")) return null;
					return new GlslAst.BinaryOperatorExpression {
						Left = Transform(ie.Arguments.Skip(1).First()), 
						Right = Transform(ie.Arguments.First()), 
						Operator = "*"
					};
				}

				var ifuncs = new List<Func<GlslAst>> { VectorStatics, MatrixTransform };
				return ifuncs.Select(x => x()).FirstOrDefault(x => x != null);
			}

			GlslAst Transform(AstNode node, bool bareExpr = false) {
				var intrinsic = MatchIntrinsic(node);
				if(intrinsic != null) return intrinsic;
				switch(node) {
					case DefaultValueExpression _: return null;
					case VariableDeclarationStatement decl:
						return new GlslAst.DeclareStatement
						{
							Name = decl.Variables.First().Name, Type = ToType(decl.Type),
							Value = Transform(decl.Variables.First().Initializer)
						};
					case ReturnStatement ret:
						return new GlslAst.ReturnStatement { Expression = Transform(ret.Expression) };
					case IdentifierExpression id:
						return new GlslAst.IdentifierExpression { Name = id.Identifier };
					case ExpressionStatement expr:
						var tf = Transform(expr.Expression);
						if(bareExpr) return tf;
						return tf is GlslAst.Expression e ? new GlslAst.ExpressionStatement { Expression = e } : tf;
					case AssignmentExpression ass:
						var right = Transform(ass.Right);
						if(right == null) return null;
						return new GlslAst.AssignExpression
							{ Target = Transform(ass.Left), Value = right };
					case MemberReferenceExpression mre:
						return new GlslAst.MemberAccessExpression
							{ Base = Transform(mre.Target), Member = mre.MemberName };
					case PrimitiveExpression pe:
						return pe.Value switch {
							int i => (GlslAst) new GlslAst.IntExpression { Value = i }, // TODO: Report Rider bug
							float f => new GlslAst.FloatExpression { Value = f },
							_ => throw new NotImplementedException($"Unhandled primitive type {pe.Value.GetType()}")
						};
					case BinaryOperatorExpression boe:
						return new GlslAst.BinaryOperatorExpression {
							Left = Transform(boe.Left), Right = Transform(boe.Right), 
							Operator = boe.Operator switch {
								BinaryOperatorType.Add => "+", 
								BinaryOperatorType.Subtract => "-", 
								BinaryOperatorType.Multiply => "*", 
								BinaryOperatorType.Divide => "/", 
								BinaryOperatorType.Equality => "==", 
								BinaryOperatorType.InEquality => "!=", 
								BinaryOperatorType.LessThan => "<", 
								BinaryOperatorType.LessThanOrEqual => "<=", 
								BinaryOperatorType.GreaterThan => ">", 
								BinaryOperatorType.GreaterThanOrEqual => ">=", 
								BinaryOperatorType.ConditionalOr => "||", 
								BinaryOperatorType.ConditionalAnd => "&&", 
								_ => throw new NotImplementedException($"Unhandled binary operator {boe.Operator}")
							}
						};
					case UnaryOperatorExpression uoe:
						return uoe.Operator switch {
							UnaryOperatorType.Increment => (GlslAst) new GlslAst.PrefixUnaryExpression
								{ Operator = "++", Target = Transform(uoe.Expression) },
							UnaryOperatorType.PostIncrement => new GlslAst.SuffixUnaryExpression
								{ Operator = "++", Target = Transform(uoe.Expression) },
							_ => throw new NotImplementedException($"Unhandled unary operator {uoe.Operator}")
						};
					case InvocationExpression ie:
						return new GlslAst.CallExpression {
							Function = ie.Target.ToString().Split('.').Last(), 
							Arguments = ie.Arguments.Select(x => Transform(x)).ToArray()
						};
					case ConditionalExpression ce:
						return new GlslAst.TernaryExpression {
							Cond = Transform(ce.Condition),
							If = Transform(ce.TrueExpression),
							Else = Transform(ce.FalseExpression)
						};
					case IfElseStatement ies:
						return new GlslAst.IfElseStatement {
							Cond = Transform(ies.Condition), 
							If = Transform(ies.TrueStatement), 
							Else = Transform(ies.FalseStatement)
						};
					case ForStatement fs:
						return new GlslAst.ForStatement {
							Init = EnsureStmt(Transform(fs.Initializers.First())), 
							Cond = Transform(fs.Condition, bareExpr: true), 
							Iter = Transform(fs.Iterators.First(), bareExpr: true), 
							Body = Transform(fs.EmbeddedStatement)
						};
					case BlockStatement bs:
						return new GlslAst.Block {
							Body = bs.Statements.Select(x => Transform(x)).Where(x => x != null).ToArray()
						};
					case ParenthesizedExpression pe:
						return Transform(pe.Expression);
					case BreakStatement _: return new GlslAst.BreakStatement();
					case ContinueStatement _: return new GlslAst.ContinueStatement();
					case Expression x when x == Expression.Null: return null;
					case Statement x when x == Statement.Null: return null;
					default:
						Console.WriteLine($"Unhandled node {node.GetType()}: {node}");
						break;
				}
			
				return null;
			}

			var gast = new List<GlslAst>();

			gast.AddRange(GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Select(x => new GlslAst.DeclareStatement {
					Name = x.Name, Type = x.FieldType, Modifiers = new[] { "uniform" }
				}));
			
			gast.Add(new GlslAst.Struct {
				Name = typeof(OutputT).Name,
				Members = typeof(OutputT).GetFields().Select(x => (x.Name, x.FieldType)).ToArray()
			});

			var funcs = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
			                                 BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

			void DefineFunctions(bool declareOnly) =>
				gast.AddRange(funcs.Select(x => new GlslAst.Function {
					Name = x.Name,
					ReturnType = x.ReturnType,
					Parameters = x.GetParameters().Select(y => (y.Name, y.ParameterType)).ToArray(),
					Body = declareOnly
						? null
						: TransformBody(
							decompiler.Decompile(td.GetMethods().First(y => y.Name == x.Name).MetadataToken))
				}));
			
			DefineFunctions(true);
			DefineFunctions(false);
			
			var code = string.Join("\n\n", gast) + "\n\n";

			code += "in vec2 vPosition;\n";
			var sf = typeof(OutputT).GetFields();
			foreach(var fi in sf)
				code += $"out {fi.FieldType.ToGlslString()} _Out_{fi.Name};\n";
			code += "\nvoid main() {\n";
			code += $"\t{typeof(OutputT).ToGlslString()} _output = Evaluate(vPosition);\n";
			foreach(var fi in sf)
				code += $"\t_Out_{fi.Name} = _output.{fi.Name};\n";
			code += "}";

			code = "#version 410\nprecision highp float;\n\n" + code;

			return code;
		}
	}
}