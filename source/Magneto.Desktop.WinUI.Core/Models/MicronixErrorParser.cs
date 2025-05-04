using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MicronixConstants;

namespace Magneto.Desktop.WinUI.Core.Models;
public static class MicronixErrorParser
{
    // TODO: test this!
    private static readonly Regex ErrorPattern = new Regex(
        @"#Error (\d+)\s*-\s*(\w+)\s*-\s*(.*?)(?=(#Error|\z))",
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static List<MicronixError> ParseErrors(string response)
    {
        var errors = new List<MicronixError>();
        var matches = ErrorPattern.Matches(response);

        foreach (Match match in matches)
        {
            int code = int.TryParse(match.Groups[1].Value, out var parseCode) ? parseCode : -1;
            var command = match.Groups[2].Value;
            var message = match.Groups[3].Value;
            errors.Add(new MicronixError
            {
                code = Enum.IsDefined(typeof(MICRONIX_ERROR_CODE), code)
                    ? (MICRONIX_ERROR_CODE)code
                    : MICRONIX_ERROR_CODE.COM_PORT_ERROR,
                command = command,
                message = message,
            });
        }
        return errors;
    }
    public static void HandleErrors(string response)
    {
        var errors = ParseErrors(response);
        foreach (var error in errors)
        {
            MagnetoLogger.Log($"[Micronix Error] Code: {(int)error.code}, Command: {error.command}, Message: {error.message}", Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            switch (error.code)
            {
                // TODO: handle errors internally
                case MICRONIX_ERROR_CODE.PROGRAM_FAILED_TO_RECORD:
                // TODO: retry command
                default:
                    break;
            }
        }
    }
}
