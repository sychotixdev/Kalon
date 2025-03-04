﻿using System.ComponentModel;
using System.Diagnostics;
using Kalon.Native.PInvoke;
using Kalon.Native.Structs;
using Kalon.Records;

namespace Kalon;

/// <summary>
/// Provides the functionality to move the cursor in a human realistic manner
/// </summary>
public static class CursorMover
{
    /// <summary>
    /// Moves the cursor to a set of coordinates in a timespan
    /// </summary>
    public static void MoveCursor(int x, int y, TimeSpan timeSpan)
    {
        if (x < 0 || y < 0)
        {
            throw new ArgumentException("The provided coordinates were invalid");
        }

        if (timeSpan.TotalMilliseconds <= 0)
        {
            throw new ArgumentException("The provided timespan was invalid");
        }

        // Generate a randomised set of movements between the current cursor position and the point

        if (!User32.GetCursorPos(out var currentCursorPosition))
        {
            throw new Win32Exception();
        }

        var cursorMovements = GenerateMovements(currentCursorPosition, new Point(x, y), (int) timeSpan.TotalMilliseconds);

        // Move the cursor

        var stopwatch = Stopwatch.StartNew();

        foreach (var (delay, points) in cursorMovements)
        {
            if (points.Any(movementPoint => !User32.SetCursorPos(movementPoint.X, movementPoint.Y)))
            {
                throw new Win32Exception();
            }

            while (stopwatch.ElapsedMilliseconds < delay.Milliseconds)
            {
                // Wait
            }

            stopwatch.Restart();
        }
    }

    /// <summary>
    /// Generates a set of movements from start to end in a given amount of milliseconds
    /// </summary>
    public static IEnumerable<Movement> GenerateMovements(Point start, Point end, int milliseconds)
    {
        IEnumerable<int> FisherYatesShuffle(IList<int> collection, int elements)
        {
            for (var elementIndex = 0; elementIndex < collection.Count; elementIndex += 1)
            {
                var randomIndex = Random.Shared.Next(0, elementIndex);
                (collection[elementIndex], collection[randomIndex]) = (collection[randomIndex], collection[elementIndex]);
            }

            return collection.Take(elements);
        }

        // Generate the path points

        var pathPoints = GeneratePath(start, end).ToList();

        if (milliseconds <= pathPoints.Count)
        {
            var pointsPerMovement = pathPoints.Count / milliseconds;

            // Randomly distribute the remaining points using a Fisher Yates shuffle

            var remainingPoints = pathPoints.Count - milliseconds * pointsPerMovement;
            var distributionIndices = FisherYatesShuffle(Enumerable.Range(0, milliseconds).ToArray(), remainingPoints).ToHashSet();

            // Initialise the movements

            var pointsUsed = 0;

            for (var movementIndex = 0; movementIndex < milliseconds; movementIndex += 1)
            {
                var movementPoints = pointsPerMovement;

                if (distributionIndices.Contains(movementIndex))
                {
                    movementPoints += 1;
                }

                yield return new Movement(TimeSpan.FromMilliseconds(1), pathPoints.Skip(pointsUsed).Take(movementPoints));

                pointsUsed += movementPoints;
            }
        }

        else
        {
            var delayPerMovement = milliseconds / pathPoints.Count;

            // Randomly distribute the remaining milliseconds using a Fisher Yates shuffle

            var remainingMilliseconds = milliseconds - pathPoints.Count * delayPerMovement;
            var distributionIndexes = FisherYatesShuffle(Enumerable.Range(0, pathPoints.Count).ToArray(), remainingMilliseconds).ToHashSet();

            // Initialise the movements

            for (var movementIndex = 0; movementIndex < pathPoints.Count; movementIndex += 1)
            {
                var movementDelay = delayPerMovement;

                if (distributionIndexes.Contains(movementIndex))
                {
                    movementDelay += 1;
                }

                yield return new Movement(TimeSpan.FromMilliseconds(movementDelay), pathPoints.Skip(movementIndex).Take(1));
            }
        }
    }

    private static IEnumerable<Point> GeneratePath(Point start, Point end)
    {
        yield return start;

        // Generate randomised control points with a displacement of 15% to 30% between the start and end points

        var arcMultipliers = new[] { -1, 1 };
        var arcMultiplier = arcMultipliers[Random.Shared.Next(arcMultipliers.Length)];

        Point GenerateControlPoint()
        {
            var x = start.X + arcMultiplier * (Math.Abs(end.X - start.X) + 50) * 0.01 * Random.Shared.Next(15, 30);
            var y = start.Y + arcMultiplier * (Math.Abs(end.Y - start.Y) + 50) * 0.01 * Random.Shared.Next(15, 30);

            return new Point((int) x, (int) y);
        }

        var anchorPoints = new[] { start, GenerateControlPoint(), GenerateControlPoint(), end };

        // Generate 5000 points of a third order Bezier curve using De Casteljau's algorithm

        var binomialCoefficients = new[] { 1, 3, 3, 1 };

        for (var pointIndex = 0; pointIndex < 4998; pointIndex += 1)
        {
            var tValue = pointIndex / 4998d;

            var x = 0d;
            var y = 0d;

            for (var anchorPointIndex = 0; anchorPointIndex < anchorPoints.Length; anchorPointIndex += 1)
            {
                var binomialMultiplier = binomialCoefficients[anchorPointIndex] * Math.Pow(1 - tValue, 3 - anchorPointIndex) * Math.Pow(tValue, anchorPointIndex);

                x += anchorPoints[anchorPointIndex].X * binomialMultiplier;
                y += anchorPoints[anchorPointIndex].Y * binomialMultiplier;
            }

            yield return new Point((int) x, (int) y);
        }

        yield return end;
    }
}