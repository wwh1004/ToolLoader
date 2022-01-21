namespace Tool.Interface;

/// <summary>
/// Tool interface
/// </summary>
/// <typeparam name="TOptions"></typeparam>
public interface ITool<TOptions> where TOptions : new() {
	/// <summary>
	/// Returns tool title
	/// </summary>
	string Title { get; }

	/// <summary>
	/// Executes current tool
	/// </summary>
	/// <param name="options">Tool options</param>
	void Execute(TOptions options);
}
