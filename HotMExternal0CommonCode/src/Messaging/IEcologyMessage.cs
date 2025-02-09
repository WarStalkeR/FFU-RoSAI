using System;
using System.Collections.Generic;


using System.Threading.Tasks;

namespace Arcen.HotM.External
{
	public interface IEcologyMessage
	{
		short DistrictID { get; }
		string EcoObjectID { get; }
		string ModificationID { get; }
		int ModificationValue { get; }
	}
}
