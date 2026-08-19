using System.Windows;
using System.Windows.Controls;

namespace Teyemer.App;

public partial class TimePicker : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty SelectedTimeProperty = DependencyProperty.Register(
        nameof(SelectedTime),
        typeof(TimeOnly),
        typeof(TimePicker),
        new FrameworkPropertyMetadata(
            TimeOnly.MinValue,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnSelectedTimeChanged));

    private bool _synchronizing;

    public TimePicker()
    {
        InitializeComponent();
        HourPicker.ItemsSource = Enumerable.Range(0, 24).Select(value => value.ToString("00")).ToArray();
        MinutePicker.ItemsSource = Enumerable.Range(0, 60).Select(value => value.ToString("00")).ToArray();
        SynchronizePickers(SelectedTime);
    }

    public TimeOnly SelectedTime
    {
        get => (TimeOnly)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    private static void OnSelectedTimeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is TimePicker picker && args.NewValue is TimeOnly value)
            picker.SynchronizePickers(value);
    }

    private void SynchronizePickers(TimeOnly value)
    {
        if (HourPicker is null || MinutePicker is null)
            return;

        _synchronizing = true;
        HourPicker.SelectedIndex = value.Hour;
        MinutePicker.SelectedIndex = value.Minute;
        _synchronizing = false;
    }

    private void Picker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_synchronizing || HourPicker.SelectedIndex < 0 || MinutePicker.SelectedIndex < 0)
            return;

        SetCurrentValue(SelectedTimeProperty, new TimeOnly(HourPicker.SelectedIndex, MinutePicker.SelectedIndex));
    }
}
