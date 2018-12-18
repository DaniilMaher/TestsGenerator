namespace TestsGenerator
{
    class GeneratedTestClassFile
    {
        public string Name
        {
            get;
            private set;
        }

        public string Code
        {
            get;
            private set;
        }

        public GeneratedTestClassFile(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }
}
