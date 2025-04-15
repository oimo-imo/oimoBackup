using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32; // ← フォルダ選択ダイアログを使うために必要
using Microsoft.WindowsAPICodePack.Dialogs;

namespace oimoBackup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // --- フォルダ選択ボタンのイベントハンドラ (変更なし) ---
        private void SelectSourceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "バックアップ元のフォルダを選択してください";
            if (dialog.ShowDialog(this) == CommonFileDialogResult.Ok)
            {
                SourcePathTextBox.Text = dialog.FileName;
            }
        }

        private void SelectDestButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "バックアップ先のフォルダを選択してください";
            if (dialog.ShowDialog(this) == CommonFileDialogResult.Ok)
            {
                DestPathTextBox.Text = dialog.FileName;
            }
        }

        // --- バックアップ実行ボタンのクリックイベントハンドラ ---
        private async void ExecuteBackupButton_Click(object sender, RoutedEventArgs e)
        {
            string sourceDirectory = SourcePathTextBox.Text;
            string destinationDirectory = DestPathTextBox.Text;

            // --- 入力チェック ---
            if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Log("エラー: バックアップ元とバックアップ先の両方のフォルダを選択してください。");
                MessageBox.Show("バックアップ元とバックアップ先の両方のフォルダを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // 処理中断
            }

            if (!Directory.Exists(sourceDirectory))
            {
                Log($"エラー: バックアップ元フォルダが見つかりません: {sourceDirectory}");
                MessageBox.Show($"バックアップ元フォルダが見つかりません:\n{sourceDirectory}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // 処理中断
            }

            // バックアップ先がバックアップ元のサブフォルダになっていないかチェック (無限ループ防止)
            if (IsSameOrSubDirectory(sourceDirectory, destinationDirectory))
            {
                Log("エラー: バックアップ先をバックアップ元フォルダまたはそのサブフォルダにすることはできません。");
                MessageBox.Show("バックアップ先をバックアップ元フォルダまたはそのサブフォルダにすることはできません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // 処理中断
            }

            // --- バックアップ処理の開始 ---
            Log("--------------------------------------------------");
            Log($"バックアップを開始します: {sourceDirectory} -> {destinationDirectory}");
            ExecuteBackupButton.IsEnabled = false; // 処理中はボタンを無効化

            // IProgress<T> を使って、別スレッドからUI(ログ)へ安全に進捗を報告
            var progress = new Progress<string>(message => Log(message));

            try
            {
                // ★実際のコピー処理は重い可能性があるので、Task.Runで別スレッドで実行
                await Task.Run(() => CopyDirectoryRecursive(sourceDirectory, destinationDirectory, progress));

                // 処理完了
                Log("バックアップが正常に完了しました。");
                MessageBox.Show("バックアップが完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // 予期せぬエラー処理
                Log($"!!! 重大なエラーが発生しました: {ex.Message}");
                Log($"詳細: {ex.StackTrace}"); // デバッグ用にスタックトレースも記録
                MessageBox.Show($"バックアップ中に予期せぬエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 成功・失敗に関わらずボタンを再度有効化
                ExecuteBackupButton.IsEnabled = true;
                Log("--------------------------------------------------");
            }
        }

        // --- 再帰的にフォルダをコピーするメソッド ---
        // (IProgress<string> progress で進捗状況を呼び出し元に報告)
        private void CopyDirectoryRecursive(string sourcePath, string destPath, IProgress<string> progress)
        {
            DirectoryInfo destDirInfo = Directory.CreateDirectory(destPath);
            // フォルダ作成ログは変更なし
            // progress.Report($"フォルダを作成/確認しました: {destPath}"); // 必要ならコメントアウト解除

            DirectoryInfo sourceDirInfo = new DirectoryInfo(sourcePath);

            // --- ファイルのコピー (変更点あり) ---
            FileInfo[] files = sourceDirInfo.GetFiles();
            foreach (FileInfo sourceFile in files) // 変数名を sourceFile に変更して分かりやすく
            {
                string targetFilePath = System.IO.Path.Combine(destPath, sourceFile.Name);

                try
                {
                    // --- ★★★ 変更点：ここから ★★★ ---
                    bool performCopy = true; // コピーを実行すべきかどうかのフラグ

                    // コピー先に同名のファイルが存在するかチェック
                    if (File.Exists(targetFilePath))
                    {
                        FileInfo destFile = new FileInfo(targetFilePath);

                        // 最終更新日時(UTC)を比較
                        // コピー元の最終更新日時が、コピー先の最終更新日時より新しくない (同じか古い) 場合はコピーしない
                        if (sourceFile.LastWriteTimeUtc <= destFile.LastWriteTimeUtc)
                        {
                            performCopy = false;

                        }
                        // else の場合は performCopy は true のまま (コピー元が新しいので上書きする)
                    }
                    // else の場合は performCopy は true のまま (コピー先にファイルが存在しないので新規コピーする)

                    // --- ★★★ 変更点：ここまで ★★★ ---

                    // コピーを実行すべき場合のみ File.Copy を実行
                    if (performCopy)
                    {
                        // File.Copy の第3引数 true は上書きを許可する設定
                        sourceFile.CopyTo(targetFilePath, true);
                        // コピー/上書きしたことをログに出力
                        progress.Report($"ファイルをコピー/上書きしました: {sourceFile.Name}");
                    }
                }
                catch (IOException ioEx)
                {
                    progress.Report($"エラー (ファイルコピー IO): {sourceFile.Name} - {ioEx.Message}");
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    progress.Report($"エラー (ファイルコピー アクセス権): {sourceFile.Name} - {uaEx.Message}");
                }
                catch (Exception ex)
                {
                    progress.Report($"エラー (ファイルコピー 不明): {sourceFile.Name} - {ex.Message}");
                }
            }

            // --- サブディレクトリのコピー (変更なし) ---
            DirectoryInfo[] subDirs = sourceDirInfo.GetDirectories();
            foreach (DirectoryInfo subDir in subDirs)
            {
                string newDestPath = System.IO.Path.Combine(destPath, subDir.Name);
                try
                {
                    // サブフォルダは常に再帰的に処理する (フォルダ自体のタイムスタンプ比較は通常しない)
                    progress.Report($"-> サブフォルダ処理中: {subDir.Name}"); // フォルダ処理開始ログ
                    CopyDirectoryRecursive(subDir.FullName, newDestPath, progress);
                }
                catch (IOException ioEx)
                {
                    progress.Report($"エラー (サブフォルダ処理 IO): {subDir.Name} - {ioEx.Message}");
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    progress.Report($"エラー (サブフォルダ処理 アクセス権): {subDir.Name} - {uaEx.Message}");
                }
                catch (Exception ex)
                {
                    progress.Report($"エラー (サブフォルダ処理 不明): {subDir.Name} - {ex.Message}");
                }
            }
        }

        // --- LogTextBox にログを出力するメソッド ---
        private void Log(string message)
        {
            // 現在時刻を追加して見やすくする (任意)
            string timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            // AppendTextで追記し、改行を追加
            LogTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            // 自動で一番下までスクロール
            LogTextBox.ScrollToEnd();
        }

        // --- バックアップ先がバックアップ元のサブフォルダかチェックするヘルパーメソッド ---
        private bool IsSameOrSubDirectory(string candidateSource, string candidateDest)
        {
            try
            {
                // DirectoryInfoを使ってパスを正規化する
                var sourceInfo = new DirectoryInfo(candidateSource);
                var destInfo = new DirectoryInfo(candidateDest);

                // パス文字列の末尾に '\' を付けて比較しやすくする
                string sourceFullPath = sourceInfo.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
                string destFullPath = destInfo.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;

                // 同じパスか、コピー先がコピー元のサブディレクトリの場合 true (大文字小文字を区別しない)
                return string.Equals(sourceFullPath, destFullPath, StringComparison.OrdinalIgnoreCase) ||
                       destFullPath.StartsWith(sourceFullPath, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) // パスが不正な場合など
            {
                Log($"パスの検証中にエラー: {ex.Message}");
                return true; // 安全のため、検証失敗時はコピー不可とする
            }
        }
    }

}
