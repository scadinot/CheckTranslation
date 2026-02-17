namespace CheckTranslation;

internal sealed partial class ConfigForm : Form
{
    public ConfigForm()
    {
        InitializeComponent();
        btnOk.Click += (_, _) => SaveConfig();
        LoadConfig();
    }

    private void LoadConfig()
    {
        var config = AppConfig.Current;
        txtTranslatePrompt.Text = config.TranslatePrompt;
        txtVerifyPrompt.Text = config.VerifyPrompt;
        txtKey.Text = config.Key;
        txtUrl.Text = config.Url;
        txtModelName.Text = config.ModelName;
    }

    private void SaveConfig()
    {
        var config = new AppConfig
        {
            TranslatePrompt = txtTranslatePrompt.Text.Trim(),
            VerifyPrompt = txtVerifyPrompt.Text.Trim(),
            Key = txtKey.Text.Trim(),
            Url = txtUrl.Text.Trim(),
            ModelName = txtModelName.Text.Trim(),
            ShowDetails = AppConfig.Current.ShowDetails,
        };
        config.Save();
    }
}
