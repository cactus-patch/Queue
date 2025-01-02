using Exiled.API.Interfaces;

namespace Queue;

public class Config : IConfig {
  public bool IsEnabled { get; set; } = true;
  public bool Debug { get; set; } = false;
  
  public uint Players { get; set; } = 32;
}