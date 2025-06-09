using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerators
{
    public class FileDebugLoggerSyntaxReceiver : ISyntaxReceiver
    {
        public StructDeclarationSyntax? MainContainerSyntax { get; private set; }
        public List<StructDeclarationSyntax> ConcreteLoggerSyntaxes { get; } = new List<StructDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is StructDeclarationSyntax structDeclaration)
            {
                if (!this.IsPartialStruct(structDeclaration)) return;

                if (this.HasAttribute(structDeclaration, "MainFileDebugLoggerContainer"))
                {
                    this.MainContainerSyntax = structDeclaration;
                    return;
                }

                if (!this.ImplementsIFileDebugLogger(structDeclaration)) return;

                this.ConcreteLoggerSyntaxes.Add(structDeclaration);
            }
                
        }

        private bool IsPartialStruct(StructDeclarationSyntax structDeclaration)
        {
            return structDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        }

        private bool HasAttribute(StructDeclarationSyntax structDeclaration, string attributeName)
        {
            return structDeclaration.AttributeLists.Any(
                attributeList => attributeList.Attributes.Any(
                    attribute => attribute.Name.ToString() == attributeName));
        }

        private bool ImplementsIFileDebugLogger(StructDeclarationSyntax structDeclaration)
        {
            if (structDeclaration.BaseList == null) return false;

            return structDeclaration.BaseList.Types
                .Any(baseType =>
                {
                    if (baseType.Type is GenericNameSyntax genericName)
                    {
                        return genericName.Identifier.Text == "IFileDebugLogger" &&
                            genericName.TypeArgumentList.Arguments.Count == 1;
                    }

                    return false;
                });

        }

    }

}