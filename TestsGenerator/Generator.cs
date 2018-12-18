using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestsGenerator
{
    public class Generator
    {
        private readonly string outputDirectory;
        private int maxReadersCount;
        private int maxWritersCount;
        private int maxTasksCount;
        private FileReader fileReader;
        private FileWriter fileWriter;

        public Generator(int maxReadersCount, int maxWritersCount, int maxTasksCount, string outputDirectory)
        {
            this.maxReadersCount = maxReadersCount;
            this.maxWritersCount = maxWritersCount;
            this.maxTasksCount = maxTasksCount;
            this.outputDirectory = outputDirectory;
            fileReader = new FileReader();
            fileWriter = new FileWriter();
        }

        public Task Generate(List<string> filesPaths)
        {
            ExecutionDataflowBlockOptions readingOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxReadersCount
            };
            TransformBlock<string, string> readerTransformBlock
                = new TransformBlock<string, string>(filePath => fileReader.ReadFileAsync(filePath), readingOptions);

            ExecutionDataflowBlockOptions writingOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxWritersCount
            };
            ActionBlock<List<GeneratedTestClassFile>> writerTransformBlock
                = new ActionBlock<List<GeneratedTestClassFile>>(generatedTestsClasses => fileWriter.WriteFileAsync(generatedTestsClasses), writingOptions);

            ExecutionDataflowBlockOptions generationOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxTasksCount
            };
            Func<string, List<GeneratedTestClassFile>> generationFunc = new Func<string, List<GeneratedTestClassFile>>(GenerateTestClasses);
            TransformBlock<string, List<GeneratedTestClassFile>> generatorTransformBlock
                = new TransformBlock<string, List<GeneratedTestClassFile>>(generationFunc, generationOptions);

            DataflowLinkOptions linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = true
            };
            
            readerTransformBlock.LinkTo(generatorTransformBlock, linkOptions);
            generatorTransformBlock.LinkTo(writerTransformBlock, linkOptions);

            foreach (string path in filesPaths)
            {
                readerTransformBlock.Post(path);
            }
            readerTransformBlock.Complete();
            return writerTransformBlock.Completion;
        }

        private List<GeneratedTestClassFile> GenerateTestClasses(string sourceFile)
        {
            List<GeneratedTestClassFile> result = new List<GeneratedTestClassFile>();
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceFile);
            CompilationUnitSyntax compilationUnitRoot = syntaxTree.GetCompilationUnitRoot();
            IEnumerable<ClassDeclarationSyntax> classes = compilationUnitRoot.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (ClassDeclarationSyntax classDeclaration in classes)
            {
                string className = classDeclaration.Identifier.ValueText;
                string name = Path.Combine(outputDirectory, className + "Test.cs");
                CompilationUnitSyntax testClassFile = GenerateTestClassFile(classDeclaration);
                result.Add(new GeneratedTestClassFile(name, testClassFile.NormalizeWhitespace().ToFullString()));
            }
            return result;
        }

        private CompilationUnitSyntax GenerateTestClassFile(ClassDeclarationSyntax classDeclaration)
        {
            var methods = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .Where(x => x.Modifiers.Any(y => y.ValueText == "public"));
            string ns = (classDeclaration.Parent as NamespaceDeclarationSyntax)?.Name.ToString();
            List<string> methodsNames = new List<string>();

            foreach (MethodDeclarationSyntax method in methods)
            {
                string tempMethodName = GetMethodName(methodsNames, method.Identifier.ToString(), 0);
                methodsNames.Add(tempMethodName);
            }            
            CompilationUnitSyntax result = CompilationUnit();
            result = result.WithUsings(GenerateUsingDirective());
            string className = classDeclaration.Identifier.ValueText;
            ClassDeclarationSyntax testClassDeclaration = ClassDeclaration(className + "Test");
            var classAttribute = Attribute(IdentifierName("TestClass"));
            var classAttributesList = SingletonList(AttributeList(SingletonSeparatedList(classAttribute)));
            testClassDeclaration = testClassDeclaration.WithAttributeLists(classAttributesList);
            testClassDeclaration = testClassDeclaration.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));
            testClassDeclaration = testClassDeclaration.WithMembers(GenerateMethodsList(methodsNames));
            SyntaxList<MemberDeclarationSyntax> classes = SingletonList<MemberDeclarationSyntax>(testClassDeclaration);
            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(QualifiedName(IdentifierName(ns), IdentifierName("Test")));
            namespaceDeclaration = namespaceDeclaration.WithMembers(classes);
            result = result.WithMembers(SingletonList<MemberDeclarationSyntax>(namespaceDeclaration));
            return result;
        }

        private string GetMethodName(List<string> methods, string method, int count)
        {
            string res = method + (count == 0 ? "" : count.ToString());
            if (methods.Contains(res)) return GetMethodName(methods, method, count + 1);
            return res;
        }

        private SyntaxList<UsingDirectiveSyntax> GenerateUsingDirective()
        {
            NameSyntax ns = IdentifierName("Mycrosoft");
            ns = QualifiedName(ns, IdentifierName("VisualStudio"));
            ns = QualifiedName(ns, IdentifierName("TestTools"));
            ns = QualifiedName(ns, IdentifierName("UnitTesting"));
            return new SyntaxList<UsingDirectiveSyntax>(UsingDirective(ns));
        }

        private SyntaxList<MemberDeclarationSyntax> GenerateMethodsList(List<string> methods)
        {
            List<MemberDeclarationSyntax> result = new List<MemberDeclarationSyntax>();
            foreach (string method in methods)
            {
                result.Add(GenerateMethodDeclaration(method));
            }
            return SyntaxFactory.List(result);
        }

        private MethodDeclarationSyntax GenerateMethodDeclaration(string methodName)
        {           
            var methodDeclaration = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), 
                                                                     Identifier(methodName + "Test"));
            var methodAttributes 
                = SingletonList(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("TestMethod")))));

            methodDeclaration = methodDeclaration.WithAttributeLists(methodAttributes);
            methodDeclaration = methodDeclaration.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));
            var assertFailExpression 
                = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                              IdentifierName("Assert"), 
                                                              IdentifierName("Fail")));
            var assertFailExpressionArgument 
                = Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("autogenerated")));
            var assertFailExpressionArgumentsList 
                = ArgumentList(SingletonSeparatedList(assertFailExpressionArgument));
            assertFailExpression = assertFailExpression.WithArgumentList(assertFailExpressionArgumentsList);
            methodDeclaration = methodDeclaration.WithBody(Block(ExpressionStatement(assertFailExpression)));
            return methodDeclaration;
        }
    }
}
