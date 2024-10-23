namespace Nullinside.Api.Common;

/// <summary>
///   Common SQL scripts for performing functions in the database.
/// </summary>
public static class SqlScripts {
  /// <summary>
  ///   Performs the Levenshtein distance fuzzy search in a MySQL database.
  ///   https://en.wikipedia.org/wiki/Levenshtein_distance
  /// </summary>
  /// <remarks>Also requires the <see cref="LEVENSHTEIN_DISTANCE_SEARCH_INNER_FUNCTION" />.</remarks>
  public const string LEVENSHTEIN_DISTANCE_SEARCH =
    """
    DELIMITER ;;;
    CREATE DEFINER=`INSERT_USER_HERE`@`%` FUNCTION `LEVENSHTEIN_RATIO`(s1 VARCHAR(255), s2 VARCHAR(255)) RETURNS int(11) DETERMINISTIC
    BEGIN
        DECLARE s1_len, s2_len, max_len INT;
        SET s1_len = LENGTH(s1), s2_len = LENGTH(s2);
        IF s1_len > s2_len THEN SET max_len = s1_len; ELSE SET max_len = s2_len; END IF;
        RETURN ROUND((1 - LEVENSHTEIN(s1, s2) / max_len) * 100);
    END;;;
    """;

  /// <summary>
  ///   The inner function for the <see cref="LEVENSHTEIN_DISTANCE_SEARCH" />. Not meant to be called directly.
  /// </summary>
  public const string LEVENSHTEIN_DISTANCE_SEARCH_INNER_FUNCTION =
    """
    DELIMITER ;;;
    CREATE DEFINER=`INSERT_USER_HERE`@`%` FUNCTION `LEVENSHTEIN`(s1 VARCHAR(255), s2 VARCHAR(255)) RETURNS int DETERMINISTIC
    BEGIN
        DECLARE s1_len, s2_len, i, j, c, c_temp, cost INT;
        DECLARE s1_char CHAR;
        DECLARE cv0, cv1 VARBINARY(256);
        SET s1_len = CHAR_LENGTH(s1), s2_len = CHAR_LENGTH(s2), cv1 = 0x00, j = 1, i = 1, c = 0;
        IF s1 = s2 THEN
            RETURN 0;
        ELSEIF s1_len = 0 THEN
            RETURN s2_len;
        ELSEIF s2_len = 0 THEN
            RETURN s1_len;
        ELSE
            WHILE j <= s2_len DO
                SET cv1 = CONCAT(cv1, UNHEX(HEX(j))), j = j + 1;
            END WHILE;
            WHILE i <= s1_len DO
                SET s1_char = SUBSTRING(s1, i, 1), c = i, cv0 = UNHEX(HEX(i)), j = 1;
                WHILE j <= s2_len DO
                    SET c = c + 1;
                    IF s1_char = SUBSTRING(s2, j, 1) THEN SET cost = 0; ELSE SET cost = 1; END IF;
                    SET c_temp = CONV(HEX(SUBSTRING(cv1, j, 1)), 16, 10) + cost;
                    IF c > c_temp THEN SET c = c_temp; END IF;
                    SET c_temp = CONV(HEX(SUBSTRING(cv1, j+1, 1)), 16, 10) + 1;
                    IF c > c_temp THEN SET c = c_temp; END IF;
                    SET cv0 = CONCAT(cv0, UNHEX(HEX(c))), j = j + 1;
                END WHILE;
                SET cv1 = cv0, i = i + 1;
            END WHILE;
        END IF;
        RETURN c;
    END;;;
    """;

  /// <summary>
  ///   Removes <see cref="LEVENSHTEIN_DISTANCE_SEARCH" />.
  /// </summary>
  /// <remarks>Also requires the <see cref="LEVENSHTEIN_DISTANCE_SEARCH_INNER_FUNCTION" />.</remarks>
  public const string LEVENSHTEIN_DISTANCE_SEARCH_REMOVE =
    """
    DROP FUNCTION IF EXISTS `LEVENSHTEIN_RATIO`;
    """;

  /// <summary>
  ///   Removes <see cref="LEVENSHTEIN_DISTANCE_SEARCH_INNER_FUNCTION" />.
  /// </summary>
  public const string LEVENSHTEIN_DISTANCE_SEARCH_INNER_FUNCTION_REMOVE =
    """
    DROP FUNCTION IF EXISTS `LEVENSHTEIN`;
    """;
}