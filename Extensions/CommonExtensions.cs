using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qrame.Web.TransactServer.Extensions
{
	public static class CommonExtensions
	{
		public static string ToMessage(this Exception source)
		{
			string result = null;

			if (source == null)
			{
				result = "";
			}
			else {
				result = StaticConfig.IsExceptionDetailText == true ? source.ToString() : source.Message;
			}

			return result;
		}
	}
}
