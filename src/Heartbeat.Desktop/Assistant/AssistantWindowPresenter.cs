using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Heartbeat.Desktop.Assistant;

public interface IAssistantWindowPresenter
{
    void Show(Window owner);
}

public sealed class AssistantWindowPresenter(IServiceProvider services) : IAssistantWindowPresenter
{
    private AssistantWindow? _window;

    public void Show(Window owner)
    {
        if (_window is not null)
        {
            if (_window.WindowState == WindowState.Minimized)
            {
                _window.WindowState = WindowState.Normal;
            }

            _window.Activate();
            return;
        }

        _window = services.GetRequiredService<AssistantWindow>();
        _window.Owner = owner;
        _window.Closed += (_, _) => _window = null;
        _window.Show();
    }
}
