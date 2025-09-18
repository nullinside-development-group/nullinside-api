using System.Drawing;
using System.Runtime.InteropServices;

namespace Nullinside.Api.Common;

public class Mouse {
  [Flags]
  public enum MouseEventFlags
  {
    LeftDown = 0x00000002,
    LeftUp = 0x00000004,
    MiddleDown = 0x00000020,
    MiddleUp = 0x00000040,
    Move = 0x00000001,
    Absolute = 0x00008000,
    RightDown = 0x00000008,
    RightUp = 0x00000010
  }

  [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool SetCursorPos(int x, int y);      

  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool GetCursorPos(out MousePoint lpMousePoint);

  [DllImport("user32.dll")]
  private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

  public static async Task Click() {
    MousePoint point = new MousePoint(0, 0);
    if (GetCursorPos(out point)) {
      mouse_event((int)MouseEventFlags.LeftDown, point.X, point.Y, 0, 0);
      await Task.Delay(Random.Shared.Next(30, 40)).ConfigureAwait(false);
      mouse_event((int)MouseEventFlags.LeftUp, point.X, point.Y, 0, 0);
    }
  }
  
  [StructLayout(LayoutKind.Sequential)]
  public struct MousePoint
  {
    public int X;
    public int Y;

    public MousePoint(int x, int y)
    {
      X = x;
      Y = y;
    }
  }
}