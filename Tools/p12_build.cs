if(UnityEditor.EditorApplication.isPlaying) return "Stop Play first";
CampusRift.Validation.P12BenchmarkBuild.Request();
return "Build queued; poll Artifacts/P12/performance/Build-status.txt";
