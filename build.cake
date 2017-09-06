var target = Argument("target", "Default");

Task("Default")
	.IsDependentOn("Build");

Task("Build")
  .Does(() =>
{
	MSBuild("./src/EventStore.Client.sln");
});

RunTarget(target);