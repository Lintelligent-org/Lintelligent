using System.Linq;
using Lintelligent.Core.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.Core.CodeFixes;

/// <summary>
/// Code fix that transforms methods returning nullable types (T?) into methods returning Option&lt;T&gt;
/// </summary>
/// <remarks>
/// This code fix performs the following transformations:
/// - Changes return type from T? to Option&lt;T&gt;
/// - Transforms 'return null' to 'return Option&lt;T&gt;.None'
/// - Transforms 'return value' to 'return Option&lt;T&gt;.Some(value)'
/// - Adds 'using LanguageExt;' directive if not present
/// </remarks>
public sealed class PreferOptionMonadCodeFix : ICodeFix
{
    /// <summary>
    /// Determines whether this code fix can handle the specified diagnostic
    /// </summary>
    /// <param name="diagnostic">The diagnostic to check</param>
    /// <returns>True if the diagnostic ID is LINT003; otherwise false</returns>
    public bool CanFix(DiagnosticResult diagnostic) => diagnostic.Id == "LINT003";

    /// <summary>
    /// Applies the code fix to transform nullable return types to Option&lt;T&gt;
    /// </summary>
    /// <param name="diagnostic">The diagnostic identifying the nullable return type</param>
    /// <param name="tree">The syntax tree containing the code to fix</param>
    /// <returns>A CodeFixResult containing the updated syntax tree</returns>
    public CodeFixResult ApplyFix(DiagnosticResult diagnostic, SyntaxTree tree)
    {
        var root = tree.GetRoot();
        
        // The diagnostic span points to the return type, so find the method containing it
        var methodNode = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.ReturnType?.Span.Equals(diagnostic.Span) == true);

        if (methodNode == null) return new CodeFixResult(tree);

        // Extract the non-nullable type from the return type and determine if it's async
        var returnType = methodNode.ReturnType;
        var (innerType, isAsync, asyncWrapper) = ExtractInnerTypeAndCheckAsync(returnType);
        
        // Create new return type: either Option<T> or Task<Option<T>>
        TypeSyntax newReturnType;
        if (isAsync)
        {
            // Create Task<Option<T>> or ValueTask<Option<T>>
            var optionType = CreateOptionTypeExpression(innerType).WithoutTrivia();
            newReturnType = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(asyncWrapper!),
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(optionType)
                    ))
                .WithLeadingTrivia(returnType.GetLeadingTrivia())
                .WithTrailingTrivia(SyntaxFactory.Space);
        }
        else
        {
            // Create Option<T>
            newReturnType = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("Option"),
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(innerType.WithoutTrivia())
                    ))
                .WithLeadingTrivia(returnType.GetLeadingTrivia())
                .WithTrailingTrivia(SyntaxFactory.Space);
        }

        // Update method return type
        var newMethod = methodNode.WithReturnType(newReturnType);

        // Transform return statements (both in method body and expression body)
        newMethod = TransformReturnStatements(newMethod, innerType);
        
        // Transform expression-bodied members
        if (newMethod.ExpressionBody != null)
        {
            newMethod = TransformExpressionBody(newMethod, innerType);
        }

        // Replace the method node
        var newRoot = root.ReplaceNode(methodNode, newMethod);

        // Add using directive if not present
        newRoot = AddUsingDirectiveIfNeeded(newRoot);

        return new CodeFixResult(tree.WithRootAndOptions(newRoot, tree.Options));
    }

    /// <summary>
    /// Extracts the inner type from a nullable type syntax and determines if it's async
    /// </summary>
    /// <param name="returnType">The return type (e.g., string?, Task&lt;string?&gt;, T?)</param>
    /// <returns>A tuple of (innerType, isAsync, asyncWrapper) where asyncWrapper is "Task" or "ValueTask"</returns>
    private static (TypeSyntax innerType, bool isAsync, string? asyncWrapper) ExtractInnerTypeAndCheckAsync(TypeSyntax returnType)
    {
        // Handle Task<T?> or ValueTask<T?> (async methods)
        if (returnType is GenericNameSyntax asyncGeneric &&
            (asyncGeneric.Identifier.Text == "Task" || asyncGeneric.Identifier.Text == "ValueTask") &&
            asyncGeneric.TypeArgumentList.Arguments.Count == 1)
        {
            var asyncInnerType = asyncGeneric.TypeArgumentList.Arguments[0];
            if (asyncInnerType is NullableTypeSyntax asyncNullable)
            {
                return (asyncNullable.ElementType, true, asyncGeneric.Identifier.Text);
            }
        }
        
        // Handle nullable reference types (e.g., string?, T?)
        if (returnType is NullableTypeSyntax nullableType)
        {
            return (nullableType.ElementType, false, null);
        }

        // Handle System.Nullable<T> (value types)
        if (returnType is GenericNameSyntax genericName && 
            (genericName.Identifier.Text == "Nullable" || genericName.Identifier.Text == "System.Nullable"))
        {
            return (genericName.TypeArgumentList.Arguments[0], false, null);
        }

        // Fallback: return as-is
        return (returnType, false, null);
    }

    /// <summary>
    /// Extracts the inner type from a nullable type syntax (legacy - kept for compatibility)
    /// </summary>
    /// <param name="returnType">The nullable return type (e.g., string? or Nullable&lt;int&gt;)</param>
    /// <returns>The inner non-nullable type (e.g., string or int)</returns>
    private static TypeSyntax ExtractInnerType(TypeSyntax returnType)
    {
        var (innerType, _, _) = ExtractInnerTypeAndCheckAsync(returnType);
        return innerType;
    }

    /// <summary>
    /// Transforms all return statements in a method to use Option&lt;T&gt; syntax
    /// </summary>
    /// <param name="method">The method declaration containing return statements</param>
    /// <param name="innerType">The inner type T for Option&lt;T&gt;</param>
    /// <returns>The method with all return statements transformed</returns>
    private static MethodDeclarationSyntax TransformReturnStatements(
        MethodDeclarationSyntax method, 
        TypeSyntax innerType)
    {
        var returnStatements = method.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .ToList();

        if (!returnStatements.Any()) return method;

        // Build a dictionary of replacements
        var replacements = returnStatements.ToDictionary(
            r => r,
            r => TransformSingleReturn(r, innerType)
        );

        // Replace all return statements at once
        return method.ReplaceNodes(returnStatements, (original, _) => replacements[original]);
    }

    /// <summary>
    /// Transforms a single return statement to use Option&lt;T&gt; syntax
    /// </summary>
    /// <param name="returnStatement">The return statement to transform</param>
    /// <param name="innerType">The inner type T for Option&lt;T&gt;</param>
    /// <returns>The transformed return statement</returns>
    private static ReturnStatementSyntax TransformSingleReturn(
        ReturnStatementSyntax returnStatement, 
        TypeSyntax innerType)
    {
        var expression = returnStatement.Expression;
        
        if (expression == null)
        {
            // return; (no expression)
            return returnStatement;
        }

        // Handle await expressions (for async methods) - transform the awaited value
        if (expression is AwaitExpressionSyntax awaitExpr)
        {
            // For async methods, we can directly return Option<T> values without await
            // The compiler will wrap them in Task<Option<T>>
            // So we recursively check what's being awaited and transform accordingly
            
            // If awaiting Task.FromResult(null) or similar, extract the inner value
            if (awaitExpr.Expression is InvocationExpressionSyntax invocation)
            {
                // Check if it's Task.FromResult<T?>(value)
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text == "FromResult")
                {
                    // Get the argument (the value being wrapped)
                    if (invocation.ArgumentList.Arguments.Count > 0)
                    {
                        var argument = invocation.ArgumentList.Arguments[0].Expression;
                        
                        // Transform the argument (null → None, value → Some(value))
                        if (argument is LiteralExpressionSyntax literal &&
                            literal.IsKind(SyntaxKind.NullLiteralExpression))
                        {
                            return returnStatement.WithExpression(
                                CreateOptionNoneExpression(innerType).WithTriviaFrom(expression));
                        }
                        
                        return returnStatement.WithExpression(
                            CreateOptionSomeExpression(innerType, argument).WithTriviaFrom(expression));
                    }
                }
            }
            
            // For other await expressions, just unwrap and transform
            // This handles: return await SomeAsyncMethod() where the method returns T?
            // We transform to: return Option<T>.None or Option<T>.Some(await ...)
            // But since we can't easily determine what the awaited result is,
            // we'll wrap the await expression itself
            return returnStatement.WithExpression(
                CreateOptionSomeExpression(innerType, expression).WithTriviaFrom(expression));
        }

        // Check if returning null
        if (expression is LiteralExpressionSyntax nullLiteral && 
            nullLiteral.IsKind(SyntaxKind.NullLiteralExpression))
        {
            // Transform: return null → return Option<T>.None
            var noneExpression = CreateOptionNoneExpression(innerType)
                .WithTriviaFrom(expression);

            return returnStatement.WithExpression(noneExpression);
        }

        // Check if returning default or default(T?)
        if (expression is LiteralExpressionSyntax defaultLiteral && 
            defaultLiteral.IsKind(SyntaxKind.DefaultLiteralExpression))
        {
            // Transform: return default → return Option<T>.None
            var noneExpression = CreateOptionNoneExpression(innerType)
                .WithTriviaFrom(expression);

            return returnStatement.WithExpression(noneExpression);
        }

        if (expression is DefaultExpressionSyntax)
        {
            // Transform: return default(T?) → return Option<T>.None
            var noneExpression = CreateOptionNoneExpression(innerType)
                .WithTriviaFrom(expression);

            return returnStatement.WithExpression(noneExpression);
        }

        // Transform: return value → return Option<T>.Some(value)
        var someExpression = CreateOptionSomeExpression(innerType, expression)
            .WithTriviaFrom(expression);

        return returnStatement.WithExpression(someExpression);
    }

    /// <summary>
    /// Creates an Option&lt;T&gt;.None expression
    /// </summary>
    /// <param name="innerType">The inner type T for Option&lt;T&gt;</param>
    /// <returns>A MemberAccessExpressionSyntax representing Option&lt;T&gt;.None</returns>
    private static MemberAccessExpressionSyntax CreateOptionNoneExpression(TypeSyntax innerType)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            CreateOptionTypeExpression(innerType),
            SyntaxFactory.IdentifierName("None")
        );
    }

    /// <summary>
    /// Creates an Option&lt;T&gt;.Some(value) invocation expression
    /// </summary>
    /// <param name="innerType">The inner type T for Option&lt;T&gt;</param>
    /// <param name="valueExpression">The value expression to wrap in Some()</param>
    /// <returns>An InvocationExpressionSyntax representing Option&lt;T&gt;.Some(value)</returns>
    private static InvocationExpressionSyntax CreateOptionSomeExpression(
        TypeSyntax innerType, 
        ExpressionSyntax valueExpression)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                CreateOptionTypeExpression(innerType),
                SyntaxFactory.IdentifierName("Some")
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(valueExpression)
                )
            )
        );
    }

    /// <summary>
    /// Creates an Option&lt;T&gt; generic name expression
    /// </summary>
    /// <param name="innerType">The inner type T for Option&lt;T&gt;</param>
    /// <returns>A GenericNameSyntax representing Option&lt;T&gt;</returns>
    private static GenericNameSyntax CreateOptionTypeExpression(TypeSyntax innerType)
    {
        return SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("Option"),
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList(innerType.WithoutTrivia())
            )
        );
    }

    /// <summary>
    /// Transforms an expression-bodied member to use Option&lt;T&gt; syntax
    /// </summary>
    /// <param name="method">The method with an expression body</param>
    /// <param name="innerType">The inner type T for Option&lt;T&gt;</param>
    /// <returns>The method with transformed expression body</returns>
    private static MethodDeclarationSyntax TransformExpressionBody(
        MethodDeclarationSyntax method,
        TypeSyntax innerType)
    {
        if (method.ExpressionBody == null)
            return method;
        
        var expression = method.ExpressionBody.Expression;
        var transformedExpression = TransformExpression(expression, innerType);
        
        if (transformedExpression != expression)
        {
            var newExpressionBody = method.ExpressionBody.WithExpression(transformedExpression);
            return method.WithExpressionBody(newExpressionBody);
        }
        
        return method;
    }

    /// <summary>
    /// Transforms an expression to use Option&lt;T&gt; syntax (handles null, values, and switch expressions)
    /// </summary>
    /// <param name="expression">The expression to transform</param>
    /// <param name="innerType">The inner type T for Option&lt;T&gt;</param>
    /// <returns>The transformed expression</returns>
    private static ExpressionSyntax TransformExpression(ExpressionSyntax expression, TypeSyntax innerType)
    {
        // Handle null literal
        if (expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.NullLiteralExpression))
        {
            return CreateOptionNoneExpression(innerType)
                .WithTriviaFrom(expression);
        }
        
        // Handle default literal → Option<T>.None
        if (expression is LiteralExpressionSyntax defaultLiteral &&
            defaultLiteral.IsKind(SyntaxKind.DefaultLiteralExpression))
        {
            return CreateOptionNoneExpression(innerType)
                .WithTriviaFrom(expression);
        }
        
        // Handle default(T?) → Option<T>.None
        if (expression is DefaultExpressionSyntax)
        {
            return CreateOptionNoneExpression(innerType)
                .WithTriviaFrom(expression);
        }
        
        // Handle switch expressions
        if (expression is SwitchExpressionSyntax switchExpr)
        {
            return TransformSwitchExpression(switchExpr, innerType);
        }
        
        // Wrap other values in Option.Some()
        return CreateOptionSomeExpression(innerType, expression)
            .WithTriviaFrom(expression);
    }

    /// <summary>
    /// Transforms a switch expression to use Option&lt;T&gt; syntax in all arms
    /// </summary>
    /// <param name="switchExpr">The switch expression to transform</param>
    /// <param name="innerType">The inner type T for Option&lt;T&gt;</param>
    /// <returns>The transformed switch expression</returns>
    private static SwitchExpressionSyntax TransformSwitchExpression(
        SwitchExpressionSyntax switchExpr,
        TypeSyntax innerType)
    {
        var transformedArms = switchExpr.Arms.Select(arm =>
        {
            var originalExpression = arm.Expression;
            var transformedExpression = TransformExpressionForSwitchArm(originalExpression, innerType);
            
            // Preserve the trailing trivia from the original expression on the new expression
            // (but not within the argument to Some() to avoid breaking formatting)
            if (originalExpression.GetTrailingTrivia().Any())
            {
                transformedExpression = transformedExpression.WithTrailingTrivia(
                    originalExpression.GetTrailingTrivia());
            }
            
            return arm.WithExpression(transformedExpression);
        }).ToArray();
        
        return switchExpr.WithArms(
            SyntaxFactory.SeparatedList(transformedArms)
        );
    }

    /// <summary>
    /// Transforms an expression in a switch arm, avoiding trivia issues
    /// </summary>
    /// <param name="expression">The expression to transform</param>
    /// <param name="innerType">The inner type T for Option&lt;T&gt;</param>
    /// <returns>The transformed expression without inheriting argument trivia</returns>
    private static ExpressionSyntax TransformExpressionForSwitchArm(ExpressionSyntax expression, TypeSyntax innerType)
    {
        // Handle null literal
        if (expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.NullLiteralExpression))
        {
            return CreateOptionNoneExpression(innerType);
        }
        
        // Handle default literal → Option<T>.None
        if (expression is LiteralExpressionSyntax defaultLiteral &&
            defaultLiteral.IsKind(SyntaxKind.DefaultLiteralExpression))
        {
            return CreateOptionNoneExpression(innerType);
        }
        
        // Handle default(T?) → Option<T>.None
        if (expression is DefaultExpressionSyntax)
        {
            return CreateOptionNoneExpression(innerType);
        }
        
        // Wrap values in Option.Some() but strip trivia from the argument
        return CreateOptionSomeExpression(innerType, expression.WithoutTrivia());
    }

    /// <summary>
    /// Adds 'using LanguageExt;' directive to the compilation unit if not already present
    /// </summary>
    /// <param name="root">The root syntax node</param>
    /// <returns>The updated syntax node with the using directive added</returns>
    private static SyntaxNode AddUsingDirectiveIfNeeded(SyntaxNode root)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
            return root;

        // Check if using LanguageExt already exists
        var hasLanguageExtUsing = compilationUnit.Usings.Any(u =>
            u.Name?.ToString() == "LanguageExt");

        if (hasLanguageExtUsing)
            return root;

        // Create the new using directive
        var newUsing = SyntaxFactory.UsingDirective(
                SyntaxFactory.IdentifierName("LanguageExt"))
            .WithUsingKeyword(
                SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(),
                    SyntaxKind.UsingKeyword,
                    SyntaxFactory.TriviaList(SyntaxFactory.Space)))
            .WithSemicolonToken(
                SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(),
                    SyntaxKind.SemicolonToken,
                    SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed)));

        // Preserve any leading trivia (like #nullable enable) from the compilation unit
        // and attach it to the first using directive
        var leadingTrivia = compilationUnit.GetLeadingTrivia();
        if (leadingTrivia.Any())
        {
            newUsing = newUsing.WithLeadingTrivia(leadingTrivia);
            compilationUnit = compilationUnit.WithoutLeadingTrivia();
        }

        var newUsings = compilationUnit.Usings.Add(newUsing);
        return compilationUnit.WithUsings(newUsings);
    }
}
