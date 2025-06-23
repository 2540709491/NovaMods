private string FilterMP3Paths(string input)
{
    return string.Join("\n", input.Split('\n')
        .Where(path => 
            path.Trim().EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        .Distinct());
}