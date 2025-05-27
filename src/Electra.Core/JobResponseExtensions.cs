namespace Electra.Core;

public static class JobResponseExtensions
{
    public static void Merge(this JobResponse res, JobResponse response)
    {
        res.Info.AddRange(response.Info);
        res.Errors.AddRange(response.Errors);
        res.Warnings.AddRange(response.Warnings);
        res.Message += Environment.NewLine + response.Message;
    }

    public static string ToString(this JobResponse res, bool verbose = false)
    {
        var cr = Environment.NewLine + "\t";
        var sb = new StringBuilder();
        if(verbose)
            sb.AppendLine($"Info ({res.Info.Count}): {string.Join(cr, res.Info)}");
        sb.AppendLine($"Errors ({res.Errors.Count}): {string.Join(cr, res.Errors)}");
        sb.AppendLine($"Warnings ({res.Warnings.Count}): {string.Join(cr, res.Warnings)}");
        return sb.ToString();
    }
}