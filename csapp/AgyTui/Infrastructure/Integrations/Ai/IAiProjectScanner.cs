using AgyTui.Infrastructure.Integrations.Ai.Services;

namespace AgyTui.Infrastructure.Integrations.Ai;

public interface IAiProjectScanner
{
    ProjectScanResult[] ScanProjectsForClaude(string? baseDir = null);
    ProjectScanResult[] ScanProjectsForOllama(string? baseDir = null);
    ProjectScanResult[] ScanProjectsForAgy(string? baseDir = null);
    ProjectScanResult[] ScanProjects(string provider, string? baseDir = null);
}
