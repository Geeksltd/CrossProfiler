using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Geeks.Profiler
{
    internal class MethodsTransformer
    {
        public const string MethodNameSuffix = "_impl";
        public readonly List<ProjectMethodsInfo> MethodsInfo;

        public MethodsTransformer()
        {
            MethodsInfo = new List<ProjectMethodsInfo>();
        }

        public Solution TransformMethods(Solution solution, ProfilerClass profilerClass)
        {
            foreach (var projectId in solution.ProjectIds)
            {
                var projectMethodsInfo = new ProjectMethodsInfo(projectId, new List<DocumentMethodsInfo>());
                MethodsInfo.Add(projectMethodsInfo);

                var project = solution.GetProject(projectId);

                foreach (var documentId in project.DocumentIds)
                {
                    var methodInfos = new List<MethodInfo>();

                    var doc = project.GetDocument(documentId);

                    var documentMethodsInfo = MethodsInfo.SelectMany(item => item.DocumentMethodsInfo)
                        .FirstOrDefault(item => item.FilePath.Equals(doc.FilePath));
                    if (documentMethodsInfo != null)
                    {
                        methodInfos.AddRange(documentMethodsInfo.Methods);
                        documentMethodsInfo = new DocumentMethodsInfo(doc.FilePath, methodInfos);
                        projectMethodsInfo.DocumentMethodsInfo.Add(documentMethodsInfo);

                        continue;
                    }

                    documentMethodsInfo = new DocumentMethodsInfo(doc.FilePath, methodInfos);
                    projectMethodsInfo.DocumentMethodsInfo.Add(documentMethodsInfo);

                    var docRoot = doc.GetSyntaxRootAsync().Result;
                    var semanticModel = doc.GetSemanticModelAsync().Result;
                    var methods = docRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
                    var index = 0;
                    var totalCount = methods.Length;
                    var renamePhase = true;
                    MethodInfo originalMethod = null;

                    while (index < totalCount)
                    {
                        var method = methods.Skip(index).First();
                        var methodSymbol = method.GetMethodSymbol(semanticModel);
                        var methodInfo = method.GetMethodInfo(methodSymbol);
                        if (renamePhase)
                        {
                            methodInfos.Add(methodInfo);
                        }

                        if (!methodInfo.IsEligibleForTransform())
                        {
                            index++;
                            continue;
                        }

                        if (renamePhase)
                        {
                            // Rename method to xxx_impl
                            originalMethod = methodInfo;
                            docRoot = docRoot.ReplaceNode(method, CoreMethodCreator.CreateMethod(methodInfo, MethodNameSuffix));
                        }
                        else
                        {
                            // Add wrapper method
                            var wrapperMethod = WrapperMethodCreator.GetMethod(methodInfo, originalMethod, profilerClass);
                            docRoot = docRoot.InsertNodesAfter(method, new List<SyntaxNode> { wrapperMethod });
                        }

                        doc = doc.WithSyntaxRoot(docRoot);
                        docRoot = doc.GetSyntaxRootAsync().Result;
                        semanticModel = doc.GetSemanticModelAsync().Result;

                        methods = docRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
                        totalCount = methods.Length;

                        if (!renamePhase)
                        {
                            index += 2;
                        }

                        renamePhase = !renamePhase;
                    }

                    docRoot = Formatter.Format(docRoot, solution.Workspace, solution.Workspace.Options);
                    doc = doc.WithSyntaxRoot(docRoot);
                    project = doc.Project;
                }

                solution = project.Solution;
            }

            return solution;
        }
    }
}
