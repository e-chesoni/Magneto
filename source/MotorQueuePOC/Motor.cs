using System;
using System.Threading;
using System.Threading.Tasks;

public class Motor
{
    public int MotorId
    {
        get; private set;
    }
    public int Position
    {
        get; private set;
    }

    public Motor(int motorId)
    {
        MotorId = motorId;
        Position = 0; // Initial position
    }

    public async Task ExecuteCommand(string command, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Motor {MotorId}: Executing command {command}");
        int desiredPosition = int.Parse(command.Replace("Move to position ", ""));
        while (Position != desiredPosition && !cancellationToken.IsCancellationRequested)
        {
            Position += (Position < desiredPosition) ? 10 : -10;
            Console.WriteLine($"Motor {MotorId}: Moving to {Position}");
            await Task.Delay(100, cancellationToken);
        }
        Console.WriteLine($"Motor {MotorId}: Command {command} completed at position {Position}");
    }
}
