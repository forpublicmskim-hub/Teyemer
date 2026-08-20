using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace Teyemer.App;

/// <summary>
/// Keeps the app menu above the Explorer overflow flyout without activating it.
/// A click inside the menu is handled by ContextMenuStrip first; a new mouse
/// press outside the menu closes only the app menu.
/// </summary>
internal sealed class TrayContextMenuCoordinator : IDisposable
{
    private const int WmMouseActivate = 0x0021;
    private const int MaNoActivate = 3;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private static readonly IntPtr HwndTopmost = new(-1);

    private readonly Forms.ContextMenuStrip _menu;
    private readonly Forms.Timer _outsideClickTimer = new() { Interval = 10 };
    private readonly NonActivatingClickWindow _menuWindow = new();
    private Forms.MouseButtons _previousMouseButtons;
    private Forms.ToolStripItem? _pressedItem;
    private bool _pressedItemClicked;
    private bool _disposed;

    public TrayContextMenuCoordinator(Forms.ContextMenuStrip menu)
    {
        _menu = menu;
        _menu.AutoClose = true;
        _menu.Opened += OnOpened;
        _menu.Closed += OnClosed;
        _menu.ItemClicked += OnItemClicked;
        _outsideClickTimer.Tick += OnOutsideClickTimer;
    }

    public void Show(System.Drawing.Point screenLocation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_menu.Visible) return;

        _menu.Show(screenLocation);

        // Do not activate the menu: Explorer remains the active owner of its
        // overflow flyout. Topmost only establishes click priority/Z-order.
        KeepAboveOverflow();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _menuWindow.Attach(_menu.Handle);
        _pressedItem = null;
        _pressedItemClicked = false;
        _previousMouseButtons = Forms.Control.MouseButtons;
        _outsideClickTimer.Start();
    }

    private void OnClosed(object? sender, Forms.ToolStripDropDownClosedEventArgs e)
    {
        _outsideClickTimer.Stop();
        _pressedItem = null;
        _pressedItemClicked = false;
        _menuWindow.Detach();
    }

    private void OnItemClicked(object? sender, Forms.ToolStripItemClickedEventArgs e)
    {
        if (ReferenceEquals(e.ClickedItem, _pressedItem))
            _pressedItemClicked = true;
    }

    private void OnOutsideClickTimer(object? sender, EventArgs e)
    {
        if (!_menu.Visible)
        {
            _outsideClickTimer.Stop();
            return;
        }

        var previous = _previousMouseButtons;
        var current = Forms.Control.MouseButtons;
        var newlyPressed = current & ~previous;
        var newlyReleased = previous & ~current;
        _previousMouseButtons = current;

        if (newlyPressed != Forms.MouseButtons.None)
        {
            if (!_menu.Bounds.Contains(Forms.Cursor.Position))
            {
                _menu.Close(Forms.ToolStripDropDownCloseReason.AppClicked);
                return;
            }

            KeepAboveOverflow();
            _pressedItem = _menu.GetItemAt(_menu.PointToClient(Forms.Cursor.Position));
            _pressedItemClicked = false;
        }

        if (newlyReleased == Forms.MouseButtons.None || _pressedItem is null) return;

        var pressedItem = _pressedItem;
        var wasClicked = _pressedItemClicked;
        _pressedItem = null;
        _pressedItemClicked = false;

        if (wasClicked || !pressedItem.Enabled || !pressedItem.Available) return;
        if (!pressedItem.Bounds.Contains(_menu.PointToClient(Forms.Cursor.Position))) return;

        // A non-activating menu can lose the first native click when focus is
        // transferred between a visible WPF window and Explorer. Execute only
        // when ContextMenuStrip did not observe that click itself.
        pressedItem.PerformClick();
        if (_menu.Visible)
            _menu.Close(Forms.ToolStripDropDownCloseReason.ItemClicked);
    }

    private void KeepAboveOverflow() =>
        SetWindowPos(
            _menu.Handle,
            HwndTopmost,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoActivate);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _outsideClickTimer.Stop();
        _outsideClickTimer.Tick -= OnOutsideClickTimer;
        _menu.Opened -= OnOpened;
        _menu.Closed -= OnClosed;
        _menu.ItemClicked -= OnItemClicked;
        _menuWindow.Dispose();
        _outsideClickTimer.Dispose();
    }

    /// <summary>
    /// Keeps Explorer's overflow flyout active while allowing the first click
    /// on this non-activating menu to reach its ToolStripItem.
    /// </summary>
    private sealed class NonActivatingClickWindow : Forms.NativeWindow, IDisposable
    {
        public void Attach(IntPtr handle)
        {
            if (Handle == handle) return;
            Detach();
            AssignHandle(handle);
        }

        public void Detach()
        {
            if (Handle != IntPtr.Zero) ReleaseHandle();
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmMouseActivate)
            {
                message.Result = new IntPtr(MaNoActivate);
                return;
            }

            base.WndProc(ref message);
        }

        public void Dispose() => Detach();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
