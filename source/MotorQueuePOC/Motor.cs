using System;
using System.Threading;
using System.Threading.Tasks;

public class Motor
{
    public int AxisId
    {
        get; private set;
    }
    public double Position
    {
        get; private set;
    }

    public Motor(int axisId)
    {
        AxisId = axisId;
        Position = 0.0;  // Initial position
    }

    public async Task ExecuteCommand(string command, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        Console.WriteLine($"Motor Axis {AxisId}: Received command {command}");

        if (command.StartsWith("MVR"))  // Relative move
        {
            double moveBy = double.Parse(command.Substring(3));
            await MoveToPosition(Position + moveBy, cancellationToken);
        }
        else if (command.StartsWith("MVA"))  // Absolute move
        {
            double moveTo = double.Parse(command.Substring(3));
            await MoveToPosition(moveTo, cancellationToken);
        }
        else if (command.EndsWith("?"))  // Position query
        {
            Console.WriteLine($"Current position of Axis {AxisId} is {Position} mm.");
        }
    }

    private async Task MoveToPosition(double targetPosition, CancellationToken cancellationToken)
    {
        while (Position != targetPosition && !cancellationToken.IsCancellationRequested)
        {
            Position += Math.Sign(targetPosition - Position) * 1;  // Move in increments of 1 mm
            Console.WriteLine($"Motor Axis {AxisId}: Moving to {Position} mm");
            await Task.Delay(100);  // Simulate time it takes to move 1 mm
        }
        Console.WriteLine($"Motor Axis {AxisId}: Reached target position {Position} mm.");
    }
}
