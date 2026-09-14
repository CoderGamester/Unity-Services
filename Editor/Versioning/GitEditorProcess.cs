using System;
using System.Diagnostics;

namespace GameLovers.Services.Versioning.Editor
{
	/// <summary>
	/// Run git commands processes that would otherwise be used in the terminal.
	/// </summary>
	/// <author>
	/// https://blog.somewhatabstract.com/2015/06/22/getting-information-about-your-git-repository-with-c/
	/// </author>
	public class GitEditorProcess : IDisposable
	{
		private const string DefaultPathToGitBinary = "git";

		private readonly Process _process;

		/// <summary>
		/// <inheritdoc cref="Process.ExitCode"/>
		/// </summary>
		public int ExitCode => _process.ExitCode;

		public GitEditorProcess(string workingDir, string pathToGitBinary = DefaultPathToGitBinary)
		{
			var startInfo = new ProcessStartInfo
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				FileName = pathToGitBinary,
				CreateNoWindow = true,
				WorkingDirectory = workingDir
			};

			_process = new Process { StartInfo = startInfo };
		}

		/// <summary>
		/// Is this unity project a git repo?
		/// </summary>
		public bool IsValidRepo()
		{
			return ExecuteCommand("rev-parse --is-inside-work-tree") == "true";
		}

		/// <summary>
		/// Get the git branch name of the unity project.
		/// </summary>
		public string GetBranch()
		{
			return ExecuteCommand("rev-parse --abbrev-ref HEAD");
		}

		/// <summary>
		/// Get the git commit hash of the unity project.
		/// </summary>
		public string GetCommitHash()
		{
			return ExecuteCommand($"rev-parse HEAD");
		}

		/// <summary>
		/// Get the diff of the working directory in its current state from the state it was at at
		/// a given commit.
		/// </summary>
		public string GetDiffFromCommit(string commitHash)
		{
			return ExecuteCommand($"diff --word-diff=porcelain {commitHash} -- {_process.StartInfo.WorkingDirectory}");
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			_process?.Dispose();
		}
		
		private string ExecuteCommand(string args)
		{
			_process.StartInfo.Arguments = args;
			_process.Start();
			var output = _process.StandardOutput.ReadToEnd().Trim();
			_process.WaitForExit();
			return output;
		}
	}
}
