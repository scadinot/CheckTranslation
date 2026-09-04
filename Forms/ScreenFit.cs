namespace CheckTranslation;

/// <summary>
/// Borne un formulaire à la zone de travail de son écran — la <c>MinimumSize</c> d'abord, sans
/// quoi elle interdirait le rétrécissement et le réglage n'aurait aucun effet. Cas vécu sur
/// <c>ConfigForm</c> : le dialogue dépasse un écran 1080p dès 125 % de mise à l'échelle et les
/// boutons OK / Annuler sortent du champ, donc hors de portée.
///
/// À appeler au chargement (après <c>base.OnLoad</c> : la mise à l'échelle DPI est faite, les
/// tailles lues sont celles réellement affichées). L'écran de référence est celui du
/// <c>Owner</c>, pas celui du dialogue : un modal (<c>ShowDialog(this)</c>) n'a pas encore été
/// recentré sur son parent à ce moment-là — se fier à sa propre position dimensionnerait et
/// recentrerait sur l'écran principal une fenêtre qui doit s'afficher sur celui du parent.
/// </summary>
internal static class ScreenFit
{
    public static void Apply(Form form)
    {
        var reference = form.Owner ?? (Control)form;
        var working = Screen.FromControl(reference).WorkingArea;

        form.MinimumSize = new Size(
            Math.Min(form.MinimumSize.Width, working.Width),
            Math.Min(form.MinimumSize.Height, working.Height));

        form.Size = new Size(
            Math.Min(form.Width, working.Width),
            Math.Min(form.Height, working.Height));

        form.Location = new Point(
            working.Left + Math.Max(0, (working.Width - form.Width) / 2),
            working.Top + Math.Max(0, (working.Height - form.Height) / 2));
    }
}
