using System;

[Flags]
public enum PipeConnections {
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Front = 1 << 2,
    Back = 1 << 3,
    Top = 1 << 4,
    Bottom = 1 << 5
}