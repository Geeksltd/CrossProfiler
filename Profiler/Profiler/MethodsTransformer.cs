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

        public Solution TransformMethods(Solution solution, ProfilerClass profilerClass)
        {
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                foreach (var documentId in project.DocumentIds)
                {
                    var doc = project.GetDocument(documentId);
                    var docRoot = doc.GetSyntaxRootAsync().Result;

                    var semanticModel = doc.GetSemanticModelAsync().Result;
                    var methods = docRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
                    var index = 0;
                    var totalCount = methods.Length;
                    var renamePart = true;
                    MethodInfo originalMethod = null;
                    while (index < totalCount)
                    {
                        var method = methods.Skip(index).First();
                        var methodSymbol = method.GetMethodSymbol(semanticModel);
                        var methodInfo = method.GetMethodInfo(methodSymbol);

                        if (!methodInfo.IsEligibleForTransform())
                        {
                            index++;
                            continue;
                        }

                        if (renamePart)
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

                        if (!renamePart)
                        {
                            index += 2;
                        }
                        
                        renamePart = !renamePart;
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
