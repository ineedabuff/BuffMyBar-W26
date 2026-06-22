using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace BuffBar.Widgets.Common;

/// <summary>
/// Ouvre un <see cref="Popup"/> au survol d'un module et le referme lorsque le
/// pointeur quitte à la fois le module ET le popup.
///
/// Un court délai de fermeture (≈280 ms) couvre le petit espace vide entre le
/// module et le popup : sans lui, le flyout se refermerait pendant la transition
/// avant que le pointeur n'atteigne le popup.
/// </summary>
public static class HoverPopup
{
    public static void Attach(
        FrameworkElement trigger,
        Popup popup,
        FrameworkElement content,
        Action? onOpening = null,
        Func<bool>? canOpen = null)
    {
        var closeTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(280)
        };
        closeTimer.Tick += (_, _) => { closeTimer.Stop(); popup.IsOpen = false; };

        void Open()
        {
            if (canOpen != null && !canOpen()) return;
            closeTimer.Stop();
            if (!popup.IsOpen)
            {
                onOpening?.Invoke();
                popup.IsOpen = true;
            }
        }

        void ScheduleClose()
        {
            closeTimer.Stop();
            closeTimer.Start();
        }

        trigger.MouseEnter += (_, _) => Open();
        trigger.MouseLeave += (_, _) => ScheduleClose();
        content.MouseEnter += (_, _) => closeTimer.Stop();
        content.MouseLeave += (_, _) => ScheduleClose();
    }
}
