# Tingen Transmorger: Development Roadmap (Development)

## Test

- [ ] `private async Task StartApp()`
```csharp
var config = Configuration.Load(); <--[Verify]-->
...
Framework.Verify(config); <--[If the config file does not have an Import path, the app crashes.]-->
...
if (!flowControl)
{
    return; <--[The app should exit before it even gets to the main UI.]-->
}
```