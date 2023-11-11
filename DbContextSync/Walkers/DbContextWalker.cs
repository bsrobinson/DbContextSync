using System.Collections.Generic;
using System.Linq;
using DbContextSync.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DbContextSync.Models;
using System.IO;
using Ganss.IO;
using DbContextSync.Helpers;

namespace DbContextSync.Walkers
{
    public static class DbContextWalker
    {
        public static List<DbContextDatabase> GetDbContext(this Arguments args)
        {
            List<DbContextDatabase> databases = new();
            Dictionary<string, List<DbContextTable>> tables = new();

            foreach (string file in Glob.Expand(args.DbContextFileGlob).Select(p => p.FullName))
            {
                string source = File.ReadAllText(file);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
                CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();

                ModelWalker modelCollector = new(Path.GetFullPath(file));
                modelCollector.Visit(root);

                databases.AddRange(modelCollector.DbContexts);
                foreach (string ns in modelCollector.Tables.Keys)
                {
                    tables.AddToKeyedList(ns, modelCollector.Tables[ns]);
                }
            }

            if (databases.Count == 0)
            {
                throw ErrorState.NoDbContexts.Kill(args.DbContextFileGlob, true);
            }

            foreach (DbContextDatabase database in databases)
            {
                foreach (DbContextTable table in database.Tables)
                {
                    if (tables.ContainsKey(database.Namespace))
                    {
                        DbContextTable? matchingTable = tables[database.Namespace].FirstOrDefault(t => t.ClassName == table.ClassName);
                        if (matchingTable != null)
                        {
                            table.ClassPath = matchingTable.ClassPath;
                            table.ClassSpan = matchingTable.ClassSpan;
                            table.ClassFullSpan = matchingTable.ClassFullSpan;
                            table.PrimryKeyAttributePosition = matchingTable.PrimryKeyAttributePosition;
                            table.Fields = matchingTable.Fields;
                        }
                    }
                }
            }

            return databases;
        }
    }
    

    public class ModelWalker : CSharpSyntaxWalker
    {
        private string _path;

        public readonly Dictionary<string, List<DbContextTable>> Tables = new();
        public readonly List<DbContextDatabase> DbContexts = new();

        public ModelWalker(string path)
        {
            _path = path;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node) { CreateObject(node); }
        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) { CreateObject(node); }
        public override void VisitRecordDeclaration(RecordDeclarationSyntax node) { CreateObject(node); }

        private void CreateObject(TypeDeclarationSyntax node)
        {
            if (node.HasBaseClass("DbContext"))
            {
                CreateDbContext(node);
            }
            else
            {
                CreateTable(node);
            }
        }

        private void CreateDbContext(TypeDeclarationSyntax node)
        {
            DbContexts.Add(new DbContextDatabase(node.Identifier.ToString())
            {
                Path = _path,
                Namespace = node.GetNamespace(),
                CloseBraceSpan = node.CloseBraceToken.FullSpan,
                Tables = node.Members.OfType<PropertyDeclarationSyntax>()
                    .Select(p => p.Type).OfType<GenericNameSyntax>()
                    .Where(t => t.Identifier.ToString() == "DbSet")
                    .Select(CreateDbContextTable)
                    .Cast<Table>()
                    .ToList(),
            });
        }

        private DbContextTable CreateDbContextTable(GenericNameSyntax genericName)
        {
            return new()
            {
                Name = ((PropertyDeclarationSyntax)genericName.Parent!).Identifier.ToString(),
                ClassName = genericName.TypeArgumentList.Arguments.First().ToString(),
                PositionInContextClass = genericName.Parent.Span,
                FullPositionInContextClass = genericName.Parent.FullSpan,
            };
        }

        private void CreateTable(TypeDeclarationSyntax node)
        {
            DbContextTable table = new DbContextTable()
            {
                Name = $"{node.Identifier}{node.TypeParameterList?.ToString()}",
                ClassName = $"{node.Identifier}{node.TypeParameterList?.ToString()}",
                Fields = node.Members.OfType<PropertyDeclarationSyntax>().Select(CreateField).Cast<Field>().ToList(),
                ClassPath = _path,
                ClassSpan = node.Members.Count > 0 ? node.Members.Span : node.CloseBraceToken.FullSpan,
                ClassFullSpan = node.FullSpan,
            };

            if (node.TryGetAttribute("PrimaryKey", out AttributeSyntax? pkAttr))
            {
                table.PrimryKeyAttributePosition = pkAttr.Span;
                foreach (string key in node.StringAttributeValues("PrimaryKey"))
                {
                    int index = table.Fields.FindIndex(f => f.Name == key);
                    if (index >= 0)
                    {
                        table.Fields[index].PrimaryKey = true;
                    }
                }
            }
            foreach (AttributeSyntax attribute in node.GetAttributes("Index"))
            {
                IEnumerable<AttributeArgumentSyntax>? arguments = attribute.ArgumentList?.Arguments;
                if (arguments != null && arguments.Any(a => a.ToString().Contains("IsUnique") && a.ToString().Contains("true")))
                {
                    string field = arguments.First().ToString();
                    if (field.StartsAndEndsNoCase('"')) { field = field[1..^1]; }
                    if (field.StartsNoCase("nameof(") && field.EndsWith(")")) { field = field[7..^1]; }

                    int index = table.Fields.FindIndex(f => f.Name == field);
                    if (index >= 0)
                    {
                        table.Fields[index].IsUnique = true;
                        ((DbContextField)table.Fields[index]).IndexAttributePosition = attribute.Span;
                    }
                }
            }
            
            Tables.AddToKeyedList(node.GetNamespace(), table);
        }

        private DbContextField CreateField(PropertyDeclarationSyntax property)
        {
            string typeName = property.StringAttributeValue("DataType") ?? property.Type.ToString();
            bool nullable = typeName.EndsWith("?");
            if (nullable)
            {
                typeName = typeName[..^1];
            }

            return new DbContextField
            {
                Name = property.Identifier.ToString(),
                Type = new(typeName, property.IntAttributeValue("MaxLength")),
                PrimaryKey = property.ContainsAttribute("Key"),
                IsUnique = false,
                Required = nullable ? false : (property.ContainsAttribute("Required") || property.ContainsModifier("required")),
                PositionInClass = property.Span,
                FullPositionInClass = property.FullSpan,
            };
        }
    }
}