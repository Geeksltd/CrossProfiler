using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Geeks.Profiler
{
    internal class RegionRemoverSyntaxRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            var updatedTrivia = base.VisitTrivia(trivia);
            if (trivia.Kind() == SyntaxKind.RegionDirectiveTrivia ||
                trivia.Kind() == SyntaxKind.EndRegionDirectiveTrivia)
            {
                // Remove the trivia entirely by returning default(SyntaxTrivia).
                updatedTrivia = default(SyntaxTrivia);
            }

            return updatedTrivia;
        }
    }
}
