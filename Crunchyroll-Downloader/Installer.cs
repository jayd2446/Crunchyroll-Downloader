﻿using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using CrunchyrollDownloader.Progress;
using CrunchyrollDownloader.ViewModels;
using CrunchyrollDownloader.Views;
using ICSharpCode.SharpZipLib.Zip;

namespace CrunchyrollDownloader
{
	public class Installer
	{
		public const string InstallFolder = @"C:\ProgramData\Crunchy-DL";
		public async Task InstallAll()
		{
			var data = new ProgressViewModel
			{
				Progress = new TaskManager(new[]
				{
					new ProgressTask("Downloading dependencies: Youtube-DL"),
					new ProgressTask("Downloading dependencies: FFmpeg-base"),
					new ProgressTask("Downloading dependencies: FFmpeg-play"),
					new ProgressTask("Downloading dependencies: FFmpeg-probe"),
					new ProgressTask("Extracting downloaded files")
				})
				{ FinalizingText = "Cleaning up..." }
			};
			var window = Application.Current.Dispatcher.Invoke(() => new ProgressWindow(data));
			_ = Task.Run(() => Application.Current.Dispatcher.Invoke(() => window.ShowDialog()));
			await SetupDependencies(data);
			window.Close();
		}

		private static async Task SetupDependencies(ProgressViewModel data)
		{
			var zip = new FastZip();
			using (var client = new WebClient())
			{
				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				client.DownloadProgressChanged += (sender, args) =>
				{
					var progress = args.BytesReceived / (double) args.TotalBytesToReceive;
					if (progress >= 0.99) return;
					data.Progress.CurrentTask.Progress = progress;
				};
				Directory.CreateDirectory(@"C:\ProgramData\Crunchy-DL");
				await client.DownloadFileTaskAsync(new Uri("https://yt-dl.org/downloads/latest/youtube-dl.exe"),
					@"C:\ProgramData\Crunchy-DL\youtube-dl.exe");
				data.Progress.GoNext();

				await client.DownloadFileTaskAsync(
					new Uri("https://raw.githubusercontent.com/skid9000/Crunchyroll-Downloader/develop/FFmpeg/ffmpeg.zip"),
					@"C:\ProgramData\Crunchy-DL\ffmpeg.zip");
				data.Progress.GoNext();

				await client.DownloadFileTaskAsync(
					new Uri("https://raw.githubusercontent.com/skid9000/Crunchyroll-Downloader/develop/FFmpeg/ffplay.zip"),
					@"C:\ProgramData\Crunchy-DL\ffplay.zip");
				data.Progress.GoNext();

				await client.DownloadFileTaskAsync(
					new Uri("https://raw.githubusercontent.com/skid9000/Crunchyroll-Downloader/develop/FFmpeg/ffprobe.zip"),
					@"C:\ProgramData\Crunchy-DL\ffprobe.zip");
			}
			data.Progress.GoNext();
		   
			await Task.Run(() => zip.ExtractZip(InstallFolder + @"\ffmpeg.zip", InstallFolder, ""));
			data.Progress.CurrentTask.Progress = 0.33;
			await Task.Run(() => zip.ExtractZip(InstallFolder + @"\ffplay.zip", InstallFolder, ""));
			data.Progress.CurrentTask.Progress = 0.66;
			await Task.Run(() => zip.ExtractZip(InstallFolder + @"\ffprobe.zip", InstallFolder, ""));
            data.Progress.CurrentTask.Progress = 0.99;
            data.Progress.GoNext();
			data.IsIndeterminate = true;
			await Task.Run(() => 
			{                
				File.Delete(@"C:\ProgramData\Crunchy-DL\ffmpeg.zip");
				File.Delete(@"C:\ProgramData\Crunchy-DL\ffplay.zip");
				File.Delete(@"C:\ProgramData\Crunchy-DL\ffprobe.zip");
			});
		}

		private void ShowErrorMessage() => MessageBox.Show("Dependencies seems corrupted or missing, click OK to re-download them.", "Important Note", MessageBoxButton.OK, MessageBoxImage.Information);
		public bool CheckIfInstalled()
		{
			if (Directory.Exists(InstallFolder) &&
				File.Exists(@"C:\ProgramData\Crunchy-DL\ffmpeg.exe") &&
				File.Exists(@"C:\ProgramData\Crunchy-DL\ffplay.exe") &&
				File.Exists(@"C:\ProgramData\Crunchy-DL\ffprobe.exe") &&
				File.Exists(@"C:\ProgramData\Crunchy-DL\youtube-dl.exe"))
			{
				return true;
			}
			ShowErrorMessage();
			if (Directory.Exists(InstallFolder))
				Directory.Delete(InstallFolder, true);
			return false;
		}

		public void DeleteInstallation()
		{
			Directory.Delete(InstallFolder, true);
		}
	}
}