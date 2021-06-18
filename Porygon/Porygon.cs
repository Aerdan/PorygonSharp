using System;
using System.IO;

using Tommy;

namespace Porygon {
    class Porygon {
        static int Main(string[] args) {
            TomlTable cfg = null;
            Project prj;

            if (args.Length < 2) {
                Console.WriteLine("Usage: Porygon.exe project.toml file.rom");
                return 1;
            }
            using (StreamReader r = new StreamReader(File.OpenRead(args[0]))) {
                try {
                    cfg = TOML.Parse(r);
                } catch (TomlParseException ex) {
                    Console.WriteLine($"Error parsing {args[0]}:");

                    foreach (TomlSyntaxException sex in ex.SyntaxErrors) {
                        Console.WriteLine($"{sex.Line}:{sex.Column}: {sex.Message}");
                    }

                    return 1;
                }
            }
            prj = new Project(cfg);
            prj.Process(args[1]);
            
            return 0;
        }
    }
}