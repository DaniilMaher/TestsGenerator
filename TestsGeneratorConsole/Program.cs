using System;
using System.Collections.Generic;
using TestsGenerator;

namespace ConsoleTestsGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                List<string> paths = new List<string>();

                paths.Add("e:\\Projects\\SPP\\TestsGenerator\\TestsGenerator\\FileReader.cs");
                paths.Add("e:\\Projects\\SPP\\TestsGenerator\\TestsGenerator\\FileWriter.cs");
                paths.Add("e:\\Projects\\SPP\\TestsGenerator\\TestsGenerator\\GeneratedTestClassFile.cs");
                paths.Add("e:\\Projects\\SPP\\TestsGenerator\\TestsGenerator\\Generator.cs");                
                string outputDirectory = "../../../GeneratedTests";
                int numberOfFilesToRead = 5;
                int numberOfFilesToWrite = 5;
                int maxTasksAmmount = 5;
                Generator generator = new Generator(numberOfFilesToRead, numberOfFilesToWrite, maxTasksAmmount, outputDirectory);
                generator.Generate(paths).Wait();
                Console.WriteLine("Files generated");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
