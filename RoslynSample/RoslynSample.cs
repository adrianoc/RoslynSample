using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static System.Console;

namespace RoslynSample;

public class Driver
{
	static void Main()
	{

		var syntaxTree = CSharpSyntaxTree.ParseText("""
		                                            class C 
		                                            { 
		                                                void Foo() 
		                                                {
		                                                    int i = 10; 
		                                                    i = i + 1; /* My Comment */ 
		                                                }
		                                            }
		                                            """);

		//var visitor = new MyVisitorNonWalker();
		var visitor = new MyVisitor();
		visitor.Visit(syntaxTree.GetRoot());
		
		var updater = new UpdaterVisitor();
		var updated = updater.Visit(syntaxTree.GetRoot());
		
		WriteLine($"\n{updated.ToFullString()}");
	}
}

internal class UpdaterVisitor : CSharpSyntaxRewriter
{
	public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
	{
		var stringToken = SyntaxFactory.Token(SyntaxKind.StringKeyword)
								.WithLeadingTrivia(SyntaxFactory.Space, SyntaxFactory.Space, SyntaxFactory.Space, SyntaxFactory.Space)
								.WithTrailingTrivia(SyntaxFactory.Space);

		var newField = SyntaxFactory.FieldDeclaration(
			SyntaxFactory.VariableDeclaration(
				SyntaxFactory.PredefinedType(stringToken),
				SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>().Add(SyntaxFactory.VariableDeclarator("foo")))).WithTrailingTrivia(SyntaxFactory.LineFeed);

		var members = new SyntaxList<MemberDeclarationSyntax>();
		members = members.Add(newField);

		return node.WithMembers(members.AddRange(node.Members));
	}
}

class MyVisitor : CSharpSyntaxWalker
{
	public override void VisitClassDeclaration(ClassDeclarationSyntax node)
	{
		WriteLine($"class {node.Identifier}");
		WriteLine("{");
		base.VisitClassDeclaration(node);
		WriteLine("}");
	}

	public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
	{
		Write($"\t{node.ReturnType} {node.Identifier}(");
		node.ParameterList.Accept(this);
		WriteLine(")");
		WriteLine("\t{");
		node.Body.Accept(this);
		WriteLine("\t}");
	}

	public override void VisitExpressionStatement(ExpressionStatementSyntax node)
	{
		WriteNode(node);
	}

	public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
	{
		WriteNode(node);
	}

	private static void WriteNode(SyntaxNode node)
	{
		if (node.HasLeadingTrivia)
			Write(node.GetLeadingTrivia().ToFullString());

		Write(node.ToString());

		if (node.HasTrailingTrivia)
			Write(node.GetTrailingTrivia().ToFullString());
	}
}