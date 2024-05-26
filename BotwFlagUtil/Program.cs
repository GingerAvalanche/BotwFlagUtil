using BotwFlagUtil;

Helpers.RootDir = @"/i/dunno/some/path/i/guess";

Generator generator = new();
generator.GenerateMapFlags();
generator.GenerateItemFlags();
generator.GenerateEventFlags();
