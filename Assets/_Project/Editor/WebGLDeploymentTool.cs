#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// The Fusion Engineer의 WebGL 빌드와 GitHub Pages 배포를 Unity 메뉴에서 수행한다.
/// 게임 런타임 설정은 변경하지 않으며, 기존 Web Build Profile을 그대로 사용한다.
/// </summary>
public static class WebGLDeploymentTool
{
    private const string LogPrefix = "[WebGL Deploy]";
    private const string RequiredBranch = "main";
    private const string PagesUrl = "https://kbsand.github.io/TheFusionEngineer/";
    private const string BuildProfileAssetPath = "Assets/Settings/Build Profiles/Web.asset";
    private const string DeploymentToolAssetPath = "Assets/_Project/Editor/WebGLDeploymentTool.cs";
    private const string WebGLBuildDirectoryName = "WebGLBuild";
    private const string PagesDirectoryName = "docs";
    private const string BurstDebugMarker = "BurstDebugInformation_DoNotShip";
    private const string AnalyticsPrivacyFileName = "analytics-privacy.html";
    private const string AnalyticsPrivacyMarker = "<!-- TFE_ANALYTICS_PRIVACY_LINK -->";
    private const int DeleteRetryCount = 3;
    private const int DeleteRetryDelayMilliseconds = 500;

    private static readonly string[] ExactAllowedGitPaths =
    {
        ".gitignore",
        BuildProfileAssetPath,
        DeploymentToolAssetPath,
        DeploymentToolAssetPath + ".meta"
    };

    private static bool isRunning;

    private static string ProjectRoot =>
        Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    private static string WebGLBuildPath =>
        Path.Combine(ProjectRoot, WebGLBuildDirectoryName);

    private static string PagesPath =>
        Path.Combine(ProjectRoot, PagesDirectoryName);

    [MenuItem("Tools/The Fusion Engineer/Build WebGL Only")]
    private static void BuildWebGLOnlyMenu()
    {
        RunExclusive(
            () =>
            {
                Debug.Log($"{LogPrefix} Building a release WebGL player.");
                BuildWebGL();
                RemoveBurstDebugDirectories(WebGLBuildPath);
                ValidateWebGLBuild(WebGLBuildPath);
                Debug.Log($"{LogPrefix} WebGL build completed: {ToProjectRelativePath(WebGLBuildPath)}");
            });
    }

    [MenuItem("Tools/The Fusion Engineer/Prepare GitHub Pages")]
    private static void PrepareGitHubPagesMenu()
    {
        RunExclusive(
            () =>
            {
                Debug.Log($"{LogPrefix} Preparing the docs folder.");
                PrepareGitHubPages();
                Debug.Log($"{LogPrefix} GitHub Pages files are ready: {ToProjectRelativePath(PagesPath)}");
            });
    }

    [MenuItem("Tools/The Fusion Engineer/Build & Deploy WebGL")]
    private static void BuildAndDeployWebGLMenu()
    {
        const string dialogMessage =
            "This will build WebGL, replace the docs folder,\n" +
            "commit the deployment files, and push to origin/main.";

        if (!EditorUtility.DisplayDialog(
                "Deploy The Fusion Engineer",
                dialogMessage,
                "Deploy",
                "Cancel"))
        {
            return;
        }

        RunExclusive(BuildAndDeployWebGL);
    }

    [MenuItem("Tools/The Fusion Engineer/Open GitHub Pages")]
    private static void OpenGitHubPagesMenu()
    {
        OpenPagesUrl();
    }

    /// <summary>
    /// 자동화 메뉴가 동시에 두 번 실행되지 않도록 보호하고 예외를 한 곳에서 기록한다.
    /// </summary>
    private static void RunExclusive(Action operation)
    {
        if (isRunning)
        {
            Debug.LogWarning($"{LogPrefix} Deployment is already running.");
            return;
        }

        isRunning = true;
        try
        {
            operation();
        }
        catch (Exception exception)
        {
            Debug.LogError($"{LogPrefix} Automation stopped.\n{exception}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            isRunning = false;
        }
    }

    /// <summary>
    /// 검증부터 Push까지 8단계로 실행한다. 어느 단계든 실패하면 예외가 전파되어
    /// 이후 Commit, Push, 브라우저 실행이 수행되지 않는다.
    /// </summary>
    private static void BuildAndDeployWebGL()
    {
        try
        {
            ShowStep(1, "Validating project");
            ValidateProjectFiles();
            ValidateGitEnvironment();
            ValidateStagedPaths();
            WarnAboutUnrelatedWorkingTreeChanges();

            ShowStep(2, "Building WebGL");
            BuildWebGL();

            ShowStep(3, "Removing debug output");
            RemoveBurstDebugDirectories(WebGLBuildPath);
            ValidateWebGLBuild(WebGLBuildPath);

            ShowStep(4, "Preparing docs");
            PrepareGitHubPages();

            ShowStep(5, "Updating .gitignore");
            EnsureGitIgnoreEntries();

            ShowStep(6, "Staging deployment files");
            RemoveWebGLBuildFromGitTracking();
            StageDeploymentFiles();
            ValidateStagedPaths();

            ShowStep(7, "Committing changes");
            if (!HasStagedChanges())
            {
                Debug.Log($"{LogPrefix} No deployment changes detected");
                OpenPagesUrl();
                return;
            }

            string commitMessage = $"Update WebGL build {DateTime.Now:yyyy-MM-dd HH:mm}";
            RunGitRequired("commit", "-m", commitMessage);

            ShowStep(8, "Pushing origin/main");
            GitResult pushResult = RunGit("push", "origin", RequiredBranch);
            if (pushResult.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    "git push origin main failed. Authentication or network configuration may be the cause. " +
                    "The local commit and docs folder were preserved; no rollback was performed.");
            }

            Debug.Log($"{LogPrefix} Deployment succeeded: {PagesUrl}");
            OpenPagesUrl();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void ShowStep(int step, string description)
    {
        Debug.Log($"{LogPrefix} {step}/8 {description}");
        EditorUtility.DisplayProgressBar(
            "The Fusion Engineer WebGL Deployment",
            description,
            step / 8f);
    }

    /// <summary>
    /// Web Build Profile과 전역 빌드 씬 목록을 검증한 뒤 Release WebGL 빌드를 생성한다.
    /// PlayerSettings나 Build Profile의 값을 자동으로 변경하지 않는다.
    /// </summary>
    private static void BuildWebGL()
    {
        ValidateProjectFiles();

        BuildProfile buildProfile =
            AssetDatabase.LoadAssetAtPath<BuildProfile>(BuildProfileAssetPath);
        if (buildProfile == null)
        {
            throw new FileNotFoundException(
                $"Unable to load the Web Build Profile: {BuildProfileAssetPath}");
        }

        string[] enabledGlobalScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (enabledGlobalScenes.Length == 0)
        {
            throw new InvalidOperationException(
                "No enabled scenes were found in EditorBuildSettings.");
        }

        string[] profileScenes = buildProfile.GetScenesForBuild()
            .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (!enabledGlobalScenes.SequenceEqual(profileScenes, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "The Web Build Profile scene list differs from the enabled EditorBuildSettings scenes. " +
                "The automation will not modify either setting automatically.");
        }

        DeleteDirectoryWithRetry(WebGLBuildPath);
        Directory.CreateDirectory(WebGLBuildPath);

        Debug.Log(
            $"{LogPrefix} Enabled scenes ({enabledGlobalScenes.Length}):\n" +
            string.Join("\n", enabledGlobalScenes));
        Debug.Log($"{LogPrefix} Using Build Profile without modification: {BuildProfileAssetPath}");

        BuildPlayerWithProfileOptions buildOptions = new BuildPlayerWithProfileOptions
        {
            buildProfile = buildProfile,
            locationPathName = WebGLBuildPath,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        if (report == null || report.summary.result != BuildResult.Succeeded)
        {
            string result = report == null ? "No BuildReport" : report.summary.result.ToString();
            long errorCount = report == null ? -1 : report.summary.totalErrors;
            throw new InvalidOperationException(
                $"WebGL build failed. Result: {result}, Errors: {errorCount}. " +
                "Copy, Git, Commit, and Push were not executed.");
        }

        Debug.Log(
            $"{LogPrefix} WebGL build succeeded: " +
            $"{report.summary.totalSize:N0} bytes, {report.summary.totalTime}.");
    }

    /// <summary>
    /// WebGLBuild 전체를 docs로 복사하고 GitHub Pages에 필요한 .nojekyll을 만든다.
    /// Unity 메타 파일은 배포 산출물이 아니므로 복사하지 않는다.
    /// </summary>
    private static void PrepareGitHubPages()
    {
        ValidateWebGLBuild(WebGLBuildPath);
        DeleteDirectoryWithRetry(PagesPath);
        Directory.CreateDirectory(PagesPath);

        CopyDirectoryWithoutMetaFiles(WebGLBuildPath, PagesPath);
        File.WriteAllBytes(Path.Combine(PagesPath, ".nojekyll"), Array.Empty<byte>());
        RemoveBurstDebugDirectories(PagesPath);
        ValidateWebGLBuild(PagesPath);
    }

    private static void CopyDirectoryWithoutMetaFiles(string sourceRoot, string destinationRoot)
    {
        foreach (string directory in Directory.GetDirectories(
                     sourceRoot,
                     "*",
                     SearchOption.AllDirectories))
        {
            string relativePath = GetRelativePath(sourceRoot, directory);
            Directory.CreateDirectory(Path.Combine(destinationRoot, relativePath));
        }

        foreach (string sourceFile in Directory.GetFiles(
                     sourceRoot,
                     "*",
                     SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetExtension(sourceFile), ".meta", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relativePath = GetRelativePath(sourceRoot, sourceFile);
            string destinationFile = Path.Combine(destinationRoot, relativePath);
            string destinationDirectory = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourceFile, destinationFile, true);
        }
    }

    /// <summary>
    /// 빌드 폴더 전체에서 Burst의 배포 불필요 디버그 폴더를 찾아 제거한다.
    /// 잠금으로 삭제하지 못한 경우 경고만 남겨 정상 실행 파일의 배포는 계속한다.
    /// </summary>
    private static void RemoveBurstDebugDirectories(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            return;
        }

        string[] debugDirectories = Directory.GetDirectories(
                rootPath,
                "*",
                SearchOption.AllDirectories)
            .Where(path =>
                Path.GetFileName(path).IndexOf(
                    BurstDebugMarker,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderByDescending(path => path.Length)
            .ToArray();

        foreach (string debugDirectory in debugDirectories)
        {
            Debug.Log(
                $"{LogPrefix} Removing debug folder:\n" +
                ToProjectRelativePath(debugDirectory));

            try
            {
                DeleteDirectoryWithRetry(debugDirectory);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Could not remove debug folder " +
                    $"{ToProjectRelativePath(debugDirectory)}. Deployment will continue.\n" +
                    exception.Message);
            }
        }
    }

    private static void ValidateWebGLBuild(string rootPath)
    {
        string indexPath = Path.Combine(rootPath, "index.html");
        string buildPath = Path.Combine(rootPath, "Build");

        if (!File.Exists(indexPath))
        {
            throw new FileNotFoundException(
                $"Required WebGL file is missing: {ToProjectRelativePath(indexPath)}");
        }

        if (!Directory.Exists(buildPath))
        {
            throw new DirectoryNotFoundException(
                $"Required WebGL directory is missing: {ToProjectRelativePath(buildPath)}");
        }

        string privacyPath = Path.Combine(rootPath, AnalyticsPrivacyFileName);
        if (!File.Exists(privacyPath))
        {
            throw new FileNotFoundException(
                $"Required analytics privacy page is missing: {ToProjectRelativePath(privacyPath)}");
        }

        string indexHtml = File.ReadAllText(indexPath, Encoding.UTF8);
        if (indexHtml.IndexOf(AnalyticsPrivacyMarker, StringComparison.Ordinal) < 0)
        {
            throw new InvalidOperationException(
                $"Analytics privacy link marker is missing: {ToProjectRelativePath(indexPath)}");
        }
    }

    private static void ValidateProjectFiles()
    {
        string profilePath = Path.Combine(
            ProjectRoot,
            BuildProfileAssetPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(profilePath))
        {
            throw new FileNotFoundException(
                $"Required Build Profile is missing: {BuildProfileAssetPath}");
        }
    }

    /// <summary>
    /// Windows 파일 잠금을 고려하여 폴더 삭제를 최대 3번 재시도한다.
    /// 최종 실패 시 문제 경로를 포함한 예외로 자동화를 중단한다.
    /// </summary>
    private static void DeleteDirectoryWithRetry(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        Exception lastException = null;
        for (int attempt = 1; attempt <= DeleteRetryCount; attempt++)
        {
            try
            {
                Directory.Delete(directoryPath, true);
                return;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException)
            {
                lastException = exception;
                if (attempt < DeleteRetryCount)
                {
                    Debug.LogWarning(
                        $"{LogPrefix} Delete attempt {attempt}/{DeleteRetryCount} failed: " +
                        $"{ToProjectRelativePath(directoryPath)}. Retrying in " +
                        $"{DeleteRetryDelayMilliseconds} ms.\n{exception.Message}");
                    Thread.Sleep(DeleteRetryDelayMilliseconds);
                }
            }
        }

        throw new IOException(
            $"Failed to delete directory after {DeleteRetryCount} attempts: " +
            $"{ToProjectRelativePath(directoryPath)}",
            lastException);
    }

    /// <summary>
    /// 필요한 ignore 규칙만 중복 없이 추가한다. docs는 배포 대상이므로 ignore하지 않는다.
    /// </summary>
    private static void EnsureGitIgnoreEntries()
    {
        string gitIgnorePath = Path.Combine(ProjectRoot, ".gitignore");
        string[] requiredLines =
        {
            "# Local WebGL build output",
            "/WebGLBuild/",
            "# Unity Burst debug output",
            "*_BurstDebugInformation_DoNotShip/",
            "*BurstDebugInformation_DoNotShip/"
        };

        List<string> lines = File.Exists(gitIgnorePath)
            ? File.ReadAllLines(gitIgnorePath, Encoding.UTF8).ToList()
            : new List<string>();

        bool changed = false;
        foreach (string requiredLine in requiredLines)
        {
            if (lines.Any(line =>
                    string.Equals(line.Trim(), requiredLine, StringComparison.Ordinal)))
            {
                continue;
            }

            if (lines.Count > 0 && lines[lines.Count - 1].Length > 0 &&
                requiredLine.StartsWith("#", StringComparison.Ordinal))
            {
                lines.Add(string.Empty);
            }

            lines.Add(requiredLine);
            changed = true;
        }

        if (lines.Any(line =>
                string.Equals(line.Trim(), "/docs/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(line.Trim(), "docs/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(line.Trim(), "/docs/Build/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(line.Trim(), "docs/Build/", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                ".gitignore contains a rule that directly ignores docs or docs/Build. " +
                "Remove that rule before deployment.");
        }

        if (changed)
        {
            File.WriteAllLines(gitIgnorePath, lines, new UTF8Encoding(false));
            Debug.Log($"{LogPrefix} Updated .gitignore.");
        }
    }

    private static void ValidateGitEnvironment()
    {
        RunGitRequired("--version");

        string repositoryRoot = RunGitRequired("rev-parse", "--show-toplevel")
            .StandardOutput.Trim();
        if (!PathsEqual(repositoryRoot, ProjectRoot))
        {
            throw new InvalidOperationException(
                $"Git repository root mismatch. Expected '{ProjectRoot}', got '{repositoryRoot}'.");
        }

        string branch = RunGitRequired("branch", "--show-current").StandardOutput.Trim();
        if (!string.Equals(branch, RequiredBranch, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Current branch is '{branch}'. Automatic push is allowed only on '{RequiredBranch}'.");
        }

        string origin = RunGitRequired("remote", "get-url", "origin").StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(origin))
        {
            throw new InvalidOperationException("Git remote 'origin' is not configured.");
        }

        GitResult ignoreResult = RunGit(
            "check-ignore",
            "--no-index",
            "--",
            "docs/index.html",
            "docs/Build");
        if (ignoreResult.ExitCode == 0)
        {
            throw new InvalidOperationException(
                ".gitignore excludes docs or docs/Build. GitHub Pages files must remain trackable.");
        }

        if (ignoreResult.ExitCode != 1)
        {
            throw new InvalidOperationException(
                $"Unable to validate docs ignore rules. git check-ignore exited with code " +
                $"{ignoreResult.ExitCode}.");
        }

        Debug.Log($"{LogPrefix} Git repository: {repositoryRoot}\nBranch: {branch}\nOrigin: {origin}");
    }

    /// <summary>
    /// 배포와 무관한 작업 트리 변경은 경고만 표시하고 수정하거나 staging하지 않는다.
    /// </summary>
    private static void WarnAboutUnrelatedWorkingTreeChanges()
    {
        GitResult status = RunGitRequired("status", "--short", "--untracked-files=all");
        string[] unrelatedLines = SplitLines(status.StandardOutput)
            .Where(line => !IsStatusLineAllowedForDeployment(line))
            .ToArray();

        if (unrelatedLines.Length > 0)
        {
            Debug.LogWarning(
                $"{LogPrefix} Unrelated working-tree changes detected. " +
                "They will not be staged or modified:\n" +
                string.Join("\n", unrelatedLines));
        }
    }

    private static bool IsStatusLineAllowedForDeployment(string statusLine)
    {
        if (statusLine.Length < 4)
        {
            return false;
        }

        string path = statusLine.Substring(3).Trim().Trim('"').Replace('\\', '/');
        int renameSeparatorIndex = path.LastIndexOf(" -> ", StringComparison.Ordinal);
        if (renameSeparatorIndex >= 0)
        {
            path = path.Substring(renameSeparatorIndex + 4);
        }

        return IsAllowedGitPath(path);
    }

    /// <summary>
    /// WebGLBuild가 과거에 추적되었다면 로컬 파일은 유지하고 Git 인덱스에서만 제거한다.
    /// </summary>
    private static void RemoveWebGLBuildFromGitTracking()
    {
        GitResult trackedFiles = RunGitRequired(
            "ls-files",
            "-z",
            "--",
            WebGLBuildDirectoryName);

        if (string.IsNullOrEmpty(trackedFiles.StandardOutput))
        {
            return;
        }

        RunGitRequired(
            "rm",
            "-r",
            "--cached",
            "--ignore-unmatch",
            "--",
            WebGLBuildDirectoryName);
        Debug.Log($"{LogPrefix} Removed WebGLBuild from Git tracking; local files were preserved.");
    }

    private static void StageDeploymentFiles()
    {
        RunGitRequired("add", "-A", "--", PagesDirectoryName);
        StageRequiredFile(".gitignore");
        StageRequiredFile(BuildProfileAssetPath);
        StageRequiredFile(DeploymentToolAssetPath);

        string metaPath = DeploymentToolAssetPath + ".meta";
        string absoluteMetaPath = Path.Combine(
            ProjectRoot,
            metaPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absoluteMetaPath))
        {
            RunGitRequired("add", "--", metaPath);
        }
        else
        {
            Debug.LogWarning(
                $"{LogPrefix} Unity has not generated the deployment tool .meta file yet: {metaPath}");
        }
    }

    private static void StageRequiredFile(string repositoryRelativePath)
    {
        string absolutePath = Path.Combine(
            ProjectRoot,
            repositoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException(
                $"Required staging file is missing: {repositoryRelativePath}");
        }

        RunGitRequired("add", "--", repositoryRelativePath);
    }

    /// <summary>
    /// 사용자가 미리 staging한 파일까지 검사한다. 허용 범위 밖 파일은 unstage하지 않고
    /// 목록을 표시한 뒤 Commit과 Push를 중단한다.
    /// </summary>
    private static void ValidateStagedPaths()
    {
        string[] stagedPaths = GetNullSeparatedPaths(
            RunGitRequired("diff", "--cached", "--name-only", "-z").StandardOutput);

        string[] disallowedPaths = stagedPaths
            .Where(path => !IsAllowedGitPath(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (disallowedPaths.Length > 0)
        {
            throw new InvalidOperationException(
                "Push was blocked because unrelated files are already staged. " +
                "They were not unstaged automatically:\n" +
                string.Join("\n", disallowedPaths));
        }
    }

    private static bool IsAllowedGitPath(string rawPath)
    {
        string path = rawPath.Trim().Trim('"').Replace('\\', '/');
        if (path.Equals(PagesDirectoryName, StringComparison.Ordinal) ||
            path.StartsWith(PagesDirectoryName + "/", StringComparison.Ordinal) ||
            path.Equals(WebGLBuildDirectoryName, StringComparison.Ordinal) ||
            path.StartsWith(WebGLBuildDirectoryName + "/", StringComparison.Ordinal))
        {
            return true;
        }

        return ExactAllowedGitPaths.Contains(path, StringComparer.Ordinal);
    }

    private static bool HasStagedChanges()
    {
        GitResult result = RunGit("diff", "--cached", "--quiet");
        if (result.ExitCode == 0)
        {
            return false;
        }

        if (result.ExitCode == 1)
        {
            return true;
        }

        throw new InvalidOperationException(
            $"Unable to inspect staged changes. git diff exited with code {result.ExitCode}.");
    }

    /// <summary>
    /// Git을 셸 없이 실행하고 stdout/stderr를 각각 읽는다.
    /// 모든 명령은 프로젝트 루트에서 실행되며 실패 코드는 호출자가 검사한다.
    /// </summary>
    private static GitResult RunGit(params string[] arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = string.Join(" ", arguments.Select(QuoteProcessArgument)),
            WorkingDirectory = ProjectRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using (Process process = new Process { StartInfo = startInfo })
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("Unable to start git.");
                }

                Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                GitResult result = new GitResult(
                    process.ExitCode,
                    standardOutputTask.GetAwaiter().GetResult(),
                    standardErrorTask.GetAwaiter().GetResult());

                if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                {
                    Debug.Log($"{LogPrefix} git {string.Join(" ", arguments)}\n{result.StandardOutput.TrimEnd()}");
                }

                if (!string.IsNullOrWhiteSpace(result.StandardError))
                {
                    if (result.ExitCode == 0)
                    {
                        Debug.Log($"{LogPrefix} git output:\n{result.StandardError.TrimEnd()}");
                    }
                    else
                    {
                        Debug.LogError($"{LogPrefix} git error:\n{result.StandardError.TrimEnd()}");
                    }
                }

                return result;
            }
        }
        catch (Exception exception) when (
            exception is System.ComponentModel.Win32Exception ||
            exception is InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Git could not be executed. Verify that Git for Windows is installed and available in PATH.",
                exception);
        }
    }

    private static GitResult RunGitRequired(params string[] arguments)
    {
        GitResult result = RunGit(arguments);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(" ", arguments)} failed with exit code {result.ExitCode}.");
        }

        return result;
    }

    private static string QuoteProcessArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return "\"\"";
        }

        if (argument.All(character =>
                !char.IsWhiteSpace(character) &&
                character != '"' &&
                character != '\\'))
        {
            return argument;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append('"');
        int backslashCount = 0;

        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                builder.Append('\\', backslashCount * 2 + 1);
                builder.Append('"');
                backslashCount = 0;
                continue;
            }

            builder.Append('\\', backslashCount);
            backslashCount = 0;
            builder.Append(character);
        }

        builder.Append('\\', backslashCount * 2);
        builder.Append('"');
        return builder.ToString();
    }

    private static void OpenPagesUrl()
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = PagesUrl,
                    UseShellExecute = true
                });
        }
        catch (Exception exception)
        {
            Debug.LogError($"{LogPrefix} Could not open {PagesUrl}\n{exception.Message}");
        }
    }

    private static string GetRelativePath(string rootPath, string childPath)
    {
        string normalizedRoot = Path.GetFullPath(rootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedChild = Path.GetFullPath(childPath);
        if (!normalizedChild.StartsWith(
                normalizedRoot + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Path '{normalizedChild}' is outside root '{normalizedRoot}'.");
        }

        return normalizedChild.Substring(normalizedRoot.Length + 1);
    }

    private static string ToProjectRelativePath(string absolutePath)
    {
        string normalizedProjectRoot = ProjectRoot
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedPath = Path.GetFullPath(absolutePath);
        if (normalizedPath.StartsWith(
                normalizedProjectRoot + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath
                .Substring(normalizedProjectRoot.Length + 1)
                .Replace('\\', '/');
        }

        return normalizedPath;
    }

    private static bool PathsEqual(string firstPath, string secondPath)
    {
        return string.Equals(
            Path.GetFullPath(firstPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(secondPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string[] SplitLines(string value)
    {
        return value.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.RemoveEmptyEntries);
    }

    private static string[] GetNullSeparatedPaths(string value)
    {
        return value.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Replace('\\', '/'))
            .ToArray();
    }

    private sealed class GitResult
    {
        public GitResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? string.Empty;
            StandardError = standardError ?? string.Empty;
        }

        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
    }
}
#endif
