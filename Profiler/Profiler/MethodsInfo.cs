using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Geeks.Profiler
{
    internal class ProjectMethodsInfo
    {
        public ProjectMethodsInfo(ProjectId projectId, IList<DocumentMethodsInfo> documentMethodsInfo)
        {
            ProjectId = projectId;
            DocumentMethodsInfo = documentMethodsInfo;
        }

        public ProjectId ProjectId { get; }

        public IList<DocumentMethodsInfo> DocumentMethodsInfo { get; }
    }

    internal class DocumentMethodsInfo
    {
        public DocumentMethodsInfo(string filePath, IList<MethodInfo> methods)
        {
            FilePath = filePath;
            Methods = methods;
        }

        public string FilePath { get; }

        public IList<MethodInfo> Methods { get; }
    }

    internal class MethodInfo
    {
        public MethodInfo(MethodDeclarationSyntax method, IMethodSymbol methodSymbol, string fullName, bool isAsync,
            bool returnsVoid, bool returnsTask, bool isEnumerable)
        {
            Method = method;
            MethodSymbol = methodSymbol;
            FullName = fullName;
            IsAsync = isAsync;
            ReturnsVoid = returnsVoid;
            ReturnsTask = returnsTask;
            IsEnumerable = isEnumerable;
        }

        public MethodDeclarationSyntax Method { get; }

        public IMethodSymbol MethodSymbol { get; }

        public string FullName { get; }

        public bool IsAsync { get; }

        public bool ReturnsVoid { get; }

        public bool ReturnsTask { get; }

        public bool IsEnumerable { get; }

        public bool IsEligibleForTransform()
        {
            if (MethodSymbol.IsAbstract)
            {
                return false;
            }

            if (MethodSymbol.IsExtern)
            {
                return false;
            }

            // Is partial and its the definition part
            if (MethodSymbol.PartialDefinitionPart == null && MethodSymbol.PartialImplementationPart != null)
            {
                return false;
            }

            if (Method.Body == null && Method.ExpressionBody == null)
            {
                return false;
            }

            return true;
        }
    }
}