namespace Nullinside.Api.Common.Json;

public class BasicServerFailure {
  public BasicServerFailure(string error) {
    Error = error;
  }

  public string Error { get; set; }
}