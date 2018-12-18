using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestsGenerator
{
    class FileWriter
    {
        public async Task WriteFileAsync(List<GeneratedTestClassFile> generatedTests)
        {    
            foreach (var resultTest in generatedTests)
            {
                using (StreamWriter sw = new StreamWriter(resultTest.Name))
                {
                    await sw.WriteAsync(resultTest.Code);
                }
            }
        }
    }
}
