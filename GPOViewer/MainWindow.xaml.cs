using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GPOViewer
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<GpoInfo> Gpos { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                Gpos = LoadGpos();
                GpoDataGrid.ItemsSource = Gpos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading GPO backups: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ObservableCollection<GpoInfo> LoadGpos()
        {
            var gpoList = new ObservableCollection<GpoInfo>();
            var backupDir = @"C:\GPOBackups";

            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
                MessageBox.Show($"Backup directory not found. Created: {backupDir}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            foreach (var file in Directory.GetFiles(backupDir, "*.meta"))
            {
                try
                {
                    var metaContent = File.ReadAllLines(file);

                    var lastModifiedRaw = metaContent[1].Split(new[] { ':' }, 2)[1].Trim();
                    var lastModifiedFormatted = DateTime.Parse(lastModifiedRaw).ToString("dd.MM.yyyy HH:mm:ss");

                    gpoList.Add(new GpoInfo
                    {
                        Name = metaContent[0].Split(new[] { ':' }, 2)[1].Trim(),
                        LastModified = lastModifiedFormatted,
                        Summary = metaContent[2].Split(new[] { ':' }, 2)[1].Trim(),
                        BackupPath = file
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading file {file}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            return gpoList;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (GpoDataGrid.SelectedItem is GpoInfo selectedGpo)
            {
                var restorePath = Path.Combine(@"C:\GPORestores", Path.GetFileName(selectedGpo.BackupPath));

                try
                {
                    Directory.CreateDirectory(@"C:\GPORestores");
                    File.Copy(selectedGpo.BackupPath, restorePath, overwrite: true);

                    MessageBox.Show($"GPO restored to: {restorePath}", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error restoring GPO: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a GPO to restore.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            if (GpoDataGrid.SelectedItems.Count < 2)
            {
                MessageBox.Show("Please select two GPOs to compare.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedGpos = GpoDataGrid.SelectedItems;
            var leftGpo = (GpoInfo)selectedGpos[0];
            var rightGpo = (GpoInfo)selectedGpos[1];

            try
            {
                LeftConfig.Text = File.ReadAllText(leftGpo.BackupPath);
                RightConfig.Text = File.ReadAllText(rightGpo.BackupPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class GpoInfo
    {
        public string Name { get; set; }
        public string LastModified { get; set; }
        public string Summary { get; set; }
        public string BackupPath { get; set; }
    }
}
