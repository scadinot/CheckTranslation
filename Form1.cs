namespace CheckTransation
{
    public partial class Form1 : Form
    {
        private const string InputFile = "Input.xlsx";

        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
        }

        private async void Form1_Load(object? sender, EventArgs e)
        {
            statusLabel.Text = "Chargement du fichier Excel...";
            dataGridView.AutoGenerateColumns = false;

            try
            {
                var rows = await Task.Run(() => ExcelReader.Load(GetInputPath()));

                dataGridView.DataSource = rows;
                statusLabel.Text = $"{rows.Count} traductions chargees (lignes @Invariant ignorees)";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Erreur de chargement";
                MessageBox.Show(
                    $"Impossible de charger le fichier Excel :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string GetInputPath()
        {
            // Cherche Input.xlsx a cote de l'executable, sinon dans le repertoire courant
            var exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
            var path = Path.Combine(exeDir, InputFile);
            if (File.Exists(path))
                return path;

            if (File.Exists(InputFile))
                return Path.GetFullPath(InputFile);

            throw new FileNotFoundException(
                $"Le fichier '{InputFile}' est introuvable dans '{exeDir}' ni dans '{Environment.CurrentDirectory}'.");
        }
    }
}
