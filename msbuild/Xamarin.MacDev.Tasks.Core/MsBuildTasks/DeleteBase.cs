extern alias SystemMSBuild;

namespace Microsoft.Build.Tasks
{
	public abstract class DeleteBase : SystemMSBuild.Microsoft.Build.Tasks.Delete
	{
		public string SessionId { get; set; }
	}
}

