using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace Geeks.Profiler
{
    internal static class MethodDeclarationSyntaxExtensions
    {
        public static IMethodSymbol GetMethodSymbol(this MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            return (IMethodSymbol)semanticModel.GetDeclaredSymbol(method);
        }

        public static MethodInfo GetMethodInfo(this MethodDeclarationSyntax method, IMethodSymbol methodSymbol)
        {
            var fullName = methodSymbol.ToString();

            // TODO: seems a bug in roslyn, I hope we can remove this in next roslyn release
            // If method is an expression body and explicitly implements an interface we need to add the interface to name
            if (method.Body == null && method.ExpressionBody != null && method.ExplicitInterfaceSpecifier != null)
            {
                fullName = Regex.Replace(fullName, "(.*\\.)(.+)?(\\(.*)", m => m.Groups[1].Value + method.ExplicitInterfaceSpecifier.Name.ToString() + "." + m.Groups[2].Value + m.Groups[3].Value);
            }

            var isVoid = false;
            var isTask = false;
            var isEnumerable = false;

            if (methodSymbol.IsAsync)
            {
                if (methodSymbol.ReturnsVoid)
                {
                    isVoid = true;
                }
                // TODO: Hopefully will replace this with a better api in next roslyn versions
                else if (methodSymbol.ReturnType.ToString().Equals("System.Threading.Tasks.Task") || methodSymbol.ReturnType.ToString().Equals("Task"))
                {
                    isTask = true;
                }
            }
            else
            {
                isVoid = methodSymbol.ReturnsVoid;
                if (methodSymbol.ReturnsVoid)
                {
                    isVoid = true;
                }
                else if (methodSymbol.ReturnType.ToString().StartsWith("System.Collections.Generic.IEnumerable"))
                {
                    isEnumerable = true;
                }
            }

            return new MethodInfo(method, methodSymbol, fullName, methodSymbol.IsAsync, isVoid, isTask, isEnumerable);
        }
    }
}
