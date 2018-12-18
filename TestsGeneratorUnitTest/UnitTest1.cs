using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestsGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.IO;
using System.Linq;
using System.Threading;

namespace TestsGeneratorUnitTests
{
    [TestClass]
    public class UnitTests
    {
        private string outputDirectory = "../../../../GeneratedTests";
        private Generator generator;
        private List<string> paths;
        private CompilationUnitSyntax compilationUnit1, compilationUnit2;

        [TestInitialize]
        public void Initialize()
        {
            generator = new Generator(4, 4, 4, outputDirectory);
            paths = new List<string>()
            {
                "../../../../InputFiles/Foo.cs",
                "../../../../InputFiles/IntGenerator.cs"
            };

            generator.Generate(paths).Wait();
            compilationUnit1 = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(outputDirectory, "FooTest.cs"))).GetCompilationUnitRoot();
            compilationUnit2 = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(outputDirectory, "IntGeneratorTest.cs"))).GetCompilationUnitRoot();
        }

        [TestMethod]
        public void GeneratedFilesTest()
        {
            var files = Directory.GetFiles(outputDirectory);

            Assert.AreEqual(2, files.Length);
        }

        [TestMethod]
        public void NamespacesTest()
        {
            IEnumerable<NamespaceDeclarationSyntax> namespaces;
            namespaces = compilationUnit1.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
            Assert.AreEqual(1, namespaces.Count());
            Assert.AreEqual("FakerLib.Test", namespaces.First().Name.ToString());
            namespaces = compilationUnit2.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
            Assert.AreEqual(1, namespaces.Count());
            Assert.AreEqual("FakerLib.Test", namespaces.First().Name.ToString());
        }

        [TestMethod]
        public void ClassesTest()
        {
            IEnumerable<ClassDeclarationSyntax> classes;

            classes = compilationUnit1.DescendantNodes().OfType<ClassDeclarationSyntax>();
            Assert.AreEqual(1, classes.Count());
            Assert.AreEqual("FooTest", classes.First().Identifier.ToString());

            classes = compilationUnit2.DescendantNodes().OfType<ClassDeclarationSyntax>();
            Assert.AreEqual(1, classes.Count());
            Assert.AreEqual("IntGeneratorTest", classes.First().Identifier.ToString());
        }

        [TestMethod]
        public void ClassesAttributeTest()
        {
            Assert.AreEqual(1, compilationUnit1.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where((classDeclaration) => classDeclaration.AttributeLists.Any((attributeList) => attributeList.Attributes
                .Any((attribute) => attribute.Name.ToString() == "TestClass"))).Count());
        }

        [TestMethod]
        public void MethodsTest()
        {
            List<string> expected = new List<string>
            {
                "GenerateTest"
            };
            List<string> actual = compilationUnit2.DescendantNodes().OfType<MethodDeclarationSyntax>().Select((method) => method.Identifier.ToString()).ToList();

            CollectionAssert.AreEquivalent(expected, compilationUnit2.DescendantNodes().OfType<MethodDeclarationSyntax>().Select((method) => method.Identifier.ToString()).ToList());
        }

        [TestCleanup]
        public void CleanUp()
        {
            var files = Directory.GetFiles(outputDirectory);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }
}
