using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Geeks.Profiler
{
    internal class MethodCollector : CSharpSyntaxWalker
    {
        public readonly List<MethodInfo> Methods;
        private readonly SemanticModel _semanticModel;

        public MethodCollector(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
            Methods = new List<MethodInfo>();
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var methodSymbol = node.GetMethodSymbol(_semanticModel);
            var methodInfo = node.GetMethodInfo(methodSymbol);

            if (!methodInfo.IsEligibleForTransform())
            {
                return;
            }
                
            Methods.Add(methodInfo);
        }
    }
}
