using System.Drawing;
using System.Runtime.InteropServices;

namespace Nullinside.Api.Common;

public static class Screenshot {
  /// <summary>
  ///   Copies the source rectangle directly to the destination rectangle.
  /// </summary>
  private const int SRCCOPY = 0x00CC0020;

  /// <summary>
  ///   Includes any windows that are layered on top of your window in the resulting image. By default, the image only
  ///   contains your window. Note that this generally cannot be used for printing device contexts.
  /// </summary>
  private const int CAPTUREBLT = 0x40000000;

  /// <summary>
  ///   Performs a bit-block transfer of the color data corresponding to a
  ///   rectangle of pixels from a source device context into a destination
  ///   device context.
  /// </summary>
  /// <param name="hdcDest">Handle to the destination device context.</param>
  /// <param name="nxDest">The x-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
  /// <param name="nyDest">The y-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
  /// <param name="nWidth">The width, in logical units, of the source and destination rectangles.</param>
  /// <param name="nHeight">The height, in logical units, of the source and destination rectangles.</param>
  /// <param name="hdcSrc">Handle to the source device context.</param>
  /// <param name="nXSrc">The x-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
  /// <param name="nYSrc">The y-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
  /// <param name="dwRop">
  ///   A raster-operation code. This code determines how the color data for the source rectangle is
  ///   combined with the color data for the destination rectangle to achieve the final color. Common values include SRCCOPY
  ///   and CAPTUREBLT.
  /// </param>
  /// <returns>Returns true if the operation succeeds; otherwise, false.</returns>
  [DllImport("gdi32.dll")]
  private static extern bool BitBlt(IntPtr hdcDest, int nxDest, int nyDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

  /// <summary>
  ///   Creates a bitmap compatible with the device context provided.
  /// </summary>
  /// <param name="hdc">
  ///   The handle to the device context for which the bitmap is compatible.
  /// </param>
  /// <param name="width">
  ///   The width of the bitmap in pixels.
  /// </param>
  /// <param name="nHeight">
  ///   The height of the bitmap in pixels.
  /// </param>
  /// <returns>
  ///   A handle to the created compatible bitmap. If the function fails, the return value is IntPtr.Zero.
  /// </returns>
  [DllImport("gdi32.dll")]
  private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int nHeight);

  /// <summary>
  ///   Creates a memory device context (DC) that is compatible with the specified device context.
  ///   A memory DC is used to prepare data to be written to a specific device, such as a printer,
  ///   but the preparation occurs off-screen in memory.
  /// </summary>
  /// <param name="hdc">
  ///   A handle to an existing device context. If this parameter is IntPtr.Zero, the function creates
  ///   a memory DC compatible with the application's current screen.
  /// </param>
  /// <returns>
  ///   If the function succeeds, the return value is the handle to a memory device context.
  ///   If the function fails, the return value is IntPtr.Zero.
  /// </returns>
  [DllImport("gdi32.dll")]
  private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

  /// <summary>
  ///   Deletes the specified device context (DC).
  ///   A device context is a Windows GDI object that defines a set of graphic objects
  ///   and their associated attributes, used for drawing operations on a physical or virtual device.
  /// </summary>
  /// <param name="hdc">A handle to the device context to be deleted.</param>
  /// <returns>
  ///   Returns true if the device context is successfully deleted, otherwise false.
  /// </returns>
  [DllImport("gdi32.dll")]
  private static extern IntPtr DeleteDC(IntPtr hdc);

  /// <summary>
  ///   Deletes a GDI object, such as a bitmap, brush, font, region, or pen, to free up system resources.
  /// </summary>
  /// <param name="hObject">A handle to the GDI object to be deleted.</param>
  /// <returns>
  ///   True if the GDI object is successfully deleted; otherwise, false if the handle is invalid,
  ///   or if the object is still being used.
  /// </returns>
  [DllImport("gdi32.dll")]
  private static extern IntPtr DeleteObject(IntPtr hObject);

  /// <summary>
  ///   Retrieves a handle to the desktop window. The desktop window is the area on top of which other windows are painted.
  ///   This function does not take any parameters.
  /// </summary>
  /// <return>
  ///   A handle to the desktop window.
  /// </return>
  [DllImport("user32.dll")]
  private static extern IntPtr GetDesktopWindow();

  /// <summary>
  ///   Retrieves the device context (DC) for the entire window, including title bar, menus, and scroll bars, if present.
  ///   A device context is a structure that defines a set of graphic objects and their associated attributes, as well as
  ///   the graphic modes that affect output. It is used to draw in the window's client area.
  /// </summary>
  /// <param name="hWnd">
  ///   A handle to the window whose device context is to be retrieved. If this value is IntPtr.Zero, the device context
  ///   for the entire screen is retrieved.
  /// </param>
  /// <returns>
  ///   The handle to the device context (DC) for the specified window or for the entire screen if hWnd is IntPtr.Zero.
  ///   Returns IntPtr.Zero if the function fails.
  /// </returns>
  [DllImport("user32.dll")]
  private static extern IntPtr GetWindowDC(IntPtr hWnd);

  /// <summary>
  ///   Releases the device context (DC) for a specified window. This method must be
  ///   called following the use of a device context acquired by GetWindowDC or similar methods.
  /// </summary>
  /// <param name="hWnd">
  ///   A handle to the window whose device context is to be released. If this value
  ///   is 0, the DC is from the entire screen.
  /// </param>
  /// <param name="hDc">A handle to the device context to be released.</param>
  /// <returns>
  ///   True if the device context is released successfully; otherwise, false.
  /// </returns>
  [DllImport("user32.dll")]
  private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

  /// <summary>
  ///   Selects an object into the specified device context, enabling it to be used for drawing or other operations.
  /// </summary>
  /// <param name="hdc">The handle to the device context (DC) into which the object is to be selected.</param>
  /// <param name="hObject">
  ///   The handle to the object to be selected into the DC. The object can be a bitmap, pen, brush, or
  ///   other graphical object.
  /// </param>
  /// <returns>
  ///   A handle to the object being replaced in the device context, or IntPtr.Zero if the selection fails.
  /// </returns>
  [DllImport("gdi32.dll")]
  private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

  /// <summary>
  ///   Captures a region of the display.
  /// </summary>
  /// <param name="region">
  ///   The region to capture. NOTE: This uses the windows coordinate system where 0,0 is the top-left
  ///   corner of the main monitor. x increasing moves to the right. y increasing moves down. If the main monitor is not
  ///   the left-most monitor, use a negative x value to access any monitor positioned to the left.
  /// </param>
  /// <returns>A bitmap if successful, null otherwise.</returns>
  public static Bitmap? CaptureRegion(Rectangle region) {
    IntPtr desktophWnd;
    IntPtr desktopDc;
    IntPtr memoryDc;
    IntPtr bitmap;
    IntPtr oldBitmap;
    bool success;
    Bitmap result;

    desktophWnd = GetDesktopWindow();
    desktopDc = GetWindowDC(desktophWnd);
    memoryDc = CreateCompatibleDC(desktopDc);
    bitmap = CreateCompatibleBitmap(desktopDc, region.Width, region.Height);
    oldBitmap = SelectObject(memoryDc, bitmap);

    success = BitBlt(memoryDc, 0, 0, region.Width, region.Height, desktopDc, region.Left, region.Top, SRCCOPY | CAPTUREBLT);

    try {
      if (!success) {
        return null;
      }

      result = Image.FromHbitmap(bitmap);
    }
    finally {
      SelectObject(memoryDc, oldBitmap);
      DeleteObject(bitmap);
      DeleteDC(memoryDc);
      ReleaseDC(desktophWnd, desktopDc);
    }

    return result;
  }
}