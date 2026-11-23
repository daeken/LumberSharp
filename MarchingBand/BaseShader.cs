using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace MarchingBand {
	public abstract class BaseShader<OutputT> {
		public abstract OutputT Evaluate(Vec2 position);
		
		public string CompileGlsl() {
			var shaderType = GetType();
			// Keep the decompiler's output in the C# subset consumed by the GLSL translator.
			var settings = new DecompilerSettings(LanguageVersion.CSharp7_3) { DefaultLiterals = false };
			var decompiler = new CSharpDecompiler(shaderType.Assembly.Location, settings);
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var outputFields = typeof(OutputT).GetFields(BindingFlags.Public | BindingFlags.Instance);

			GlslAst[] TransformBody(MethodInfo method) {
				var tree = decompiler.Decompile(MetadataTokens.EntityHandle(method.MetadataToken));
				var body = tree.Members.OfType<MethodDeclaration>().Single().Body;
				return [.. body.Statements.Select(statement => Transform(statement)).OfType<GlslAst>()];
			}

			Type ToType(IType type) => assemblies.Select(assembly => assembly.GetType(type.ReflectionName))
				.FirstOrDefault(resolved => resolved is not null)
				?? throw new NotSupportedException($"Cannot resolve shader type {type.ReflectionName}.");

			static string Format(float value) => new GlslAst.FloatExpression { Value = value }.ToString();

			GlslAst DefaultValue(Type type) {
				if(type == typeof(OutputT))
					return new GlslAst.CallExpression {
						Function = type.ToGlslString(), Arguments = [.. outputFields.Select(field => DefaultValue(field.FieldType))]
					};
				var code = type.ToGlslString() switch {
					"float" => "0.0f",
					"int" => "0",
					"bool" => "false",
					var name => $"{name}(0.0f)"
				};
				return new GlslAst.InlineExpression { Code = code };
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
				if(node is MemberReferenceExpression { Target: TypeReferenceExpression reference } member) {
					var type = ToType(reference.Type.GetResolveResult().Type);
					return type.GetMember(member.MemberName).FirstOrDefault() switch {
						FieldInfo { IsStatic: true, IsInitOnly: true } field => ToLiteral(field.GetValue(null)),
						PropertyInfo { SetMethod: null, GetMethod.IsStatic: true } property => ToLiteral(property.GetValue(null)),
						_ => null
					};
				}

				if(node is InvocationExpression { Target: MemberReferenceExpression { MemberName: "Transform" } target } call
				   && target.Target.ToString().StartsWith("Vector", StringComparison.Ordinal)) {
					return new GlslAst.BinaryOperatorExpression {
						Left = Transform(call.Arguments.Last()),
						Right = Transform(call.Arguments.First()),
						Operator = "*"
					};
				}

				return null;
			}

			GlslAst Transform(AstNode node, bool bareExpr = false) {
				if(node is null) return null;
				var intrinsic = MatchIntrinsic(node);
				if(intrinsic != null) return intrinsic;
				switch(node) {
					case DefaultValueExpression defaultValue:
						return DefaultValue(ToType(defaultValue.Type.GetResolveResult().Type));
					case VariableDeclarationStatement decl: {
						var variable = decl.Variables.Single();
						return new GlslAst.DeclareStatement {
							Name = variable.Name,
							Type = ToType(variable.GetILVariable()?.Type ?? decl.Type.GetResolveResult().Type),
							Value = Transform(variable.Initializer)
						};
					}
					case ReturnStatement ret:
						return new GlslAst.ReturnStatement { Expression = Transform(ret.Expression) };
					case IdentifierExpression id:
						return new GlslAst.IdentifierExpression { Name = id.Identifier };
					case ExpressionStatement expr:
						var tf = Transform(expr.Expression);
						if(bareExpr) return tf;
						return tf is GlslAst.Expression e ? new GlslAst.ExpressionStatement { Expression = e } : tf;
					case AssignmentExpression ass:
						return new GlslAst.AssignExpression {
							Target = Transform(ass.Left), Value = Transform(ass.Right),
							Operator = AssignmentExpression.GetOperatorToken(ass.Operator)
						};
					case MemberReferenceExpression { Target: ThisReferenceExpression } field:
						return new GlslAst.IdentifierExpression { Name = field.MemberName };
					case MemberReferenceExpression mre:
						return new GlslAst.MemberAccessExpression
							{ Base = Transform(mre.Target), Member = mre.MemberName };
					case ObjectCreateExpression creation: {
						var type = ToType(creation.Type.GetResolveResult().Type);
						if(type == typeof(OutputT) && !creation.Arguments.Any()) {
							var initializers = creation.Initializer?.Elements.OfType<NamedExpression>()
								.ToDictionary(initializer => initializer.Name, initializer => initializer.Expression)
								?? new Dictionary<string, Expression>();
							return new GlslAst.CallExpression {
								Function = type.ToGlslString(),
								Arguments = [.. outputFields.Select(field => initializers.TryGetValue(field.Name, out var value)
									? Transform(value)
									: DefaultValue(field.FieldType))]
							};
						}
						return new GlslAst.CallExpression {
							Function = type.ToGlslString(),
							Arguments = [.. creation.Arguments.Select(argument => Transform(argument))]
						};
					}
					case PrimitiveExpression pe:
						return pe.Value switch {
							int i => new GlslAst.IntExpression { Value = i },
							float f => new GlslAst.FloatExpression { Value = f },
							bool b => new GlslAst.InlineExpression { Code = b ? "true" : "false" },
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
							UnaryOperatorType.Increment => new GlslAst.PrefixUnaryExpression
								{ Operator = "++", Target = Transform(uoe.Expression) },
							UnaryOperatorType.PostIncrement => new GlslAst.SuffixUnaryExpression
								{ Operator = "++", Target = Transform(uoe.Expression) },
							UnaryOperatorType.Minus => new GlslAst.PrefixUnaryExpression
								{ Operator = "-", Target = Transform(uoe.Expression) },
							UnaryOperatorType.Not => new GlslAst.PrefixUnaryExpression
								{ Operator = "!", Target = Transform(uoe.Expression) },
							_ => throw new NotImplementedException($"Unhandled unary operator {uoe.Operator}")
						};
					case InvocationExpression ie:
						return new GlslAst.CallExpression {
							Function = ie.Target is MemberReferenceExpression reference ? reference.MemberName : ie.Target.ToString(),
							Arguments = [.. ie.Arguments.Select(argument => Transform(argument))]
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
							Body = [.. bs.Statements.Select(statement => Transform(statement)).OfType<GlslAst>()]
						};
					case ParenthesizedExpression pe:
						return Transform(pe.Expression);
					case BreakStatement _: return new GlslAst.BreakStatement();
					case ContinueStatement _: return new GlslAst.ContinueStatement();
					default:
						throw new NotSupportedException($"Unhandled shader syntax {node.GetType().Name}: {node}");
				}
			}

			var gast = new List<GlslAst>();

			gast.AddRange(shaderType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Select(x => new GlslAst.DeclareStatement {
					Name = x.Name, Type = x.FieldType, Modifiers = ["uniform"]
				}));
			
			gast.Add(new GlslAst.Struct {
				Name = typeof(OutputT).Name,
				Members = [.. outputFields.Select(field => (field.Name, field.FieldType))]
			});

			var funcs = shaderType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
			                                 BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

			void DefineFunctions(bool declareOnly) =>
				gast.AddRange(funcs.Select(x => new GlslAst.Function {
					Name = x.Name,
					ReturnType = x.ReturnType,
					Parameters = [.. x.GetParameters().Select(y => (y.Name, y.ParameterType))],
					Body = declareOnly ? null : TransformBody(x)
				}));
			
			DefineFunctions(true);
			DefineFunctions(false);
			
			var code = string.Join("\n\n", gast) + "\n\n";

			code += "in vec2 vPosition;\n";
			foreach(var fi in outputFields)
				code += $"out {fi.FieldType.ToGlslString()} _Out_{fi.Name};\n";
			code += "\nvoid main() {\n";
			code += $"\t{typeof(OutputT).ToGlslString()} _output = Evaluate(vPosition);\n";
			foreach(var fi in outputFields)
				code += $"\t_Out_{fi.Name} = _output.{fi.Name};\n";
			code += "}";

			code = "#version 410\nprecision highp float;\n\n" + code;

			return code;
		}
	}
}
