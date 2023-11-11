using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DbContextSync.Extensions
{
    internal static class SyntaxExtensions
    {
        public static string GetNamespace(this TypeSyntax typeDeclarationSyntax) =>
            typeDeclarationSyntax.Ancestors().OfType<NamespaceDeclarationSyntax>().First().Name.ToString();

        public static string GetNamespace(this TypeDeclarationSyntax typeDeclarationSyntax) =>
            typeDeclarationSyntax.Ancestors().OfType<NamespaceDeclarationSyntax>().First().Name.ToString();

        public static bool HasBaseClass(this TypeDeclarationSyntax typeDeclarationSyntax, string baseClass) =>
            typeDeclarationSyntax.BaseList?.Types.Any(t => t.ToString() == baseClass) ?? false;

        public static bool ContainsModifier(this PropertyDeclarationSyntax propertyDeclarationSyntax, string modifierName) =>
                propertyDeclarationSyntax.Modifiers.Any(m => m.Value?.ToString() == modifierName);

        public static bool TryGetAttribute(this TypeDeclarationSyntax propertyDeclarationSyntax, string attributeName, [NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            attribute = propertyDeclarationSyntax.AttributeLists.SelectMany(attributeList =>
                attributeList.Attributes).FirstOrDefault(attribute =>
                    attribute.Name.ToString().Equals(attributeName));

            return attribute != null;
        }

        public static List<AttributeSyntax> GetAttributes(this TypeDeclarationSyntax propertyDeclarationSyntax, string attributeName)
            => propertyDeclarationSyntax.AttributeLists.SelectMany(attributeList =>
                attributeList.Attributes).Where(attribute =>
                    attribute.Name.ToString().Equals(attributeName)).ToList();

        public static bool ContainsAttribute(this PropertyDeclarationSyntax propertyDeclarationSyntax, string attributeName) =>
                propertyDeclarationSyntax.AttributeLists.Any(attributeList =>
                    attributeList.Attributes.Any(attribute =>
                        attribute.Name.ToString().Equals(attributeName)));

        public static string? StringAttributeValue(this TypeDeclarationSyntax typeDeclarationSyntax, string attributeName)
        {
            List<string> values = typeDeclarationSyntax.AttributeLists.StringAttributeValues(attributeName);
            return values.Count == 0 ? null : values[0];
        }

        public static string? StringAttributeValue(this PropertyDeclarationSyntax propertyDeclarationSyntax, string attributeName)
        {
            List<string> values = propertyDeclarationSyntax.AttributeLists.StringAttributeValues(attributeName);
            return values.Count == 0 ? null : values[0];
        }

        public static List<string> StringAttributeValues(this TypeDeclarationSyntax typeDeclarationSyntax, string attributeName) =>
            typeDeclarationSyntax.AttributeLists.StringAttributeValues(attributeName);

        public static List<string> StringAttributeValues(this PropertyDeclarationSyntax propertyDeclarationSyntax, string attributeName) =>
            propertyDeclarationSyntax.AttributeLists.StringAttributeValues(attributeName);

        public static List<string> StringAttributeValues(this SyntaxList<AttributeListSyntax> attributeListSyntaxes, string attributeName)
        {
            List<string> values = new();
            foreach (AttributeListSyntax attributeListSyntax in attributeListSyntaxes)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (attributeSyntax.Name.ToString() == attributeName && attributeSyntax.ArgumentList != null)
                    {
                        foreach (AttributeArgumentSyntax argument in attributeSyntax.ArgumentList.Arguments)
                        {
                            string s = argument.Expression.ToString();
                            if (s.StartsAndEndsNoCase('"')) { s = s[1..^1]; }
                            if (s.StartsNoCase("nameof(") && s.EndsWith(")")) { s = s[7..^1]; }
                            values.Add(s);
                        }
                    }
                }
            }
            return values;
        }

        public static int? IntAttributeValue(this PropertyDeclarationSyntax propertyDeclarationSyntax, string attributeName)
        {
            foreach (AttributeListSyntax attributeListSyntax in propertyDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (attributeSyntax.Name.ToString() == attributeName && attributeSyntax.ArgumentList != null)
                    {
                        foreach (AttributeArgumentSyntax argument in attributeSyntax.ArgumentList.Arguments)
                        {
                            if (int.TryParse(argument.Expression.ToString(), out int value))
                            {
                                return value;
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
