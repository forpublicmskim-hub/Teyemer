using System.ComponentModel;
using System.Windows;
namespace Teyemer.App;
public partial class MainWindow : Window
{
 public MainWindow(MainViewModel viewModel) { InitializeComponent(); DataContext = viewModel; ThemeService.Attach(this); }
 private void Quick_Click(object sender, RoutedEventArgs e) { if (sender is System.Windows.Controls.Button { Tag: string value } && int.TryParse(value, out var minutes) && DataContext is MainViewModel vm) vm.ReminderIntervalMinutes = minutes; }
 protected override void OnClosing(CancelEventArgs e) { if (!AppController.IsExiting) { e.Cancel = true; Hide(); } base.OnClosing(e); }
}
