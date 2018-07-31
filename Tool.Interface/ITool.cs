namespace Tool.Interface {
	/// <summary>
	/// .NET逆向工具接口
	/// </summary>
	public interface ITool {
		/// <summary>
		/// 显示在控制台上的标题
		/// </summary>
		string Title { get; }

		/// <summary>
		/// 执行
		/// </summary>
		/// <param name="filePath">文件路径</param>
		/// <param name="otherArgs">其它参数</param>
		void Execute(string filePath, string[] otherArgs);
	}
}
