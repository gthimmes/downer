using Avalonia;
using Avalonia.Headless;
using Downer;
using Downer.UiTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Downer.UiTests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false, // real Skia rendering so frames can be captured
        })
        .UseSkia()
        .WithInterFont();
}
