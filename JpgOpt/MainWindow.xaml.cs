using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;

namespace JpgOpt {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            DataContext = vm;
        }
        public static RoutedCommand Start = new RoutedCommand();
        ViewModel vm = new ViewModel();
        const string mozjpeg_arg = @"/c jpegtran -copy all";
        int total = 0, current = 0;

        private void Window_DragEnter(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        private async void Window_Drop(object sender, DragEventArgs e) {
            vm.Idle = false;
            var dropfiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            var jpgs = dropfiles.Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (jpgs.Length > 0) {
                await Encode(jpgs);
            }
            else {
                var folders = dropfiles.Where(f => File.GetAttributes(f).HasFlag(FileAttributes.Directory)).ToArray();
                List<string> jpglist = new List<string>();
                foreach (var dir in folders) {
                    jpglist.AddRange(Directory.GetFiles(dir, "*.jpg", SearchOption.AllDirectories));
                }
                if (jpglist.Count > 0) await Encode(jpglist.ToArray());

            }
            vm.Idle = true;
        }
        private async void Start_Executed(object sender, ExecutedRoutedEventArgs e) {
            string path;
            using (var dlg = new CommonOpenFileDialog() { IsFolderPicker = true }) {
                if (dlg.ShowDialog() != CommonFileDialogResult.Ok) return;
                path = dlg.FileName;
            }
            vm.Idle = false;
            var files = Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);
            await Encode(files);
        }

        Task Encode(string[] files) => Task.Run((Action)(() => {
            total = files.Length;
            long TotalDelta = 0;
            vm.Ptext = $"0 / {total}";
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, f => {
                try {
                    var tempf = Path.Combine(Directory.GetParent(f).FullName, $"{Guid.NewGuid()}.jpg");
                    Process.Start(Psi($"{mozjpeg_arg} {f.WQ()} > {tempf.WQ()}")).WaitForExit();
                    FileInfo fiI = new FileInfo(f), fiT = new FileInfo(tempf);
                    if (fiT.Length > 0) {
                        var delta = fiI.Length - fiT.Length;
                        if (delta != 0) Interlocked.Add(ref TotalDelta, fiI.Length - fiT.Length);
                        vm.DeltaText = $"{FileSizeHelpler.SizeSuffix(TotalDelta)} decreased";
                        fiI.IsReadOnly = false;
                        fiI.Delete();
                        fiT.MoveTo(f);
                        Interlocked.Increment(ref current);
                    }
                    else {
                        MessageBox.Show($"error on: {f}");
                        fiT.Delete();
                    }
                    vm.Pvalue = (double)current / total;
                    vm.Ptext = $"{current} / {total}";
                }
                catch (Exception ex) {
                    MessageBox.Show($"{ex.Message}{Environment.NewLine}on: {f}");
                }
            });
            SystemSounds.Asterisk.Play();
            MessageBox.Show("complete");
            vm.Pvalue = total = current = 0;
            vm.Ptext = null;
            vm.Idle = true;
        }));

        ProcessStartInfo Psi(string arg) => new ProcessStartInfo() { FileName = "cmd.exe", Arguments = arg, UseShellExecute = false, CreateNoWindow = true };
    }
}