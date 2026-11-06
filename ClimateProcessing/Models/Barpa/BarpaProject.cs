namespace ClimateProcessing.Models.Barpa;

public enum BarpaProject
{
    Output,
    OutputCmip6,
    CordexCmip6
}

public static class BarpaProjectExtensions
{
    private const string output = "output";
    private const string outputCmip6 = "output-CMIP6";
    private const string cordexCmip6 = "CORDEX-CMIP6";

    public static string ToProjectId(this BarpaProject project)
    {
        return project switch
        {
            BarpaProject.Output => output,
            BarpaProject.OutputCmip6 => outputCmip6,
            BarpaProject.CordexCmip6 => cordexCmip6,
            _ => throw new ArgumentOutOfRangeException(nameof(project), project, null)
        };
    }

    public static BarpaProject FromString(string project)
    {
        return project switch
        {
            output => BarpaProject.Output,
            outputCmip6 => BarpaProject.OutputCmip6,
            cordexCmip6 => BarpaProject.CordexCmip6,
            _ => throw new ArgumentOutOfRangeException(nameof(project), project, null)
        };
    }
}
