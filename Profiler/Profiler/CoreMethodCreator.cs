using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Geeks.Profiler
{
    internal static class CoreMethodCreator
    {
        public static SyntaxNode CreateMethod(MethodInfo methodInfo, string methodNameSuffix)
        {
            // If a generic method is overriding and its base has constraint clauses
            // We need to copy constraint clauses from base to core version
            var method = FixConstraintClauses(methodInfo);

            // We place this method first so we move the trailing trivia to wrapper method
            method = method.WithTrailingTrivia(null);

            // Remove all modifiers but these modifiers
            var modifiers = new SyntaxTokenList();
            foreach (var modifier in method.Modifiers)
            {
                if (modifier.Kind().Equals(SyntaxKind.PublicKeyword) || modifier.Kind().Equals(SyntaxKind.PrivateKeyword) ||
                    modifier.Kind().Equals(SyntaxKind.ProtectedKeyword) || modifier.Kind().Equals(SyntaxKind.InternalKeyword) ||
                    modifier.Kind().Equals(SyntaxKind.StaticKeyword) || modifier.Kind().Equals(SyntaxKind.AsyncKeyword))
                {
                    modifiers = modifiers.Add(modifier);
                }
            }
            method = method.WithModifiers(modifiers);

            var newName = method.Identifier.Text + methodNameSuffix;

            if (method.ExplicitInterfaceSpecifier != null)
            {
                var interfaceName = ExplicitInterfaceSpecifierToMethodName(method.ExplicitInterfaceSpecifier);
                newName = method.Identifier.Text + "_" + interfaceName + methodNameSuffix;
                method = method.WithExplicitInterfaceSpecifier(null);
            }

            var newMethodName = SyntaxFactory.Identifier(newName);
            return method.WithIdentifier(newMethodName);
        }

        private static MethodDeclarationSyntax FixConstraintClauses(MethodInfo methodInfo)
        {
            if (methodInfo.MethodSymbol.IsOverride && methodInfo.MethodSymbol.IsGenericMethod)
            {
                var currentMethod = methodInfo.MethodSymbol;
                ImmutableArray<SyntaxReference> overriddenMethodRefrences;

                while (currentMethod.IsOverride && currentMethod.IsGenericMethod)
                {
                    overriddenMethodRefrences = currentMethod.OverriddenMethod.DeclaringSyntaxReferences;
                    currentMethod = currentMethod.OverriddenMethod;
                }

                SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses;
                // If the base method is declared here we have the syntax
                // otherwise it's probably in an external library and we have to
                // build the constraints ourselves
                if (overriddenMethodRefrences.Any())
                {
                    foreach (var overriddenMethodRefrence in overriddenMethodRefrences)
                    {
                        var overriddenMethod = overriddenMethodRefrence.GetSyntax() as MethodDeclarationSyntax;

                        if (overriddenMethod.ConstraintClauses.Any())
                        {
                            constraintClauses = overriddenMethod.ConstraintClauses;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var overriddenMethodTypeParameter in methodInfo.MethodSymbol.OverriddenMethod.TypeParameters)
                    {
                        foreach (var constraintType in overriddenMethodTypeParameter.ConstraintTypes)
                        {
                            var constraints = new SeparatedSyntaxList<TypeParameterConstraintSyntax>();
                            constraints = constraints.Add(SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName(
                                $"{constraintType.ContainingNamespace}.{constraintType.Name}")));

                            var typeParameterConstraintClauseSyntax = SyntaxFactory.TypeParameterConstraintClause(
                                SyntaxFactory.Token(SyntaxKind.WhereKeyword),
                                SyntaxFactory.IdentifierName(overriddenMethodTypeParameter.Name),
                                SyntaxFactory.Token(SyntaxKind.ColonToken),
                                constraints);

                            constraintClauses = constraintClauses.Add(typeParameterConstraintClauseSyntax);
                        }
                    }
                }

                return methodInfo.Method.WithConstraintClauses(constraintClauses);
            }

            return methodInfo.Method;
        }

        private static string ExplicitInterfaceSpecifierToMethodName(ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierSyntax)
        {
            return explicitInterfaceSpecifierSyntax.Name.ToString().Replace(".", "");
        }
    }
}
