namespace Tool.Interface {
	/// <summary>
	/// Tool interface
	/// </summary>
	/// <typeparam name="TToolSettings"></typeparam>
	public interface ITool<TToolSettings> where TToolSettings : new() {
		/// <summary>
		/// Returns tool title
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Executes current tool
		/// </summary>
		/// <param name="settings">Tool settings</param>
		void Execute(TToolSettings settings);
	}
}
