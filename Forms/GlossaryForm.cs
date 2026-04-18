using System.ComponentModel;

namespace CheckTranslation;

internal sealed partial class GlossaryForm : Form
{
    private readonly IGlossaryService _glossaryService;

    private readonly Dictionary<string, SortableBindingList<GlossaryEntry>> _bindingsByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private string _currentLanguageCode = string.Empty;
    private bool _dirty;

    public GlossaryForm() : this(new GlossaryService())
    {
    }

    public GlossaryForm(IGlossaryService glossaryService)
    {
        _glossaryService = glossaryService;
        InitializeComponent();

        languageCombo.SelectedIndexChanged += LanguageCombo_SelectedIndexChanged;
        grid.CellValueChanged += (_, _) => MarkDirty();
        grid.UserAddedRow += (_, _) => MarkDirty();
        grid.UserDeletedRow += (_, _) => MarkDirty();
        btnAdd.Click += (_, _) => AddEntry();
        btnRemove.Click += (_, _) => RemoveSelectedEntries();
        btnOk.Click += BtnOk_Click;
        FormClosing += GlossaryForm_FormClosing;

        InitLanguages();
    }

    /// <summary>
    /// Ouvre l'éditeur en pré-sélectionnant une langue particulière.
    /// </summary>
    public void SelectLanguage(string languageCode)
    {
        for (int i = 0; i < languageCombo.Items.Count; i++)
        {
            if (languageCombo.Items[i] is LanguageInfo lang
                && string.Equals(lang.Code, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                languageCombo.SelectedIndex = i;
                return;
            }
        }
    }

    private void InitLanguages()
    {
        foreach (var lang in MainForm.Languages)
            languageCombo.Items.Add(lang);
        languageCombo.DisplayMember = nameof(LanguageInfo.Name);
        if (languageCombo.Items.Count > 0)
            languageCombo.SelectedIndex = 0;
    }

    private void LanguageCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (languageCombo.SelectedItem is not LanguageInfo lang)
            return;

        _currentLanguageCode = lang.Code;

        if (!_bindingsByLanguage.TryGetValue(lang.Code, out var binding))
        {
            var entries = _glossaryService.GetEntries(lang.Code).Select(Clone).ToList();
            binding = new SortableBindingList<GlossaryEntry>(entries);
            binding.ListChanged += (_, _) => UpdateCountLabel();
            _bindingsByLanguage[lang.Code] = binding;
        }

        grid.DataSource = binding;
        UpdateCountLabel();
    }

    private void AddEntry()
    {
        if (grid.DataSource is not SortableBindingList<GlossaryEntry> binding)
            return;
        binding.Add(new GlossaryEntry());
        MarkDirty();
    }

    private void RemoveSelectedEntries()
    {
        if (grid.DataSource is not SortableBindingList<GlossaryEntry> binding)
            return;

        var toRemove = grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as GlossaryEntry)
            .Where(e => e is not null)
            .Cast<GlossaryEntry>()
            .ToList();

        foreach (var entry in toRemove)
            binding.Remove(entry);

        if (toRemove.Count > 0)
            MarkDirty();
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        try
        {
            CommitCurrentEdit();
            foreach (var (langCode, binding) in _bindingsByLanguage)
            {
                var cleaned = binding
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Source) && !string.IsNullOrWhiteSpace(entry.Destination))
                    .ToList();
                _glossaryService.ReplaceEntries(langCode, cleaned);
            }
            _glossaryService.Save();
            _dirty = false;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Impossible d'enregistrer le glossaire :\n\n{ex.Message}",
                "Glossaire",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void CommitCurrentEdit()
    {
        grid.EndEdit();
        if (grid.DataSource is SortableBindingList<GlossaryEntry> binding)
        {
            if (grid.BindingContext?[binding] is CurrencyManager cm)
                cm.EndCurrentEdit();
        }
    }

    private void GlossaryForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK || !_dirty)
            return;

        var result = MessageBox.Show(this,
            "Des modifications du glossaire n'ont pas été enregistrées. Les perdre ?",
            "Glossaire",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            e.Cancel = true;
    }

    private void MarkDirty() => _dirty = true;

    private void UpdateCountLabel()
    {
        if (grid.DataSource is SortableBindingList<GlossaryEntry> binding)
            lblCount.Text = $"{binding.Count} entrée(s)";
        else
            lblCount.Text = string.Empty;
    }

    private static GlossaryEntry Clone(GlossaryEntry source) => new()
    {
        Source = source.Source,
        Destination = source.Destination,
        Context = source.Context,
    };
}
