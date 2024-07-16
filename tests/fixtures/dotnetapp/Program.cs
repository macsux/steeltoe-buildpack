//Console.WriteLine(string.Join('\n', Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>().Select(x => $"{x.Key}={x.Value}")));
//Console.WriteLine(File.ReadAllText("/home/vcap/deps/0/MyBuildpackHostingStartup.deps.json"));

// using MyBuildpackHostingStartup;

using System.Net;

// Console.WriteLine(File.ReadAllText("/home/vcap/deps/1/dotnet_publish/SampleApp.runtimeconfig.json"));
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();
// var type = new MyBuildpackStartupInjector();
// Console.WriteLine(type.ToString());
app.MapGet("/", () => "Hello World!");
// app.Use(async (context, next2) =>
// {
//     Console.WriteLine($"Someone called me on {context.Request.Path}");
//     if (context.Request.Path == "/hello")
//     {
//         context.Response.StatusCode = (int)HttpStatusCode.OK;
//         await context.Response.WriteAsync("Hello world");
//     }
//     await next2();
// });
app.Run();
