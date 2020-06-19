using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Maestro.ScenarioTests
{
    public static class TestHelpers
    {
        public static async Task<string> RunExecutableAsync(string executable, params string[] args)
        {
            return await RunExecutableAsyncWithInput(executable, "& REM does a comment work?", args);
        }

        public static async Task<string> RunExecutableAsyncWithInput(string executable, string input, params string[] args)
        {
            string call = FormatExecutableCall(executable, args);
            TestContext.WriteLine(FormatExecutableCall(executable, args));
            //Debugging logging
            TestContext.WriteLine("Input to stdin: " + (input ?? "Input is null"));
            var output = new StringBuilder();

            void WriteOutput(string message)
            {
                if (message != null)
                {
                    Debug.WriteLine(message);
                    output.AppendLine(message);
                    TestContext.WriteLine(message);
                }
            }
            TestContext.WriteLine("About to set up process options.");
            var psi = new ProcessStartInfo(executable)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            TestContext.WriteLine("Adding arguments to list");
            foreach (string arg in args)
            {
                if (arg != null)
                {
                    psi.ArgumentList.Add(arg);
                }
                else
                {
                    TestContext.WriteLine("Found null argument");
                }
            }

            TestContext.WriteLine("Creating new process object");
            using var process = new Process
            {
                StartInfo = psi
            };

            TestContext.WriteLine("Set up task");
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => { tcs.TrySetResult(true); };

            TestContext.WriteLine("Starting process");
            process.Start();

            TestContext.WriteLine("Setting up input and output tasks");
            Task<bool> exitTask = tcs.Task;

            TestContext.WriteLine("Setting up output");
            Task<string> stdout = process.StandardOutput.ReadLineAsync();

            TestContext.WriteLine("Setting up error");
            Task<string> stderr = process.StandardError.ReadLineAsync();

            TestContext.WriteLine("Setting up input");
            Task stdin = Task.Run(() => { process.StandardInput.Write(input); process.StandardInput.Close(); });

            TestContext.WriteLine("Looping over tasks");
            var list = new List<Task> { exitTask, stdout, stderr, stdin };
            while (list.Count != 0)
            {
                var done = await Task.WhenAny(list);
                list.Remove(done);
                if (done == exitTask)
                {
                    continue;
                }

                if (done == stdout)
                {
                    var data = await stdout;
                    WriteOutput(data);
                    if (data != null)
                    {
                        list.Add(stdout = process.StandardOutput.ReadLineAsync());
                    }
                    continue;
                }

                if (done == stderr)
                {
                    var data = await stderr;
                    WriteOutput(data);
                    if (data != null)
                    {
                        list.Add(stderr = process.StandardError.ReadLineAsync());
                    }
                    continue;
                }

                if (done == stdin)
                {
                    TestContext.WriteLine("Hit stdin task");
                    await stdin;
                    continue;
                }

                throw new InvalidOperationException("Unexpected Task completed.");
            }

            if (process.ExitCode != 0)
            {
                throw new MaestroTestException($"{executable} exited with code {process.ExitCode}");
            }

            return output.ToString();
        }

        public static async Task<string> Which(string command)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string cmd = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd";
                return (await RunExecutableAsync(cmd, "/c", $"where {command}")).Trim()
                       // get the first line of where's output
                       .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            }

            return (await RunExecutableAsync("/bin/sh", "-c", $"which {command}")).Trim();
        }

        internal static string FormatExecutableCall(string executable, params string[] args)
        {
            var output = new StringBuilder();
            var secretArgNames = new[] { "-p", "--password", "--github-pat", "--azdev-pat" };

            output.Append(executable);
            for (var i = 0; i < args.Length; i++)
            {
                output.Append(' ');

                if (i > 0 && secretArgNames.Contains(args[i - 1]))
                {
                    output.Append("\"***\"");
                    continue;
                }

                output.Append($"\"{args[i]}\"");
            }

            return output.ToString();
        }
    }

    public class MaestroTestException : Exception
    {
        public MaestroTestException(string message)
        {
            TestContext.WriteLine(message);
        }
    }

}
