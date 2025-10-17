using System;
using System.Diagnostics;
using System.IO;
using System.Text;

/// <summary>
/// A C# console application to automate Git operations across the main repository 
/// and all nested submodules.
/// Usage: app.exe [Optional: "Your commit message here"]
/// </summary>
public class GitAutomator
{
    private const string GitCommand = "git";

    public static void Main(string[] args)
    {
        string commitMessage = string.Empty;

        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            // Commit message provided via command-line argument
            commitMessage = args[0].Trim();
        }
        else
        {
            // Commit message not provided, prompt the user
            Console.Write("Enter your Git commit message (required): ");
            string input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("\nERROR: A commit message is required to proceed. Exiting.");
                return;
            }
            commitMessage = input.Trim();
        }

        Console.WriteLine($"Starting Git automation with commit message: '{commitMessage}'");
        Console.WriteLine("------------------------------------------");

        try
        {
            // 1. Stage changes in all submodules recursively
            Console.WriteLine("\n--- Staging changes in all submodules (git submodule foreach --recursive 'git add .') ---");
            // Note: We only stage (add) in the submodules. We commit the submodule changes 
            // and the main repo changes together in the final step.
            RunGitCommand($"submodule foreach --recursive \"git add .\"", "Failed to stage changes in submodules.");


            // 2. Stage changes in the main repository
            Console.WriteLine("\n--- Staging changes in the main repository (git add .) ---");
            RunGitCommand("add .", "Failed to stage changes in main repository.");

            
            // 3. Commit staged changes with the provided message
            Console.WriteLine($"\n--- Committing all staged changes (git commit -m \"{commitMessage}\") ---");
            
            // Use git commit -m instead of -am, since we explicitly ran 'git add .' already.
            // Using -m allows committing the staged submodule reference updates as well.
            // We use Replace to escape any quotes within the message for the command line argument.
            RunGitCommand($"commit -m \"{commitMessage.Replace("\"", "\"\"")}\"", "Commit failed. Check if there are any staged changes.");

            Console.WriteLine("\nSuccessfully completed staging and commit operation.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL ERROR] An unexpected error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a Git command and streams its output to the console.
    /// Throws an exception if the command fails (non-zero exit code).
    /// </summary>
    /// <param name="arguments">The arguments to pass to the git executable (e.g., "add .", "commit -m 'msg'").</param>
    /// <param name="errorMessage">The error message to display if the command fails.</param>
    private static void RunGitCommand(string arguments, string errorMessage)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = GitCommand,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory() // Ensure we start in the correct directory
        };

        using (Process process = Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new Exception($"Could not start '{GitCommand}'. Is it installed and in your PATH?");
            }

            // Stream and log output in real-time
            process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[GIT] {e.Data}"); };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // Handle a common non-failure case where nothing is committed (exit code 1)
                if (process.ExitCode == 1 && arguments.StartsWith("commit"))
                {
                    Console.WriteLine("[INFO] Commit command completed, but may have exited with code 1 because 'nothing to commit'. This is often acceptable.");
                }
                else
                {
                    throw new Exception($"{errorMessage} (Exit Code: {process.ExitCode})");
                }
            }
        }
    }
}

