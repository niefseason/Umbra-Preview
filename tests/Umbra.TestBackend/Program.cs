using System.Text.Json;
// Synthetic process fixture. Contains no emulator or game software.
var report=Environment.GetEnvironmentVariable("UMBRA_TEST_REPORT");
if(report is not null)File.WriteAllText(report,JsonSerializer.Serialize(args));
await Task.Delay(300);
return Environment.GetEnvironmentVariable("UMBRA_TEST_CRASH")=="1"?17:0;
