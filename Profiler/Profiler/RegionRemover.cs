using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace Geeks.Profiler
{
    internal class RegionRemover
    {
        public Solution RemoveRegions(Solution solution)
        {
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                foreach (var documentId in project.DocumentIds)
                {
                    var doc = project.GetDocument(documentId);
                    var docRoot = doc.GetSyntaxRootAsync().Result;

                    // Remove all regions
                    var regionRemover = new RegionRemoverSyntaxRewriter();
                    docRoot = regionRemover.Visit(docRoot);

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
