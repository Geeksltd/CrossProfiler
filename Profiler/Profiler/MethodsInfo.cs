using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Geeks.Profiler
{
    internal class ProjectMethodsInfo
    {
        public ProjectId ProjectId
        {
            get;
            private set;
        }

        public ICollection<DocumentMethodsInfo> DocumentMethodsInfo
        {
            get;
            private set;
        }

        public ProjectMethodsInfo(ProjectId projectId, ICollection<DocumentMethodsInfo> documentMethodsInfo)
        {
            ProjectId = projectId;
            DocumentMethodsInfo = documentMethodsInfo;
        }
    }

    internal class DocumentMethodsInfo
    {
        public DocumentId DocumentId
        {
            get;
            private set;
        }

        public ICollection<MethodInfo> Methods
        {
            get;
            private set;
        }

        public DocumentMethodsInfo(DocumentId documentId, ICollection<MethodInfo> methods)
        {
            DocumentId = documentId;
            Methods = methods;
        }
    }

    internal class MethodInfo
    {
        public MethodDeclarationSyntax Method
        {
            get;
            private set;
        }

        public IMethodSymbol MethodSymbol
        {
            get;
            private set;
        }

        public string FullName
        {
            get;
            private set;
        }

        public bool IsAsync
        {
            get;
            private set;
        }

        public bool ReturnsVoid
        {
            get;
            private set;
        }

        public bool ReturnsTask
        {
            get;
            private set;
        }

        public bool IsEnumerable
        {
            get;
            private set;
        }

        public MethodInfo(MethodDeclarationSyntax method, IMethodSymbol methodSymbol, string fullName, bool isAsync, bool returnsVoid, bool returnsTask, bool isEnumerable)
        {
            Method = method;
            MethodSymbol = methodSymbol;
            FullName = fullName;
            IsAsync = isAsync;
            ReturnsVoid = returnsVoid;
            ReturnsTask = returnsTask;
            IsEnumerable = isEnumerable;
        }
    }
}
