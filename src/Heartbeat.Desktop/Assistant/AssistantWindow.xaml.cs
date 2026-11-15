using System.Collections.Specialized;
using System.Windows;

namespace Heartbeat.Desktop.Assistant;

public partial class AssistantWindow : Window
{
    public AssistantWindow(AssistantViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Messages.CollectionChanged += OnMessagesChanged;
        Closed += (_, _) => viewModel.Messages.CollectionChanged -= OnMessagesChanged;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.BeginInvoke(ChatScroll.ScrollToEnd);
}
