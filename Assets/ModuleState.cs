using System.Collections.Generic;
using static Enums;
using UnityEngine;
/// <summary>
/// Used to keep track of the state of a the maze module.
/// Used to find the shortest path through the maze with Bredth First Search algorithm.
/// </summary>
public class ModuleState {
    private int dimensionLength = 4; // the length of the maze for width and height.
    public int Row { get; set; }
    public int Col { get; set; }
    public PlayerDirection PlayerDirection { get; set; } //the direction we are facing in the maze.

    public string MazeName { get; set; }
    public bool[,] Maze { get; set; } //2d array that represents the maze. First index is the cell in reading order, second index is the direction that we can move through order of walls corresponding to WallDirection enum

    public ModuleState ParentState { get; set; } //the state we were at before we got here

    public ModuleState(int currentRow, int currentColumn, PlayerDirection playerDirection, string mazeName, bool[,] maze, ModuleState parentState)
    {
        this.Row = currentRow;
        this.MazeName = mazeName;
        this.Col = currentColumn;
        this.PlayerDirection = playerDirection;
        this.Maze = maze;
        this.ParentState = parentState;
    }

    /// <summary>
    /// Get all the available game states we can go from our current one
    /// </summary>
    /// <returns></returns>
    public List<ModuleState> GetAvaiableGameStatesNieghbors()
    {
        List<ModuleState> neighborStates = new List<ModuleState>();

        int cellIndex = Row * dimensionLength + Col;
        //check if we can go up
        if (PlayerDirection == PlayerDirection.Up && Row > 0 && Maze[cellIndex, (int)WallDirection.Up])
        { 
            neighborStates.Add(new ModuleState(Row - 1, Col, PlayerDirection, MazeName, Maze, this));
        }

        //check if we can go down
        if (PlayerDirection == PlayerDirection.Down && Row < 4 && Maze[cellIndex, (int)WallDirection.Down])
        {
            neighborStates.Add(new ModuleState(Row + 1, Col, PlayerDirection, MazeName, Maze, this));
        }

        //check if we can go left
        if (PlayerDirection == PlayerDirection.Left && Col > 0 && Maze[cellIndex, (int)WallDirection.Left])
        {
            neighborStates.Add(new ModuleState(Row, Col - 1, PlayerDirection, MazeName, Maze, this));
        }

        //check if we can go right
        if (PlayerDirection == PlayerDirection.Right && Col < 4 && Maze[cellIndex, (int)WallDirection.Right])
        {
            neighborStates.Add(new ModuleState(Row, Col + 1, PlayerDirection, MazeName, Maze, this));
        }

        //turn to the right
        neighborStates.Add(new ModuleState(Row, Col, (PlayerDirection)(Modulo((int)PlayerDirection + 1, 4)), MazeName, Maze, this));

        //turn to the left
        neighborStates.Add(new ModuleState(Row, Col, (PlayerDirection)(Modulo((int)PlayerDirection - 1, 4)), MazeName, Maze, this));

        return neighborStates;
    }

    public static int Modulo(int num, int mod)
    { 
        return (num % mod + mod) % mod;
    }

    /// <summary>
    /// Checks if a module state has the goal cell in it
    /// </summary>
    /// <param name="state"></param>
    /// <param name="goalRow"></param>
    /// <param name="goalColumn"></param>
    /// <returns></returns>
    public bool HasGoalCell(int goalRow, int goalColumn)
    {
        return Row == goalRow && Col == goalColumn;
    }

    public bool Equals(ModuleState other)
    {
        return Row == other.Row && Col == other.Col && PlayerDirection == other.PlayerDirection;
    }

    private string GetBattshipCoorinate()
    { 
        return (char)('A' + Col) + (Row + 1).ToString();
    }
    public override string ToString()
    {
        return string.Format("Position: {0}\nDirection: {1}", GetBattshipCoorinate(), PlayerDirection);
    }
}
