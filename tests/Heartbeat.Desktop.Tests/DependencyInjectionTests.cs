using Heartbeat.Desktop.Navigation;
using Heartbeat.Desktop.Assistant;
using Heartbeat.Desktop.ViewModels;
using Heartbeat.Desktop.Views;

using Microsoft.Extensions.DependencyInjection;

namespace Heartbeat.Desktop.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void The_container_graph_is_valid()
    {
        ServiceCollection services = TestContainer.Services(out _);

        // ValidateOnBuild comprueba que todo lo registrado (incluida MainWindow) tiene sus dependencias resueltas,
        // sin necesidad de crear la ventana (que exige un hilo STA).
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        Assert.NotNull(provider.GetRequiredService<MainWindowViewModel>());
        Assert.NotNull(provider.GetRequiredService<IAssistantWindowPresenter>());
    }

    [Theory]
    [InlineData(typeof(LoginViewModel))]
    [InlineData(typeof(DashboardViewModel))]
    [InlineData(typeof(CheckUpViewModel))]
    [InlineData(typeof(HistoryViewModel))]
    [InlineData(typeof(MessagesViewModel))]
    [InlineData(typeof(VisionViewModel))]
    [InlineData(typeof(ProfileViewModel))]
    public void Every_page_can_be_navigated_to(Type pageType)
    {
        ServiceCollection services = TestContainer.Services(out _);
        using ServiceProvider provider = services.BuildServiceProvider();

        PageViewModel page = provider.GetRequiredService<INavigationService>().NavigateTo(pageType);

        Assert.IsType(pageType, page);
        Assert.False(string.IsNullOrWhiteSpace(page.Title));
    }

    [Fact]
    public void Main_window_is_registered_as_a_singleton()
    {
        ServiceCollection services = TestContainer.Services(out _);

        ServiceDescriptor descriptor = Assert.Single(services, d => d.ServiceType == typeof(MainWindow));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }
}
