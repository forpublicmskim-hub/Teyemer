using System.Windows;
using System.Windows.Controls;
using Teyemer.Core;

namespace Teyemer.App;

public partial class AlarmTypePicker : System.Windows.Controls.UserControl
{
    private static readonly AlarmTypeItem[] Items =
    [
        new(CustomAlarmType.Periodic, "주기적"),
        new(CustomAlarmType.DailyTime, "매일 특정 시간")
    ];

    public static readonly DependencyProperty SelectedTypeProperty = DependencyProperty.Register(
        nameof(SelectedType),
        typeof(CustomAlarmType),
        typeof(AlarmTypePicker),
        new FrameworkPropertyMetadata(
            CustomAlarmType.Periodic,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnSelectedTypeChanged));

    private bool _synchronizing;

    public AlarmTypePicker()
    {
        InitializeComponent();
        TypePicker.ItemsSource = Items;
        TypePicker.DisplayMemberPath = nameof(AlarmTypeItem.Label);
        SynchronizePicker(SelectedType);
    }

    public CustomAlarmType SelectedType
    {
        get => (CustomAlarmType)GetValue(SelectedTypeProperty);
        set => SetValue(SelectedTypeProperty, value);
    }

    private static void OnSelectedTypeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is AlarmTypePicker picker && args.NewValue is CustomAlarmType value)
            picker.SynchronizePicker(value);
    }

    private void SynchronizePicker(CustomAlarmType value)
    {
        if (TypePicker is null) return;
        _synchronizing = true;
        TypePicker.SelectedIndex = Array.FindIndex(Items, item => item.Value == value);
        _synchronizing = false;
    }

    private void TypePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_synchronizing || TypePicker.SelectedItem is not AlarmTypeItem item) return;
        SetCurrentValue(SelectedTypeProperty, item.Value);
    }

    private sealed record AlarmTypeItem(CustomAlarmType Value, string Label)
    {
        public override string ToString() => Label;
    }
}
