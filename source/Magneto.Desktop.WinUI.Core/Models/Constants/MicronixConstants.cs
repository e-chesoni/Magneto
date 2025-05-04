using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Models.Constants;
public class MicronixConstants
{
    public static class MicronixCommand
    {
        public const string MOVE_RELATIVE = "MVR";
        public const string MOVE_ABSOLUTE = "MVA";
        public const string STOP_MOTION = "STP";
        public const string STOP_ALL_MOTORS = "0STP";
        public const string WAIT_FOR_STOP = "WST";
        public const string ERASE_PROGRAM = "ERA";
        public const string EXECUTE_PROGRAM = "EXC";
        public const string BEGIN_PROGRAM_RECORDING = "PGM";
        public const string END_PROGRAM = "END";
        public const string STATUS_BYTE = "STA?";
        public const string READ_CURRENT_POSITION = "POS?";
        public const string READ_AND_CLEAR_ERRORS = "ERR?";
    }

    public enum MICRONIX_STATUS_BIT
    {
        NEGATIVE_SWITCH_ACTIVATED = 0,
        POSITIVE_SWITCH_ACTIVATED = 1,
        PROGRAM_RUNNING = 2,
        STAGE_STOPPED = 3,
        DECELERATION_PHASE = 4,
        CONSTANT_ACCELERATION_PHASE = 5,
        ACCELERATION_PHASE = 6,
        ONE_OR_MORE_ERRORS = 7
    }

    public enum MICRONIX_ERROR_CODE
    {
        COM_PORT_ERROR = 0, // custom error
        RECEIVE_BUFFER_OVERRUN = 10,
        MOTOR_DISABLED = 11,
        NO_ENCODER_DETECTED = 12,
        INDEX_NOT_FOUND = 13,
        HOME_REQUIRES_ENCODER = 14,
        MOVE_LIMIT_REQUIRES_ENCODER = 15,
        COMMAND_IS_READ_ONLY = 20,
        ONE_READ_OPERATION_PER_LINE = 21,
        TOO_MANY_COMMANDS_ON_LINE = 22,
        LINE_CHARACTER_LIMIT_EXCEEDED = 23,
        MISSING_AXIS_NUMBER = 24,
        MALFORMED_COMMAND = 25,
        INVALID_COMMAND = 26,
        GLOBAL_READ_OPERATION_REQUEST = 27,
        INVALID_PARAMETER_TYPE = 28,
        INVALID_CHARACTER_IN_PARAMETER = 29,
        COMMAND_CANNOT_BE_USED_IN_GLOBAL_CONTEXT = 30,
        PARAMETER_OUT_OF_BOUNDS = 31,
        INCORRECT_JOG_VELOCITY_REQUEST = 32,
        NOT_IN_JOG_MODE = 33,
        TRACE_ALREADY_IN_PROGRESS = 34,
        TRACE_DID_NOT_COMPLETE = 35,
        COMMAND_CANNOT_BE_EXECUTED_DURING_MOTION = 36,
        MOVE_OUTSIDE_SOFT_LIMITS = 37,
        READ_NOT_AVAILABLE_FOR_THIS_COMMAND = 38,
        PROGRAM_NUMBER_OUT_OF_RANGE = 39,
        PROGRAM_SIZE_LIMIT_EXCEEDED = 40,
        PROGRAM_FAILED_TO_RECORD = 41, // resend
        END_COMMAND_MUST_BE_ON_ITS_OWN_LINE = 42, // resend
        FAILED_TO_READ_PROGRAM = 43,
        COMMAND_ONLY_VALID_WITHIN_PROGRAM = 44,
        PROGRAM_ALREADY_EXISTS = 45, // try clearing program and sending again
        LIMIT_ACTIVATED = 50,
        END_OF_TRAVEL_LIMIT = 51,
        HOME_IN_PROGRESS = 52, // motion commands are disallowed while homing is happening
        IO_FUNCTION_ALREADY_IN_USE = 53,
        INVALID_RESOLUTION = 54,
        LIMITS_ARE_NOT_CONFIGURED_PROPERLY = 55,
        COMMAND_NOT_AVAILABLE_IN_THIS_VERSION = 80,
        ANALOG_ENCODER_NOT_AVAILABLE_IN_THIS_VERSION = 81,
    }
}
