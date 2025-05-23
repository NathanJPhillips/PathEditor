namespace NobleTech.Products.PathEditor.Utils;

internal static class TimerExtensions
{
    public static void Start(this Timer timer, TimeSpan interval, bool repeat = false) =>
        timer.Change(interval, repeat ? interval : Timeout.InfiniteTimeSpan);

    public static void Stop(this Timer timer) =>
        timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
}
