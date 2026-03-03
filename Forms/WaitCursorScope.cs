namespace CheckTranslation;

internal readonly struct WaitCursorScope : IDisposable
{
    private readonly Control _control;
    private readonly bool _previousControlUseWaitCursor;
    private readonly bool _previousApplicationUseWaitCursor;
    private readonly Cursor? _previousCursor;

    public WaitCursorScope(Control control)
    {
        _control = control;
        _previousControlUseWaitCursor = control.UseWaitCursor;
        _previousApplicationUseWaitCursor = Application.UseWaitCursor;
        _previousCursor = Cursor.Current;

        control.UseWaitCursor = true;
        Application.UseWaitCursor = true;
        Cursor.Current = Cursors.WaitCursor;
    }

    public void Dispose()
    {
        _control.UseWaitCursor = _previousControlUseWaitCursor;
        Application.UseWaitCursor = _previousApplicationUseWaitCursor;
        Cursor.Current = _previousCursor ?? Cursors.Default;
    }
}
