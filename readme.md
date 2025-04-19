# CommandLine

This tiny library has an purpose to extract values
from the command line. It is not capable of interpreting
subcommands and does not use reflection to extract data.
It is so small that it contains a class and that
class contains 2 methods and 2 properties.

Consider this command line:

```
curl.exe -h "Content-Length: 0" -h "User-Agent: foo/1.0" -X POST https://example.com/posts
```

You can parse it and get each parameter with:

```csharp
static void Main(string[] args)
{
    var parser = new CommandLine.CommandLineParser(args, StringComparison.InvariantCulture);
    
    string[] headers = parser.GetValues("header", 'h').ToArray();
    string? method = parser.GetValue("method", 'X');
    string? url = parser.GetRemainder()?.LastOrDefault();
        
    Console.WriteLine("Headers  = {0}", string.Join(", ", headers));
    Console.WriteLine("Method   = {0}", method);
    Console.WriteLine("Url      = {0}", url);
}
```

### Getting multiple values

```
example.exe -p param1 param2 param3 -d "another command" --parameter param4 --foobar
```

```csharp
static void Main(string[] args)
{
    var parser = new CommandLine.CommandLineParser(args);
    
    string[] headers = parser.GetValues("parameter", 'p').ToArray();
    // ["param1", "param2", "param3", "param4"]
}
```

### Getting a single value

```
example.exe -d "command" --foo "hello"
```

```csharp
static void Main(string[] args)
{
    var parser = new CommandLine.CommandLineParser(args);
    
    string? d = parser.GetValue("directive", 'd'); // "command"
    string? f = parser.GetValue("foo", 'f'); // "hello"
    string? x = parser.GetValue("bar"); // null
}
```

### Check if has switch

```
example.exe --verbose
```

```csharp
static void Main(string[] args)
{
    var parser = new CommandLine.CommandLineParser(args);
    
    bool verbose = parser.IsDefined("verbose"); // short-terms are optional for all methods
}
```

### Get parameterless values

```
example.exe build --language "csharp" -r "System.dll" program.cs lib.cs --out item.cs foobar
```

```csharp
static void Main(string[] args)
{
    var parser = new CommandLine.CommandLineParser(args);
    
    string[] remainder = parser.GetRemainder().ToArray();
    // ["build", "program.cs", "lib.cs", "foobar"]
}
```

And that's all this lib can do.