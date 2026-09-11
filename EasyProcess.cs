using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ytwrapper
{
	public record EasyProcessResult(int ExitCode);
	public record EasyProcessResult<T>(int ExitCode, T? Value);
	
	public class EasyProcess
	{
		private readonly ProcessStartInfo _processStartInfo;
		
		private readonly List<string> _stdout = new List<string>(capacity: 50);
		private readonly List<string> _stderr = new List<string>(capacity: 50);

		private bool _alreadyRun = false;
		private readonly bool _repeatOnConsole = false;
		
		public EasyProcess(ProcessStartInfo processStartInfo)
			: this(processStartInfo, false)
		{ }

		public EasyProcess(ProcessStartInfo processStartInfo, bool repeatOnConsole)
		{
			ArgumentNullException.ThrowIfNull(processStartInfo);

			_processStartInfo = processStartInfo;
			_repeatOnConsole = repeatOnConsole;
		}

		public EasyProcessResult Run()
			=> RunInternal(null);
		
		public EasyProcessResult Run(Action<List<string>, List<string>> action)
			=> RunInternal(action);

		private EasyProcessResult RunInternal(Action<List<string>, List<string>>? action)
		{
			if (_alreadyRun)
			{
				throw new Exception("already called run");
			}

			_alreadyRun = true;
			
			int exitCode = RunProcess();

			if (action is not null)
			{
				action(_stdout, _stderr);
			}

			return new EasyProcessResult(exitCode);
		}
		
		public EasyProcessResult<T> Run<T>(Func<List<string>, List<string>, T?> func)
		{
			if (_alreadyRun)
			{
				throw new Exception("already called run");
			}

			_alreadyRun = true;
			
			int exitCode = RunProcess();

			return new EasyProcessResult<T>(exitCode, func(_stdout, _stderr));
		}

		private int RunProcess()
		{
			using Process process = new Process()
			{
				StartInfo = _processStartInfo
			};
			
			process.ErrorDataReceived += OnErrorDataReceived;
			process.OutputDataReceived += OnOutputDataReceived;

			process.Start();

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			process.WaitForExit();

			process.ErrorDataReceived -= OnErrorDataReceived;
			process.OutputDataReceived -= OnOutputDataReceived;

			return process.ExitCode;
		}

		private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data is string data)
			{
				_stderr.Add(data);

				if (_repeatOnConsole)
				{
					Console.Error.WriteLine(data);
				}
			}
		}

		private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data is string data)
			{
				_stdout.Add(data);

				if (_repeatOnConsole)
				{
					Console.Out.WriteLine(data);
				}
			}
		}
	}
}