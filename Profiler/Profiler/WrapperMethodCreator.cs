using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Geeks.Profiler
{
    internal static class WrapperMethodCreator
    {
        public static SyntaxNode GetMethod(MethodInfo methodInfo, MethodInfo originalMethod, ProfilerClass profilerClass)
        {
            var method = methodInfo.Method;
            
            // We might add some constarint calauses to core method but for wapper we keep the original ones
            method = method.WithConstraintClauses(originalMethod.Method.ConstraintClauses);

            // Add all original modifiers to new method
            method = method.WithModifiers(originalMethod.Method.Modifiers);

            // Update the body to just call the xxx_impl
            var newBody = GetWrapperMethodBody(methodInfo, originalMethod, profilerClass);
            var newBodySyntax = SyntaxFactory.Block(SyntaxFactory.ParseStatement(newBody));

            if (method.Body == null && method.ExpressionBody != null)
            {
                method = method.RemoveNode(method.ExpressionBody, SyntaxRemoveOptions.KeepDirectives);
                method = method.WithBody(newBodySyntax);
                method = method.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));
            }
            else
            {
                method = method.ReplaceNode(method.Body, newBodySyntax);
            }

            // We place this method last so we move the leading trivia to core method
            method = method.WithLeadingTrivia(null);

            // If original method has explicit interface specifier add it back
            method = method.WithExplicitInterfaceSpecifier(originalMethod.Method.ExplicitInterfaceSpecifier);

            return method.WithIdentifier(originalMethod.Method.Identifier);
        }

        private static string GetWrapperMethodBody(MethodInfo methodInfo, MethodInfo originalMethod, ProfilerClass profilerClass)
        {
            var callAndReturnStatement = GetInvocationAndReturnStatement(methodInfo);
            var callStatement = callAndReturnStatement.Item1;
            var returnStatement = callAndReturnStatement.Item2;

            var result = profilerClass.GetProfilerCallStatements(originalMethod, callStatement);

            if (!string.IsNullOrEmpty(returnStatement))
            {
                result += returnStatement + Environment.NewLine;
            }

            return result;
        }

        private static Tuple<string, string> GetInvocationAndReturnStatement(MethodInfo methodInfo)
        {
            var invocationStatement = string.Empty;
            var returnStatement = string.Empty;

            invocationStatement = GetInvocationStatementCore(methodInfo);

            if (methodInfo.ReturnsVoid)
            {
                invocationStatement = $"{invocationStatement};";
                returnStatement = string.Empty;
            }
            else if (methodInfo.IsAsync && methodInfo.ReturnsTask)
            {
                invocationStatement = $"await {invocationStatement};";
                returnStatement = string.Empty;
            }
            else if (methodInfo.IsAsync)
            {
                invocationStatement = $"var result = await {invocationStatement};";
                returnStatement = "return result;";
            }
            else
            {
                invocationStatement = $"var result = {invocationStatement};";
                returnStatement = "return result;";
            }

            return Tuple.Create(invocationStatement, returnStatement);
        }

        private static string GetInvocationStatementCore(MethodInfo methodInfo)
        {
            var methodName = methodInfo.Method.Identifier.ValueText;
            var typeParameterNames = methodInfo.Method.TypeParameterList?.Parameters.Select(item => item.Identifier.Text).ToList();
            var validModifiers = new[] { "ref", "out" };

            var parameterNamesAndModifierList = new List<string>();
            foreach (var parameter in methodInfo.Method.ParameterList.Parameters)
            {
                if (parameter.Modifiers.Any())
                {
                    var modifiers = string.Join(" ", parameter.Modifiers.Where(item => validModifiers.Contains(item.Text)));
                    parameterNamesAndModifierList.Add($"{modifiers} {parameter.Identifier.Text}");
                }
                else
                {
                    parameterNamesAndModifierList.Add(parameter.Identifier.Text);
                }
            }
            var parameters = string.Join(", ", parameterNamesAndModifierList);

            if (methodInfo.MethodSymbol.IsGenericMethod)
            {
                var typeParameters = string.Join(", ", typeParameterNames);

                return $"{methodName}<{typeParameters}>({parameters})";
            }

            return $"{methodName}({parameters})";
        }
    }
}
