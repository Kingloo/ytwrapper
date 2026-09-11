using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ytwrapper
{
	public static class Program
	{
		private static readonly Encoding _encoding = new UTF8Encoding(
			encoderShouldEmitUTF8Identifier: false,
			throwOnInvalidBytes: true
		);

		public static int Main(string[] args)
		{
			Uri? uri = GetUri(args);

			if (uri is null)
			{
				Console.Error.WriteLine($"not a URI");
				
				return -1;
			}

			Console.Out.WriteLine($"[yt-dlp] downloading '{uri.AbsoluteUri}'");

			EasyProcessResult<FileInfo?> ytdlpResult = RunYtdlp(uri);

			if (ytdlpResult.ExitCode == 0
				&& ytdlpResult.Value is FileInfo originalFile)
			{
				Console.Out.WriteLine($"[yt-dlp] finished - downloaded to '{ytdlpResult.Value.FullName}'");
			}
			else
			{
				Console.Error.WriteLine($"[yt-dlp] exited with code {ytdlpResult.ExitCode}, couldn't get filename from yt-dlp output");

				return -1;
			}

			FileInfo convertedFile = CreateConvertedFileName(originalFile);

			EasyProcessResult ffmpegResult = RunFfmpeg(originalFile, convertedFile);

			if (ffmpegResult.ExitCode == 0)
			{
				Console.Out.WriteLine($"[ffmpeg] converted from '{originalFile.FullName}' to '{convertedFile.FullName}'");
			}
			else
			{
				Console.Error.WriteLine($"[ffmpeg] exited with code {ffmpegResult.ExitCode}");
			}

			return 0;
		}

		private static Uri? GetUri(string[] args)
		{
			if (args.Length == 0)
			{
				return null;
			}
			
			return Uri.TryCreate(args[0], UriKind.Absolute, out Uri? uri) ? uri : null;
		}

		private static EasyProcessResult<FileInfo?> RunYtdlp(Uri uri)
		{
			EasyProcess ytdlp = new EasyProcess(CreateYtDlpStartInfo(uri));

			return ytdlp.Run<FileInfo?>(ExtractFileName);
		}

		private static ProcessStartInfo CreateYtDlpStartInfo(Uri url)
		{
			return new ProcessStartInfo()
			{
				Arguments = $"\"{url.AbsoluteUri}\"",
				CreateNoWindow = true,
				FileName = "yt-dlp.exe",
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				StandardErrorEncoding = _encoding,
				StandardOutputEncoding = _encoding,
				UseShellExecute = false
			};
		}

		private static FileInfo? ExtractFileName(List<string> stdout, List<string> stderr)
		{
			foreach (string line in stdout)
			{
				if (line.StartsWith("[download] Destination: ", StringComparison.Ordinal))
				{
					return new FileInfo(line[24..]);
				}
			}

			return null;
		}

		private static FileInfo CreateConvertedFileName(FileInfo originalFile)
		{
			string originalNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFile.FullName);
			string extension = Path.GetExtension(originalFile.FullName);
			
			string convertedName = originalNameWithoutExtension + "--conved" + extension;
			string convertedFullName = Path.Combine(originalFile.DirectoryName!, convertedName);

			return new FileInfo(convertedFullName);
		}

		private static EasyProcessResult RunFfmpeg(FileInfo originalFile, FileInfo convertedFile)
		{
			EasyProcess ffmpeg = new EasyProcess(CreateFFmpegStartInfo(originalFile, convertedFile));

			return ffmpeg.Run();
		}

		private static ProcessStartInfo CreateFFmpegStartInfo(FileInfo originalFile, FileInfo convertedFile)
		{
			return new ProcessStartInfo()
			{
				Arguments = $"-i \"{originalFile.FullName}\" -c copy \"{convertedFile.FullName}\"",
				CreateNoWindow = true,
				FileName = "ffmpeg.exe",
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				StandardErrorEncoding = _encoding,
				StandardOutputEncoding = _encoding,
				UseShellExecute = false
			};
		}
	}
}