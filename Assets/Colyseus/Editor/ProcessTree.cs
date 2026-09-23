using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Colyseus.Editor
{
    internal struct TreeKillResult
    {
        public bool AllGone;
        public string Error;
    }

    /// <summary>
    /// Inspects and kills processes the editor started but no longer owns.
    ///
    /// Two constraints shape everything here. Mono has no
    /// <c>Process.Kill(bool entireProcessTree)</c>, so a tree has to be walked by
    /// hand; and after a domain reload the managed <see cref="Process"/> object is
    /// gone, which on Unix means the child is never reaped and lingers as a zombie
    /// that <c>kill -0</c>, <c>Process.GetProcessById</c> and <c>HasExited</c> all
    /// report as alive. Hence the <c>ps</c>-based state check.
    ///
    /// Every method blocks; call them from a background thread.
    /// </summary>
    internal static class ProcessTree
    {
        private const int ProbeTimeoutMs = 5000;
        private static readonly char[] Whitespace = { ' ', '\t' };

        // ------------------------------------------------------------------
        // Liveness and identity
        // ------------------------------------------------------------------

        /// <summary>
        /// An opaque token identifying this specific process, so a later process
        /// that reuses the pid is not mistaken for it. Unix uses <c>ps lstart</c>
        /// (locale-formatted, so only ever compared for equality); Windows uses the
        /// start time in ticks. Empty when it cannot be read.
        /// </summary>
        public static string CaptureStartToken(int pid, Process started)
        {
            try
            {
                return NodeLocator.IsWindows
                    ? started.StartTime.Ticks.ToString(CultureInfo.InvariantCulture)
                    : QueryUnix(pid).startToken ?? "";
            }
            catch { return ""; }
        }

        /// <summary>Is <paramref name="pid"/> running (zombies count as exited)?</summary>
        public static bool IsRunning(int pid) => Query(pid).running;

        /// <summary>
        /// Is <paramref name="pid"/> running and still the process that produced
        /// <paramref name="token"/>? An unreadable token on either side is a no:
        /// after a crash the pid may have been recycled, and Stop would then kill a
        /// stranger's process tree.
        /// </summary>
        public static bool IsSameProcess(int pid, string token)
        {
            var info = Query(pid);
            return info.running && !string.IsNullOrEmpty(token) && info.startToken == token;
        }

        private static (bool running, string startToken) Query(int pid)
        {
            if (pid <= 0) return (false, null);
            try
            {
                if (!NodeLocator.IsWindows)
                {
                    var unix = QueryUnix(pid);
                    return (unix.found && !unix.zombie, unix.startToken);
                }

                Process p;
                try { p = Process.GetProcessById(pid); }
                catch (ArgumentException) { return (false, null); } // no such process

                using (p)
                {
                    if (p.HasExited) return (false, null);
                    string token = null;
                    try { token = p.StartTime.Ticks.ToString(CultureInfo.InvariantCulture); }
                    catch { /* no rights to read it: identity stays unproven */ }
                    return (true, token);
                }
            }
            catch { return (false, null); }
        }

        private static (bool found, bool zombie, string startToken) QueryUnix(int pid)
        {
            var result = NodeLocator.RunQuick("/bin/ps", $"-o pid=,stat=,lstart= -p {pid}", null, ProbeTimeoutMs);
            var line = result.exitCode == 0 ? FirstLine(result.stdout) : null;
            if (line == null) return (false, false, null);

            // "40504 S+   Tue 22 Sep 12:46:25 2026" -> pid, stat, everything else
            var parts = line.Trim().Split(Whitespace, 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return (false, false, null);

            var zombie = parts[1].StartsWith("Z", StringComparison.Ordinal);
            var token = parts.Length >= 3 ? parts[2].Trim() : "";
            return (true, zombie, token);
        }

        // ------------------------------------------------------------------
        // Killing
        // ------------------------------------------------------------------

        /// <summary>
        /// Terminates <paramref name="rootPid"/> and every descendant: SIGTERM
        /// first so the server can close its listeners, SIGKILL for whatever is
        /// left. Safe to call on a process that already exited.
        /// </summary>
        public static TreeKillResult Kill(int rootPid, int termWaitMs = 2500, int killWaitMs = 1500)
        {
            if (rootPid <= 0) return new TreeKillResult { AllGone = true };

            try
            {
                return NodeLocator.IsWindows
                    ? KillWindows(rootPid, termWaitMs + killWaitMs)
                    : KillUnix(rootPid, termWaitMs, killWaitMs);
            }
            catch (Exception ex)
            {
                return new TreeKillResult { AllGone = false, Error = ex.Message };
            }
        }

        private static TreeKillResult KillWindows(int rootPid, int waitMs)
        {
            // /T walks the child chain (cmd.exe -> npm.cmd's cmd.exe -> node.exe).
            var taskkill = "taskkill";
            try
            {
                var full = Path.Combine(Environment.SystemDirectory, "taskkill.exe");
                if (File.Exists(full)) taskkill = full;
            }
            catch { /* fall back to PATH */ }

            var result = NodeLocator.RunQuick(taskkill, $"/PID {rootPid} /T /F", null, ProbeTimeoutMs);

            var clock = Stopwatch.StartNew();
            while (IsRunning(rootPid))
            {
                if (clock.ElapsedMilliseconds >= waitMs)
                    return new TreeKillResult { Error = Tidy(result.stderr) ?? "taskkill did not terminate the process" };
                Thread.Sleep(100);
            }
            return new TreeKillResult { AllGone = true };
        }

        private static TreeKillResult KillUnix(int rootPid, int termWaitMs, int killWaitMs)
        {
            var victims = Descendants(ProcessTable(), new[] { rootPid });

            // One invocation for the whole tree: killing a parent first would give
            // a watcher (tsx watch, nodemon) the chance to respawn its child.
            var termError = Signal("TERM", victims);
            var survivors = WaitForExit(victims, termWaitMs);
            if (survivors.Count == 0) return new TreeKillResult { AllGone = true };

            // Anything respawned during the grace period is a descendant again.
            var remaining = Descendants(ProcessTable(), survivors);
            var killError = Signal("KILL", remaining);
            survivors = WaitForExit(remaining, killWaitMs);
            if (survivors.Count == 0) return new TreeKillResult { AllGone = true };

            return new TreeKillResult
            {
                Error = Tidy(killError) ?? Tidy(termError) ??
                        $"pid {string.Join(", ", survivors.Select(p => p.ToString()).ToArray())} survived SIGKILL",
            };
        }

        /// <summary>Polls until every pid is gone or the time runs out; returns the survivors.</summary>
        private static List<int> WaitForExit(List<int> pids, int timeoutMs)
        {
            var clock = Stopwatch.StartNew();
            while (true)
            {
                var survivors = Survivors(pids);
                if (survivors.Count == 0 || clock.ElapsedMilliseconds >= timeoutMs) return survivors;
                Thread.Sleep(100);
            }
        }

        /// <summary>Sends a signal to every pid in one shell invocation.</summary>
        private static string Signal(string signal, List<int> pids)
        {
            if (pids.Count == 0) return null;
            // `kill` is a shell builtin, so it has to go through sh.
            var result = NodeLocator.RunQuick("/bin/sh", $"-c 'kill -{signal} {Join(pids, " ")} 2>/dev/null'", null, ProbeTimeoutMs);
            return result.stderr;
        }

        /// <summary>The subset of <paramref name="pids"/> still running (zombies don't count).</summary>
        private static List<int> Survivors(List<int> pids)
        {
            var alive = new List<int>();
            if (pids.Count == 0) return alive;

            var result = NodeLocator.RunQuick("/bin/ps", $"-o pid=,stat= -p {Join(pids, ",")}", null, ProbeTimeoutMs);
            if (result.exitCode != 0) return alive;

            foreach (var parts in Rows(result.stdout))
            {
                if (parts[1].StartsWith("Z", StringComparison.Ordinal)) continue; // exited, not yet reaped
                if (int.TryParse(parts[0], out var pid)) alive.Add(pid);
            }
            return alive;
        }

        /// <summary>Parent pid to children, from one `ps` call; null when ps is unavailable.</summary>
        private static Dictionary<int, List<int>> ProcessTable()
        {
            var result = NodeLocator.RunQuick("/bin/ps", "-eo pid=,ppid=", null, ProbeTimeoutMs);
            if (result.exitCode != 0 || string.IsNullOrEmpty(result.stdout)) return null;

            var childrenOf = new Dictionary<int, List<int>>();
            foreach (var parts in Rows(result.stdout))
            {
                if (!int.TryParse(parts[0], out var pid) || !int.TryParse(parts[1], out var ppid)) continue;
                if (!childrenOf.TryGetValue(ppid, out var list)) childrenOf[ppid] = list = new List<int>();
                list.Add(pid);
            }
            return childrenOf;
        }

        /// <summary>
        /// <paramref name="roots"/> followed by every descendant, parents first.
        /// Without a process table (minimal container, no ps) the roots are all we can reach.
        /// </summary>
        private static List<int> Descendants(Dictionary<int, List<int>> childrenOf, IEnumerable<int> roots)
        {
            var ordered = new List<int>();
            var seen = new HashSet<int>();
            var queue = new Queue<int>();
            foreach (var root in roots)
            {
                if (seen.Add(root)) queue.Enqueue(root);
            }

            while (queue.Count > 0)
            {
                var pid = queue.Dequeue();
                ordered.Add(pid);
                if (childrenOf == null || !childrenOf.TryGetValue(pid, out var children)) continue;
                foreach (var child in children)
                {
                    if (seen.Add(child)) queue.Enqueue(child); // also guards a cyclic ppid table
                }
            }
            return ordered;
        }

        // ------------------------------------------------------------------
        // Parsing
        // ------------------------------------------------------------------

        /// <summary>Whitespace-split rows of `ps` output with at least two columns.</summary>
        private static IEnumerable<string[]> Rows(string output)
        {
            if (string.IsNullOrEmpty(output)) yield break;
            foreach (var raw in output.Split('\n'))
            {
                var parts = raw.Trim().Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) yield return parts;
            }
        }

        private static string FirstLine(string text) =>
            text?.Split('\n').FirstOrDefault(l => l.Trim().Length > 0);

        private static string Join(List<int> pids, string separator) =>
            string.Join(separator, pids.Select(p => p.ToString(CultureInfo.InvariantCulture)).ToArray());

        private static string Tidy(string text)
        {
            var trimmed = text?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }
}
