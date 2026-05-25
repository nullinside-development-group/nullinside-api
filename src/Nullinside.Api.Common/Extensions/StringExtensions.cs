using System.Globalization;
using System.Text;

using ICU4N.Text;

namespace Nullinside.Api.Common.Extensions;

/// <summary>
///   Extensions for the <see cref="string" /> class.
/// </summary>
public static class StringExtensions {
  // Characters that visually resemble ASCII letters
  private static readonly Dictionary<char, string> LOOKALIKE_MAP = new() {
    // Cyrillic
    ['а'] = "a",
    ['е'] = "e",
    ['о'] = "o",
    ['р'] = "p",
    ['с'] = "c",
    ['у'] = "y",
    ['х'] = "x",
    ['і'] = "i",
    ['ј'] = "j",
    ['к'] = "k",
    ['м'] = "m",
    ['н'] = "h",
    ['т'] = "t",
    ['в'] = "b",

    // Greek
    ['Α'] = "A",
    ['Β'] = "B",
    ['Ε'] = "E",
    ['Ζ'] = "Z",
    ['Η'] = "H",
    ['Ι'] = "I",
    ['Κ'] = "K",
    ['Μ'] = "M",
    ['Ν'] = "N",
    ['Ο'] = "O",
    ['Ρ'] = "P",
    ['Τ'] = "T",
    ['Χ'] = "X",

    ['α'] = "a",
    ['β'] = "b",
    ['γ'] = "y",
    ['δ'] = "d",
    ['ε'] = "e",
    ['ι'] = "i",
    ['κ'] = "k",
    ['ο'] = "o",
    ['ρ'] = "p",
    ['τ'] = "t",
    ['χ'] = "x"
  };

  /// <summary>
  ///   Calculate the difference between 2 strings using the Levenshtein distance algorithm
  /// </summary>
  /// <param name="source1">First string</param>
  /// <param name="source2">Second string</param>
  /// <remarks>https://gist.github.com/Davidblkx/e12ab0bb2aff7fd8072632b396538560</remarks>
  /// <returns>The distance between two strings.</returns>
  public static int Calculate(this string source1, string source2) //O(n*m)
  {
    int source1Length = source1.Length;
    int source2Length = source2.Length;

    int[,] matrix = new int[source1Length + 1, source2Length + 1];

    // First calculation, if one entry is empty return full length
    if (source1Length == 0) {
      return source2Length;
    }

    if (source2Length == 0) {
      return source1Length;
    }

    // Initialization of matrix with row size source1Length and columns size source2Length
    for (int i = 0; i <= source1Length; matrix[i, 0] = i++) { }

    for (int j = 0; j <= source2Length; matrix[0, j] = j++) { }

    // Calculate rows and columns distances
    for (int i = 1; i <= source1Length; i++) {
      for (int j = 1; j <= source2Length; j++) {
        int cost = source2[j - 1] == source1[i - 1] ? 0 : 1;

        matrix[i, j] = Math.Min(
          Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
          matrix[i - 1, j - 1] + cost);
      }
    }

    // return result
    return matrix[source1Length, source2Length];
  }

  /// <summary>
  ///   Handles normalization of text to ASCII characters.
  /// </summary>
  /// <param name="text">The text to normalize.</param>
  /// <remarks>Characters that cannot be converted are left in-place in their ascii forms.</remarks>
  /// <returns>The ascii representation of the text, where possible.</returns>
  public static string NormalizeToAscii(this string text) {
    text = Transliterator.GetInstance("Latin-ASCII").Transliterate(text);

    // Unicode normalize (turn fancy chars into decomposed forms)
    text = text.Normalize(NormalizationForm.FormKD);
    var sb = new StringBuilder();

    foreach (char c in text) {
      UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

      // Skip accent marks/formatting
      if (category == UnicodeCategory.NonSpacingMark) {
        continue;
      }

      // Remap look-a-likes
      if (LOOKALIKE_MAP.TryGetValue(c, out string? mapped)) {
        sb.Append(mapped);
        continue;
      }

      // Keep only letters/numbers
      if (char.IsLetterOrDigit(c) | char.IsPunctuation(c)) {
        sb.Append(c);
      }
      else if (char.IsWhiteSpace(c)) {
        sb.Append(' ');
      }
    }

    return sb.ToString();
  }

  /// <summary>
  ///   Removes all the spaces from a string.
  /// </summary>
  /// <param name="source">The string to remove the spaces from.</param>
  /// <returns>The string without spaces.</returns>
  public static string RemoveAllSpace(this string source) {
    return source.Where(c => !char.IsWhiteSpace(c)).Aggregate("", (curr, c) => curr + c);
  }
}