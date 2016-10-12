using System;
using Xunit;

namespace SideBySide.New
{
	public class JsonTheoryAttribute : TheoryAttribute {

		public JsonTheoryAttribute() {
			if(!AppConfig.SupportsJson) {
				Skip = "No JSON Support";
			}
		}

	}

	public class PasswordlessUserFactAttribute : FactAttribute {

		public PasswordlessUserFactAttribute() {
			if(string.IsNullOrWhiteSpace(AppConfig.PasswordlessUser)) {
				Skip = "No passwordless user";
			}
		}

	}

}
