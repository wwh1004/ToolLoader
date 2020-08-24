namespace System.Cli {
	/// <summary>
	/// Represents a command line argument. The property to which <see cref="ArgumentAttribute"/> is applied must be of type <see cref="string"/> or <see cref="bool"/> and an instance property.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class ArgumentAttribute : Attribute {
		/// <summary>
		/// Parameter name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Whether it is a required parameter
		/// </summary>
		public bool IsRequired { get; set; }

		/// <summary>
		/// By default, when <see cref="IsRequired"/> is <see langword="true"/>, <see cref="DefaultValue"/> must be <see langword="null"/>
		/// </summary>
		public object DefaultValue { get; set; }

		/// <summary>
		/// Parameter type, which is used to display the type to briefly describe the parameter. If applied to an attribute with a return type of <see cref= "bool"/>, <see cref="Type"/> must be <see langword="null"/>
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Parameter description, which is used to describe the parameters
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Parameter name</param>
		public ArgumentAttribute(string name) {
			Name = name;
		}
	}
}
