namespace ZauberCMS.Core.Sections.Interfaces;

public interface ISectionDashboard
{
    string TabName { get; }
    int SortOrder { get; }
    string SectionAlias { get; }
}