namespace Tool.Interface {
	/// <summary>
	/// .NET逆向工具接口
	/// </summary>
	/// <typeparam name="TToolSettings"></typeparam>
	public interface ITool<TToolSettings> where TToolSettings : new() {
		/// <summary>
		/// 显示在控制台上的标题
		/// </summary>
		string Title { get; }

		/// <summary>
		/// 执行
		/// </summary>
		/// <param name="settings">设置</param>
		void Execute(TToolSettings settings);
	}
}
