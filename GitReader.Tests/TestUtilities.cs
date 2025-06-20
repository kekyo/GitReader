////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader;

internal static class TestUtilities
{
    public static Task WriteAllTextAsync(string path, string? contents, CancellationToken ct = default)
    {
#if NETFRAMEWORK
        return Task.Run(() => File.WriteAllText(path, contents), ct);
#else
        return File.WriteAllTextAsync(path, contents, ct);
#endif
    }
    
    public static async Task RunGitCommandAsync(string workingDirectory, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
#if NETFRAMEWORK
        await Task.Run(() => process.WaitForExit());
#else
        await process.WaitForExitAsync();
#endif

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Git command failed: git {arguments}\nError: {error}");
        }
    }
}
