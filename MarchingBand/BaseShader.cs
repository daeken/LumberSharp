using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem;
using PrettyPrinter;
using InvocationExpression = ICSharpCode.Decompiler.CSharp.Syntax.InvocationExpression;

namespace MarchingBand {
	public abstract class BaseShader<OutputT> {
		public abstract OutputT Evaluate(Vector2 position);
		
		public string CompileGlsl() {
			var decompiler = new CSharpDecompiler(GetType().Assembly.Location, new DecompilerSettings());
			var td = decompiler.TypeSystem.MainModule.Compilation.FindType(GetType()).GetDefinition();

			var usings = decompiler.DecompileType(new FullTypeName(GetType().FullName)).Members
				.Where(x => x is UsingDeclaration).Select(x => ((UsingDeclaration) x).Namespace)
				.Concat(new[] { GetType().Namespace }).ToList();

			GlslAst[] TransformBody(SyntaxTree func) =>
				func.Members.First(x => x.NodeType == NodeType.Member).Children
					.First(x => x.NodeType == NodeType.Statement)
					.Children.Select(Transform).Where(x => x != null).ToArray();

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
					Vector2 v => $"vec2({Format(v.X)}, {Format(v.Y)})", 
					Vector3 v => $"vec3({Format(v.X)}, {Format(v.Y)}, {Format(v.Z)})", 
					Vector4 v => $"vec4({Format(v.X)}, {Format(v.Y)}, {Format(v.Z)}, {Format(v.W)})", 
					_ => throw new NotImplementedException($"Unknown type for GLSL literal {value.GetType()}: {value}")
				}};

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

				var ifuncs = new List<Func<GlslAst>> { VectorStatics };
				return ifuncs.Select(x => x()).FirstOrDefault(x => x != null);
			}

			GlslAst Transform(AstNode node) {
				var intrinsic = MatchIntrinsic(node);
				if(intrinsic != null) return intrinsic;
				switch(node) {
					case VariableDeclarationStatement decl:
						return new GlslAst.DeclareStatement
							{ Name = decl.Variables.First().Name, Type = ToType(decl.Type) };
					case ReturnStatement ret:
						return new GlslAst.ReturnStatement { Expression = Transform(ret.Expression) };
					case IdentifierExpression id:
						return new GlslAst.IdentifierExpression { Name = id.Identifier };
					case ExpressionStatement expr:
						var tf = Transform(expr.Expression);
						return tf is GlslAst.Expression e ? new GlslAst.ExpressionStatement { Expression = e } : tf;
					case AssignmentExpression ass:
						return new GlslAst.AssignExpression
							{ Target = Transform(ass.Left), Value = Transform(ass.Right) };
					case MemberReferenceExpression mre:
						return new GlslAst.MemberAccessExpression
							{ Base = Transform(mre.Target), Member = mre.MemberName };
					case PrimitiveExpression pe:
						return pe.Value switch {
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
								_ => throw new NotImplementedException($"Unhandled binary operator {boe.Operator}")
							}
						};
					case InvocationExpression ie:
						return new GlslAst.CallExpression {
							Function = ie.Target.ToString().Split('.').Last(), 
							Arguments = ie.Arguments.Select(Transform).ToArray()
						};
					default:
						Console.WriteLine($"Unhandled node {node.GetType()}: {node}");
						break;
				}
			
				return null;
			}

			var gast = new List<GlslAst> {
				new GlslAst.Struct {
					Name = typeof(OutputT).Name,
					Members = typeof(OutputT).GetFields().Select(x => (x.Name, x.FieldType)).ToArray()
				}
			};

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
			
			var code = string.Join("\n\n", gast);
			return code;
		}
	}
}