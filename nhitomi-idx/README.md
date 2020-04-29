# nhitomi-idx

This is nhitomi backend server written in C#.

### Prerequisites

- [.NET Core SDK](https://dotnet.microsoft.com/download) 3.1+
- Any C# IDE such as [Visual Studio](https://visualstudio.microsoft.com/vs/), [VS Code](https://code.visualstudio.com/) or [Rider](https://www.jetbrains.com/rider/)

## Testing

[nhitomi.Tests](https://github.com/chiyadev/nhitomi/tree/master/nhitomi-idx/nhitomi.Tests) contains tests written using NUnit. These include many integration tests that depend on Elasticsearch and Redis.

It is strongly recommend to have a locally running Elasticsearch instance to speed up the testing time. Each test initializes a new set of indexes with a random prefix to avoid collision with other tests, and are automatically deleted when finished. Redis must also be running (easily installable on Linux, using [WSL](https://en.wikipedia.org/wiki/Windows_Subsystem_for_Linux) on Windows).