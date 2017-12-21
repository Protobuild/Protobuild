param()

[xml]$ModuleDocument = Get-Content -Path $PSScriptRoot\Module.xml

$SemanticVersion = $ModuleDocument.Module.SemanticVersion
$BuildDate = Get-Date -Date ((Get-Date).ToUniversalTime()) -UFormat "+%Y-%m-%d %H:%M:%S"
$BuildDate = "$BuildDate UTC"
$BuildCommit = $(git rev-parse HEAD)
$JenkinsGitBranch = $env:GIT_BRANCH
if ($JenkinsGitBranch -ne "" -and $JenkinsGitBranch -ne $null) {
  $BuildCommit = "$BuildCommit; $JenkinsGitBranch"
}
$BuildIsDirty = "false"
if ( ((git status --porcelain) | Out-String) -ne "" ) {
  $BuildIsDirty = "true"
}

Write-Output @"
namespace Protobuild
{
    public static class ProtobuildVersion
    {
        public const string SemanticVersion = "$SemanticVersion";

        public const string BuildDate = "$BuildDate";

        public const string BuildCommit = "$BuildCommit";

        public const bool BuiltWithPendingChanges = $BuildIsDirty;
    }
}
"@.Trim() | Out-File -Encoding ASCII ProtobuildVersion.cs