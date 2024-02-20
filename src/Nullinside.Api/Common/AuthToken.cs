namespace Nullinside.Api.Common;

public class AuthToken {
  public AuthToken(string token) {
    Token = token;
  }

  public string Token { get; set; }
}