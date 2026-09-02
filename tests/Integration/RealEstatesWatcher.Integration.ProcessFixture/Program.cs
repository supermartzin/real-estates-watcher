using System.Globalization;

if (args is ["write", var output])
{
    Console.Write(output);
    return 0;
}

if (args is ["delay", var milliseconds] &&
    int.TryParse(milliseconds, NumberStyles.None, CultureInfo.InvariantCulture, out var delay))
{
    await Task.Delay(delay);
    Console.Write("completed");
    return 0;
}

return 1;
